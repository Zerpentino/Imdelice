using Imdeliceapp.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Imdeliceapp.Models;



namespace Imdeliceapp.Pages;

[QueryProperty(nameof(GroupId), "groupId")]
[QueryProperty(nameof(GroupName), "groupName")]
public partial class GroupLinkedProductsPage : ContentPage
{
    readonly ModifiersApi _api = new();

    public int GroupId { get; set; }
    public string GroupName { get; set; } = "";

    public ObservableCollection<GroupProductLinkDTO> Links { get; } = new();
    List<GroupProductLinkDTO> _all = new();

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
            : _all.Where(x => (x.product.name ?? "").ToLowerInvariant().Contains(q));
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
        if ((s as SwipeItem)?.BindingContext is not GroupProductLinkDTO link) return;
        var ok = await DisplayAlert("Desvincular", $"¿Quitar el grupo de “{link.product.name}”?", "Quitar", "Cancelar");
        if (!ok) return;

        try
        {
            await _api.DetachGroupFromProductByLinkAsync(link.linkId);
            Links.Remove(link);
            _all.RemoveAll(x => x.linkId == link.linkId);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    async void EditPosition_Clicked(object s, EventArgs e)
    {
        if ((s as SwipeItem)?.BindingContext is not GroupProductLinkDTO link) return;

        var posStr = await DisplayPromptAsync("Posición", "Nueva posición (0..n):", "OK", "Cancelar",
                                              link.position.ToString(), keyboard: Keyboard.Numeric);
        if (!int.TryParse(posStr, out var newPos)) return;

        try
        {
            await _api.UpdateLinkPositionAsync(link.linkId, newPos);
            // actualizar en memoria y reordenar
            link.position = newPos;
            var temp = _all.FirstOrDefault(x => x.linkId == link.linkId);
            if (temp != null) temp.position = newPos;

            // reordenar vista
            var ordered = _all.OrderBy(x => x.product.name).ThenBy(x => x.position).ToList();
            Links.Clear();
            foreach (var x in ordered) Links.Add(x);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

}
