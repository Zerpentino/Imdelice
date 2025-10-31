using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Imdeliceapp.Helpers;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Collections.Concurrent;
using Microsoft.Maui.ApplicationModel; // MainThread
using System.Linq;
using System.IO;
using System.Threading;
namespace Imdeliceapp.Pages;

#region DTOs
class CategoryDTO2
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? slug { get; set; }
    public int position { get; set; }
    public bool isComboOnly { get; set; }   // <-- NUEVO
}
class ProductListDTO
{
    public int id { get; set; }
    public string? type { get; set; } // SIMPLE | VARIANTED | COMBO
    public string? name { get; set; }
    public int categoryId { get; set; }
    public int? priceCents { get; set; } // null si VARIANTED
    public bool isActive { get; set; }
    public bool isAvailable { get; set; }  // <-- NUEVO
    public string? imageUrl { get; set; }
    public bool hasImage { get; set; }
}
class ApiEnvelope2<T> { public bool ok { get; set; } public T? data { get; set; } public string? error { get; set; } }

public class ProductListItem : INotifyPropertyChanged
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string type { get; set; } = "";
    public string priceLabel { get; set; } = "";
    public double basePrice { get; set; } 

    bool _isActive;
    public bool isActive                                  // <- NUEVO
    {
        get => _isActive;
        set { _isActive = value; PropertyChanged?.Invoke(this, new(nameof(isActive))); }
    }

    bool _isAvailable;
    public bool isAvailable                // <-- NUEVO
    {
        get => _isAvailable;
        set { _isAvailable = value; PropertyChanged?.Invoke(this, new(nameof(isAvailable))); }
    }

    ImageSource _thumb;
    public ImageSource thumb
    {
        get => _thumb;
        set { _thumb = value; PropertyChanged?.Invoke(this, new(nameof(thumb))); }
    }

    public string? imageUrl { get; set; }
    public bool hasImageUrl { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
}

#endregion



[QueryProperty(nameof(InitialCategorySlug), "categorySlug")]
[QueryProperty(nameof(RefreshFlag), "refresh")]

