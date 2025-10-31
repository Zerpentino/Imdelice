using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Imdeliceapp.Models;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Text;
using Microsoft.Maui.ApplicationModel;
using System.Net.Http;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(ProductId), "productId")]
public partial class VariantModifierOverridesPage : ContentPage
{
    readonly ModifiersApi _modifiersApi = new();
    readonly MenusApi _menusApi = new();

    public int ProductId { get; set; }

    public ObservableCollection<VariantOptionVm> Variants { get; } = new();
    public ObservableCollection<VariantOverrideItem> Overrides { get; } = new();

    VariantOptionVm? _selectedVariant;
    bool _loadedInitial;
    bool _isVariantLoading;
    bool _isRefreshing;
    string _productName = string.Empty;
    List<ProductGroupLinkDTO> _productGroups = new();

    public VariantModifierOverridesPage()
    {
        InitializeComponent();
        BindingContext = this;
        if (AddOverrideToolbar != null)
            AddOverrideToolbar.IsEnabled = false;
    }

    public VariantOptionVm? SelectedVariant
    {
        get => _selectedVariant;
        set
        {
            if (_selectedVariant == value) return;
            _selectedVariant = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasVariantSelected));
            OnPropertyChanged(nameof(NoVariantSelectedMessageVisible));
            OnPropertyChanged(nameof(OverridesVisible));
            UpdateToolbarState();

