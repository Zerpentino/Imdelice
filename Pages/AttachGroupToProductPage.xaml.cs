using Imdeliceapp.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Imdeliceapp.Models;
using ModelsCategoryDTO = Imdeliceapp.Models.CategoryDTO;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(GroupId), "groupId")]
[QueryProperty(nameof(GroupName), "groupName")]
public partial class AttachGroupToProductPage : ContentPage
{
    readonly ModifiersApi _api = new();

    public int GroupId { get; set; }
    public string GroupName { get; set; } = "";

    public ObservableCollection<ModifiersApi.SimpleProductDTO> Products { get; } = new();
    List<ModifiersApi.SimpleProductDTO> _all = new();

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

            _all = list
                .OrderByDescending(p => p.isActive)
                .ThenBy(p => p.name)
                .ToList();
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

        // recargar con el nuevo filtro
        ApplyFilters();
    }
void ApplyFilters()
{
    var q = (SearchBox?.Text ?? "").Trim().ToLowerInvariant();
    int? catId = (_selectedCategory != null && _selectedCategory.id > 0) ? _selectedCategory.id : (int?)null;

    IEnumerable<ModifiersApi.SimpleProductDTO> src = _all;

    // filtro por categoría (local)
    if (catId.HasValue)
        src = src.Where(p => p.categoryId == catId.Value);

    // filtro por texto (local)
    if (!string.IsNullOrEmpty(q))
        src = src.Where(p => (p.name ?? "").ToLowerInvariant().Contains(q));

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
        await LoadProductsAsync();
    }

    async void AttachRow_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ModifiersApi.SimpleProductDTO p) return;

        // posición: toma la que esté en la parte inferior (DefaultPosEntry) o pregunta
        int position = 0;
        if (!int.TryParse(DefaultPosEntry.Text, out position))
            position = 0;

        try
        {
            await _api.AttachGroupToProductAsync(p.id, GroupId, position);
            await DisplayAlert("OK", $"Grupo adjuntado a “{p.name}”.", "Cerrar");
            // Vuelve a la pantalla de vinculados (para que se vea el nuevo):
            await Navigation.PopAsync();
             
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
}
