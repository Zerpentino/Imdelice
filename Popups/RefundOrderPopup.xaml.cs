using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;

namespace Imdeliceapp.Popups;

public partial class RefundOrderPopup : Popup, INotifyPropertyChanged
{
    string? _reason;
    string? _adminEmail;
    string? _adminPin;
    string? _password;
    string? _validationMessage;

    public RefundOrderPopup()
    {
        InitializeComponent();
        BindingContext = this;
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

    public string? AdminEmail
    {
        get => _adminEmail;
        set
        {
            if (_adminEmail == value) return;
            _adminEmail = value;
            OnPropertyChanged();
        }
    }

    public string? AdminPin
    {
        get => _adminPin;
        set
        {
            if (_adminPin == value) return;
            _adminPin = value;
            OnPropertyChanged();
        }
    }

    public string? Password
    {
        get => _password;
        set
        {
            if (_password == value) return;
            _password = value;
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

    void Confirm_Clicked(object sender, EventArgs e)
    {
        ValidationMessage = null;

        var trimmedEmail = string.IsNullOrWhiteSpace(AdminEmail) ? null : AdminEmail.Trim();
        var trimmedPin = string.IsNullOrWhiteSpace(AdminPin) ? null : AdminPin.Trim();
        var trimmedPassword = string.IsNullOrWhiteSpace(Password) ? null : Password;

        if (string.IsNullOrWhiteSpace(trimmedPassword))
        {
            ValidationMessage = "La contraseña del supervisor es obligatoria.";
            return;
        }

        if (string.IsNullOrWhiteSpace(trimmedEmail) && string.IsNullOrWhiteSpace(trimmedPin))
        {
            ValidationMessage = "Captura el correo o PIN del supervisor.";
            return;
        }

        var request = new RefundOrderRequest
        {
            reason = string.IsNullOrWhiteSpace(Reason) ? null : Reason.Trim(),
            adminEmail = trimmedEmail,
            adminPin = trimmedPin,
            password = trimmedPassword
        };

        Close(request);
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
