using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Imdeliceapp.Helpers;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.Text;
using Imdeliceapp.Services;



namespace Imdeliceapp.Pages;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(CategoryId), "id")]
[QueryProperty(nameof(InitName), "name")]
[QueryProperty(nameof(InitSlug), "slug")]
[QueryProperty(nameof(InitPosition), "position")]
[QueryProperty(nameof(InitActive), "isActive")]
[QueryProperty(nameof(InitIsComboOnly), "isComboOnly")]  // ✅

public partial class CategoryEditorPage : ContentPage
{
    bool ParseBoolFlag(string? v)
    => !string.IsNullOrWhiteSpace(v) &&
       (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase));
    public string? InitIsComboOnly { get; set; }
    bool _origIsCombo;     // ← NUEVO

    public string? Mode { get; set; }   // create | edit
    public int CategoryId { get; set; }
    public string? InitName { get; set; }
    public string? InitSlug { get; set; }
    public string? InitPosition { get; set; }
    public string? InitActive { get; set; }

    static string? MapFriendlyMessage(HttpStatusCode status, string? body, bool cambiandoNombre, bool cambiandoSlug)
    {
        var b = (body ?? "").ToLowerInvariant();

        bool esDuplicado =
            status == HttpStatusCode.Conflict ||
            b.Contains("p2002") ||
            b.Contains("unique constraint failed") ||
            b.Contains("category_slug_key") ||
            b.Contains("category_name_key") ||         // <- nombre
            (b.Contains("constraint") && (b.Contains("slug") || b.Contains("name")));

        if (esDuplicado)
        {
            if (cambiandoNombre) return "Ya existe una categoría con ese nombre.";
            if (cambiandoSlug) return "Ya existe una categoría con ese slug.";
            return "Ya existe esa categoría.";
        }
        return null;
    }


    // originales (para PATCH parcial)
    string? _origName, _origSlug;
    int? _origPos;
    bool _origActive;

    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    static readonly JsonSerializerOptions _jsonWrite = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public CategoryEditorPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
        TitleLabel.Text = esEdicion ? "Editar categoría" : "Crear categoría";
        HintEdit.IsVisible = esEdicion;

        if (esEdicion && CategoryId > 0)
        {
            if (!Perms.CategoriesUpdate)
            {
                await DisplayAlert("Acceso restringido", "No puedes editar categorías.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            if (!string.IsNullOrWhiteSpace(InitName))
            {
                // Pre-cargar desde los parámetros (sin ir al backend)
                _origName = InitName ?? "";
                _origSlug = InitSlug ?? "";
                _origPos = int.TryParse(InitPosition, out var p) ? p : (int?)null;
                _origActive = ParseBoolFlag(InitActive);
                _origIsCombo = ParseBoolFlag(InitIsComboOnly);

                TxtName.Text = _origName;
                TxtSlug.Text = _origSlug;
                TxtPosition.Text = _origPos?.ToString();
                SwActive.IsToggled = _origActive;
                SwIsCombo.IsToggled = _origIsCombo;
            }
            else
            {
                // Fallback opcional (si por alguna razón no llegaron datos)
                // Si tu backend NO tiene este endpoint, deja comentado:
                // await CargarAsync(CategoryId);
            }
        }
        else
        {
            if (!Perms.CategoriesCreate)
            {
                await DisplayAlert("Acceso restringido", "No puedes crear categorías.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }
        }
    
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

    void SetSaving(bool v) { BtnGuardar.IsEnabled = !v; BtnCancelar.IsEnabled = !v; }
    Task ServidorNoDisponible(string causa = "") =>
        DisplayAlert("Servidor no disponible",
            causa == "sin_internet" ? "Sin conexión a Internet." :
            causa == "timeout" ? "Tiempo de espera agotado." :
                                    "No se pudo contactar al servidor.", "OK");

    static string ToSlug(string? s)
    {
        s = (s ?? "").Trim().ToLowerInvariant();
        s = s.Normalize(NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        s = sb.ToString().Normalize(NormalizationForm.FormC);
        s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"-+", "-");
        return s.Trim('-');
    }

    void SetError(Border b, Label l, bool isError, string? msg = null)
    {
        if (isError) { b.Stroke = Colors.Red; if (msg != null) l.Text = msg; l.IsVisible = true; }
        else { b.ClearValue(Border.StrokeProperty); l.IsVisible = false; }
    }

    async Task CargarAsync(int id)
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.GetAsync($"/api/categories/{id}");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

                await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(body) ? "Error al cargar." : body);
                await Shell.Current.GoToAsync("..");
                return;
            }

            var env = JsonSerializer.Deserialize<ApiEnvelope<CategoryDTO>>(body, _json);
            var c = env?.data;
            if (c is null) { await ErrorHandler.MostrarErrorUsuario("Categoría no encontrada."); await Shell.Current.GoToAsync(".."); return; }

            _origName = c.name ?? "";
            _origSlug = c.slug ?? "";
            _origPos = c.position;
            _origActive = c.isActive;

            TxtName.Text = _origName;
            TxtSlug.Text = _origSlug;
            TxtPosition.Text = _origPos?.ToString();
            SwActive.IsToggled = _origActive;
        }
        catch (Exception ex)
        { await ErrorHandler.MostrarErrorTecnico(ex, "Categories – Cargar detalle"); }
    }

    void TxtName_TextChanged(object s, TextChangedEventArgs e)
    {
        // autogenerar slug si el usuario no lo ha tocado
        if (string.IsNullOrWhiteSpace(TxtSlug.Text) || TxtSlug.Text == ToSlug(e.OldTextValue))
            TxtSlug.Text = ToSlug(e.NewTextValue);
        SetError(BdrName, ErrName, string.IsNullOrWhiteSpace(TxtName.Text));
    }
    void TxtPosition_TextChanged(object s, TextChangedEventArgs e)
    {
        var ok = int.TryParse(e.NewTextValue ?? "", out var _pos) && _pos >= 0;
        SetError(BdrPos, ErrPos, !ok && !string.IsNullOrEmpty(e.NewTextValue));
    }

    bool SlugValido(string? slug) => !string.IsNullOrWhiteSpace(slug) && slug.Length >= 2 && Regex.IsMatch(slug, @"^[a-z0-9-]+$");

    async void Guardar_Clicked(object sender, EventArgs e)
    {
        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
        if (esEdicion && !Perms.CategoriesUpdate)
    { await DisplayAlert("Acceso restringido", "No puedes editar categorías.", "OK"); return; }

if (!esEdicion && !Perms.CategoriesCreate)
    { await DisplayAlert("Acceso restringido", "No puedes crear categorías.", "OK"); return; }

        var name = TxtName.Text?.Trim();
        var slug = (TxtSlug.Text?.Trim() ?? "");
        var posOk = int.TryParse(TxtPosition.Text?.Trim() ?? "", out var position);
        var active = SwActive.IsToggled;

        if (!esEdicion) // crear: requeridos
        {
            if (string.IsNullOrWhiteSpace(name)) { SetError(BdrName, ErrName, true); await DisplayAlert("Nombre", "Escribe el nombre.", "OK"); return; }
            if (!SlugValido(slug)) { SetError(BdrSlug, ErrSlug, true); await DisplayAlert("Slug", "Slug inválido.", "OK"); return; }
            if (!posOk || position < 0) { SetError(BdrPos, ErrPos, true); await DisplayAlert("Posición", "Posición inválida.", "OK"); return; }
        }
        else
        {
            // edición: solo valida si se especifica
            if (!string.IsNullOrWhiteSpace(slug) && !SlugValido(slug)) { SetError(BdrSlug, ErrSlug, true); await DisplayAlert("Slug", "Slug inválido.", "OK"); return; }
            if (!string.IsNullOrWhiteSpace(TxtPosition.Text) && (!posOk || position < 0)) { SetError(BdrPos, ErrPos, true); await DisplayAlert("Posición", "Posición inválida.", "OK"); return; }
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        { await ServidorNoDisponible("sin_internet"); return; }

        BtnGuardar.IsEnabled = false; BtnCancelar.IsEnabled = false;
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            HttpResponseMessage resp;
            string body;

            if (esEdicion && CategoryId > 0)
            {
                // PATCH parcial solo con cambios
                var payload = new Dictionary<string, object?>();
                if (!string.IsNullOrWhiteSpace(name) && name != _origName) payload["name"] = name;
                if (!string.IsNullOrWhiteSpace(slug) && slug != _origSlug) payload["slug"] = slug;
                if (!string.IsNullOrWhiteSpace(TxtPosition.Text) && position != _origPos) payload["position"] = position;
                if (active != _origActive) payload["isActive"] = active;
                if (SwIsCombo.IsToggled != _origIsCombo) payload["isComboOnly"] = SwIsCombo.IsToggled;
                if (payload.Count == 0) { await DisplayAlert("Sin cambios", "No hay cambios por guardar.", "OK"); return; }

                var json = JsonSerializer.Serialize(payload, _jsonWrite);
                // 👀 Muestra lo que vas a mandar
                await DisplayAlert($"Payload (PATCH /api/categories/{CategoryId})", json, "OK");

                resp = await http.PatchAsync($"/api/categories/{CategoryId}", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                if (resp.StatusCode == HttpStatusCode.Forbidden)
{
    await DisplayAlert("Acceso restringido", "No tienes permiso para esta acción.", "OK");
    return;
}


                body = await resp.Content.ReadAsStringAsync();
                // 👀 Muestra status y cuerpo de respuesta
                await DisplayAlert(
                    "Respuesta PATCH",
                    $"Status: {(int)resp.StatusCode} {resp.StatusCode}\n\nBody:\n{body}",
                    "OK"
                );
            }
            else
            {
                var payload = new
                {
                    name,
                    slug,
                    parentId = (int?)null,
                    position,
                    isActive = active,
                    isComboOnly = SwIsCombo.IsToggled
                };

                var json = JsonSerializer.Serialize(payload, _jsonWrite);

                // 👀 Muestra lo que vas a mandar
                await DisplayAlert("Payload (POST /api/categories)", json, "OK");

                resp = await http.PostAsync("/api/categories",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                    if (resp.StatusCode == HttpStatusCode.Forbidden)
{
    await DisplayAlert("Acceso restringido", "No tienes permiso para esta acción.", "OK");
    return;
}



                body = await resp.Content.ReadAsStringAsync();

                // 👀 Muestra status y cuerpo de respuesta
                await DisplayAlert(
                    "Respuesta POST",
                    $"Status: {(int)resp.StatusCode} {resp.StatusCode}\n\nBody:\n{body}",
                    "OK"
                );

                // 👀 Extra: intenta leer isComboOnly de la respuesta (si tu API lo regresa)
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("data", out var dataEl) &&
                        dataEl.ValueKind == JsonValueKind.Object &&
                        dataEl.TryGetProperty("isComboOnly", out var comboEl))
                    {
                        await DisplayAlert("Eco del server", $"isComboOnly guardado = {comboEl.GetBoolean()}", "OK");
                    }
                }
                catch { /* ignorar parse errors */ }
            }


            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

                if (resp.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
                { await ServidorNoDisponible("timeout"); return; }

                // ¿qué campos cambiaron?
                bool cambiandoNombre = !string.IsNullOrWhiteSpace(name) && name != _origName;
                bool cambiandoSlug = !string.IsNullOrWhiteSpace(slug) && slug != _origSlug;

                // Intentar mensaje amigable
                var friendly = MapFriendlyMessage(resp.StatusCode, body, cambiandoNombre, cambiandoSlug);
                if (friendly != null)
                {
                    if (cambiandoNombre) SetError(BdrName, ErrName, true, friendly);
                    if (cambiandoSlug) SetError(BdrSlug, ErrSlug, true, friendly);
                    await DisplayAlert("Aviso", friendly, "OK");
                    if (cambiandoNombre) TxtName.Focus();
                    return;
                }

                // Fallback a tu envoltura habitual
                var envErr = JsonSerializer.Deserialize<ApiEnvelope<object>>(body, _json);
                var msg = envErr?.message ?? (envErr?.error as string) ?? envErr?.error?.ToString() ?? body;

                await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(msg) ? "Error al guardar." : msg);
                return;
            }


            await DisplayAlert("Listo", "Cambios guardados.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (TaskCanceledException) { await ServidorNoDisponible("timeout"); }
        catch (HttpRequestException) { await ServidorNoDisponible(""); }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Categories – Guardar"); }
        finally { BtnGuardar.IsEnabled = true; BtnCancelar.IsEnabled = true; }
    }

    async void Cancelar_Clicked(object s, EventArgs e) => await Shell.Current.GoToAsync("..");
}
