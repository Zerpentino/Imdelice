using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Imdeliceapp.Helpers;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.Linq;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(MenuId), "menuId")]
[QueryProperty(nameof(MenuName), "menuName")]
[QueryProperty(nameof(SectionId), "sectionId")]
[QueryProperty(nameof(SectionName), "sectionName")]
public partial class SectionItemsPage : ContentPage
{
    readonly MenusApi _menusApi = new();

    public int MenuId { get; set; }
    public string? MenuName { get; set; }
    public int SectionId { get; set; }
    public string? SectionName { get; set; }
    public string Titulo
    {
        get
        {
            var menu = string.IsNullOrWhiteSpace(MenuName) ? "Menú" : MenuName;
            var section = string.IsNullOrWhiteSpace(SectionName) ? "Ítems" : SectionName;
            return $"{menu} · {section}";
        }
    }

    public bool CanRead => Perms.MenusRead;
    public bool CanCreate => Perms.MenusUpdate;
    public bool CanUpdate => Perms.MenusUpdate;
    public bool CanDelete => Perms.MenusDelete;
    public bool ShowTrashFab => (CanUpdate || CanDelete) && _trash.Count > 0;

    readonly List<MenuItemVm> _all = new();
    readonly List<MenuItemVm> _trash = new();
    CancellationTokenSource? _filterCts;

    bool _silenceSwitch;
    readonly HashSet<int> _busyToggles = new();
    bool _isLoading;
    CancellationTokenSource? _loadCts;

