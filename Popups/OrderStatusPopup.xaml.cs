using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;

namespace Imdeliceapp.Popups;

public partial class OrderStatusPopup : Popup
{
    readonly ObservableCollection<StatusOption> _options = new();
    readonly string _currentStatus;

    public OrderStatusPopup(string currentStatus, IReadOnlyList<StatusOption> allowedStatuses)
    {
        InitializeComponent();
        _currentStatus = currentStatus ?? string.Empty;
        CurrentStatusLabel.Text = $"Estado actual: {StatusOption.GetDisplayName(_currentStatus)}";

        foreach (var option in allowedStatuses.OrderBy(o => o.Display))
            _options.Add(option);

        StatusPicker.ItemsSource = _options;
        if (_options.Count > 0)
            StatusPicker.SelectedItem = _options[0];
    }

    void Cancel_Clicked(object sender, EventArgs e)
    {
        Close();
    }

    void Save_Clicked(object sender, EventArgs e)
    {
        ValidationLabel.IsVisible = false;
        ValidationLabel.Text = string.Empty;

        if (StatusPicker.SelectedItem is not StatusOption selected)
        {
            ShowError("Selecciona un nuevo estado.");
            return;
        }

        var dto = new UpdateOrderStatusDto
        {
            status = selected.Code,
            reason = string.IsNullOrWhiteSpace(ReasonEditor.Text) ? null : ReasonEditor.Text.Trim()
        };

        Close(dto);
    }

    void ShowError(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.IsVisible = true;
    }

    public record StatusOption(string Code, string Display)
    {
        public static string GetDisplayName(string? status)
        {
            return status?.ToUpperInvariant() switch
            {
                "OPEN" => "Abierta",
                "HOLD" => "En espera",
                "CLOSED" => "Cerrada",
                "CANCELED" => "Cancelada",
                "READY" => "Lista",
                "SERVED" => "Servida",
                "ACCEPTED" => "Aceptada",
                "DRAFT" => "Borrador",
                _ => status ?? "Desconocido"
            };
        }
    }
}
