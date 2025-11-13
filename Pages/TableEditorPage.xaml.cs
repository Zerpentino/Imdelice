using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Imdeliceapp.Helpers;
using Imdeliceapp.Services;
using TableDTO = Imdeliceapp.Models.TableDTO;
using TableInput = Imdeliceapp.Models.TableInput;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(TableId), "id")]
public partial class TableEditorPage : ContentPage
{
    public string? Mode { get; set; }
    public int TableId { get; set; }

    bool IsEdit => string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
    bool _loaded = false;
    bool _hasOriginal = false;
    string _origName = string.Empty;
    int _origSeats;
    bool _origActive;

    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    static readonly JsonSerializerOptions _jsonWrite = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public TableEditorPage()
    {
        InitializeComponent();
        SwIsActive.IsToggled = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (IsEdit && !Perms.TablesUpdate ||
            !IsEdit && !Perms.TablesCreate)
        {
            await DisplayAlert("Acceso restringido", "No tienes permisos para esta acción.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        TitleLabel.Text = IsEdit ? "Editar mesa" : "Crear mesa";
        HintEdit.IsVisible = IsEdit;

        if (!_loaded && IsEdit && TableId > 0)
        {
            await CargarMesaAsync(TableId);
        }
        _loaded = true;
    }

    async Task CargarMesaAsync(int id)
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

            var resp = await http.GetAsync($"/api/tables/{id}?includeInactive=true");
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

            var env = JsonSerializer.Deserialize<ApiEnvelopeTables<TableDTO>>(body, _json);
            if (env?.data is TableDTO table)
            {
                _origName = table.name ?? string.Empty;
                _origSeats = table.seats;
                _origActive = table.isActive;
                _hasOriginal = true;

                TxtName.Text = _origName;
                TxtSeats.Text = _origSeats <= 0 ? string.Empty : _origSeats.ToString();
                SwIsActive.IsToggled = _origActive;
            }
        }
        catch (TaskCanceledException)
        {
            await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado. El servidor no responde.");
            await Shell.Current.GoToAsync("..");
        }
        catch (HttpRequestException)
        {
            await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Mesa – Cargar detalle");
            await Shell.Current.GoToAsync("..");
        }
    }

    async void Guardar_Clicked(object sender, EventArgs e)
    {
        if (!Validar(out var name, out var seats))
            return;

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
            return;
        }

        try
        {
            SetSaving(true);

            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            bool newActive = SwIsActive.IsToggled;
            bool changedStatus = IsEdit && _hasOriginal && newActive != _origActive;
            bool changedOther = !IsEdit || !_hasOriginal || !string.Equals(name, _origName, StringComparison.Ordinal) || seats != _origSeats;

            if (!IsEdit)
            {
                var payload = new TableInput(name, seats, newActive);
                var json = JsonSerializer.Serialize(payload, _jsonWrite);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var respCreate = await http.PostAsync("/api/tables", content);
                var bodyCreate = await respCreate.Content.ReadAsStringAsync();

                if (!respCreate.IsSuccessStatusCode)
                {
                    await HandleHttpErrorAsync(respCreate, bodyCreate);
                    return;
                }

                await DisplayAlert("Listo", "Mesa creada correctamente.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Editar
            if (!IsEdit || TableId <= 0)
                return;

            // PUT solo si cambian datos o se reactivará
            if (changedOther || (changedStatus && newActive))
            {
                bool? isActiveForPut = null;
                if (changedStatus && newActive)
                    isActiveForPut = true;

                var payload = new TableInput(name, seats, isActiveForPut);
                var json = JsonSerializer.Serialize(payload, _jsonWrite);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = changedStatus && newActive
                    ? $"/api/tables/{TableId}?includeInactive=true"
                    : $"/api/tables/{TableId}";
                var respUpdate = await http.PatchAsync(url, content);
                var bodyUpdate = await respUpdate.Content.ReadAsStringAsync();

                if (!respUpdate.IsSuccessStatusCode)
                {
                    await HandleHttpErrorAsync(respUpdate, bodyUpdate);
                    return;
                }
            }

            if (changedStatus && !newActive)
            {
                var respDelete = await http.DeleteAsync($"/api/tables/{TableId}");
                if (respDelete.StatusCode != HttpStatusCode.NoContent && !respDelete.IsSuccessStatusCode)
                {
                    var bodyDelete = await respDelete.Content.ReadAsStringAsync();
                    await HandleHttpErrorAsync(respDelete, bodyDelete);
                    return;
                }
            }

            await DisplayAlert("Listo", changedStatus && !newActive ? "Mesa desactivada correctamente." : "Mesa actualizada correctamente.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (TaskCanceledException)
        {
            await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado. El servidor no responde.");
        }
        catch (HttpRequestException)
        {
            await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Mesa – Guardar");
        }
        finally
        {
            SetSaving(false);
        }
    }

    void SetSaving(bool saving)
    {
        BtnGuardar.IsEnabled = !saving;
    }

    bool Validar(out string name, out int seats)
    {
        name = TxtName.Text?.Trim() ?? string.Empty;
        seats = 0;

        bool ok = true;

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            SetFieldError(BdrName, ErrName, true, "Escribe el nombre de la mesa.");
            ok = false;
        }
        else
        {
            SetFieldError(BdrName, ErrName, false);
        }

        if (!int.TryParse(TxtSeats.Text, out seats) || seats <= 0)
        {
            SetFieldError(BdrSeats, ErrSeats, true, "Ingresa un número de asientos mayor a 0.");
            ok = false;
        }
        else
        {
            SetFieldError(BdrSeats, ErrSeats, false);
        }

        return ok;
    }

    void SetFieldError(Border border, Label label, bool isError, string? message = null)
    {
        if (isError)
        {
            border.Stroke = Colors.Red;
            if (!string.IsNullOrWhiteSpace(message))
                label.Text = message;
            label.IsVisible = true;
        }
        else
        {
            border.ClearValue(Border.StrokeProperty);
            label.IsVisible = false;
        }
    }

    async void Cancelar_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    async Task HandleHttpErrorAsync(HttpResponseMessage resp, string body)
    {
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            await AuthHelper.VerificarYRedirigirSiExpirado(this);
            return;
        }

        if (resp.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
        {
            await ErrorHandler.MostrarErrorUsuario("El servidor no responde.");
            return;
        }

        await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
    }

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s)) return s;
        var p = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }

    static HttpClient NewAuthClient(string baseUrl, string token)
    {
        var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }
}
