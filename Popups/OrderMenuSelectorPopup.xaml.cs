using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Pages;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Popups;

public partial class OrderMenuSelectorPopup : Popup
{
    readonly MenusApi _menusApi = new();
    readonly List<SectionVm> _allSections = new();
    readonly Dictionary<int, List<TakeOrderPage.MenuItemVm>> _itemsByProduct = new();
    readonly List<MenuOptionVm> _menuOptions = new();
    bool _isInitializing;

    public ObservableCollection<SectionVm> Sections { get; } = new();
    public ObservableCollection<MenuOptionVm> MenuOptions { get; } = new();

    MenuOptionVm? _selectedMenu;
    public MenuOptionVm? SelectedMenu
    {
        get => _selectedMenu;
        set
        {
            if (_selectedMenu == value) return;
            _selectedMenu = value;
            OnPropertyChanged();

            if (_isInitializing || _selectedMenu == null)
                return;

            Preferences.Default.Set("active_menu_id", _selectedMenu.Id);
            _ = LoadMenuPublicAsync(_selectedMenu.Id);
        }
    }

    string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value) return;
            _searchQuery = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading == value) return; _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowEmptyState)); }
    }

    public bool ShowEmptyState => !IsLoading && Sections.All(s => !s.HasItems);

    public OrderMenuSelectorPopup()
    {
        InitializeComponent();
        BindingContext = this;
        _ = LoadMenuAsync();
    }

    async Task LoadMenuAsync()
    {
        try
        {
            IsLoading = true;
            _isInitializing = true;
            var menus = await _menusApi.GetMenusAsync();
            if (menus == null || menus.Count == 0)
            {
                await Application.Current.MainPage.DisplayAlert("Menú", "No hay menús publicados.", "OK");
                Close();
                return;
            }

            var storedId = Preferences.Default.Get("active_menu_id", 0);
            _menuOptions.Clear();
            MenuOptions.Clear();
            foreach (var m in menus)
            {
                var option = new MenuOptionVm(m.id, m.name ?? $"Menú #{m.id}");
                _menuOptions.Add(option);
                MenuOptions.Add(option);
            }

            var initial = _menuOptions.FirstOrDefault(m => m.Id == storedId)
                          ?? _menuOptions.FirstOrDefault(m => menus.FirstOrDefault(x => x.id == m.Id)?.isActive == true)
                          ?? _menuOptions.FirstOrDefault();

            SelectedMenu = initial;
            if (initial != null)
                await LoadMenuPublicAsync(initial.Id);
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Menú", $"No se pudo cargar el menú: {ex.Message}", "OK");
            Close();
        }
        finally
        {
            _isInitializing = false;
            IsLoading = false;
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    async Task LoadMenuPublicAsync(int menuId)
    {
        try
        {
            IsLoading = true;
            var menuPublic = await _menusApi.GetMenuPublicAsync(menuId);
            if (menuPublic?.sections == null)
            {
                await Application.Current.MainPage.DisplayAlert("Menú", "No se pudieron cargar las secciones.", "OK");
                return;
            }

            _allSections.Clear();
            Sections.Clear();
            _itemsByProduct.Clear();

            foreach (var section in menuPublic.sections.OrderBy(s => s.position))
            {
                var items = new List<ProductVm>();
                if (section.items != null)
                {
                    foreach (var item in section.items.OrderBy(i => i.position))
                    {
                        var vm = TakeOrderPage.MenuItemVm.From(section, item);
                        if (vm == null || vm.ProductId == null || vm.ProductId <= 0)
                            continue;
                        items.Add(new ProductVm(vm));
                        if (!_itemsByProduct.TryGetValue(vm.ProductId.Value, out var list))
                        {
                            list = new List<TakeOrderPage.MenuItemVm>();
                            _itemsByProduct[vm.ProductId.Value] = list;
                        }
                        list.Add(vm);
                    }
                }

                if (items.Count == 0)
                    continue;

                var sectionVm = new SectionVm(section.name ?? "Sección", items);
                _allSections.Add(sectionVm);
                Sections.Add(sectionVm);
            }

            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    void ApplyFilter()
    {
        var query = (SearchQuery ?? string.Empty).Trim().ToLowerInvariant();
        foreach (var section in _allSections)
            section.ApplyFilter(query);
    }

    void BackButton_Clicked(object sender, EventArgs e) => Close(null);

    void CloseButton_Clicked(object sender, EventArgs e) => Close(null);

    void SelectProduct_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ProductVm vm)
            return;
        IReadOnlyList<TakeOrderPage.MenuItemVm> variants;
        if (vm.MenuItem.ProductId.HasValue)
        {
            variants = _itemsByProduct.TryGetValue(vm.MenuItem.ProductId.Value, out var list)
                ? list
                : new List<TakeOrderPage.MenuItemVm> { vm.MenuItem };
        }
        else
        {
            variants = new List<TakeOrderPage.MenuItemVm> { vm.MenuItem };
        }

        Close(new SelectionResult(vm.MenuItem, variants));
    }


    public class SectionVm : INotifyPropertyChanged
    {
        readonly List<ProductVm> _allItems;
        public ObservableCollection<ProductVm> Items { get; } = new();
        public string Title { get; }

        public SectionVm(string title, IEnumerable<ProductVm> items)
        {
            Title = title;
            _allItems = items.ToList();
            foreach (var item in _allItems)
                Items.Add(item);
        }

        public bool HasItems => Items.Count > 0;

        public void ApplyFilter(string query)
        {
            Items.Clear();
            foreach (var item in _allItems)
            {
                if (item.Matches(query))
                    Items.Add(item);
            }
            OnPropertyChanged(nameof(HasItems));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ProductVm
    {
        public ProductVm(TakeOrderPage.MenuItemVm menuItem)
        {
            MenuItem = menuItem;
            Title = menuItem.Title;
            Subtitle = menuItem.Subtitle;
            PriceLabel = menuItem.UnitPrice > 0 ? menuItem.UnitPrice.ToString("$0.00") : "Configurar";
        }

        public TakeOrderPage.MenuItemVm MenuItem { get; }
        public string Title { get; }
        public string? Subtitle { get; }
        public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
        public string PriceLabel { get; }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return true;
            var q = query.ToLowerInvariant();
            return (Title ?? string.Empty).ToLowerInvariant().Contains(q)
                   || (Subtitle ?? string.Empty).ToLowerInvariant().Contains(q);
        }
    }

    public class MenuOptionVm
    {
        public MenuOptionVm(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }

    public class SelectionResult
    {
        public SelectionResult(TakeOrderPage.MenuItemVm menuItem, IReadOnlyList<TakeOrderPage.MenuItemVm> variants)
        {
            MenuItem = menuItem;
            Variants = variants;
        }

        public TakeOrderPage.MenuItemVm MenuItem { get; }
        public IReadOnlyList<TakeOrderPage.MenuItemVm> Variants { get; }
    }
}
