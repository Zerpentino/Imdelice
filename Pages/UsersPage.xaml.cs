using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Imdeliceapp.Helpers;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking; // Connectivity

namespace Imdeliceapp.Pages;

#region DTOs API
class RoleDTO { public int id { get; set; } public string? name { get; set; } public string? description { get; set; } }
class UserDTO
{
    public int id { get; set; }
    public string? email { get; set; }
    public string? name { get; set; }
    public int roleId { get; set; }
    public RoleDTO? role { get; set; }
}
class ApiEnvelope<T>
{
    public object? error { get; set; }
    public T? data { get; set; }
    public string? message { get; set; }
}
#endregion

public class UserListItem
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string email { get; set; } = "";
    public string role { get; set; } = "";  // "Admin" | "Mesero"
    public bool active { get; set; } = true; // (placeholder para futuro)

    public string Initials => string.Join("", (name ?? "")
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(s => char.ToUpperInvariant(s[0])));
}

public partial class UsersPage : ContentPage
{
    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ObservableCollection<UserListItem> Users { get; } = new();
    private List<UserListItem> _all = new(); // fuente para el filtro
                                             // mensaje mostrado en EmptyView
    string _emptyMessage = "No hay usuarios";
    public string EmptyMessage
    {
        get => _emptyMessage;
        set { _emptyMessage = value; OnPropertyChanged(nameof(EmptyMessage)); }
    }

    // estado de pull-to-refresh
    bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set { _isRefreshing = value; OnPropertyChanged(nameof(IsRefreshing)); }
    }
    public UsersPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarUsuariosAsync();
    }

    #region Helpers HTTP
    void MostrarServidorNoDisponible()
    {
        EmptyMessage = "Servidor no disponible. Revisa tu red o inténtalo más tarde.";
        Users.Clear(); // fuerza a que se vea el EmptyView con ese mensaje
    }

    void MostrarEmptyPorDefecto()
    {
        EmptyMessage = "No hay usuarios";
    }

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s))
            return s;

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

    static string RoleName(UserDTO u)
        => u.role?.name ?? (u.roleId == 1 ? "Admin" : u.roleId == 2 ? "Mesero" : $"Rol {u.roleId}");
    #endregion

    private async Task CargarUsuariosAsync()
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

            Users.Clear();
            _all.Clear();
            MostrarEmptyPorDefecto();

            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.GetAsync("/api/users");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthHelper.VerificarYRedirigirSiExpirado(this);
                    return;
                }
                // 503 u otros errores del servidor => cambia EmptyView
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

            var env = JsonSerializer.Deserialize<ApiEnvelope<List<UserDTO>>>(body, _json);
            var list = env?.data ?? new List<UserDTO>();

            _all = list.Select(u => new UserListItem
            {
                id = u.id,
                name = u.name ?? "",
                email = u.email ?? "",
                role = RoleName(u),
                active = true
            }).ToList();

            foreach (var item in _all) Users.Add(item);
            // si la API contestó OK pero vacía, el EmptyView mostrará "No hay usuarios"
            if (Users.Count == 0) MostrarEmptyPorDefecto();
        }
        catch (TaskCanceledException) // timeout
        {
            MostrarServidorNoDisponible();
            await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado. El servidor no responde.");
        }
        catch (HttpRequestException) // fallo de conexión/sin servidor
        {
            MostrarServidorNoDisponible();
            await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
        }
        catch (Exception ex)
        {
            // errores inesperados => conserva tu handler, pero usa el Empty message amable
            MostrarServidorNoDisponible();
            await ErrorHandler.MostrarErrorTecnico(ex, "Users – CargarUsuarios");
        }
    
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? "").Trim().ToLowerInvariant();
        IEnumerable<UserListItem> src = _all;

        if (!string.IsNullOrEmpty(q))
            src = _all.Where(u =>
                (u.name ?? "").ToLowerInvariant().Contains(q) ||
                (u.email ?? "").ToLowerInvariant().Contains(q));

        Users.Clear();
        foreach (var u in src) Users.Add(u);
    }

    private async void AddUser_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(UserEditorPage)}?mode=create");
    }

    private async void EditSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.BindingContext is UserListItem item)
            await Shell.Current.GoToAsync($"{nameof(UserEditorPage)}?mode=edit&id={item.id}");
    }

    private async void DeleteSwipe_Invoked(object sender, EventArgs e)
    {
        if ((sender as SwipeItem)?.BindingContext is not UserListItem item) return;

        var ok = await DisplayAlert("Eliminar usuario",
            $"¿Eliminar a {item.name}?", "Sí", "Cancelar");
        if (!ok) return;

        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.DeleteAsync($"/api/users/{item.id}");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthHelper.VerificarYRedirigirSiExpirado(this);
                    return;
                }

                await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
                return;
            }

            // ok
            Users.Remove(item);
            _all.RemoveAll(u => u.id == item.id);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Users – Delete");
        }
    }
    private async void UsersRefresh_Refreshing(object sender, EventArgs e)
{
    try { await CargarUsuariosAsync(); }
    finally { IsRefreshing = false; }
}

private async void Retry_Clicked(object sender, EventArgs e)
{
    IsRefreshing = true;
    await CargarUsuariosAsync();
    IsRefreshing = false;
}

}
