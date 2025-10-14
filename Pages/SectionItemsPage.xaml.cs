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
[QueryProperty(nameof(SectionId), "sectionId")]
[QueryProperty(nameof(SectionName), "sectionName")]
public partial class SectionItemsPage : ContentPage
{
    public int MenuId { get; set; }
    public int SectionId { get; set; }
    public string? SectionName { get; set; }
    public string Titulo => $"Ítems – {SectionName}";

    #region DTOs
    class ApiEnvelope<T> { public object? error { get; set; } public T? data { get; set; } public string? message { get; set; } }

    class ProductBasicDTO { public int id { get; set; } public string name { get; set; } = ""; public string type { get; set; } = ""; public int? priceCents { get; set; } public bool isActive { get; set; } public string? imageUrl { get; set; } }

    class MenuItemDTO
    {
        public int id { get; set; }
        public int sectionId { get; set; }
        public int productId { get; set; }
        public string? displayName { get; set; }
        public int? displayPriceCents { get; set; } // null = usar product.priceCents / regla
        public int position { get; set; }
        public bool isFeatured { get; set; }
        public ProductBasicDTO product { get; set; } = new();
        // helpers UI
        public string displayNameOrProduct => string.IsNullOrWhiteSpace(displayName) ? product?.name ?? "" : displayName!;
        public string priceLabel => displayPriceCents.HasValue ? CentsToMoney(displayPriceCents.Value)
                                  : product?.type == "SIMPLE" || product?.type == "COMBO"
                                    ? CentsToMoney(product?.priceCents ?? 0)
                                    : "Desde…";
    }

    class MenuPublicDTO
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public bool isActive { get; set; }
        public List<SectionDTO> sections { get; set; } = new();
    }
    class SectionDTO
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public int position { get; set; }
        public List<MenuItemDTO> items { get; set; } = new();
    }
    #endregion

    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    readonly ObservableCollection<MenuItemDTO> _items = new();

    public SectionItemsPage()
    {
        InitializeComponent();
        BindingContext = this;
        ItemsCV.ItemsSource = _items;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarItemsAsync();
    }

    #region helpers
    static string CentsToMoney(int cents) => (cents / 100.0m).ToString("$0.00");
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

    async Task CargarItemsAsync()
    {
        try
        {
            _items.Clear();

            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.GetAsync($"/api/menus/{MenuId}/public");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body)); return; }

            var env = JsonSerializer.Deserialize<ApiEnvelope<MenuPublicDTO>>(body, _json);
            var sec = env?.data?.sections?.FirstOrDefault(s => s.id == SectionId);
            foreach (var it in sec?.items ?? new()) _items.Add(it);
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Items – Cargar"); }
    }

    async void Refrescar_Clicked(object sender, EventArgs e) => await CargarItemsAsync();

    // Agregar: abrimos selector de producto y luego preguntamos datos opcionales
    async void AgregarItem_Clicked(object sender, EventArgs e)
    {
        var prod = await ProductPickerPage.PickAsync(Navigation);
        if (prod is null) return;

        var dispName = await DisplayPromptAsync("Nombre a mostrar", "Deja vacío para usar el del producto.", initialValue: prod.name);
        var dispPriceStr = await DisplayPromptAsync("Precio a mostrar", "En centavos (o vacío para usar el normal).", keyboard: Keyboard.Numeric);

        int? dispPrice = null;
        if (int.TryParse(dispPriceStr, out var cents) && cents >= 0) dispPrice = cents;

        try
        {
            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var payload = JsonContent.Create(new
            {
                sectionId = SectionId,
                productId = prod.id,
                displayName = string.IsNullOrWhiteSpace(dispName) ? null : dispName,
                displayPriceCents = dispPrice,
                position = 0,
                isFeatured = false
            });

            var resp = await http.PostAsync("/api/menus/items", payload);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body)); return; }

            await CargarItemsAsync();
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Items – Agregar"); }
    }

    async void Editar_Clicked(object sender, EventArgs e)
    {
        if ((sender as ImageButton)?.BindingContext is not MenuItemDTO it) return;

        var newName = await DisplayPromptAsync("Nombre a mostrar", "(vacío = usar producto)", initialValue: it.displayName ?? "");
        var newPriceStr = await DisplayPromptAsync("Precio a mostrar (centavos)", "(vacío = usar normal)", keyboard: Keyboard.Numeric,
            initialValue: it.displayPriceCents?.ToString() ?? "");
        var newPosStr = await DisplayPromptAsync("Posición", "Número entero (0 = arriba)", keyboard: Keyboard.Numeric,
            initialValue: it.position.ToString());
        var wantFeatured = await DisplayAlert("Destacar", "¿Marcar como destacado?", "Sí", "No");

        int? newPrice = null; if (int.TryParse(newPriceStr, out var cents)) newPrice = cents >= 0 ? cents : null;
        int pos = it.position; if (int.TryParse(newPosStr, out var p)) pos = Math.Max(0, p);

        try
        {
            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.PatchAsync($"/api/menus/items/{it.id}",
                JsonContent.Create(new
                {
                    displayName = string.IsNullOrWhiteSpace(newName) ? null : newName,
                    displayPriceCents = newPrice,
                    position = pos,
                    isFeatured = wantFeatured
                }));
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body)); return; }

            await CargarItemsAsync();
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Items – Editar"); }
    }

    async void MoverArriba_Clicked(object sender, EventArgs e)
    {
        if ((sender as ImageButton)?.BindingContext is not MenuItemDTO it) return;
        try
        {
            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);
            var resp = await http.PatchAsync($"/api/menus/items/{it.id}", JsonContent.Create(new { position = Math.Max(0, it.position - 1) }));
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body)); return; }
            await CargarItemsAsync();
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Items – Reordenar"); }
    }

    async void Eliminar_Clicked(object sender, EventArgs e)
    {
        if ((sender as ImageButton)?.BindingContext is not MenuItemDTO it) return;
        var ok = await DisplayAlert("Eliminar ítem", $"¿Quitar “{it.displayNameOrProduct}” de la sección?", "Sí", "Cancelar");
        if (!ok) return;

        try
        {
            var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);
            var resp = await http.DeleteAsync($"/api/menus/items/{it.id}");
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
                return;
            }
            await CargarItemsAsync();
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Items – Eliminar"); }
    }
}
