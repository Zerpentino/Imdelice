using Imdeliceapp.Services;
using System.Collections.ObjectModel; // <— faltaba
using System.Linq;
using Imdeliceapp.Models;
using System.ComponentModel;

namespace Imdeliceapp.Pages;

public partial class ModifierGroupsPage : ContentPage
{
        public bool CanRead   => Perms.ModifiersRead;
    public bool CanCreate => Perms.ModifiersCreate;
    public bool CanUpdate => Perms.ModifiersUpdate;
    public bool CanDelete => Perms.ModifiersDelete;

    readonly ModifiersApi _api = new();
    public class GroupListItem : INotifyPropertyChanged
    {
        int _id, _minSelect, _position;
        int? _maxSelect;
        string _name = "", _description = "";
        bool _isActive, _isRequired;

        public int id { get => _id; set { if (_id != value) { _id = value; PropertyChanged?.Invoke(this, new(nameof(id))); } } }
        public string name { get => _name; set { if (_name != value) { _name = value; PropertyChanged?.Invoke(this, new(nameof(name))); } } }
        public string description { get => _description; set { if (_description != value) { _description = value; PropertyChanged?.Invoke(this, new(nameof(description))); } } }
        public int minSelect { get => _minSelect; set { if (_minSelect != value) { _minSelect = value; PropertyChanged?.Invoke(this, new(nameof(minSelect))); } } }
        public int? maxSelect { get => _maxSelect; set { if (_maxSelect != value) { _maxSelect = value; PropertyChanged?.Invoke(this, new(nameof(maxSelect))); } } }
        public bool isRequired { get => _isRequired; set { if (_isRequired != value) { _isRequired = value; PropertyChanged?.Invoke(this, new(nameof(isRequired))); } } }
        public bool isActive { get => _isActive; set { if (_isActive != value) { _isActive = value; PropertyChanged?.Invoke(this, new(nameof(isActive))); } } }
        public int position { get => _position; set { if (_position != value) { _position = value; PropertyChanged?.Invoke(this, new(nameof(position))); } } }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    public ObservableCollection<GroupListItem> Groups { get; } = new();
    List<GroupListItem> _all = new();

    bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set { _isRefreshing = value; OnPropertyChanged(); }
    }
    bool _loadedOnce;          // ya cargué al menos una vez
    bool _needsReload;         // pedir recarga la próxima vez que aparezca
    bool _loading;             // evita llamadas simultáneas