            if (value != null)
                _ = LoadVariantOverridesAsync(value.Id);
            else
                Overrides.Clear();
        }
    }

    public bool HasVariantSelected => SelectedVariant != null;
    public bool OverridesVisible => HasVariantSelected && !IsVariantLoading;
    public bool NoVariantSelectedMessageVisible => !HasVariantSelected && !IsVariantLoading && Variants.Count > 0;
    public bool NoVariantsAvailableVisible => !IsVariantLoading && Variants.Count == 0;

    public bool IsVariantLoading
    {
        get => _isVariantLoading;
        private set
        {
            if (_isVariantLoading == value) return;
            _isVariantLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OverridesVisible));
            OnPropertyChanged(nameof(NoVariantSelectedMessageVisible));
            OnPropertyChanged(nameof(NoVariantsAvailableVisible));
            UpdateToolbarState();
        }
    }

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

    public string ProductHeadline => string.IsNullOrWhiteSpace(_productName)
        ? $"Producto #{ProductId}"
        : _productName;

    public string VariantHint => Variants.Count == 0
        ? "Este producto no tiene variantes configuradas."
        : "Selecciona una variante y ajusta la cantidad mínima/máxima de modificadores.";

    public string FooterNote => "Los overrides solo aplican a la variante seleccionada. Las demás variantes usarán los valores definidos a nivel producto.";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loadedInitial)
        {
            UpdateToolbarState();
            return;
        }

        _loadedInitial = true;
        await LoadInitialAsync();
    }

    async Task LoadInitialAsync()
    {
        if (ProductId <= 0)
        {
            await DisplayAlert("Producto", "Falta el identificador del producto.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        try
        {
            IsVariantLoading = true;
            var productTask = _menusApi.GetProductAsync(ProductId);
            var groupsTask = _modifiersApi.GetGroupsByProductAsync(ProductId);

            var product = await productTask;
            _productGroups = await groupsTask ?? new List<ProductGroupLinkDTO>();

            _productName = product?.name ?? string.Empty;
            OnPropertyChanged(nameof(ProductHeadline));

            Variants.Clear();
            if (product?.variants != null && product.variants.Count > 0)
            {
                foreach (var variant in product.variants
                             .OrderBy(v => (v.name ?? string.Empty), StringComparer.CurrentCultureIgnoreCase))
                {
                    Variants.Add(new VariantOptionVm(variant));
                }

                SelectedVariant = Variants.FirstOrDefault();
            }
            else
            {
                SelectedVariant = null;
            }

            OnPropertyChanged(nameof(VariantHint));
            OnPropertyChanged(nameof(NoVariantsAvailableVisible));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsVariantLoading = false;
        }
    }

    async Task LoadVariantOverridesAsync(int variantId, bool silent = false)
    {
        if (!silent)
            IsVariantLoading = true;

        try
        {
            var overrides = await _modifiersApi.GetVariantModifierGroupsAsync(variantId) ?? new List<VariantModifierGroupLinkDTO>();
            Overrides.Clear();

            foreach (var link in overrides
                         .OrderBy(o => _productGroups.FirstOrDefault(pg => pg.group?.id == o.groupId)?.position ?? int.MaxValue)
                         .ThenBy(o => o.group?.name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase))
            {
                if (link.inheritsFromProduct == true)
                    continue;

                var defaultGroup = _productGroups.FirstOrDefault(pg => pg.group?.id == link.groupId)?.group ?? link.group;
                Overrides.Add(new VariantOverrideItem(link, defaultGroup));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            if (!silent)
                IsVariantLoading = false;
            IsRefreshing = false;
        }
    }

    void UpdateToolbarState()
    {
        if (AddOverrideToolbar != null)
            AddOverrideToolbar.IsEnabled = HasVariantSelected && !IsVariantLoading;
    }
#if DEBUG
async Task ShowAndCopyApiErrorAsync(Exception ex, string whereTag)
{
    var detail = await BuildApiErrorDetailAsync(ex);

    // Copia al portapapeles (silencioso si falla)
    try { await Clipboard.SetTextAsync(detail); } catch { /* ignore */ }

    // Log
    System.Diagnostics.Debug.WriteLine($"[API ERROR] {whereTag}\n{detail}");

    // Alerta resumida (el detalle completo ya quedó copiado)
    var shortMsg = detail.Length > 500 ? detail[..500] + "…" : detail;
    await DisplayAlert("Error API (copiado)", shortMsg, "OK");
}
#else
// En Release mantenemos el comportamiento simple
Task ShowAndCopyApiErrorAsync(Exception ex, string _)
    => DisplayAlert("Error", ex.Message, "OK");
#endif
async Task<string> BuildApiErrorDetailAsync(Exception ex)
{
    var sb = new StringBuilder();

    void AppendBasic(Exception e)
    {
        sb.AppendLine(e.GetType().FullName);
        sb.AppendLine(e.Message);
        foreach (System.Collections.DictionaryEntry kv in e.Data)
            sb.AppendLine($"Data[{kv.Key}]={kv.Value}");
    }

    AppendBasic(ex);

    try
    {
        var t = ex.GetType();

        // Refit.ApiException
        if (t.FullName == "Refit.ApiException")
        {
            var status = t.GetProperty("StatusCode")?.GetValue(ex)?.ToString();
            var uri    = t.GetProperty("Uri")?.GetValue(ex)?.ToString();
            var content= t.GetProperty("Content")?.GetValue(ex)?.ToString();

            if (!string.IsNullOrWhiteSpace(status)) sb.AppendLine($"Status: {status}");
            if (!string.IsNullOrWhiteSpace(uri))    sb.AppendLine($"URL: {uri}");
            if (!string.IsNullOrWhiteSpace(content))
            {
                sb.AppendLine("Body:");
                sb.AppendLine(content);
            }
        }

        // Flurl.Http.FlurlHttpException
        if (t.FullName == "Flurl.Http.FlurlHttpException")
        {
            var call = t.GetProperty("Call")?.GetValue(ex);
            var resp = call?.GetType().GetProperty("Response")?.GetValue(call);
            var code = resp?.GetType().GetProperty("StatusCode")?.GetValue(resp)?.ToString();
            if (!string.IsNullOrWhiteSpace(code)) sb.AppendLine($"Status: {code}");

            var getRespStr = t.GetMethod("GetResponseStringAsync");
            if (getRespStr != null)
            {
                var task = (Task<string>)getRespStr.Invoke(ex, null);
                var body = await task;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    sb.AppendLine("Body:");
                    sb.AppendLine(body);
                }
            }
        }
    }
    catch { /* no romper el flujo si la reflexión falla */ }

    // Inner exceptions
    var inner = ex.InnerException;
    while (inner != null)
    {
        sb.AppendLine("--- Inner ---");
        AppendBasic(inner);
        inner = inner.InnerException;
    }

    return sb.ToString();
}

    void OverridesRefresh_Refreshing(object? sender, EventArgs e)
    {
        if (SelectedVariant == null)
        {
            IsRefreshing = false;
            return;
        }

        IsRefreshing = true;
        _ = LoadVariantOverridesAsync(SelectedVariant.Id, silent: true);
    }

    async void AddOverride_Clicked(object? sender, EventArgs e)
    {
        if (SelectedVariant == null)
        {
            await DisplayAlert("Variante", "Primero selecciona una variante.", "OK");
            return;
        }

        var available = _productGroups
            .Where(pg => pg.group != null && Overrides.All(o => o.GroupId != pg.group!.id))
            .OrderBy(pg => pg.position)
            .ThenBy(pg => pg.group!.name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (available.Count == 0)
        {
            await DisplayAlert("Sin grupos", "No quedan grupos disponibles para personalizar en esta variante.", "OK");
            return;
        }

        var options = available
            .Select(pg =>
            {
                var g = pg.group!;
                var maxText = g.maxSelect.HasValue ? g.maxSelect.Value.ToString() : "∞";
                return $"{g.name} · Min {g.minSelect} · Max {maxText}";
            })
            .ToArray();

        var choice = await DisplayActionSheet("Selecciona el grupo a personalizar", "Cancelar", null, options);
        if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar")
            return;

        var index = Array.IndexOf(options, choice);
        if (index < 0)
            return;

        var selectedLink = available[index];
        var group = selectedLink.group!;

        try
        {
            var dto = new ModifiersApi.AttachVariantModifierGroupDto
            {
                groupId = group.id,
                minSelect = Math.Max(0, group.minSelect),
                maxSelect = group.maxSelect,
                isRequired = group.isRequired
            };

            await _modifiersApi.AttachGroupToVariantAsync(SelectedVariant.Id, dto);
            await LoadVariantOverridesAsync(SelectedVariant.Id);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    async void OverrideSave_Clicked(object? sender, EventArgs e)
    {
        if (SelectedVariant == null)
            return;
        if ((sender as Button)?.CommandParameter is not VariantOverrideItem item)
            return;

        if (!TryParseMin(item.MinSelectText, out var min, out var minError))
        {
            item.SetStatus(minError ?? "Valor mínimo inválido.", isError: true);
            return;
        }

        if (!TryParseMax(item.MaxSelectText, out var max, out var maxError))
        {
            item.SetStatus(maxError ?? "Valor máximo inválido.", isError: true);
            return;
        }

        if (max.HasValue && max.Value < min)
        {
            item.SetStatus("El máximo debe ser mayor o igual al mínimo.", isError: true);
            return;
        }

        if (item.GroupId == 0)
        {
#if DEBUG
            var warn = "Override sin groupId (0). No se enviará la petición.";
            System.Diagnostics.Debug.WriteLine($"[API WARN] {warn}");
            await DisplayAlert("OverrideDelete Debug", warn, "OK");
#endif
            return;
        }

        item.IsBusy = true;
        item.SetStatus(null, isError: false);

        try
        {
            var dto = new ModifiersApi.UpdateVariantModifierGroupDto
            {
                minSelect = min,
                maxSelect = max,
                isRequired = item.IsRequired
            };

            await _modifiersApi.UpdateVariantModifierGroupAsync(SelectedVariant.Id, item.GroupId, dto);
            item.MinSelectText = min.ToString();
            item.MaxSelectText = max.HasValue ? max.Value.ToString() : string.Empty;
            item.SetStatus("Cambios guardados.", isError: false);
        }
        catch (HttpRequestException ex) when (IsRecordMissing(ex))
        {
            try
            {
                var createDto = new ModifiersApi.AttachVariantModifierGroupDto
                {
                    groupId = item.GroupId,
                    minSelect = min,
                    maxSelect = max,
                    isRequired = item.IsRequired
                };

                await _modifiersApi.AttachGroupToVariantAsync(SelectedVariant.Id, createDto);
                await LoadVariantOverridesAsync(SelectedVariant.Id);
                item.SetStatus("Override creado con los nuevos valores.", isError: false);
            }
            catch (Exception inner)
            {
                #if DEBUG
                var detail = await BuildApiErrorDetailAsync(inner);
                try { await Clipboard.SetTextAsync(detail); } catch { }
                System.Diagnostics.Debug.WriteLine($"[API ERROR] OverrideSave-Recreate\n{detail}");
                #endif
                item.SetStatus(inner.Message, isError: true);
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            var detail = await BuildApiErrorDetailAsync(ex);
            try { await Clipboard.SetTextAsync(detail); } catch { }
            System.Diagnostics.Debug.WriteLine($"[API ERROR] OverrideSave\n{detail}");
#endif
            item.SetStatus(ex.Message, isError: true);
        }
        finally
        {
            item.IsBusy = false;
        }
    }

    async void OverrideDelete_Clicked(object? sender, EventArgs e)
    {
        if (SelectedVariant == null)
            return;
        if ((sender as Button)?.CommandParameter is not VariantOverrideItem item)
            return;

        var ok = await DisplayAlert("Quitar override", $"¿Eliminar la personalización para “{item.GroupName}” en esta variante?", "Quitar", "Cancelar");
        if (!ok)
            return;

        item.IsBusy = true;
        item.SetStatus(null, isError: false);

        try
        {
#if DEBUG
            var debugInfo = $"DELETE /api/products/variants/{SelectedVariant.Id}/modifier-groups/{item.GroupId}";
            System.Diagnostics.Debug.WriteLine($"[API CALL] {debugInfo}");
            try { await Clipboard.SetTextAsync(debugInfo); } catch { }
            await DisplayAlert("OverrideDelete Debug", debugInfo, "OK");
#endif
            await _modifiersApi.DeleteVariantModifierGroupAsync(SelectedVariant.Id, item.GroupId);
            await LoadVariantOverridesAsync(SelectedVariant.Id);
        }
        catch (HttpRequestException ex) when (IsRecordMissing(ex))
        {
            item.SetStatus("El override ya no existía en el servidor.", isError: false);
            await LoadVariantOverridesAsync(SelectedVariant.Id, silent: true);
        }
        catch (Exception ex)
        {
            item.SetStatus(ex.Message, isError: true);
#if DEBUG
            var detail = await BuildApiErrorDetailAsync(ex);
            try { await Clipboard.SetTextAsync(detail); } catch { }
            System.Diagnostics.Debug.WriteLine($"[API ERROR] OverrideDelete\n{detail}");
            var preview = detail.Length > 400 ? detail[..400] + "…" : detail;
            await DisplayAlert("OverrideDelete Debug", preview, "OK");
#endif
        }
        finally
        {
            item.IsBusy = false;
        }
    }

    static bool TryParseMin(string? text, out int value, out string? error)
    {
        error = null;
        if (int.TryParse(text?.Trim(), out var parsed) && parsed >= 0)
        {
            value = parsed;
            return true;
        }

        value = 0;
        error = "Ingresa un mínimo válido (entero ≥ 0).";
        return false;
    }

    static bool TryParseMax(string? text, out int? value, out string? error)
    {
        error = null;
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            value = null;
            return true;
        }

        if (int.TryParse(trimmed, out var parsed) && parsed >= 0)
        {
            value = parsed;
            return true;
        }

        value = null;
        error = "Ingresa un máximo válido (entero ≥ 0) o deja el campo vacío.";
        return false;
    }

    static bool IsRecordMissing(HttpRequestException ex)
    {
        var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
        return message.Contains("p2025") || message.Contains("no record was found");
    }
}

public class VariantOptionVm
{
    public VariantOptionVm(MenusApi.ProductVariantDto dto)
    {
        Id = dto.id;
        Name = string.IsNullOrWhiteSpace(dto.name) ? $"Variante #{dto.id}" : dto.name!;
        PriceCents = dto.priceCents;
        DisplayName = dto.priceCents.HasValue
            ? $"{Name} · {(dto.priceCents.Value / 100.0m):C}"
            : Name;
    }

    public int Id { get; }
    public string Name { get; }
    public int? PriceCents { get; }
    public string DisplayName { get; }
}

public class VariantOverrideItem : BindableObject
{
    string _minSelectText;
    string _maxSelectText;
    bool _isRequired;
    bool _isBusy;
    string? _statusMessage;
    Color _statusColor = Colors.Transparent;

    public VariantOverrideItem(VariantModifierGroupLinkDTO link, ModifierGroupDTO? defaultGroup)
    {
        GroupId = link.groupId != 0
            ? link.groupId
            : (link.group?.id ?? defaultGroup?.id ?? 0);
        GroupName = defaultGroup?.name ?? link.group?.name ?? $"Grupo #{link.groupId}";
        DefaultSummary = BuildDefaultSummary(defaultGroup);

        _minSelectText = Math.Max(0, link.minSelect).ToString();
        _maxSelectText = link.maxSelect.HasValue ? Math.Max(0, link.maxSelect.Value).ToString() : string.Empty;
        _isRequired = link.isRequired;
    }

    static string BuildDefaultSummary(ModifierGroupDTO? group)
    {
        if (group == null)
            return "Se aplican los valores definidos a nivel producto.";

        var maxText = group.maxSelect.HasValue ? group.maxSelect.Value.ToString() : "sin límite";
        var requirement = group.isRequired ? "Obligatorio" : "Opcional";
        return $"Reglas del producto · Min {group.minSelect} · Max {maxText} · {requirement}";
    }

    public int GroupId { get; }
    public string GroupName { get; }
    public string DefaultSummary { get; }

    public string MinSelectText
    {
        get => _minSelectText;
        set
        {
            if (_minSelectText == value) return;
            _minSelectText = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string MaxSelectText
    {
        get => _maxSelectText;
        set
        {
            if (_maxSelectText == value) return;
            _maxSelectText = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public bool IsRequired
    {
        get => _isRequired;
        set
        {
            if (_isRequired == value) return;
            _isRequired = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public Color StatusColor
    {
        get => _statusColor;
        private set
        {
            if (_statusColor == value) return;
            _statusColor = value;
            OnPropertyChanged();
        }
    }

    public void SetStatus(string? message, bool isError)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            StatusColor = Colors.Transparent;
            StatusMessage = null;
            return;
        }

        StatusColor = isError ? Colors.IndianRed : Colors.Teal;
        StatusMessage = message;
    }
}