public partial class ProductsPage : ContentPage
{
    public string? RefreshFlag
    {
        get => null;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                // recarga solo los productos manteniendo la categoría seleccionada
                MainThread.BeginInvokeOnMainThread(async () => await CargarProductosAsync());
            }
        }
    }
    public string? InitialCategorySlug { get; set; }
    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ObservableCollection<ProductListItem> Products { get; } = new();
    List<ProductListItem> _all = new();
    List<CategoryDTO2> _cats = new();

    string _emptyMessage = "No hay productos";
    public string EmptyMessage { get => _emptyMessage; set { _emptyMessage = value; OnPropertyChanged(); } }
    bool _isRefreshing;
    public bool IsRefreshing { get => _isRefreshing; set { _isRefreshing = value; OnPropertyChanged(); } }

    public ProductsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarCategoriasAsync();
    }

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s)) return s;
        var p = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }
    HttpClient NewAuthClient(string baseUrl, string token)
    {
        var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }
    static readonly ConcurrentDictionary<string, ImageSource> _thumbCache = new();
    bool _silenceSwitch;                 // silenciar eventos durante rebinds/moves
    readonly HashSet<int> _busyToggles = new(); // evitar dobles PATCH por el mismo id

    // Orden maestros: disponibles primero, luego por nombre
    int CompareProducts(ProductListItem a, ProductListItem b)
    {
        int c = b.isActive.CompareTo(a.isActive);          // activos primero
        if (c != 0) return c;
        c = b.isAvailable.CompareTo(a.isAvailable);        // disponibles primero
        if (c != 0) return c;
        return string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase);
    }


    int FindInsertIndex(IList<ProductListItem> list, ProductListItem item)
    {
        for (int i = 0; i < list.Count; i++)
            if (CompareProducts(item, list[i]) < 0) return i;
        return list.Count;
    }

    void MoveKeepingSort(ProductListItem item)
    {
        _silenceSwitch = true;

        // mover en _all
        var oldAll = _all.IndexOf(item);
        if (oldAll >= 0)
        {
            _all.RemoveAt(oldAll);
            var newAll = FindInsertIndex(_all, item);
            if (newAll < 0) newAll = 0;
            if (newAll > _all.Count) newAll = _all.Count;
            _all.Insert(newAll, item);
        }

        // mover en ObservableCollection
        var oldUi = Products.IndexOf(item);
        if (oldUi >= 0)
        {
            var targetUi = FindInsertIndex(Products, item);
            if (targetUi > oldUi) targetUi--;       // compensar el Remove implícito de Move
            if (targetUi != oldUi) Products.Move(oldUi, targetUi);
        }

        _silenceSwitch = false;
    }


    async Task<ImageSource> GetThumbAsync(ProductListItem item, HttpClient http)
    {
        var cacheKey = !string.IsNullOrWhiteSpace(item.imageUrl) ? item.imageUrl! : $"id:{item.id}";
        if (_thumbCache.TryGetValue(cacheKey, out var cached)) return cached;

        try
        {
            string path;
            if (!string.IsNullOrWhiteSpace(item.imageUrl))
            {
                path = item.imageUrl!;
                if (!path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    if (!path.StartsWith('/'))
                        path = "/" + path;

                    if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                        path = "/api" + path;
                }
            }
            else
            {
                path = $"/api/products/{item.id}/image";
                if (!path.Contains("?"))
                {
                    var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    path += $"?ts={ts}";
                }
            }

            using var resp = await http.GetAsync(path);
            if (!resp.IsSuccessStatusCode)
                return _thumbCache[cacheKey] = ImageSource.FromFile("no_disponible.png");

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            var src = ImageSource.FromStream(() => new MemoryStream(bytes));
            _thumbCache[cacheKey] = src;
            return src;
        }
        catch
        {
            return _thumbCache[cacheKey] = ImageSource.FromFile("no_disponible.png");
        }
    }
    async Task CargarCategoriasAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                EmptyMessage = "Sin conexión a Internet.";
                Products.Clear(); return;
            }

            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.GetAsync("/api/categories");
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
                EmptyMessage = "Servidor no disponible.";
                await ErrorHandler.MostrarErrorUsuario(body);
                return;
            }

            var env = JsonSerializer.Deserialize<ApiEnvelope2<List<CategoryDTO2>>>(body, _json);
            _cats = (env?.data ?? new()).OrderBy(c => c.position).ToList();
            PkCategory.ItemsSource = _cats;


            // Si llegó slug desde Categorías, selecciona esa categoría
            if (!string.IsNullOrWhiteSpace(InitialCategorySlug))
            {
                var match = _cats.FirstOrDefault(c =>
                    string.Equals(c.slug, InitialCategorySlug, StringComparison.OrdinalIgnoreCase));
                if (match != null) PkCategory.SelectedItem = match;
            }
            // si hay item seleccionado ya, fija título
            if (PkCategory.SelectedItem is CategoryDTO2 catSel)
                Title = catSel.isComboOnly ? "Combos" : "Productos";
            // fallback si no hay slug o no hubo match
            if (PkCategory.SelectedItem == null && _cats.Count > 0)
                PkCategory.SelectedItem = _cats[0]; // esto ya dispara CargarProductosAsync

        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Products – Cargar categorías"); }
    }

    async Task CargarProductosAsync()
    {
        try
        {
            Products.Clear(); _all.Clear();
            EmptyMessage = "No hay productos";
            if (PkCategory.SelectedItem is not CategoryDTO2 cat) return;
            // <<< NUEVO: título dinámico
            Title = cat.isComboOnly ? "Combos" : "Productos";

            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);
            var slug = Uri.EscapeDataString(cat.slug ?? "");
            var url = cat.isComboOnly
                ? $"/api/products?type=COMBO&categorySlug={slug}"          // <- sin isActive=true
                : $"/api/products?categorySlug={slug}";                    // <- sin isActive=true


            var resp = await http.GetAsync(url);

            //var resp = await http.GetAsync($"/api/products?categorySlug={Uri.EscapeDataString(cat.slug ?? "")}&isActive=true");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

                if (resp.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
                { EmptyMessage = "El servidor no responde."; Products.Clear(); await ErrorHandler.MostrarErrorUsuario("El servidor no responde."); return; }

                await ErrorHandler.MostrarErrorUsuario(body);
                return;
            }

            var env = JsonSerializer.Deserialize<ApiEnvelope2<List<ProductListDTO>>>(body, _json);
            var list = env?.data ?? new();

            _all = list.Select(p => new ProductListItem
            {
                id = p.id,
                name = p.name ?? "",
                type = p.type ?? "",
                priceLabel = (p.type == "SIMPLE" || p.type == "COMBO") && p.priceCents.HasValue
                             ? $"${(p.priceCents.Value / 100.0):0.00}"
                             : "-",
                basePrice = (p.type == "SIMPLE" || p.type == "COMBO") && p.priceCents.HasValue
                 ? (p.priceCents.Value / 100.0)
                 : 0.0,   // VARIANTED u otros sin priceCents

                isActive = p.isActive,
                isAvailable = p.isAvailable,

                thumb = ImageSource.FromFile("no_disponible.png"),
                imageUrl = p.imageUrl,
                hasImageUrl = !string.IsNullOrWhiteSpace(p.imageUrl)
            }).ToList();
            _all = _all
       .OrderByDescending(p => p.isActive)     // activos primero
       .ThenByDescending(p => p.isAvailable)   // disponibles primero
       .ThenBy(p => p.name, StringComparer.CurrentCultureIgnoreCase)
       .ToList();





            foreach (var it in _all) Products.Add(it);

            // Cargar thumbnails (4 en paralelo)
            await LoadThumbnailsAsync(Products.ToList(), http);

        }
        catch (TaskCanceledException) { EmptyMessage = "Tiempo de espera agotado."; Products.Clear(); }
        catch (HttpRequestException) { EmptyMessage = "Servidor no disponible."; Products.Clear(); }
        catch (Exception ex) { EmptyMessage = "Servidor no disponible."; Products.Clear(); await ErrorHandler.MostrarErrorTecnico(ex, "Products – Cargar"); }
    }
    async Task LoadThumbnailsAsync(List<ProductListItem> items, HttpClient http)
    {
        var sem = new SemaphoreSlim(4);
        var tasks = items.Select(async item =>
        {
            await sem.WaitAsync();
            try { item.thumb = await GetThumbAsync(item, http); }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks);
    }


    async void PkCategory_SelectedIndexChanged(object s, EventArgs e) => await CargarProductosAsync();

    void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? "").Trim().ToLowerInvariant();
        var src = string.IsNullOrEmpty(q) ? _all : _all.Where(r => (r.name ?? "").ToLowerInvariant().Contains(q));
        Products.Clear();
        foreach (var r in src) Products.Add(r);
    }

    async void AddProduct_Clicked(object sender, EventArgs e)
    {
        var cat = PkCategory.SelectedItem as CategoryDTO2;
        var id = cat?.id ?? 0;
        await Shell.Current.GoToAsync($"{nameof(ProductEditorPage)}?mode=create&categoryId={id}");
    }

    async void EditSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.BindingContext is ProductListItem item)
            await Shell.Current.GoToAsync($"{nameof(ProductEditorPage)}?mode=edit&id={item.id}");
    }

    async void DeleteSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.BindingContext is not ProductListItem item) return;
        var ok = await DisplayAlert("Eliminar producto", $"¿Eliminar “{item.name}”?", "Sí", "Cancelar");
        if (!ok) return;

        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.DeleteAsync($"/api/products/{item.id}?hard=true");

            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
                await ErrorHandler.MostrarErrorUsuario(string.IsNullOrWhiteSpace(body) ? "Error al eliminar." : body);
                return;
            }

            Products.Remove(item);
            _all.RemoveAll(p => p.id == item.id);
        }
        catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Products – Eliminar"); }
    }

    async void ProdRefresh_Refreshing(object sender, EventArgs e)
    {
        try { await CargarProductosAsync(); }
        finally { IsRefreshing = false; }
    }
    async void Retry_Clicked(object sender, EventArgs e)
    {
        IsRefreshing = true;
        await CargarProductosAsync();
        IsRefreshing = false;
    }
    HttpRequestMessage BuildPatch(string url, object payload)
    {
        var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"),
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };
        req.Headers.ConnectionClose = true;
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
                var token = await GetTokenAsync() ?? "";
                var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
                using var http = NewAuthClient(baseUrl, token);
                using var req = makeRequest();
                return await http.SendAsync(req);
            }
            catch (HttpRequestException ex) when (attempt == 1 && IsTransient(ex))
            {
                await Task.Delay(150); // reintento rápido
                continue;
            }
        }
        throw new HttpRequestException("Fallo al reintentar.");
    }
    async void ToggleDisponible_Toggled(object sender, ToggledEventArgs e)
    {
        if (_silenceSwitch) return;
        if (sender is not Switch sw) return;
        if (sw.BindingContext is not ProductListItem item) return;

        // Evitar doble PATCH por el mismo producto
        if (_busyToggles.Contains(item.id))
        {
            _silenceSwitch = true;
            sw.IsToggled = item.isAvailable; // no cambiar
            _silenceSwitch = false;
            return;
        }

        var nuevo = e.Value;
        var anterior = item.isAvailable;

        _busyToggles.Add(item.id);
        sw.IsEnabled = false;

        try
        {
            // UI optimista (ya lo hizo el TwoWay), solo mover al terminar
            var resp = await SendWithOneRetryAsync(
                () => BuildPatch($"/api/products/{item.id}", new { isAvailable = nuevo })
            );

            if (!resp.IsSuccessStatusCode)
            {
                // Revertir UI y modelo
                _silenceSwitch = true;
                item.isAvailable = anterior;
                sw.IsToggled = anterior;
                _silenceSwitch = false;

                var body = await resp.Content.ReadAsStringAsync();
                await ErrorHandler.MostrarErrorUsuario(
                    string.IsNullOrWhiteSpace(body) ? "No se pudo actualizar disponibilidad." : body
                );
                return;
            }

            // Éxito: aseguramos el modelo y reordenamos
            item.isAvailable = nuevo;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Yield(); // dejar terminar animación
                MoveKeepingSort(item);
            });
        }
        catch (Exception ex)
        {
            // Revertir en excepciones
            _silenceSwitch = true;
            item.isAvailable = anterior;
            sw.IsToggled = anterior;
            _silenceSwitch = false;

            await ErrorHandler.MostrarErrorTecnico(ex, "Products – Disponibilidad");
        }
        finally
        {
            _busyToggles.Remove(item.id);
            sw.IsEnabled = true;
        }
    }
    async void ToggleActivo_Toggled(object sender, ToggledEventArgs e)
    {
        if (_silenceSwitch) return;
        if (sender is not Switch sw) return;
        if (sw.BindingContext is not ProductListItem item) return;

        // Evitar doble PATCH por el mismo producto
        if (_busyToggles.Contains(item.id))
        {
            _silenceSwitch = true;
            sw.IsToggled = item.isActive;
            _silenceSwitch = false;
            return;
        }

        var nuevo = e.Value;
        var anterior = item.isActive;

        _busyToggles.Add(item.id);
        sw.IsEnabled = false;

        try
        {
            // UI optimista ya ocurrió por TwoWay; sólo persistimos y reordenamos
            var resp = await SendWithOneRetryAsync(
                () => BuildPatch($"/api/products/{item.id}", new { isActive = nuevo })
            );

            if (!resp.IsSuccessStatusCode)
            {
                // Revertir
                _silenceSwitch = true;
                item.isActive = anterior;
                sw.IsToggled = anterior;
                _silenceSwitch = false;

                var body = await resp.Content.ReadAsStringAsync();
                await ErrorHandler.MostrarErrorUsuario(
                    string.IsNullOrWhiteSpace(body) ? "No se pudo actualizar el estado." : body);
                return;
            }

            // OK → asegurar modelo y mover según el orden
            item.isActive = nuevo;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Yield();
                MoveKeepingSort(item);
            });
        }
        catch (Exception ex)
        {
            // Revertir en fallo
            _silenceSwitch = true;
            item.isActive = anterior;
            sw.IsToggled = anterior;
            _silenceSwitch = false;

            await ErrorHandler.MostrarErrorTecnico(ex, "Products – Activar/Inactivar");
        }
        finally
        {
            _busyToggles.Remove(item.id);
            sw.IsEnabled = true;
        }
    }
    async void ProductCard_Tapped(object sender, TappedEventArgs e)
{
    if (sender is not Grid grid) return;
    if (grid.BindingContext is not ProductListItem item) return;

    if (!item.isActive)
    {
        await DisplayAlert("Inactivo", "Este producto está inactivo.", "OK");
        return;
    }

    // OJO: usa item.basePrice
    var bp = item.basePrice; // double

    // Si navegas con Shell + QueryProperty:
    await Shell.Current.GoToAsync(
        $"{nameof(ProductModifiersPage)}?productId={item.id}&basePrice={bp.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
    );

    // Alternativa (si prefieres push clásico y dejaste el ctor con params):
    // await Navigation.PushAsync(new ProductModifiersPage(item.id, bp));
}





}