    bool _silenceSwitch;
    readonly HashSet<int> _busyToggles = new();
    public ModifierGroupsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
{
        base.OnAppearing();
     // refresca bindings de permisos
    OnPropertyChanged(nameof(CanRead));
    OnPropertyChanged(nameof(CanCreate));
    OnPropertyChanged(nameof(CanUpdate));
        OnPropertyChanged(nameof(CanDelete));
        if (!CanRead)
        {
            await DisplayAlert("Acceso restringido", "No tienes permiso para ver modificadores.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }
    

    if (!_loadedOnce || _needsReload)
    {
        _loadCts?.Cancel();
        _loadCts = new();
        await LoadAsync(ct: _loadCts.Token);
        _loadedOnce = true;
        _needsReload = false;
    }
}

    // --- helpers arriba de la clase ---

     // ===== Ordenamiento/Mover como en categorías =====
    int CompareGroups(GroupListItem a, GroupListItem b)
    {
        int c = b.isActive.CompareTo(a.isActive); // activos primero
        if (c != 0) return c;
        c = a.position.CompareTo(b.position);
        if (c != 0) return c;
        return string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase);
    }

     int FindInsertIndex(IList<GroupListItem> list, GroupListItem item)
    {
        for (int i = 0; i < list.Count; i++)
            if (CompareGroups(item, list[i]) < 0) return i;
        return list.Count;
    }

     void MoveKeepingSort(GroupListItem item)
    {
        _silenceSwitch = true;

        var oldAll = _all.IndexOf(item);
        if (oldAll >= 0)
        {
            _all.RemoveAt(oldAll);
            _all.Insert(FindInsertIndex(_all, item), item);
        }

        var oldUi = Groups.IndexOf(item);
        if (oldUi >= 0)
        {
            var insUi = FindInsertIndex(Groups, item);
            if (insUi > oldUi) insUi--; // comportamiento de Move
            if (insUi != oldUi) Groups.Move(oldUi, insUi);
        }

        _silenceSwitch = false;
    }
// campos
CancellationTokenSource? _loadCts;
System.Timers.Timer? _searchDebounce;

    async Task LoadAsync(string? search = null, CancellationToken ct = default)
{
    if (_loading) return;
    _loading = true;

    try
    {
        var raw = await _api.GetGroupsAsync(search: string.IsNullOrWhiteSpace(search) ? null : search);
        ct.ThrowIfCancellationRequested();

        _all = raw.Select(g => new GroupListItem
        {
            id = g.id,
            name = g.name ?? "",
            description = g.description ?? "",
            minSelect = g.minSelect,
            maxSelect = g.maxSelect,
            isRequired = g.isRequired,
            isActive = g.isActive,
            position = g.position
        })
        .OrderByDescending(x => x.isActive)
        .ThenBy(x => x.position)
        .ThenBy(x => x.name)
        .ToList();

        ApplySearch(SearchBox?.Text);  // filtra/ordena en memoria
    }
    catch (OperationCanceledException) { /* ignorar */ }
    catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    finally
    {
        _loading = false;
        IsRefreshing = false;
    }
}



     void ApplySearch(string? q)
    {
        q = (q ?? "").Trim().ToLowerInvariant();

        IEnumerable<GroupListItem> src = _all;
        if (!string.IsNullOrEmpty(q))
            src = _all.Where(g =>
                (g.name ?? "").ToLowerInvariant().Contains(q) ||
                (g.description ?? "").ToLowerInvariant().Contains(q));

        src = src
            .OrderByDescending(g => g.isActive)
            .ThenBy(g => g.position)
            .ThenBy(g => g.name);

        _silenceSwitch = true;
        Groups.Clear();
        foreach (var g in src) Groups.Add(g);
        _silenceSwitch = false;
    }



    void SearchBox_TextChanged(object s, TextChangedEventArgs e) => ApplySearch(e.NewTextValue);
    // async void Filter_Toggled(object s, ToggledEventArgs e) { IsRefreshing = true; await LoadAsync(); }
   async void ProdRefresh_Refreshing(object s, EventArgs e)
{
    _loadCts?.Cancel();
    _loadCts = new();
    IsRefreshing = true;
    await LoadAsync(SearchBox?.Text, _loadCts.Token);
}


    async void New_Clicked(object s, EventArgs e)
    {
            if (!CanCreate) { await DisplayAlert("Acceso restringido", "No puedes crear grupos de modificadores.", "OK"); return; }


        _needsReload = true;
        await Shell.Current.GoToAsync(nameof(GroupEditorPage));
    }

    async void Edit_Clicked(object s, EventArgs e)
    {
            if (!CanUpdate) { await DisplayAlert("Acceso restringido", "No puedes editar grupos de modificadores.", "OK"); return; }

        if ((s as Element)?.BindingContext is not GroupListItem g) return;
        _needsReload = true;
        await Shell.Current.GoToAsync($"{nameof(GroupEditorPage)}?id={g.id}");
    }
   async void ToggleActivo_Toggled(object sender, ToggledEventArgs e)
{
    if (_silenceSwitch) return;
    if (sender is not Switch sw) return;
    if (sw.BindingContext is not GroupListItem g) return;

    var nuevo = e.Value;
        var anterior = g.isActive;
        if (!CanUpdate)
        {
            _silenceSwitch = true;
            sw.IsToggled = anterior; // revertir de inmediato
            _silenceSwitch = false;
            await DisplayAlert("Acceso restringido", "No puedes actualizar grupos de modificadores.", "OK");
            return;
        }
    

    if (_busyToggles.Contains(g.id))  // evita dobles PATCH por id
    {
        _silenceSwitch = true; sw.IsToggled = anterior; _silenceSwitch = false;
        return;
    }
    _busyToggles.Add(g.id);

    sw.IsEnabled = false;          // ✅ bloquear mientras se procesa
    try
    {
        // 1) Optimista: cambia el flag
        _silenceSwitch = true;
        g.isActive = nuevo;
        _silenceSwitch = false;

        // 2) Reordenar en el PRÓXIMO frame (evita crashes de RecyclerView)
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();
            MoveKeepingSort(g);
        });

        // 3) PATCH
        await _api.UpdateGroupAsync(g.id, new ModifiersApi.UpdateGroupDto { isActive = nuevo });
    }
    catch (Exception ex)
    {
        // Revertir TODO de forma segura
        _silenceSwitch = true;
        g.isActive = anterior;
        sw.IsToggled = anterior;
        _silenceSwitch = false;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();
            MoveKeepingSort(g);
        });

        await DisplayAlert("Error", ex.Message, "OK");
    }
    finally
    {
        _busyToggles.Remove(g.id);
        sw.IsEnabled = true;
    }
}



    async void Delete_Clicked(object s, EventArgs e)
    {
            if (!CanDelete) { await DisplayAlert("Acceso restringido", "No puedes eliminar grupos de modificadores.", "OK"); return; }

        if ((s as Element)?.BindingContext is not GroupListItem g) return;
        var hard = await DisplayAlert("Eliminar", $"¿Eliminar el grupo “{g.name}”?", "Borrar", "Cancelar");
        if (!hard) return;

        try
        {
            await _api.DeleteGroupAsync(g.id, hard: true);
            Groups.Remove(g);
            _all.RemoveAll(x => x.id == g.id);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }


    //   async void Attach_Clicked(object s, EventArgs e)
    // {
    //     if ((s as Element)?.BindingContext is not GroupListItem g) return;
    //     string pidStr = await DisplayPromptAsync("Adjuntar a producto", "ID de producto:", "OK", "Cancelar", keyboard: Keyboard.Numeric);
    //     if (!int.TryParse(pidStr, out var productId)) return;
    //     string posStr = await DisplayPromptAsync("Orden en la UI", "Position (0..n):", "OK", "Cancelar", "0", keyboard: Keyboard.Numeric);
    //     int.TryParse(posStr, out var position);

    //     try
    //     {
    //         await _api.AttachGroupToProductAsync(productId, g.id, position);
    //         await DisplayAlert("OK", "Adjuntado.", "OK");
    //     }
    //     catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
    // }
  async void OpenLinkedProducts_Clicked(object sender, EventArgs e)
{
    if ((sender as Button)?.CommandParameter is not GroupListItem g) return; // 👈 usa GroupListItem
    var url = $"{nameof(GroupLinkedProductsPage)}?groupId={g.id}&groupName={Uri.EscapeDataString(g.name ?? "")}";
    await Shell.Current.GoToAsync(url);
}


}
