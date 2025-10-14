using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;   // Connectivity
using System.Threading.Tasks;       // TaskCanceledException
using Imdeliceapp.Helpers;
using Imdeliceapp.Model;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(RoleId), "id")]
public partial class RoleEditorPage : ContentPage
{
    public string? Mode { get; set; }   // "create" | "edit"
    public int RoleId { get; set; }

    string? _origName, _origDesc;

    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    static readonly JsonSerializerOptions _jsonWrite = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public RoleEditorPage() => InitializeComponent();

    // ===== Helpers comunes =====
    void SetSaving(bool v)
    {
        BtnGuardar.IsEnabled = !v;
        BtnCancelar.IsEnabled = !v;
    }

    Task MostrarServidorNoDisponibleAsync(string causa = "")
    {
        var msg = causa switch
        {
            "sin_internet" => "Sin conexión a Internet. Revisa tu red e inténtalo de nuevo.",
            "timeout"      => "Tiempo de espera agotado. El servidor no responde.",
            _              => "No pudimos contactar al servidor. Inténtalo más tarde."
        };
        return DisplayAlert("Servidor no disponible", msg, "OK");
    }

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s)) return s;
        var p = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }

    HttpClient NewAuthClient(string baseUrl, string token)
    {
        var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }

    void SetError(Border b, Label l, bool isError, string? msg = null)
    {
        if (isError)
        {
            b.Stroke = Colors.Red;
            if (msg != null) l.Text = msg;
            l.IsVisible = true;
        }
        else
        {
            b.ClearValue(Border.StrokeProperty);
            l.IsVisible = false;
        }
    }

    string ExtraerMensajeApi(string body)
    {
        try
        {
            var env = JsonSerializer.Deserialize<ApiEnvelope<object>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (!string.IsNullOrWhiteSpace(env?.message)) return env!.message!;
        }
        catch { /* ignorar */ }
        return string.IsNullOrWhiteSpace(body) ? "Solicitud inválida" : body;
    }

    bool EsMensajeRolDuplicado(string? msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return false;
        // Ejemplo backend: "El nombre del rol ya existe"
        return msg.Contains("El nombre del rol ya existe", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("nombre del rol ya existe", StringComparison.OrdinalIgnoreCase);
    }

    // ===== Ciclo de vida =====
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
        TitleLabel.Text = esEdicion ? "Editar rol" : "Crear rol";
        HintEditGeneral.IsVisible = esEdicion;

        if (esEdicion && RoleId > 0)
            await CargarRolAsync(RoleId);
    }

    async Task CargarRolAsync(int id)
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.GetAsync($"/api/roles/{id}");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthHelper.VerificarYRedirigirSiExpirado(this);
                    return;
                }
                await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
                await Shell.Current.GoToAsync("..");
                return;
            }

            var env = JsonSerializer.Deserialize<ApiEnvelope<RoleDTO>>(body, _json);
            if (env?.data is RoleDTO r)
            {
                _origName = r.name ?? "";
                _origDesc = r.description ?? "";

                TxtName.Text = _origName;
                TxtDesc.Text = _origDesc;
            }
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Roles – Cargar rol");
        }
    }

    // ===== Validaciones de UI =====
    // Sólo letras y espacios. En edición: vacío = OK (no actualiza).
    void TxtName_TextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = (Entry)sender;
        var filtered = Regex.Replace(e.NewTextValue ?? "", @"[^\p{L} ]+", "");
        if (filtered != entry.Text)
        {
            var pos = entry.CursorPosition;
            entry.Text = filtered;
            entry.CursorPosition = Math.Min(pos, filtered.Length);
        }

        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
        var isEmpty = string.IsNullOrWhiteSpace(entry.Text);
        // crear: vacío o <2 chars => error. edición: vacío es permitido.
        var invalido = (!esEdicion && (isEmpty || entry.Text!.Length < 2));
        SetError(BdrName, ErrName, invalido);
    }

    // (opcional) límite suave de descripción
    void TxtDesc_TextChanged(object sender, TextChangedEventArgs e)
    {
        var t = e.NewTextValue ?? "";
        // por ejemplo, 200 máx (sólo mensaje visual; no bloquea tecleo)
        SetError(BdrDesc, ErrDesc, t.Length > 200, "Descripción demasiado larga (máx. 200).");
    }

    // ===== Guardar =====
    private async void Guardar_Clicked(object sender, EventArgs e)
    {
        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
        var name = TxtName.Text?.Trim();
        var desc = TxtDesc.Text?.Trim();

        // Validación base
        if (esEdicion)
        {
            // Si escriben algo en nombre, que cumpla reglas (>=2 letras y sólo letras/espacios)
            if (!string.IsNullOrWhiteSpace(name))
            {
                var ok = name.Length >= 2 && Regex.IsMatch(name, @"^[\p{L} ]+$");
                if (!ok)
                {
                    SetError(BdrName, ErrName, true);
                    await DisplayAlert("Nombre", "Escribe un nombre válido (sólo letras y espacios, mínimo 2).", "OK");
                    return;
                }
            }
            // Descripción es opcional; si está muy larga, avisa
            if (!string.IsNullOrEmpty(desc) && desc.Length > 200)
            {
                SetError(BdrDesc, ErrDesc, true);
                await DisplayAlert("Descripción", "Máximo 200 caracteres.", "OK");
                return;
            }
        }
        else
        {
            // Crear: nombre obligatorio y válido
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || !Regex.IsMatch(name, @"^[\p{L} ]+$"))
            {
                SetError(BdrName, ErrName, true);
                await DisplayAlert("Nombre", "Escribe el nombre del rol (sólo letras y espacios, mínimo 2).", "OK");
                return;
            }
            if (!string.IsNullOrEmpty(desc) && desc.Length > 200)
            {
                SetError(BdrDesc, ErrDesc, true);
                await DisplayAlert("Descripción", "Máximo 200 caracteres.", "OK");
                return;
            }
        }

        if (!BtnGuardar.IsEnabled) return;
        SetSaving(true);

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await MostrarServidorNoDisponibleAsync("sin_internet");
                return;
            }

            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            HttpResponseMessage resp;
            string body;

            if (esEdicion && RoleId > 0)
            {
                // Enviar sólo lo que realmente cambió y no está vacío
                string? sendName = string.IsNullOrWhiteSpace(name) ? null : (name != _origName ? name : null);
                string? sendDesc = string.IsNullOrWhiteSpace(desc) ? null : (desc != _origDesc ? desc : null);

                var hayCambios = sendName != null || sendDesc != null;
                if (!hayCambios)
                {
                    await DisplayAlert("Sin cambios", "No hay cambios por guardar.", "OK");
                    return;
                }

                var payload = new { name = sendName, description = sendDesc };
                var json = JsonSerializer.Serialize(payload, _jsonWrite);

                resp = await http.PutAsync($"/api/roles/{RoleId}",
                        new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                body = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode == HttpStatusCode.ServiceUnavailable || resp.StatusCode == HttpStatusCode.GatewayTimeout)
                {
                    await MostrarServidorNoDisponibleAsync();
                    return;
                }
            }
            else
            {
                var payload = new { name, description = string.IsNullOrWhiteSpace(desc) ? null : desc };
                var json = JsonSerializer.Serialize(payload, _jsonWrite);

                resp = await http.PostAsync("/api/roles",
                        new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                body = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode == HttpStatusCode.ServiceUnavailable || resp.StatusCode == HttpStatusCode.GatewayTimeout)
                {
                    await MostrarServidorNoDisponibleAsync();
                    return;
                }
            }

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthHelper.VerificarYRedirigirSiExpirado(this);
                    return;
                }

                var apiMsg = ExtraerMensajeApi(body);

                // 409: nombre duplicado
                if (resp.StatusCode == HttpStatusCode.Conflict && EsMensajeRolDuplicado(apiMsg))
                {
                    SetError(BdrName, ErrName, true, "El nombre del rol ya existe.");
                    await DisplayAlert("Nombre duplicado", "Ese nombre de rol ya existe. Usa otro diferente.", "OK");
                    return;
                }

                await ErrorHandler.MostrarErrorUsuario(apiMsg);
                return;
            }

            await DisplayAlert("Listo", "Cambios guardados.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (TaskCanceledException)
        {
            await MostrarServidorNoDisponibleAsync("timeout");
        }
        catch (HttpRequestException)
        {
            await MostrarServidorNoDisponibleAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Roles – Guardar rol");
        }
        finally
        {
            SetSaving(false);
        }
    }

    private async void Cancelar_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
