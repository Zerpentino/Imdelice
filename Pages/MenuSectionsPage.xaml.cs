using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;
using Imdeliceapp.Helpers;
using Microsoft.Maui.Controls;
using System.Text;
using Imdeliceapp.Model;
using System.Net.Http;       // por si no estaba
using System.Net.Http.Json;  // <- aquí vive JsonContent

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(MenuId), "menuId")]
[QueryProperty(nameof(MenuName), "menuName")]
public partial class MenuSectionsPage : ContentPage 
{
    public int MenuId { get; set; }
    public string? MenuName { get; set; }
    public string Titulo => $"Secciones – {MenuName}";

    #region DTOs
    class ApiEnvelope<T> { public object? error { get; set; } public T? data { get; set; } public string? message { get; set; } }
    class SectionDTO { public int id { get; set; } public int menuId { get; set; } public string name { get; set; } = ""; public int position { get; set; } public int? categoryId { get; set; } }
    class MenuPublicDTO { public int id { get; set; } public string name { get; set; } = ""; public bool isActive { get; set; } public List<SectionDTO> sections { get; set; } = new(); }
    #endregion

    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    readonly ObservableCollection<SectionDTO> _sections = new();
    SectionDTO? _selected;

    public MenuSectionsPage()
    {
        InitializeComponent();
        BindingContext = this;
        SectionsCV.ItemsSource = _sections;
        SectionsCV.SelectionChanged += (_, __) => { _selected = null; };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarSeccionesAsync();
    }

    #region helpers
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
    #endregion

    async Task CargarSeccionesAsync()
    {
        try
        {
            _sections.Clear();
            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            // No hay GET de secciones sueltas; usamos el menú público y tomamos sus secciones
            var resp = await http.GetAsync($"/api/menus/{MenuId}/public");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body)); return; }

            var env = JsonSerializer.Deserialize<ApiEnvelope<MenuPublicDTO>>(body, _json);
            foreach (var s in env?.data?.sections ?? new()) _sections.Add(s);
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Secciones – Cargar"); }
    }

    async void Agregar_Clicked(object sender, EventArgs e)
    {
        var name = TxtName.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { await DisplayAlert("Sección", "Escribe un nombre.", "OK"); return; }

        int.TryParse(TxtPos.Text, out var pos);
        int? catId = null;
        if (int.TryParse(TxtCatId.Text, out var cid)) catId = cid;

        try
        {
            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var payload = JsonContent.Create(new { menuId = MenuId, name, position = pos, categoryId = catId });
            var resp = await http.PostAsync("/api/menus/sections", payload);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body)); return; }

            TxtName.Text = TxtPos.Text = TxtCatId.Text = "";
            await CargarSeccionesAsync();
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Secciones – Agregar"); }
    }

    // Mover “arriba/abajo”: cambiamos position y PATCH
    async void Up_Clicked(object sender, EventArgs e)  => await MoverAsync(sender, -1);
    async void Down_Clicked(object sender, EventArgs e)=> await MoverAsync(sender, +1);

    async Task MoverAsync(object sender, int delta)
    {
        if ((sender as ImageButton)?.BindingContext is not SectionDTO s) return;
        var nuevaPos = Math.Max(0, s.position + delta);

        try
        {
            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.PatchAsync($"/api/menus/sections/{s.id}", JsonContent.Create(new { position = nuevaPos }));
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body)); return; }
            await CargarSeccionesAsync();
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Secciones – Reordenar"); }
    }

    async void Editar_Clicked(object sender, EventArgs e)
    {
        // Toma la última sección tocada (por Items/Up/Down/Delete) o pide id
        var s = _selected ?? _sections.FirstOrDefault();
        if (s is null) { await DisplayAlert("Sección", "No hay sección seleccionada.", "OK"); return; }

        var name = TxtEditName.Text?.Trim();
        int? pos = null; if (int.TryParse(TxtEditPos.Text, out var p)) pos = p;
        int? catId = null; if (int.TryParse(TxtEditCatId.Text, out var cid)) catId = cid;

        if (string.IsNullOrWhiteSpace(name) && pos is null && !catId.HasValue)
        { await DisplayAlert("Sección", "Nada que actualizar.", "OK"); return; }

        try
        {
            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.PatchAsync($"/api/menus/sections/{s.id}", JsonContent.Create(new { name, position = pos, categoryId = catId }));
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body)); return; }

            TxtEditName.Text = TxtEditPos.Text = TxtEditCatId.Text = "";
            await CargarSeccionesAsync();
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Secciones – Editar"); }
    }

    async void Items_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is not SectionDTO s) return;
        _selected = s;
        await Shell.Current.GoToAsync($"{nameof(SectionItemsPage)}?menuId={MenuId}&sectionId={s.id}&sectionName={Uri.EscapeDataString(s.name)}");
    }

    async void Eliminar_Clicked(object sender, EventArgs e)
    {
        if ((sender as ImageButton)?.BindingContext is not SectionDTO s) return;
        var hard = await DisplayAlert("Eliminar sección", $"¿Eliminar “{s.name}” definitivamente?", "Hard delete", "Ocultar/soft");
        var url = hard ? $"/api/menus/sections/{s.id}?hard=true" : $"/api/menus/sections/{s.id}";
        try
        {
            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.DeleteAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
                return;
            }
            await CargarSeccionesAsync();
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Secciones – Eliminar"); }
    }
}
