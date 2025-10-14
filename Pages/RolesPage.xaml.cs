using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Imdeliceapp.Helpers;
using Imdeliceapp.Model;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking; // Connectivity
using System.Threading.Tasks;     // TaskCanceledException

namespace Imdeliceapp.Pages;

public class RoleListItem
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string? description { get; set; }
}

public partial class RolesPage : ContentPage
{
    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // Datos y binding
    public ObservableCollection<RoleListItem> Roles { get; } = new();
    private List<RoleListItem> _all = new();

    // EmptyView dinámico
    string _emptyMessage = "No hay roles";
    public string EmptyMessage
    {
        get => _emptyMessage;
        set { _emptyMessage = value; OnPropertyChanged(nameof(EmptyMessage)); }
    }

    // Pull-to-refresh
    bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set { _isRefreshing = value; OnPropertyChanged(nameof(IsRefreshing)); }
    }

    public RolesPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarRolesAsync();
    }

    #region Helpers HTTP/UI
    void MostrarServidorNoDisponible()
    {
        EmptyMessage = "Servidor no disponible. Revisa tu red o inténtalo más tarde.";
        Roles.Clear(); // fuerza EmptyView con el mensaje
    }

    void MostrarEmptyPorDefecto()
    {
        EmptyMessage = "No hay roles";
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
        var cli = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }
    #endregion

    private async Task CargarRolesAsync()
    {
        try
        {
            // 1) Sin internet del dispositivo
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                MostrarServidorNoDisponible();
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            Roles.Clear(); _all.Clear();
            MostrarEmptyPorDefecto();

            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.GetAsync("/api/roles");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthHelper.VerificarYRedirigirSiExpirado(this);
                    return;
                }

                // 503/504 => mensaje amable y EmptyView específico
                if (resp.StatusCode == HttpStatusCode.ServiceUnavailable ||
                    resp.StatusCode == HttpStatusCode.GatewayTimeout)
                {
                    MostrarServidorNoDisponible();
                    await ErrorHandler.MostrarErrorUsuario("El servidor no responde.");
                    return;
                }

                await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
                return;
            }

            var env = JsonSerializer.Deserialize<ApiEnvelope<List<RoleDTO>>>(body, _json);
            var list = env?.data ?? new();

            _all = list.Select(r => new RoleListItem
            {
                id = r.id,
                name = r.name ?? "",
                description = r.description
            }).ToList();

            foreach (var r in _all) Roles.Add(r);

            // Si viene vacío, dejamos "No hay roles"
            if (Roles.Count == 0) MostrarEmptyPorDefecto();
        }
        catch (TaskCanceledException) // timeout
        {
            MostrarServidorNoDisponible();
            await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado. El servidor no responde.");
        }
        catch (HttpRequestException) // sin servidor / DNS / conexión
        {
            MostrarServidorNoDisponible();
            await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
        }
        catch (Exception ex)
        {
            MostrarServidorNoDisponible();
            await ErrorHandler.MostrarErrorTecnico(ex, "Roles – Cargar");
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? "").Trim().ToLowerInvariant();
        var src = string.IsNullOrEmpty(q)
            ? _all
            : _all.Where(r => (r.name ?? "").ToLowerInvariant().Contains(q));

        Roles.Clear();
        foreach (var r in src) Roles.Add(r);
    }

    private async void AddRole_Clicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync($"{nameof(RoleEditorPage)}?mode=create");

    private async void EditSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.BindingContext is RoleListItem item)
            await Shell.Current.GoToAsync($"{nameof(RoleEditorPage)}?mode=edit&id={item.id}");
    }

    private async void DeleteSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.BindingContext is not RoleListItem item) return;

        var ok = await DisplayAlert("Eliminar rol", $"¿Eliminar “{item.name}”?", "Sí", "Cancelar");
        if (!ok) return;

        try
        {
            // chequeo de red antes de llamar
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.DeleteAsync($"/api/roles/{item.id}");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthHelper.VerificarYRedirigirSiExpirado(this);
                    return;
                }

                if (resp.StatusCode == HttpStatusCode.ServiceUnavailable ||
                    resp.StatusCode == HttpStatusCode.GatewayTimeout)
                {
                    await ErrorHandler.MostrarErrorUsuario("El servidor no responde.");
                    return;
                }

                await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
                return;
            }

            Roles.Remove(item);
            _all.RemoveAll(r => r.id == item.id);
        }
        catch (TaskCanceledException)
        {
            await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado. El servidor no responde.");
        }
        catch (HttpRequestException)
        {
            await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Roles – Eliminar");
        }
    }

    private async void RolesRefresh_Refreshing(object sender, EventArgs e)
    {
        try { await CargarRolesAsync(); }
        finally { IsRefreshing = false; }
    }

    private async void Retry_Clicked(object sender, EventArgs e)
    {
        IsRefreshing = true;
        await CargarRolesAsync();
        IsRefreshing = false;
    }
}
