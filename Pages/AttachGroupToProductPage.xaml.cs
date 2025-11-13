using Imdeliceapp.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Imdeliceapp.Models;
using ModelsCategoryDTO = Imdeliceapp.Models.CategoryDTO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(GroupId), "groupId")]
[QueryProperty(nameof(GroupName), "groupName")]
public partial class AttachGroupToProductPage : ContentPage
{
    readonly ModifiersApi _api = new();

    public int GroupId { get; set; }
    public string GroupName { get; set; } = "";

    public ObservableCollection<ProductRow> Products { get; } = new();
    List<ProductRow> _all = new();
    HashSet<int> _attachedProductIds = new();

    public ObservableCollection<ModelsCategoryDTO> Categories { get; } = new();
    ModelsCategoryDTO? _selectedCategory;

    bool _isRefreshing;
    public bool IsRefreshing { get => _isRefreshing; set { _isRefreshing = value; OnPropertyChanged(); } }
    bool _loading;

    public AttachGroupToProductPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Hdr.Text = $"Grupo #{GroupId} — {GroupName}";

        if (Categories.Count == 0) await LoadCategoriesAsync();
        await LoadAttachedProductsAsync();
        await LoadProductsAsync();
    }

    async Task LoadCategoriesAsync()
    {
        try
        {
            Categories.Clear();
            Categories.Add(new ModelsCategoryDTO { id = 0, name = "Todas", isActive = true });

            var cats = await _api.GetCategoriesAsync(isActive: null); // o true, según prefieras
            foreach (var c in cats.OrderBy(c => c.name)) Categories.Add(c);
            CategoryPicker.ItemsSource = Categories;
            CategoryPicker.SelectedIndex = 0;
            _selectedCategory = Categories.First();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Categorías: {ex.Message}", "OK");
        }
    }

    async Task LoadAttachedProductsAsync()
    {
        try
        {
            var links = await _api.GetProductsByGroupAsync(GroupId, isActive: null, search: null, limit: 200, offset: 0);
            _attachedProductIds = links.Select(l => l.product.id).ToHashSet();
        }
        catch
        {
            _attachedProductIds = new HashSet<int>();
        }
    }

    async Task LoadProductsAsync()
    {
        if (_loading) return;
        _loading = true;
        try
        {
            var catId = (_selectedCategory != null && _selectedCategory.id > 0) ? _selectedCategory.id : (int?)null;
            var q = (SearchBox?.Text ?? "").Trim();

            var list = await _api.GetProductsAsync(
                isActive: null,             // si quieres solo activos: true
                categoryId: catId,
                search: string.IsNullOrEmpty(q) ? null : q,
                limit: 100,
                offset: 0
            );

            var rows = list
                .OrderByDescending(p => p.isActive)
                .ThenBy(p => p.name)
                .Select(p => new ProductRow(p)
                {
                    IsLinked = _attachedProductIds.Contains(p.id)
                })
                .ToList();

            _all = rows;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            _loading = false;
            IsRefreshing = false;
        }
    }

    async void CategoryPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _selectedCategory = CategoryPicker.SelectedItem as ModelsCategoryDTO;
        ApplyFilters();
    }
void ApplyFilters()
{
    var q = (SearchBox?.Text ?? "").Trim().ToLowerInvariant();
    int? catId = (_selectedCategory != null && _selectedCategory.id > 0) ? _selectedCategory.id : (int?)null;

    IEnumerable<ProductRow> src = _all;

    if (catId.HasValue)
        src = src.Where(p => p.Source.categoryId == catId.Value);

    if (!string.IsNullOrEmpty(q))
        src = src.Where(p => p.Name.ToLowerInvariant().Contains(q));

    Products.Clear();
    foreach (var p in src) Products.Add(p);
}

    void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
       ApplyFilters(); 
    }

    void SearchBox_SearchButtonPressed(object? sender, EventArgs e)
    {
        // si quieres buscar en servidor explícitamente al presionar "Buscar"
        _ = LoadProductsAsync();
    }

    async void ProdRefresh_Refreshing(object s, EventArgs e)
    {
        IsRefreshing = true;
        await LoadAttachedProductsAsync();
        await LoadProductsAsync();
    }

    async void AttachRow_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ProductRow row) return;
        if (row.IsLinked)
        {
            await DisplayAlert("Aviso", "Este producto ya tiene el grupo adjuntado.", "OK");
            return;
        }

        var p = row.Source;

        // posición: toma la que esté en la parte inferior (DefaultPosEntry) o pregunta
        int position = 0;
        if (!int.TryParse(DefaultPosEntry.Text, out position))
            position = 0;

        try
        {
            await _api.AttachGroupToProductAsync(p.id, GroupId, position);
            row.IsLinked = true;
            _attachedProductIds.Add(p.id);
            await DisplayAlert("OK", $"Grupo adjuntado a “{p.name}”.", "Cerrar");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("409") || ex.Message.Contains("unique"))
        {
            await DisplayAlert("Aviso", "Ese grupo ya estaba vinculado a este producto.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    public class ProductRow : INotifyPropertyChanged
    {
        public ProductRow(ModifiersApi.SimpleProductDTO source)
        {
            Source = source;
        }

        public ModifiersApi.SimpleProductDTO Source { get; }

        public string Name => Source.name ?? $"Producto #{Source.id}";
        public string Type => Source.type ?? "SIMPLE";
        public string PriceDisplay => Source.priceCents.HasValue
            ? (Source.priceCents.Value / 100m).ToString("C", CultureInfo.CurrentCulture)
            : "—";
        public bool IsActive => Source.isActive;

        bool _isLinked;
        public bool IsLinked
        {
            get => _isLinked;
            set
            {
                if (_isLinked == value) return;
                _isLinked = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
