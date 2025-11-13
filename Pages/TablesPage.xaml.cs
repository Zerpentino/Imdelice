using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Linq;
using Imdeliceapp.Helpers;
using Imdeliceapp.Services;
using TableDTO = Imdeliceapp.Models.TableDTO;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Pages;

class ApiEnvelopeTables<T>
{
    public T? data { get; set; }
    public string? error { get; set; }
    public string? message { get; set; }
}

public class TableListItem
{
    public int id { get; set; }
    public string name { get; set; } = string.Empty;
    public int seats { get; set; }
    public bool isActive { get; set; }

    public string StatusText => $"{seats} lugares • {(isActive ? "Activa" : "Inactiva")}";
    public Color StatusColor => isActive ? Color.FromArgb("#388E3C") : Color.FromArgb("#B00020");
}

public partial class TablesPage : ContentPage
{
    public bool CanRead => Perms.TablesRead;
    public bool CanCreate => Perms.TablesCreate;
    public bool CanUpdate => Perms.TablesUpdate;
    public bool CanDelete => Perms.TablesDelete;

    static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ObservableCollection<TableListItem> Tables { get; } = new();
    readonly List<TableListItem> _all = new();

    string _emptyMessage = "No hay mesas registradas.";
    public string EmptyMessage
    {
        get => _emptyMessage;
        set
        {
            _emptyMessage = value;
            OnPropertyChanged(nameof(EmptyMessage));
        }
    }

    bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set
        {
            _isRefreshing = value;
            OnPropertyChanged(nameof(IsRefreshing));
        }
    }

    public TablesPage()
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
            await DisplayAlert("Acceso restringido", "No tienes permiso para ver mesas.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        await LoadTablesAsync();
    }

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s)) return s;
        var p = Preferences.Default.Get("token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }

    static HttpClient NewAuthClient(string baseUrl, string token)
    {
        var cli = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };
        cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cli;
    }

    async Task LoadTablesAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                EmptyMessage = "Sin conexión a Internet.";
                Tables.Clear();
                await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
                return;
            }

            Tables.Clear();
            _all.Clear();
            EmptyMessage = "No hay mesas registradas.";

            var token = await GetTokenAsync();
            if (string.IsNullOrWhiteSpace(token))
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
            using var http = NewAuthClient(baseUrl, token);

            var resp = await http.GetAsync("/api/tables?includeInactive=true");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthHelper.VerificarYRedirigirSiExpirado(this);
                    return;
                }

                if (resp.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
                {
                    EmptyMessage = "El servidor no responde.";
                    Tables.Clear();
                    await ErrorHandler.MostrarErrorUsuario("El servidor no responde.");
                    return;
                }

                await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
                return;
            }

            var env = JsonSerializer.Deserialize<ApiEnvelopeTables<List<TableDTO>>>(body, _json);
            var list = env?.data ?? new();
            var ordered = list
                .OrderByDescending(t => t.isActive)
                .ThenBy(t => (t.name ?? string.Empty).ToLowerInvariant())
                .ToList();

            foreach (var t in ordered)
            {
                var item = new TableListItem
                {
                    id = t.id,
                    name = t.name ?? $"Mesa #{t.id}",
                    seats = t.seats,
                    isActive = t.isActive
                };
                _all.Add(item);
                Tables.Add(item);
            }

            if (Tables.Count == 0)
                EmptyMessage = "No hay mesas registradas.";
        }
        catch (TaskCanceledException)
        {
            EmptyMessage = "El servidor no responde.";
            Tables.Clear();
            await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado. El servidor no responde.");
        }
        catch (HttpRequestException)
        {
            EmptyMessage = "No se pudo contactar al servidor.";
            Tables.Clear();
            await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
        }
        catch (Exception ex)
        {
            EmptyMessage = "Ocurrió un error al cargar las mesas.";
            Tables.Clear();
            await ErrorHandler.MostrarErrorTecnico(ex, "Mesas – Cargar");
        }
    }

    void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = (e.NewTextValue ?? string.Empty).Trim().ToLowerInvariant();
        IEnumerable<TableListItem> src = _all;
        if (!string.IsNullOrWhiteSpace(q))
            src = src.Where(t => (t.name ?? string.Empty).ToLowerInvariant().Contains(q));

        Tables.Clear();
        foreach (var item in src)
            Tables.Add(item);
    }

    async void AddTable_Clicked(object sender, EventArgs e)
    {
        if (!CanCreate)
        {
            await DisplayAlert("Acceso restringido", "No puedes crear mesas.", "OK");
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(TableEditorPage)}?mode=create");
    }

    async void EditSwipe_Invoked(object sender, EventArgs e)
    {
        if (!CanUpdate)
        {
            await DisplayAlert("Acceso restringido", "No puedes editar mesas.", "OK");
            return;
        }

        if ((sender as SwipeItem)?.BindingContext is TableListItem item)
        {
            await Shell.Current.GoToAsync($"{nameof(TableEditorPage)}?mode=edit&id={item.id}");
        }
    }

    async void DeleteSwipe_Invoked(object sender, EventArgs e)
    {
        if (!CanDelete)
        {
            await DisplayAlert("Acceso restringido", "No puedes eliminar mesas.", "OK");
            return;
        }

        if ((sender as SwipeItem)?.BindingContext is not TableListItem item) return;

        var action = await DisplayActionSheet($"¿Qué deseas hacer con “{item.name}”?",
            "Cancelar", null,
            "Inactivar mesa", "Eliminar permanentemente");

        if (action is null or "Cancelar") return;

        var hard = action == "Eliminar permanentemente";
        await DeleteTableAsync(item, hard);
    }

    async Task DeleteTableAsync(TableListItem item, bool hardDelete)
    {
        try
        {
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

            var suffix = hardDelete ? "?hard=true" : string.Empty;
            var resp = await http.DeleteAsync($"/api/tables/{item.id}{suffix}");

            if (resp.StatusCode == HttpStatusCode.NoContent || resp.IsSuccessStatusCode)
            {
                await LoadTablesAsync();
                return;
            }

            var body = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                await AuthHelper.VerificarYRedirigirSiExpirado(this);
                return;
            }

            await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
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
            await ErrorHandler.MostrarErrorTecnico(ex, "Mesas – Eliminar");
        }
    }

    async void TablesRefresh_Refreshing(object sender, EventArgs e)
    {
        try { await LoadTablesAsync(); }
        finally { IsRefreshing = false; }
    }

    async void Retry_Clicked(object sender, EventArgs e)
    {
        IsRefreshing = true;
        await LoadTablesAsync();
        IsRefreshing = false;
    }
}
