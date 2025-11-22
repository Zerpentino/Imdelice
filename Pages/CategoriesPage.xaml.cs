using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Imdeliceapp.Helpers;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.Linq; // <-- importante
using System.IO; // para IOException en IsTransient
using Microsoft.Maui.ApplicationModel; // para MainThread
using System.ComponentModel;
using Imdeliceapp.Services;


namespace Imdeliceapp.Pages;

#region DTOs
class CategoryDTO
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? slug { get; set; }
    public int? parentId { get; set; }
    public int position { get; set; }
    public bool isActive { get; set; }
    public bool isComboOnly { get; set; }

}
class ApiEnvelopeCategoria<T>
{
    public bool ok { get; set; }
    public T? data { get; set; }
    public string? error { get; set; }
    public string? message { get; set; }
}
public class CategoryListItem : INotifyPropertyChanged
{
    int _id;
    string _name = "";
    string _slug = "";
    int _position;
    bool _isActive;
    bool _isComboOnly;

    public int id
    {
        get => _id;
        set { if (_id != value) { _id = value; PropertyChanged?.Invoke(this, new(nameof(id))); } }
    }
    public string name
    {
        get => _name;
        set { if (_name != value) { _name = value; PropertyChanged?.Invoke(this, new(nameof(name))); } }
    }
    public string slug
    {
        get => _slug;
        set { if (_slug != value) { _slug = value; PropertyChanged?.Invoke(this, new(nameof(slug))); } }
    }
    public int position
    {
        get => _position;
        set { if (_position != value) { _position = value; PropertyChanged?.Invoke(this, new(nameof(position))); } }
    }

    public bool isActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; PropertyChanged?.Invoke(this, new(nameof(isActive))); } }
    }

    public bool isComboOnly
    {
        get => _isComboOnly;
        set { if (_isComboOnly != value) { _isComboOnly = value; PropertyChanged?.Invoke(this, new(nameof(isComboOnly))); } }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}

#endregion

public partial class CategoriesPage : ContentPage
{
    public bool CanRead => Perms.CategoriesRead;
    public bool CanCreate => Perms.CategoriesCreate;
    public bool CanUpdate => Perms.CategoriesUpdate;
    public bool CanDelete => Perms.CategoriesDelete;
    bool _warnedNoUpdate; // evita mostrar muchas alertas al intentar toggles sin permiso

    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    public ObservableCollection<CategoryListItem> Categories { get; } = new();
    List<CategoryListItem> _all = new();

    string _emptyMessage = "No hay categorías";
    public string EmptyMessage { get => _emptyMessage; set { _emptyMessage = value; OnPropertyChanged(); } }

    bool _isRefreshing;
    public bool IsRefreshing { get => _isRefreshing; set { _isRefreshing = value; OnPropertyChanged(); } }

