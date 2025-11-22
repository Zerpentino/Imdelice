using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Perms = Imdeliceapp.Services.Perms;

namespace Imdeliceapp.Pages
{
    [QueryProperty(nameof(LocationId), "locationId")]
    public partial class InventoryLocationEditorPage : ContentPage
    {
    readonly InventoryApi _inventoryApi = new();
    int? _locationId;
    InventoryLocationDTO? _current;
    readonly TypeOption[] _typeOptions = new[]
    {
        new TypeOption("GENERAL", "General"),
        new TypeOption("KITCHEN", "Cocina"),
        new TypeOption("BAR", "Barra"),
        new TypeOption("STORAGE", "Almacén"),
    };

    public InventoryLocationEditorPage()
    {
        InitializeComponent();
        TypePicker.ItemsSource = _typeOptions;
    }

    public string LocationId
    {
        set
        {
            if (int.TryParse(value, out var id))
                _locationId = id;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!Perms.InventoryAdjust)
        {
            await DisplayAlert("Acceso restringido", "No puedes editar ubicaciones.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        if (!_locationId.HasValue) return;
        try
        {
            var all = await _inventoryApi.ListLocationsAsync();
            _current = all.FirstOrDefault(l => l.id == _locationId);
            if (_current == null) return;

            NameEntry.Text = _current.name;
            CodeEntry.Text = _current.code;
            var currentType = string.IsNullOrWhiteSpace(_current.type) ? "GENERAL" : _current.type;
            TypePicker.SelectedItem = _typeOptions.FirstOrDefault(t => t.Value.Equals(currentType, StringComparison.OrdinalIgnoreCase));
            DefaultSwitch.IsToggled = _current.isDefault;
            ActiveSwitch.IsToggled = _current.isActive;
        }
        catch (HttpRequestException ex)
        {
            await ErrorHandler.MostrarErrorUsuario(ex.Message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Ubicación");
        }
    }

    async void Save_Clicked(object sender, EventArgs e)
    {
        if (!Perms.InventoryAdjust)
        {
            await DisplayAlert("Acceso restringido", "No puedes guardar ubicaciones.", "OK");
            return;
        }

        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Ubicación", "El nombre es obligatorio.", "OK");
            return;
        }

        var code = CodeEntry.Text?.Trim();
        var type = (TypePicker.SelectedItem as TypeOption)?.Value ?? "GENERAL";
        var isDefault = DefaultSwitch.IsToggled;
        var isActive = ActiveSwitch.IsToggled;

        try
        {
            if (_locationId.HasValue)
            {
                await _inventoryApi.UpdateLocationAsync(_locationId.Value, new
                {
                    name = name,
                    code = string.IsNullOrWhiteSpace(code) ? null : code,
                    type = type,
                    isDefault = isDefault,
                    isActive = isActive
                }, allowNulls: true);
            }
            else
            {
                await _inventoryApi.CreateLocationAsync(new InventoryLocationCreateRequest
                {
                    name = name,
                    code = string.IsNullOrWhiteSpace(code) ? null : code,
                    type = type,
                    isDefault = isDefault
                });
            }

            await DisplayAlert("Ubicación", "Guardado.", "OK");
            MessagingCenter.Send(this, "LocationsChanged");
            await Shell.Current.GoToAsync("..", true);
        }
        catch (HttpRequestException ex)
        {
            await ErrorHandler.MostrarErrorUsuario(ex.Message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Inventario – Guardar ubicación");
        }
    }
}

public record TypeOption(string Value, string Label);
}