    public SectionItemsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanUpdate));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(Titulo));

        if (!CanRead)
        {
            await DisplayAlert("Acceso restringido", "No tienes permiso para ver ítems.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        if (_isLoading) return;
        _isLoading = true;
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            await CargarTodoAsync(_loadCts.Token);
        }
        finally
        {
            _isLoading = false;
        }
    }

    protected override void OnDisappearing()
    {
        _loadCts?.Cancel();
        _filterCts?.Cancel();
        base.OnDisappearing();
    }

    async Task CargarTodoAsync(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            var activos = await _menusApi.GetSectionItemsAsync(SectionId, ct);
            var eliminados = await _menusApi.GetSectionItemsTrashAsync(SectionId, ct);

            _all.Clear();
            foreach (var dto in activos)
                _all.Add(MenuItemVm.From(dto));

            _trash.Clear();
            foreach (var dto in eliminados)
                _trash.Add(MenuItemVm.From(dto));

            ApplyFilter(SearchBox?.Text);
            OnPropertyChanged(nameof(ShowTrashFab));
        }
        catch (HttpRequestException ex)
        {
            await ErrorHandler.MostrarErrorUsuario(ex.Message);
        }
        catch (TaskCanceledException)
        {
            // cancel silencioso (navegación o timeout)
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Ítems – Cargar");
        }
    }

    void ApplyFilter(string? query)
    {
        // Debounce para no recalcular en cada tecla
        _filterCts?.Cancel();
        var cts = new CancellationTokenSource();
        _filterCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, cts.Token);
                var q = (query ?? string.Empty).Trim().ToLowerInvariant();

                IEnumerable<MenuItemVm> src = _all;
                if (!string.IsNullOrEmpty(q))
                {
                    src = src.Where(i =>
                        (i.Title ?? string.Empty).ToLowerInvariant().Contains(q) ||
                        i.TypeBadge.ToLowerInvariant().Contains(q) ||
                        i.ReferenceLabel.ToLowerInvariant().Contains(q));
                }

                var result = src
                    .OrderBy(i => i.position)
                    .ThenBy(i => i.Title, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ItemsCV.ItemsSource = result;
                });
            }
            catch (TaskCanceledException) { }
        }, cts.Token);
    }

    void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyFilter(e.NewTextValue);

    async void Refresh_Refreshing(object sender, EventArgs e)
    {
        try
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await CargarTodoAsync(_loadCts.Token);
        }
        finally
        {
            if (sender is RefreshView rv)
                rv.IsRefreshing = false;
        }
    }

    async void New_Clicked(object sender, EventArgs e)
    {
        if (!CanCreate)
        {
            await DisplayAlert("Acceso restringido", "No puedes crear ítems.", "OK");
            return;
        }

        var route = $"{nameof(MenuItemEditorPage)}?mode=create" +
                    $"&menuId={MenuId}" +
                    $"&menuName={Uri.EscapeDataString(MenuName ?? string.Empty)}" +
                    $"&sectionId={SectionId}" +
                    $"&sectionName={Uri.EscapeDataString(SectionName ?? string.Empty)}";
        await Shell.Current.GoToAsync(route);
    }

    async void Edit_Clicked(object sender, EventArgs e)
    {
        if (!CanUpdate)
        {
            await DisplayAlert("Acceso restringido", "No puedes editar ítems.", "OK");
            return;
        }

        if ((sender as BindableObject)?.BindingContext is not MenuItemVm vm) return;

        var route = $"{nameof(MenuItemEditorPage)}?mode=edit" +
            $"&itemId={vm.id}" +
            $"&menuId={MenuId}" +
            $"&menuName={Uri.EscapeDataString(MenuName ?? string.Empty)}" +
            $"&sectionId={SectionId}" +
            $"&sectionName={Uri.EscapeDataString(SectionName ?? string.Empty)}" +
            $"&refType={Uri.EscapeDataString(vm.refType)}" +
            $"&refId={vm.refId}" +
            $"&displayName={Uri.EscapeDataString(vm.displayName ?? string.Empty)}" +
            $"&displayPriceCents={(vm.displayPriceCents?.ToString() ?? string.Empty)}" +
            $"&position={vm.position}" +
            $"&isFeatured={vm.isFeatured}" +
            $"&isActive={vm.isActive}";

        await Shell.Current.GoToAsync(route);
    }

    async void Delete_Clicked(object sender, EventArgs e)
    {
        if (!CanDelete)
        {
            await DisplayAlert("Acceso restringido", "No puedes eliminar ítems.", "OK");
            return;
        }

        if ((sender as BindableObject)?.BindingContext is not MenuItemVm vm) return;

        var confirm = await DisplayAlert("Enviar a papelera", $"¿Enviar “{vm.Title}” a la papelera?", "Sí", "Cancelar");
        if (!confirm) return;

        await ExecuteDeleteAsync(vm.id, hard: false);
    }

    async Task ExecuteDeleteAsync(int id, bool hard)
    {
        try
        {
            if (hard)
                await _menusApi.DeleteMenuItemHardAsync(id);
            else
                await _menusApi.ArchiveMenuItemAsync(id);

            await CargarTodoAsync();
        }
        catch (HttpRequestException ex)
        {
            await ErrorHandler.MostrarErrorUsuario(ex.Message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, hard ? "Ítems – Eliminar definitivo" : "Ítems – Eliminar");
        }
    }

    async void ToggleActivo_Toggled(object sender, ToggledEventArgs e)
    {
        if (_silenceSwitch) return;
        if (sender is not Switch sw || sw.BindingContext is not MenuItemVm vm) return;

        if (!CanUpdate)
        {
            _silenceSwitch = true;
            sw.IsToggled = vm.isActive;
            _silenceSwitch = false;
            return;
        }

        if (_busyToggles.Contains(vm.id))
        {
            _silenceSwitch = true;
            sw.IsToggled = vm.isActive;
            _silenceSwitch = false;
            return;
        }

        var nuevo = e.Value;
        var anterior = vm.isActive;
        vm.isActive = nuevo;
        _busyToggles.Add(vm.id);

        try
        {
            await _menusApi.UpdateMenuItemAsync(vm.id, new MenusApi.MenuItemUpdateDto { isActive = nuevo });
        }
        catch (Exception ex)
        {
            _silenceSwitch = true;
            vm.isActive = anterior;
            sw.IsToggled = anterior;
            _silenceSwitch = false;
            await ErrorHandler.MostrarErrorUsuario(ex.Message);
        }
        finally
        {
            _busyToggles.Remove(vm.id);
        }
    }

    async void TrashFab_Clicked(object sender, EventArgs e)
    {
        if (_trash.Count == 0) return;

        var options = _trash
            .Select(i => $"{i.Title} • {i.TrashDisplay}")
            .ToArray();

        var choice = await DisplayActionSheet("Papelera", "Cerrar", null, options);
        if (string.IsNullOrWhiteSpace(choice) || choice == "Cerrar") return;

        var index = Array.IndexOf(options, choice);
        if (index < 0) return;
        var selected = _trash[index];

        var actions = new List<string>();
        if (CanUpdate) actions.Add("Restaurar");
        if (CanDelete) actions.Add("Eliminar definitivamente");

        if (actions.Count == 0)
        {
            await DisplayAlert("Papelera", "No tienes permisos para administrar la papelera.", "OK");
            return;
        }

        var action = await DisplayActionSheet(selected.Title ?? "Ítem", "Cancelar", null, actions.ToArray());
        if (string.IsNullOrWhiteSpace(action) || action == "Cancelar") return;

        if (action == "Restaurar")
        {
            await RestoreAsync(selected.id);
        }
        else if (action == "Eliminar definitivamente")
        {
            await ExecuteDeleteAsync(selected.id, hard: true);
        }
    }

    async Task RestoreAsync(int id)
    {
        try
        {
            await _menusApi.RestoreMenuItemAsync(id);
            await CargarTodoAsync();
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Ítems – Restaurar");
        }
    }

    public class MenuItemVm : INotifyPropertyChanged
    {
        public int id { get; set; }
        public int sectionId { get; set; }
        public string refType { get; set; } = string.Empty;
        public int refId { get; set; }
        public string? displayName { get; set; }
        public int? displayPriceCents { get; set; }
        public int position { get; set; }
        bool _isActive;
        public bool isActive
        {
            get => _isActive;
            set { if (_isActive == value) return; _isActive = value; OnPropertyChanged(); }
        }
        public bool isFeatured { get; set; }
        public DateTime? deletedAt { get; set; }

        public string Title => string.IsNullOrWhiteSpace(displayName) ? $"{refType} #{refId}" : displayName!;
        public string TypeBadge => refType switch
        {
            "PRODUCT" => "Producto",
            "VARIANT" => "Variante",
            "COMBO" => "Combo",
            "OPTION" => "Opción",
            _ => refType
        };
        public string PriceLabel => displayPriceCents.HasValue ? (displayPriceCents.Value / 100.0m).ToString("$0.00") : "Precio base";
        public bool HasCustomPrice => displayPriceCents.HasValue;
        public string ReferenceLabel => $"{refType} · ID {refId}";
        public string PositionLabel => $"Pos {position}";
        public bool IsFeatured => isFeatured;
        public bool IsInactiveBadgeVisible => !isActive;
        public string TrashDisplay => deletedAt?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "";

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public static MenuItemVm From(MenusApi.MenuItemDto dto) => new()
        {
            id = dto.id,
            sectionId = dto.sectionId,
            refType = dto.refType,
            refId = dto.refId,
            displayName = dto.displayName,
            displayPriceCents = dto.displayPriceCents,
            position = dto.position,
            isFeatured = dto.isFeatured,
            isActive = dto.isActive,
            deletedAt = dto.deletedAt
        };
    }
}
