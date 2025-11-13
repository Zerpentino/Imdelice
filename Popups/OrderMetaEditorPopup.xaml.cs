using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;
using Imdeliceapp.Services;

namespace Imdeliceapp.Popups;

public partial class OrderMetaEditorPopup : Popup
{
    readonly OrdersApi _ordersApi = new();
    readonly ObservableCollection<TableOptionVm> _tableOptions = new();
    readonly int? _initialTableId;
    readonly bool _allowTableSelection;

    public OrderMetaEditorPopup(int? tableId, int? covers, string? note, int? prepEtaMinutes, bool allowTableSelection)
    {
        InitializeComponent();
        _initialTableId = tableId;
        _allowTableSelection = allowTableSelection;

        TableSection.IsVisible = _allowTableSelection;
        if (_allowTableSelection)
            TablePicker.ItemsSource = _tableOptions;

        if (covers.HasValue)
            CoversEntry.Text = covers.Value.ToString();
        if (prepEtaMinutes.HasValue)
            PrepEtaEntry.Text = prepEtaMinutes.Value.ToString();
        if (!string.IsNullOrWhiteSpace(note))
            NoteEditor.Text = note;
        if (_allowTableSelection)
            _ = LoadTablesAsync();
    }

    async Task LoadTablesAsync()
    {
        try
        {
            var tables = await _ordersApi.ListTablesAsync(includeInactive: true);
            _tableOptions.Clear();
            foreach (var dto in tables
                     .OrderByDescending(t => t.isActive)
                     .ThenBy(t => t.name ?? $"Mesa {t.id}"))
            {
                _tableOptions.Add(new TableOptionVm(dto));
            }

            if (_initialTableId.HasValue)
            {
                TablePicker.SelectedItem = _tableOptions.FirstOrDefault(t => t.Id == _initialTableId.Value);
            }
        }
        catch
        {
            // Silenciar errores; el popup seguirá permitiendo guardar sin mesa.
        }
    }

    void Cancel_Clicked(object sender, EventArgs e)
    {
        Close();
    }

    void Save_Clicked(object sender, EventArgs e)
    {
        ValidationLabel.IsVisible = false;
        ValidationLabel.Text = string.Empty;

        var tableId = _allowTableSelection
            ? (TablePicker.SelectedItem as TableOptionVm)?.Id
            : _initialTableId;
        if (!TryParseNullableInt(CoversEntry.Text, out var covers))
        {
            ShowError("Los comensales deben ser un número válido.");
            return;
        }

        if (!TryParseNullableInt(PrepEtaEntry.Text, out var prepEta))
        {
            ShowError("El tiempo estimado debe ser un número válido.");
            return;
        }

        var dto = new UpdateOrderMetaDto
        {
            tableId = tableId,
            covers = covers,
            prepEtaMinutes = prepEta,
            note = string.IsNullOrWhiteSpace(NoteEditor.Text) ? null : NoteEditor.Text.Trim()
        };

        Close(dto);
    }

    static bool TryParseNullableInt(string? text, out int? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        if (int.TryParse(text.Trim(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    void ShowError(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.IsVisible = true;
    }

    class TableOptionVm
    {
        public TableOptionVm(TableDTO dto)
        {
            Id = dto.id;
            Name = dto.name;
            Seats = dto.seats;
            IsActive = dto.isActive;
            DisplayName = string.IsNullOrWhiteSpace(Name)
                ? $"Mesa {dto.id}"
                : Name;
            if (Seats > 0)
                DisplayName += $" · {Seats} lugares";
            if (!IsActive)
                DisplayName += " (inactiva)";
        }

        public int Id { get; }
        public string? Name { get; }
        public int Seats { get; }
        public bool IsActive { get; }
        public string DisplayName { get; }
    }
}
