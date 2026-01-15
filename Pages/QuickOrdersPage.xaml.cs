using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Imdeliceapp.Generic;
using Imdeliceapp.Helpers;
using Imdeliceapp.Models;
using Imdeliceapp.Services;

namespace Imdeliceapp.Pages;

public partial class QuickOrdersPage : ContentPage
{
    readonly OrdersApi _ordersApi = new();
    readonly MenusApi _menusApi = new();

    bool _isBusy;
    string _totalText = string.Empty;
    ServiceTypeOption? _selectedServiceType;

    public ObservableCollection<QuickOrderItemVm> Items { get; } = new();
    public ObservableCollection<ServiceTypeOption> ServiceTypes { get; } = new();

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

    public string TotalText
    {
        get => _totalText;
        set
        {
            if (_totalText == value) return;
            _totalText = value;
            OnPropertyChanged();
        }
    }

    public ServiceTypeOption? SelectedServiceType
    {
        get => _selectedServiceType;
        set
        {
            if (_selectedServiceType == value) return;
            _selectedServiceType = value;
            OnPropertyChanged();
        }
    }

    public QuickOrdersPage()
    {
        InitializeComponent();
        BindingContext = this;

        ServiceTypes.Add(new ServiceTypeOption("Delivery", "DELIVERY"));
        ServiceTypes.Add(new ServiceTypeOption("En mesa", "DINE_IN"));
        SelectedServiceType = ServiceTypes.FirstOrDefault();
    }

    async void AddItem_Clicked(object sender, EventArgs e)
    {
        if (IsBusy) return;

        var product = await ProductPickerPage.PickAsync(Navigation, p => p.isActive);
        if (product == null) return;

        int? variantId = null;
        string? variantName = null;

        if (string.Equals(product.type, "VARIANTED", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                IsBusy = true;
                var detail = await _menusApi.GetProductAsync(product.id);
                var variants = detail?.variants?
                    .Where(v => v != null && (v.isActive ?? true))
                    .ToList();

                if (variants == null || variants.Count == 0)
                {
                    await DisplayAlert("Variantes", "Ese producto no tiene variantes activas.", "OK");
                    return;
                }

                var options = variants.Select(v => new VariantOption(v)).ToList();
                var choice = await DisplayActionSheet("Selecciona variante", "Cancelar", null, options.Select(o => o.Display).ToArray());
                if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar")
                    return;

                var selected = options.FirstOrDefault(o => o.Display == choice);
                if (selected == null)
                    return;

                variantId = selected.Id;
                variantName = selected.Name;
            }
            catch (Exception ex)
            {
                await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes rápidas - Variantes");
                return;
            }
            finally
            {
                IsBusy = false;
            }
        }

        var existing = Items.FirstOrDefault(i => i.ProductId == product.id && i.VariantId == variantId);
        if (existing != null)
        {
            existing.Quantity += 1;
            return;
        }

        Items.Add(new QuickOrderItemVm(product.id, variantId, product.name, variantName));
    }

    void Increase_Clicked(object sender, EventArgs e)
    {
        if (IsBusy) return;
        if (sender is Button { CommandParameter: QuickOrderItemVm vm })
            vm.Quantity += 1;
    }

    void Decrease_Clicked(object sender, EventArgs e)
    {
        if (IsBusy) return;
        if (sender is Button { CommandParameter: QuickOrderItemVm vm })
        {
            if (vm.Quantity > 1)
                vm.Quantity -= 1;
        }
    }

    void RemoveItem_Clicked(object sender, EventArgs e)
    {
        if (IsBusy) return;
        if (sender is Button { CommandParameter: QuickOrderItemVm vm })
            Items.Remove(vm);
    }

    async void Submit_Clicked(object sender, EventArgs e)
    {
        if (IsBusy) return;

        if (Items.Count == 0)
        {
            await DisplayAlert("Faltan productos", "Agrega al menos un producto.", "OK");
            return;
        }

        if (!TryParseTotalCents(TotalText, out var totalCents) || totalCents <= 0)
        {
            await DisplayAlert("Total inválido", "Escribe un total válido.", "OK");
            return;
        }

        var dto = new QuickOrderRequestDto
        {
            serviceType = SelectedServiceType?.Code ?? "DELIVERY",
            totalCents = totalCents,
            items = Items.Select(i => new QuickOrderItemDto
            {
                productId = i.ProductId,
                variantId = i.VariantId,
                quantity = i.Quantity
            }).ToList()
        };

        try
        {
            IsBusy = true;
            var order = await _ordersApi.CreateQuickOrderAsync(dto);
            var code = order?.code ?? "Orden registrada";
            await DisplayAlert("Listo", code, "OK");
            Items.Clear();
            TotalText = string.Empty;
            SelectedServiceType = ServiceTypes.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Órdenes rápidas");
        }
        finally
        {
            IsBusy = false;
        }
    }

    static bool TryParseTotalCents(string? text, out int cents)
    {
        cents = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.Replace("$", string.Empty).Trim();
        normalized = normalized.Replace(",", ".");
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return false;
        if (amount < 0)
            return false;

        cents = (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        return true;
    }

    class VariantOption
    {
        public VariantOption(MenusApi.ProductVariantDto variant)
        {
            Id = variant.id;
            Name = variant.name ?? $"Variante #{variant.id}";
            Display = variant.priceCents.HasValue
                ? $"{Name} · {(variant.priceCents.Value / 100m).ToString("C", CultureInfo.CurrentCulture)}"
                : Name;
        }

        public int Id { get; }
        public string Name { get; }
        public string Display { get; }
    }

    public class ServiceTypeOption
    {
        public ServiceTypeOption(string name, string code)
        {
            Name = name;
            Code = code;
        }

        public string Name { get; }
        public string Code { get; }
    }

    public class QuickOrderItemVm : BaseBinding
    {
        int _quantity = 1;

        public QuickOrderItemVm(int productId, int? variantId, string name, string? variantName)
        {
            ProductId = productId;
            VariantId = variantId;
            ProductName = name;
            VariantName = variantName;
        }

        public int ProductId { get; }
        public int? VariantId { get; }
        public string ProductName { get; }
        public string? VariantName { get; }

        public int Quantity
        {
            get => _quantity;
            set => SetValue(ref _quantity, value);
        }

        public string DisplayName => ProductName;
        public string VariantLabel => VariantName ?? string.Empty;
        public bool HasVariant => !string.IsNullOrWhiteSpace(VariantName);
    }
}
