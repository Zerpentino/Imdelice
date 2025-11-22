using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Maui.Views;

namespace Imdeliceapp.Popups;

public partial class InventoryMovementPopup : Popup
{
    public ObservableCollection<MovementTypeOption> MovementTypes { get; } = new()
    {
        new MovementTypeOption("PURCHASE", "Compra / Entrada"),
        new MovementTypeOption("SALE", "Venta / Salida"),
        new MovementTypeOption("ADJUSTMENT", "Ajuste"),
        new MovementTypeOption("WASTE", "Merma"),
        new MovementTypeOption("TRANSFER", "Transferencia"),
        new MovementTypeOption("SALE_RETURN", "Devolución")
    };

    MovementTypeOption? _selectedType;
    string _quantityText = string.Empty;
    string? _reason;
    string? _validationMessage;

    public InventoryMovementPopup()
    {
        InitializeComponent();
        BindingContext = this;
        SelectedType = MovementTypes.FirstOrDefault();
    }

    public MovementTypeOption? SelectedType
    {
        get => _selectedType;
        set
        {
            if (_selectedType == value) return;
            _selectedType = value;
            OnPropertyChanged();
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (_quantityText == value) return;
            _quantityText = value;
            OnPropertyChanged();
        }
    }

    public string? Reason
    {
        get => _reason;
        set
        {
            if (_reason == value) return;
            _reason = value;
            OnPropertyChanged();
        }
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        set
        {
            if (_validationMessage == value) return;
            _validationMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasValidationMessage));
        }
    }

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    void Cancel_Clicked(object sender, EventArgs e) => Close(null);

    void Save_Clicked(object sender, EventArgs e)
    {
        ValidationMessage = null;

        if (SelectedType == null)
        {
            ValidationMessage = "Selecciona el tipo de movimiento.";
            return;
        }

        if (!decimal.TryParse(QuantityText, out var quantity) || quantity == 0)
        {
            ValidationMessage = "Ingresa una cantidad diferente de cero.";
            return;
        }

        Close(new Result(SelectedType.Code, quantity, Reason?.Trim()));
    }

    public record MovementTypeOption(string Code, string Label)
    {
        public override string ToString() => Label;
    }

    public record Result(string Type, decimal Quantity, string? Reason);
}
