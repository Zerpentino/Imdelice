using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Imdeliceapp.Helpers;
using Imdeliceapp.Model;
using Imdeliceapp.Services;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

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
        public int categoryId { get; set; }
        public string? barcode { get; set; }
    }

    class ApiEnvelope<T>
    {
        public T? data { get; set; }
        public string? message { get; set; }
    }

    class ViewItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string PriceLabel { get; set; } = "";
        public bool IsLinked { get; set; }
    }

    class CategoryOption
    {
        public CategoryOption(int? id, string name)
        {
            Id = id;
            Name = name;
        }
        public int? Id { get; }
        public string Name { get; }
    }

    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    readonly MenusApi _menusApi = new();
    readonly List<ProductDTO> _all = new();
    readonly ObservableCollection<ViewItem> _view = new();
    readonly Func<ProductDTO, bool>? _filter;
    readonly HashSet<int> _highlightIds;
    readonly ObservableCollection<CategoryOption> _categoryOptions = new();
    readonly int? _preferredCategoryId;
    bool _categoriesLoaded;
    string _searchQuery = string.Empty;
    int? _selectedCategoryId;

    public ProductPickerPage(Func<ProductDTO, bool>? filter = null, IEnumerable<int>? highlightProductIds = null, int? preferredCategoryId = null)
    {
        InitializeComponent();
        _filter = filter;
        _highlightIds = highlightProductIds is null ? new HashSet<int>() : new HashSet<int>(highlightProductIds);
        _preferredCategoryId = preferredCategoryId;
        CV.ItemsSource = _view;
        CategoryFilter.ItemsSource = _categoryOptions;
        BindingContext = this;
    }

    // Devuelve el producto elegido
    public static async Task<ProductDTO?> PickAsync(INavigation nav, Func<ProductDTO, bool>? filter = null, IEnumerable<int>? highlightProductIds = null, int? preferredCategoryId = null)
    {
        var tcs = new TaskCompletionSource<ProductDTO?>();
        var page = new ProductPickerPage(filter, highlightProductIds, preferredCategoryId);
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

    async Task EnsureCategoriesAsync()
    {
        if (_categoriesLoaded) return;

        try
        {
            var categories = await _menusApi.GetCategoriesAsync(isActive: true);
            _categoryOptions.Clear();
            _categoryOptions.Add(new CategoryOption(null, "Todas las categorías"));

            foreach (var cat in categories.OrderBy(c => c.name ?? $"Categoría {c.id}", StringComparer.CurrentCultureIgnoreCase))
                _categoryOptions.Add(new CategoryOption(cat.id, cat.name ?? $"Categoría #{cat.id}"));

            _categoriesLoaded = true;

            if (_preferredCategoryId.HasValue)
            {
                var idx = -1;
                for (var i = 0; i < _categoryOptions.Count; i++)
                {
                    if (_categoryOptions[i].Id == _preferredCategoryId)
                    {
                        idx = i;
                        break;
                    }
                }
                CategoryFilter.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else
            {
                CategoryFilter.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Productos – Categorías");
            CategoryFilter.IsEnabled = false;
        }
    }

    async Task CargarProductosAsync()
    {
        try
        {
            SetLoading(true);
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
                if (_filter != null && !_filter(p)) continue;

                _all.Add(p);
            }

            await EnsureCategoriesAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Productos – Picker");
        }
        finally
        {
            SetLoading(false);
        }
    }

    void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchQuery = (e.NewTextValue ?? string.Empty).Trim();
        ApplyFilters();
    }

    void CV_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is ViewItem v)
        {
            var chosen = _all.FirstOrDefault(p => p.id == v.Id);
            ProductSelected?.Invoke(this, chosen);
        }
    }

    void Cancel_Clicked(object sender, EventArgs e)
        => ProductSelected?.Invoke(this, null);

    void SetLoading(bool value)
    {
        LoadingIndicator.IsVisible = LoadingIndicator.IsRunning = value;
        CV.IsVisible = !value;
        Search.IsEnabled = !value;
        CategoryFilter.IsEnabled = !value && _categoryOptions.Count > 0;
    }

    void ApplyFilters()
    {
        var query = _searchQuery.ToLowerInvariant();
        var hasQuery = !string.IsNullOrWhiteSpace(query);

        var filtered = _all.Where(p =>
        {
            if (_selectedCategoryId.HasValue && p.categoryId != _selectedCategoryId.Value)
                return false;

            if (!hasQuery) return true;

            return (p.name ?? string.Empty).ToLowerInvariant().Contains(query) ||
                   (p.type ?? string.Empty).ToLowerInvariant().Contains(query);
        })
        .OrderBy(p => p.name, StringComparer.CurrentCultureIgnoreCase);

        _view.Clear();
        foreach (var p in filtered)
            _view.Add(ToViewItem(p));
    }

    ViewItem ToViewItem(ProductDTO dto)
    {
        return new ViewItem
        {
            Id = dto.id,
            Name = dto.name,
            Type = TypeDisplay(dto.type),
            PriceLabel = Money(dto.priceCents),
            IsLinked = _highlightIds.Contains(dto.id)
        };
    }

    static string TypeDisplay(string? type)
    {
        return type?.ToUpperInvariant() switch
        {
            "SIMPLE" => "Producto",
            "COMBO" => "Combo",
            "VARIANTED" => "Con variantes",
            _ => type ?? "Desconocido"
        };
    }

    void CategoryFilter_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (CategoryFilter.SelectedItem is CategoryOption option)
            _selectedCategoryId = option.Id;
        else
            _selectedCategoryId = null;

        ApplyFilters();
    }
}
