using Imdeliceapp.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Imdeliceapp.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(GroupId), "groupId")]
[QueryProperty(nameof(GroupName), "groupName")]
public partial class GroupLinkedProductsPage : ContentPage
{
    readonly ModifiersApi _api = new();

    public int GroupId { get; set; }
    public string GroupName { get; set; } = "";

    public ObservableCollection<GroupProductLinkRow> Links { get; } = new();
    List<GroupProductLinkRow> _all = new();

    bool _isRefreshing;
    public bool IsRefreshing { get => _isRefreshing; set { _isRefreshing = value; OnPropertyChanged(); } }
    bool _needsReload;

    public GroupLinkedProductsPage()
    {
        InitializeComponent();
        BindingContext = this;
        Title = "Productos vinculados";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Title = $"Grupo #{GroupId} – {GroupName}";
        if (_all.Count == 0 || _needsReload)
        {
            await LoadAsync();
            _needsReload = false;
        }
    }

    async Task LoadAsync(string? search = null)
    {
        try
        {
            var list = await _api.GetProductsByGroupAsync(GroupId, isActive: null, search: search, limit: 50, offset: 0);
            _all = list
                .OrderBy(l => l.product.name)
                .ThenBy(l => l.position)
                .Select(l => new GroupProductLinkRow(l))
                .ToList();

            Links.Clear();
            foreach (var x in _all) Links.Add(x);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    void SearchBox_TextChanged(object s, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? "").Trim().ToLowerInvariant();
        var src = string.IsNullOrEmpty(q)
            ? _all
            : _all.Where(x => (x.Product.name ?? "").ToLowerInvariant().Contains(q));
        Links.Clear();
        foreach (var x in src) Links.Add(x);
    }

    async void Refresh_Refreshing(object s, EventArgs e)
    {
        IsRefreshing = true;
        await LoadAsync(SearchBox?.Text);
    }

    async void AddLink_Clicked(object s, EventArgs e)
    {
        _needsReload = true;
        await Shell.Current.GoToAsync($"{nameof(AttachGroupToProductPage)}?groupId={GroupId}&groupName={Uri.EscapeDataString(GroupName)}");
    }

    async void Detach_Clicked(object s, EventArgs e)
    {
        if ((s as SwipeItem)?.BindingContext is not GroupProductLinkRow row) return;
        var link = row.Link;
        var ok = await DisplayAlert("Desvincular", $"¿Quitar el grupo de “{link.product.name}”?", "Quitar", "Cancelar");
        if (!ok) return;

        try
        {
            await _api.DetachGroupFromProductByLinkAsync(link.linkId);
            Links.Remove(row);
            _all.RemoveAll(x => x.Link.linkId == link.linkId);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    async void EditPosition_Clicked(object s, EventArgs e)
    {
        if ((s as SwipeItem)?.BindingContext is not GroupProductLinkRow row) return;
        var link = row.Link;

        var posStr = await DisplayPromptAsync("Posición", "Nueva posición (0..n):", "OK", "Cancelar",
                                              row.Position.ToString(), keyboard: Keyboard.Numeric);
        if (!int.TryParse(posStr, out var newPos)) return;

        try
        {
            await _api.UpdateLinkPositionAsync(link.linkId, newPos);
            row.Position = newPos;

            var ordered = _all.OrderBy(x => x.Product.name).ThenBy(x => x.Position).ToList();
            _all = ordered;
            Links.Clear();
            foreach (var x in ordered) Links.Add(x);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    public class GroupProductLinkRow : INotifyPropertyChanged
    {
        public GroupProductLinkRow(GroupProductLinkDTO link)
        {
            Link = link;
        }

        public GroupProductLinkDTO Link { get; }
        public ProductLiteDTO Product => Link.product;

        public int Position
        {
            get => Link.position;
            set
            {
                if (Link.position == value) return;
                Link.position = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PositionDisplay));
            }
        }

        public string PositionDisplay => $"Posición: {Position}";

        public string PriceDisplay => Link.product.priceCents.HasValue
            ? (Link.product.priceCents.Value / 100m).ToString("C", CultureInfo.CurrentCulture)
            : "—";

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
