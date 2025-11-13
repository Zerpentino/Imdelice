using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Models;
using Imdeliceapp.Pages;

namespace Imdeliceapp.Popups;

public partial class OrderFiltersPopup : Popup
{
    readonly List<FilterOption> _statusOptions;
    readonly List<FilterOption> _serviceOptions;
    readonly List<FilterOption> _sourceOptions;
    readonly List<TableOption> _tableOptions;

    public OrderFiltersPopup(
        IEnumerable<FilterOption> statusOptions,
        FilterOption? selectedStatus,
        IEnumerable<FilterOption> serviceOptions,
        FilterOption? selectedService,
        IEnumerable<FilterOption> sourceOptions,
        FilterOption? selectedSource,
        IEnumerable<TableDTO> tables,
        bool useDateFilter,
        DateTime? fromDate,
        DateTime? toDate,
        int? tableId)
    {
        InitializeComponent();

        _statusOptions = statusOptions.Select(CloneOption).ToList();
        _serviceOptions = serviceOptions.Select(CloneOption).ToList();
        _sourceOptions = sourceOptions.Select(CloneOption).ToList();
        _tableOptions = BuildTableOptions(tables);

        StatusPicker.ItemsSource = _statusOptions;
        ServicePicker.ItemsSource = _serviceOptions;
        SourcePicker.ItemsSource = _sourceOptions;
        TablePicker.ItemsSource = _tableOptions;

        StatusPicker.SelectedIndex = FindIndex(_statusOptions, selectedStatus);
        ServicePicker.SelectedIndex = FindIndex(_serviceOptions, selectedService);
        SourcePicker.SelectedIndex = FindIndex(_sourceOptions, selectedSource);
        TablePicker.SelectedIndex = FindTableIndex(_tableOptions, tableId);

        DateSwitch.IsToggled = useDateFilter;
        DateGrid.IsVisible = useDateFilter;
        FromPicker.Date = (fromDate ?? DateTime.Today);
        ToPicker.Date = (toDate ?? DateTime.Today);
    }

    static int FindIndex(List<FilterOption> options, FilterOption? selected)
    {
        if (selected is null) return 0;
        var idx = options.FindIndex(o => string.Equals(o.Value, selected.Value, StringComparison.Ordinal) && string.Equals(o.Label, selected.Label, StringComparison.Ordinal));
        return idx >= 0 ? idx : 0;
    }

    static FilterOption CloneOption(FilterOption option) => new(option.Label, option.Value);

    static List<TableOption> BuildTableOptions(IEnumerable<TableDTO> tables)
    {
        var list = new List<TableOption>
        {
            new TableOption(null, "Todas las mesas")
        };

        list.AddRange(tables.Select(t => new TableOption(t.id, string.IsNullOrWhiteSpace(t.name) ? $"Mesa {t.id}" : t.name)));
        return list;
    }

    static int FindTableIndex(List<TableOption> options, int? tableId)
    {
        if (!tableId.HasValue) return 0;
        var idx = options.FindIndex(o => o.Id == tableId.Value);
        return idx >= 0 ? idx : 0;
    }

    void DateSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        DateGrid.IsVisible = e.Value;
    }

    void Cancel_Clicked(object sender, EventArgs e) => Close(null);

    void Apply_Clicked(object sender, EventArgs e)
    {
        var status = StatusPicker.SelectedItem as FilterOption ?? _statusOptions.FirstOrDefault();
        var service = ServicePicker.SelectedItem as FilterOption ?? _serviceOptions.FirstOrDefault();
        var source = SourcePicker.SelectedItem as FilterOption ?? _sourceOptions.FirstOrDefault();
        var tableOption = TablePicker.SelectedItem as TableOption ?? _tableOptions.FirstOrDefault();
        var tableId = tableOption?.Id;

        var useDate = DateSwitch.IsToggled;
        var from = useDate ? FromPicker.Date : (DateTime?)null;
        var to = useDate ? ToPicker.Date : (DateTime?)null;

        var result = new OrderFiltersResult(status, service, source, useDate, from, to, tableId);
        Close(result);
    }

    class TableOption
    {
        public TableOption(int? id, string label)
        {
            Id = id;
            Label = label;
        }

        public int? Id { get; }
        public string Label { get; }
        public override string ToString() => Label;
    }
}
