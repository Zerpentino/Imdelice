using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;

namespace Imdeliceapp.Pages;

public partial class ChannelConfigPage : ContentPage
{
    readonly OrdersApi _ordersApi = new();
    readonly string[] _knownSources = { "POS", "UBER", "DIDI", "RAPPI" };

    bool _isLoading;
    string _statusMessage = string.Empty;

    public ObservableCollection<ChannelConfigItem> Configs { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public ChannelConfigPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!Perms.OrdersUpdate)
        {
            await DisplayAlert("Acceso restringido", "No tienes permiso para editar configuraciones de órdenes.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        await LoadConfigsAsync();
    }

    async Task LoadConfigsAsync()
    {
        if (IsLoading) return;

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            StatusMessage = "Sin conexión a Internet.";
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;

            var configs = await _ordersApi.ListChannelConfigsAsync();
            var map = configs.ToDictionary(c => c.source, c => c, StringComparer.OrdinalIgnoreCase);

            var sources = _knownSources
                .Union(map.Keys, StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var items = new List<ChannelConfigItem>();
            foreach (var source in sources)
            {
                if (!map.TryGetValue(source, out var dto))
                    dto = new ChannelConfigDTO { source = source, markupPct = 0 };

                items.Add(new ChannelConfigItem(dto));
            }

            Configs.Clear();
            foreach (var item in items)
                Configs.Add(item);

            if (Configs.Count == 0)
                StatusMessage = "No hay canales configurados.";
        }
        catch (HttpRequestException ex)
        {
            StatusMessage = "No se pudo cargar la configuración.";
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            StatusMessage = "Ocurrió un error al cargar la configuración.";
            await ErrorHandler.MostrarErrorTecnico(ex, "Canales – Cargar configuración");
        }
        finally
        {
            IsLoading = false;
        }
    }

    async void Refresh_Clicked(object sender, EventArgs e)
        => await LoadConfigsAsync();

    async void SaveConfig_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is not ChannelConfigItem item)
            return;

        if (!item.TryGetMarkup(out var markup, out var validationMessage))
        {
            await DisplayAlert("Markup", validationMessage ?? "Valor inválido.", "OK");
            return;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
            return;
        }

        try
        {
            item.IsSaving = true;
            var dto = new ChannelConfigUpdateDto { markupPct = markup };
            var saved = await _ordersApi.UpdateChannelConfigAsync(item.Source, dto);
            var savedMarkup = saved?.markupPct ?? markup;
            item.ApplySaved(savedMarkup, saved?.updatedAt);
            await DisplayAlert("Markup", $"Se guardó el porcentaje para {item.DisplayName}.", "OK");
        }
        catch (HttpRequestException ex)
        {
            var message = ErrorHandler.ObtenerMensajeHttp(new HttpResponseMessage(ex.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), ex.Message);
            await ErrorHandler.MostrarErrorUsuario(message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Canales – Guardar configuración");
        }
        finally
        {
            item.IsSaving = false;
        }
    }
}

public class ChannelConfigItem : INotifyPropertyChanged
{
    readonly int _minPct = 0;
    readonly int _maxPct = 300;

    int _originalMarkup;
    string _markupText;
    bool _isSaving;
    DateTime? _updatedAt;

    public ChannelConfigItem(ChannelConfigDTO dto)
    {
        Source = dto.source;
        DisplayName = BuildDisplayName(dto.source);
        Description = BuildDescription(dto.source);
        _originalMarkup = dto.markupPct;
        _markupText = dto.markupPct.ToString(CultureInfo.InvariantCulture);
        _updatedAt = dto.updatedAt;
    }

    public string Source { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public string MarkupText
    {
        get => _markupText;
        set
        {
            var normalized = (value ?? string.Empty).Trim();
            if (_markupText == normalized) return;
            _markupText = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(ValidationMessage));
            OnPropertyChanged(nameof(HasValidationMessage));
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            if (_isSaving == value) return;
            _isSaving = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(IsEntryEnabled));
        }
    }

    public bool IsEntryEnabled => !IsSaving;

    public string SaveButtonText => IsSaving ? "Guardando..." : "Guardar";

    public bool CanSave
    {
        get
        {
            if (IsSaving) return false;
            if (!TryGetMarkup(out var value, out _))
                return false;
            return value != _originalMarkup;
        }
    }

    public string? ValidationMessage
    {
        get
        {
            if (IsSaving) return null;
            if (TryGetMarkup(out _, out var message))
                return null;
            return message;
        }
    }

    public bool HasValidationMessage => !string.IsNullOrEmpty(ValidationMessage);

    public string UpdatedSummary => _updatedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Sin cambios";

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool TryGetMarkup(out int value, out string? message)
    {
        if (string.IsNullOrWhiteSpace(_markupText))
        {
            value = 0;
            message = null;
            return true;
        }

        if (!int.TryParse(_markupText, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            message = "Ingresa un número entero.";
            return false;
        }

        if (value < _minPct || value > _maxPct)
        {
            message = $"Ingresa un valor entre {_minPct} y {_maxPct}.";
            return false;
        }

        message = null;
        return true;
    }

    public void ApplySaved(int newValue, DateTime? updatedAt)
    {
        _originalMarkup = newValue;
        _markupText = newValue.ToString(CultureInfo.InvariantCulture);
        _updatedAt = updatedAt;
        OnPropertyChanged(nameof(MarkupText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(ValidationMessage));
        OnPropertyChanged(nameof(HasValidationMessage));
        OnPropertyChanged(nameof(UpdatedSummary));
        OnPropertyChanged(nameof(SaveButtonText));
    }

    static string BuildDisplayName(string source)
        => source?.ToUpperInvariant() switch
        {
            "POS" => "POS / Caja",
            "UBER" => "Uber Eats",
            "DIDI" => "DiDi Food",
            "RAPPI" => "Rappi",
            _ => string.IsNullOrWhiteSpace(source) ? "Canal" : source.ToUpperInvariant()
        };

    static string BuildDescription(string source)
        => source?.ToUpperInvariant() switch
        {
            "POS" => "Pedidos tomados en el punto de venta.",
            "UBER" => "Pedidos importados desde Uber Eats.",
            "DIDI" => "Pedidos importados desde DiDi Food.",
            "RAPPI" => "Pedidos importados desde Rappi.",
            _ => "Pedidos recibidos desde este canal."
        };

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