    public CategoriesPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanUpdate));
        OnPropertyChanged(nameof(CanDelete));
        if (!CanRead)
        {
            await DisplayAlert("Acceso restringido", "No tienes permiso para ver categorías.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }


        await CargarCategoriasAsync();
    }

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s)) return s;
        var p = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }
    HttpClient? _http;

    HttpClient NewAuthClient(string baseUrl, string token)
    {
        var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }
    async Task<HttpClient> EnsureHttpAsync()
    {
        var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
        var token = await GetTokenAsync() ?? "";

        if (_http == null)
            _http = NewAuthClient(baseUrl, token);
        else
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return _http;
    }
    bool _silenceSwitch;   // <<--- NUEVO: silencia Toggled
    bool _loading;
    static IEnumerable<CategoryListItem> OrderCats(IEnumerable<CategoryListItem> src) =>
        src.OrderByDescending(c => c.isActive)     // activos primero
           .ThenBy(c => c.position)
           .ThenBy(c => c.name, StringComparer.CurrentCultureIgnoreCase);
    readonly HashSet<int> _busyToggles = new();  // NUEVO: evita dobles PATCH por id
    int CompareCats(CategoryListItem a, CategoryListItem b)
    {
        int c = b.isActive.CompareTo(a.isActive);        // activos primero
        if (c != 0) return c;
        c = a.position.CompareTo(b.position);
        if (c != 0) return c;
        return string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase);
    }

    int FindInsertIndex(IList<CategoryListItem> list, CategoryListItem item)
    {
        for (int i = 0; i < list.Count; i++)
            if (CompareCats(item, list[i]) < 0) return i;
        return list.Count;
    }
    /// mueve 'item' a su lugar correcto en _all y en la UI sin reconstruir todo
    void MoveKeepingSort(CategoryListItem item)
    {
        _silenceSwitch = true;

        // --- Lista de trabajo (_all) ---
        var oldAll = _all.IndexOf(item);
        if (oldAll >= 0)
        {
            _all.RemoveAt(oldAll);
            var newAll = FindInsertIndex(_all, item); // <- ahora con la lista ya sin el ítem
                                                      // clamp por si acaso (defensivo)
            if (newAll < 0) newAll = 0;
            if (newAll > _all.Count) newAll = _all.Count;
            _all.Insert(newAll, item);
        }

        // --- Lista bindeada (Categories) ---
        var oldUi = Categories.IndexOf(item);
        if (oldUi >= 0)
        {
            var targetUi = FindInsertIndex(Categories, item);
            if (targetUi > oldUi) targetUi--;  // porque Move no quita antes
            if (targetUi != oldUi) Categories.Move(oldUi, targetUi);
        }

        _silenceSwitch = false;
    }


    void Reload(IEnumerable<CategoryListItem> src)
    {
        _silenceSwitch = true;             // <<--- silenciar mientras se re-bindea
        Categories.Clear();
        foreach (var r in src) Categories.Add(r);
        _silenceSwitch = false;            // <<---
    }

    void ServidorNoDisponible(string why)
    {
        EmptyMessage = why switch
        {
            "sin_internet" => "Sin conexión a Internet.",
            "timeout" => "Tiempo de espera agotado. El servidor no responde.",
            _ => "Servidor no disponible. Reintenta más tarde."
        };
        Categories.Clear();
    }

    async Task CargarCategoriasAsync()
    {
        if (_loading) return;
        _loading = true;
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                ServidorNoDisponible("sin_internet");
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            EmptyMessage = "No hay categorías";

            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            // var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            var http = await EnsureHttpAsync();
            var resp = await http.GetAsync("/api/categories", HttpCompletionOption.ResponseHeadersRead);
            if (resp.StatusCode == HttpStatusCode.Forbidden)
            {
                await DisplayAlert("Acceso restringido", "No tienes permiso para ver categorías.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }


            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

                if (resp.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
                { ServidorNoDisponible("timeout"); await ErrorHandler.MostrarErrorUsuario("El servidor no responde."); return; }

                await ErrorHandler.MostrarErrorUsuario(body);
                return;
            }

            var env = JsonSerializer.Deserialize<ApiEnvelopeCategoria<List<CategoryDTO>>>(body, _json);
            var list = (env?.data ?? new())
                .Where(c => !string.Equals(c.slug, "inventario", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _all = list
                .Select(c => new CategoryListItem
                {
                    id = c.id,
                    name = c.name ?? "",
                    slug = c.slug ?? "",
                    position = c.position,
                    isActive = c.isActive,
                    isComboOnly = c.isComboOnly
                })
                .OrderByDescending(c => c.isActive)
                .ThenBy(c => c.position)
                .ThenBy(c => c.name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            // repuebla una sola vez, no dentro del toggle
            _silenceSwitch = true;
            Categories.Clear();
            foreach (var r in _all) Categories.Add(r);
            _silenceSwitch = false;
        }
        catch (TaskCanceledException)
        {
            ServidorNoDisponible("timeout");
            await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado.");
        }
        catch (HttpRequestException)
        {
            ServidorNoDisponible("");
            await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
        }
        catch (Exception ex)
        {
            ServidorNoDisponible("");
            await ErrorHandler.MostrarErrorTecnico(ex, "Categories – Cargar");
        }
        finally
        {
            _loading = false;
        }
    }
    HttpRequestMessage BuildPatch(string url, object payload)
    {
        var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };
        req.Headers.ConnectionClose = true; // evita reusar socket
        return req;
    }

    static bool IsTransient(HttpRequestException ex)
    {
        var m = ex.Message?.ToLowerInvariant() ?? "";
        return m.Contains("unexpected end of stream")
            || m.Contains("socket closed")
            || m.Contains("reset")
            || ex.InnerException is IOException;
    }

    async Task<HttpResponseMessage> SendWithOneRetryAsync(Func<HttpRequestMessage> makeRequest)
    {
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var http = await EnsureHttpAsync();
                using var req = makeRequest();
                return await http.SendAsync(req);
            }
            catch (HttpRequestException ex) when (attempt == 1 && IsTransient(ex))
            {
                // cae a un nuevo intento re-creando el cliente por si el pool quedó corrupto
                _http?.Dispose(); _http = null;
                await Task.Delay(150);
                continue;
            }
        }
        throw new HttpRequestException("Fallo al reintentar.");
    }

    void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? "").Trim().ToLowerInvariant();
        IEnumerable<CategoryListItem> src = _all;
        if (!string.IsNullOrEmpty(q))
            src = _all.Where(r =>
                (r.name ?? "").ToLowerInvariant().Contains(q) ||
                (r.slug ?? "").ToLowerInvariant().Contains(q));

        Reload(OrderCats(src));
    }

    async void AddCategory_Clicked(object sender, EventArgs e)
    {
        if (!CanCreate) { await DisplayAlert("Acceso restringido", "No puedes crear categorías.", "OK"); return; }
        await Shell.Current.GoToAsync($"{nameof(CategoryEditorPage)}?mode=create");
    }

    async void EditSwipe_Invoked(object sender, EventArgs e)
    {
        if (!CanUpdate) { await DisplayAlert("Acceso restringido", "No puedes editar categorías.", "OK"); return; }


        if ((sender as SwipeItem)?.BindingContext is CategoryListItem item)
        {
            var url = $"{nameof(CategoryEditorPage)}?mode=edit" +
           $"&id={item.id}" +
           $"&name={Uri.EscapeDataString(item.name)}" +
           $"&slug={Uri.EscapeDataString(item.slug)}" +
           $"&position={item.position}" +
           $"&isActive={(item.isActive ? "1" : "0")}" +
           $"&isComboOnly={(item.isComboOnly ? "1" : "0")}";   // <—
            await Shell.Current.GoToAsync(url);


        }
    }

    // 💥 eliminar definitivo
    async void DeleteSwipe_Invoked(object sender, EventArgs e)
    {
        if (!CanDelete) { await DisplayAlert("Acceso restringido", "No puedes eliminar categorías.", "OK"); return; }

        if ((sender as SwipeItem)?.BindingContext is not CategoryListItem item) return;
        var ok = await DisplayAlert("Eliminar definitivamente",
            $"Esta acción no se puede deshacer.\n\n¿Eliminar “{item.name}”?", "Sí, eliminar", "Cancelar");
        if (!ok) return;

        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

            var http = await EnsureHttpAsync(); // <- reutilizado

            var resp = await http.DeleteAsync($"/api/categories/{item.id}?hard=true");
            var body = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode == HttpStatusCode.Forbidden)
            {
                await DisplayAlert("Acceso restringido", "No tienes permiso para eliminar categorías.", "OK");
                return;
            }
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
                await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(body) ? "Error al eliminar." : body);
                return;
            }

            Categories.Remove(item);
            _all.RemoveAll(r => r.id == item.id);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Categories – Eliminar");
        }
    }

    // 🔀 activar/inactivar con PATCH, reordenando la lista
    async void ToggleActivo_Toggled(object sender, ToggledEventArgs e)
    {



        if (_silenceSwitch) return;
        if (sender is not Switch sw) return;

        if (sw.BindingContext is not CategoryListItem item) return;

        var nuevo = e.Value;
        var anterior = item.isActive;
        if (!CanUpdate)
        {
            _silenceSwitch = true;
            sw.IsToggled = anterior;   // vuelve al estado anterior
            _silenceSwitch = false;

            if (!_warnedNoUpdate)
            {
                _warnedNoUpdate = true;
                await DisplayAlert("Acceso restringido", "No puedes actualizar categorías.", "OK");
            }
            return;
        }


        // Evitar doble PATCH por el mismo id
        if (_busyToggles.Contains(item.id))
        {
            _silenceSwitch = true; sw.IsToggled = anterior; _silenceSwitch = false;
            return;
        }
        _busyToggles.Add(item.id);

        // 1) UI optimista: actualizar modelo YA
        _silenceSwitch = true;
        item.isActive = nuevo;           // dispara PropertyChanged
        _silenceSwitch = false;

        // 2) Mover en el siguiente frame para no pelear con la animación
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();          // deja terminar la animación
            MoveKeepingSort(item);
        });

        sw.IsEnabled = false;
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token)) throw new Exception("No token");

            //             var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            // var http = await EnsureHttpAsync();

            //             var json = JsonSerializer.Serialize(new { isActive = nuevo });

            var resp = await SendWithOneRetryAsync(() => BuildPatch($"/api/categories/{item.id}", new { isActive = nuevo }));
            if (resp.StatusCode == HttpStatusCode.Forbidden)
            {
                _silenceSwitch = true;
                item.isActive = anterior;
                sw.IsToggled = anterior;
                _silenceSwitch = false;
                MoveKeepingSort(item);
                await DisplayAlert("Acceso restringido", "No tienes permiso para actualizar categorías.", "OK");
                return;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();

                // 3) Revertir (UI + orden) si la API falla
                _silenceSwitch = true;
                item.isActive = anterior;
                sw.IsToggled = anterior;
                _silenceSwitch = false;
                MoveKeepingSort(item);

                await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(body) ? "No se pudo actualizar." : body);
            }
        }
        catch (Exception ex)
        {
            // Revertir en errores inesperados
            _silenceSwitch = true;
            item.isActive = anterior;
            sw.IsToggled = anterior;
            _silenceSwitch = false;
            MoveKeepingSort(item);

            await ErrorHandler.MostrarErrorTecnico(ex, "Categories – Activar/Inactivar");
        }
        finally
        {
            _busyToggles.Remove(item.id);
            sw.IsEnabled = true;
        }
    }

    async void CatRefresh_Refreshing(object sender, EventArgs e)
    {
        try { await CargarCategoriasAsync(); }
        finally { IsRefreshing = false; }
    }
    async void Retry_Clicked(object sender, EventArgs e)
    {
        IsRefreshing = true;
        await CargarCategoriasAsync();
        IsRefreshing = false;
    }

    // Si conservas el botón "Entrar":
    // CategoriesPage.xaml.cs
    async void OpenProducts_Clicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is CategoryListItem item)
        {
            var slug = item.slug ?? "";
            var comboOnly = item.isComboOnly ? "1" : "0";
            await Shell.Current.GoToAsync(
                $"{nameof(ProductsPage)}?categorySlug={Uri.EscapeDataString(slug)}&comboOnly={comboOnly}"
            );
        }
    }

}
