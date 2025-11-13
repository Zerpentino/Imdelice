using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;

namespace Imdeliceapp.Popups;

public partial class OrderItemStatusPopup : Popup
{
    readonly ViewModel _viewModel;

    public OrderItemStatusPopup(string currentStatus)
    {
        InitializeComponent();
        _viewModel = new ViewModel(currentStatus);
        BindingContext = _viewModel;
    }

    void Cancel_Clicked(object sender, EventArgs e) => Close(null);

    void Save_Clicked(object sender, EventArgs e)
    {
        if (!_viewModel.TryBuildPayload(out var dto, out var message))
        {
            _viewModel.ValidationMessage = message;
            return;
        }

        _viewModel.ValidationMessage = null;
        Close(dto);
    }

    class ViewModel : INotifyPropertyChanged
    {
        readonly string _currentStatus;

        public ViewModel(string currentStatus)
        {
            _currentStatus = string.IsNullOrWhiteSpace(currentStatus) ? "NEW" : currentStatus.ToUpperInvariant();
            StatusOptions = new ObservableCollection<StatusOption>(BuildOptions());
            SelectedStatus = StatusOptions.FirstOrDefault(o => !o.Code.Equals(_currentStatus, StringComparison.OrdinalIgnoreCase))
                ?? StatusOptions.FirstOrDefault();
        }

        public ObservableCollection<StatusOption> StatusOptions { get; }

        StatusOption? _selectedStatus;
        public StatusOption? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus == value) return;
                _selectedStatus = value;
                OnPropertyChanged();
            }
        }

        string _reason = string.Empty;
        public string Reason
        {
            get => _reason;
            set
            {
                if (_reason == value) return;
                _reason = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string CurrentStatusDisplay => $"Estado actual: {StatusOption.GetDisplayName(_currentStatus)}";

        string? _validationMessage;
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

        IReadOnlyList<StatusOption> BuildOptions()
        {
            var codes = new[] { "NEW", "IN_PROGRESS", "READY", "SERVED", "CANCELED" };
            return codes.Select(code => new StatusOption(code, StatusOption.GetDisplayName(code))).ToList();
        }

        public bool TryBuildPayload(out UpdateOrderItemStatusDto? dto, out string? message)
        {
            dto = null;
            message = null;

            if (SelectedStatus == null)
            {
                message = "Selecciona un estado.";
                return false;
            }

            if (SelectedStatus.Code.Equals(_currentStatus, StringComparison.OrdinalIgnoreCase))
            {
                message = "Selecciona un estado diferente para aplicar un cambio.";
                return false;
            }

            dto = new UpdateOrderItemStatusDto
            {
                status = SelectedStatus.Code,
                reason = string.IsNullOrWhiteSpace(Reason) ? null : Reason.Trim()
            };
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public record StatusOption(string Code, string Display)
    {
        public static string GetDisplayName(string code) => code?.ToUpperInvariant() switch
        {
            "NEW" => "Nuevo",
            "IN_PROGRESS" => "Preparando",
            "READY" => "Listo",
            "SERVED" => "Servido",
            "CANCELED" => "Cancelado",
            _ => code ?? string.Empty
        };
    }
}
