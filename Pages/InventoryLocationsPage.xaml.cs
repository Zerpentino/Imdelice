using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Perms = Imdeliceapp.Services.Perms;

namespace Imdeliceapp.Pages;

public partial class InventoryLocationsPage : ContentPage
{
    readonly InventoryApi _inventoryApi = new();

    public ObservableCollection<InventoryLocationDTO> Locations { get; } = new();
    bool _isLoading;

    bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            if (_isRefreshing == value) return;
            _isRefreshing = value;
            OnPropertyChanged();
        }
    }

    string _emptyMessage = "No hay ubicaciones registradas.";
    public string EmptyMessage
    {
        get => _emptyMessage;
        set
        {
            if (_emptyMessage == value) return;
            _emptyMessage = value;
            OnPropertyChanged();
        }
    }

    public InventoryLocationsPage()
    {
        InitializeComponent();
        BindingContext = this;
        MessagingCenter.Subscribe<InventoryLocationEditorPage>(this, "LocationsChanged", async _ => await LoadAsync());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!Perms.InventoryRead)
        {
            await DisplayAlert("Acceso restringido", "No puedes ver ubicaciones de inventario.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        // Solo permitir agregar si tiene permiso de ajuste
        foreach (var tb in ToolbarItems)
            tb.IsEnabled = Perms.InventoryAdjust;

        await LoadAsync();
    }

    private async void RefreshView_Refreshing(object sender, EventArgs e)
    {
        await LoadAsync();
        RefreshView.IsRefreshing = false;
    }

    private async void AddLocation_Clicked(object sender, EventArgs e)
    {
        if (!Perms.InventoryAdjust)
        {
            await DisplayAlert("Acceso restringido", "No puedes crear ubicaciones.", "OK");
            return;
        }
        await Shell.Current.GoToAsync(nameof(InventoryLocationEditorPage));
    }

    private async void EditSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.CommandParameter is not InventoryLocationDTO loc) return;
        if (!Perms.InventoryAdjust)
        {
            await DisplayAlert("Acceso restringido", "No puedes editar ubicaciones.", "OK");
            return;
        }
        await Shell.Current.GoToAsync($"{nameof(InventoryLocationEditorPage)}?locationId={loc.id}");
    }

    private async void DeleteSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.CommandParameter is not InventoryLocationDTO loc) return;
        if (!Perms.InventoryAdjust)
        {
            await DisplayAlert("Acceso restringido", "No puedes eliminar ubicaciones.", "OK");
            return;
        }
        var confirm = await DisplayAlert("Eliminar ubicación",
            $"¿Deseas eliminar \"{loc.name}\"?", "Sí, eliminar", "Cancelar");
        if (!confirm) return;

        try
        {
            await _inventoryApi.DeleteLocationAsync(loc.id);
            await LoadAsync();
        }
        catch (HttpRequestException ex)
        {
            await ErrorHandler.MostrarErrorUsuario(ex.Message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Eliminar ubicación");
        }
    }

    private async void Location_Tapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not InventoryLocationDTO loc) return;
        await Shell.Current.GoToAsync($"{nameof(InventoryProductsPage)}?locationId={loc.id}");
    }

    async Task LoadAsync()
    {
        if (_isLoading)
        {
            IsRefreshing = false;
            RefreshView.IsRefreshing = false;
            return;
        }
        try
        {
            _isLoading = true;
            IsRefreshing = true;
            Locations.Clear();
            var list = await _inventoryApi.ListLocationsAsync();
            if (list.Count == 0)
            {
                EmptyMessage = "No hay ubicaciones configuradas.";
            }
            else
            {
                foreach (var loc in list.OrderBy(l => l.name))
                    Locations.Add(loc);
                EmptyMessage = string.Empty;
            }
        }
        catch (HttpRequestException ex)
        {
            EmptyMessage = ex.Message;
        }
        catch (Exception ex)
        {
            EmptyMessage = "No pudimos cargar las ubicaciones.";
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Ubicaciones");
            RefreshView.IsRefreshing = false;
        }
        finally
        {
            IsRefreshing = false;
            RefreshView.IsRefreshing = false;
            _isLoading = false;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // nada extra, dejamos el suscriptor vivo mientras el page esté activo
    }
}
