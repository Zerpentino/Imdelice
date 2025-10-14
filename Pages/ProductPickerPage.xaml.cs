using System.Collections.ObjectModel;
using System.Text.Json;
using System.Net.Http;                // <-- importante
using System.Net.Http.Headers;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;
using Imdeliceapp.Helpers;
using System.Linq;                    // <-- importante
using Imdeliceapp.Model;

namespace Imdeliceapp.Pages;

public partial class ProductPickerPage : ContentPage
{
    // === Tipos ===
    public class ProductDTO
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public int? priceCents { get; set; }
        public bool isActive { get; set; }
    }

    class ApiEnvelope<T>
    {
        public T? data { get; set; }
        public string? message { get; set; }
    }

    class ViewItem
    {
        public int id { get; set; }
        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public string priceLabel { get; set; } = "";
    }

    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    readonly List<ProductDTO> _all = new();
    readonly ObservableCollection<ViewItem> _view = new();

    public ProductPickerPage()
    {
        InitializeComponent();
        CV.ItemsSource = _view;
    }

    // Devuelve el producto elegido
    public static async Task<ProductDTO?> PickAsync(INavigation nav)
    {
        var tcs = new TaskCompletionSource<ProductDTO?>();
        var page = new ProductPickerPage();
        page.ProductSelected += (_, p) => tcs.TrySetResult(p);

        await nav.PushModalAsync(new NavigationPage(page));
        var result = await tcs.Task;
        await nav.PopModalAsync();
        return result;
    }

    public event EventHandler<ProductDTO?>? ProductSelected;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarProductosAsync();
    }

    static string Money(int? cents) => cents.HasValue ? (cents.Value / 100.0m).ToString("$0.00") : "—";

    async Task CargarProductosAsync()
    {
        try
        {
            _all.Clear(); _view.Clear();

            var token = await SecureStorage.GetAsync("token");
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
            cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Ajusta si tu backend usa otra ruta/filtros
            var resp = await cli.GetAsync("/api/products");
            var body = await resp.Content.ReadAsStringAsync();

            var env = JsonSerializer.Deserialize<ApiEnvelope<List<ProductDTO>>>(body, _json);
            foreach (var p in env?.data ?? new())
            {
                _all.Add(p);
                _view.Add(new ViewItem
                {
                    id = p.id,
                    name = p.name,
                    type = p.type,
                    priceLabel = Money(p.priceCents)
                });
            }
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Productos – Picker");
        }
    }

    void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? "").Trim().ToLowerInvariant();
        _view.Clear();
        foreach (var p in _all.Where(p => (p.name ?? "").ToLowerInvariant().Contains(q)))
            _view.Add(new ViewItem { id = p.id, name = p.name, type = p.type, priceLabel = Money(p.priceCents) });
    }

    void CV_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is ViewItem v)
        {
            var chosen = _all.FirstOrDefault(p => p.id == v.id);
            ProductSelected?.Invoke(this, chosen);
        }
    }
}
