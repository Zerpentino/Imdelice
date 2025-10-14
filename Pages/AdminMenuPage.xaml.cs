using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using Imdeliceapp.Helpers;
using Imdeliceapp.Model;
using Microsoft.Maui.Controls;
using System.Text;
using System.Net.Http;       // por si no estaba
using System.Net.Http.Json;  // <- aquí vive JsonContent


namespace Imdeliceapp.Pages;

public partial class AdminMenuPage : ContentPage
{
	#region DTOs
	class ApiEnvelope<T> { public object? error { get; set; } public T? data { get; set; } public string? message { get; set; } }
	class MenuDTO { public int id { get; set; } public string name { get; set; } = ""; public bool isActive { get; set; } }
	#endregion

	static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
	readonly ObservableCollection<MenuDTO> _menus = new();
	MenuDTO? _lastOpened; // para renombrar
	
bool _silenceSwitch;               // evita loops en Toggled
readonly HashSet<int> _busyToggles = new(); // evita doble PATCH

int CompareMenus(MenuDTO a, MenuDTO b)
{
    int c = b.isActive.CompareTo(a.isActive); // Activos primero
    if (c != 0) return c;
    return string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase);
}

// Busca el índice de inserción ignorando la posición 'skipIndex' (el propio item)
int FindInsertIndexSkipping(int skipIndex, MenuDTO item)
{
    for (int i = 0; i < _menus.Count; i++)
    {
        if (i == skipIndex) continue;
        if (CompareMenus(item, _menus[i]) < 0)
            return i;
    }
    return _menus.Count;
}

void MoveKeepingSort(MenuDTO item)
{
    // Deferimos al siguiente frame de UI para no chocar con el layout del RecyclerView
    Dispatcher.Dispatch(() =>
    {
        if (_menus.Count == 0) return;

        var old = _menus.IndexOf(item);
        if (old < 0) return;

        var target = FindInsertIndexSkipping(old, item);

        // Si el target quedó a la derecha del old, al remover el old se corre una posición
        if (target > old) target--;

        // Normalizamos rangos
        if (target < 0) target = 0;
        if (target >= _menus.Count) target = _menus.Count - 1;

        if (target != old)
            _menus.Move(old, target); // <- un solo "move" para el adaptador
    });
}




	public AdminMenuPage()
	{
		InitializeComponent();
		MenusCV.ItemsSource = _menus;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await CargarMenusAsync();
	}

	#region Helpers comunes
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
	void MostrarServidorNoDisponible() => EmptyLbl.Text = "Servidor no disponible. Reintenta.";
	#endregion

	async Task CargarMenusAsync()
	{
		try
		{
			if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
			{
				MostrarServidorNoDisponible();
				await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
				return;
			}

			_menus.Clear();
			EmptyLbl.Text = "No hay menús";

			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }

			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			var resp = await http.GetAsync("/api/menus");
			var body = await resp.Content.ReadAsStringAsync();

			if (!resp.IsSuccessStatusCode)
			{
				if (resp.StatusCode == HttpStatusCode.Unauthorized) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
				if (resp.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout) MostrarServidorNoDisponible();
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}

			var env = JsonSerializer.Deserialize<ApiEnvelope<List<MenuDTO>>>(body, _json);
foreach (var m in (env?.data ?? new())
         .OrderByDescending(x => x.isActive)
         .ThenBy(x => x.name, StringComparer.CurrentCultureIgnoreCase))
    _menus.Add(m);

		}
		catch (TaskCanceledException) { MostrarServidorNoDisponible(); await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado."); }
		catch (HttpRequestException) { MostrarServidorNoDisponible(); await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor."); }
		catch (Exception ex) { MostrarServidorNoDisponible(); await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Cargar"); }
	}
	void MenusCV_SelectionChanged(object? sender, SelectionChangedEventArgs e)
{
    _lastOpened = e.CurrentSelection?.FirstOrDefault() as MenuDTO;
    TxtRename.Text = _lastOpened?.name ?? "";
}


	async void Refresh_Refreshing(object sender, EventArgs e)
	{ try { await CargarMenusAsync(); } finally { Refresh.IsRefreshing = false; } }

	async void Retry_Clicked(object sender, EventArgs e) => await CargarMenusAsync();

	async void CrearMenu_Clicked(object sender, EventArgs e)
	{
		var name = TxtNewMenu.Text?.Trim();
		if (string.IsNullOrWhiteSpace(name)) { await DisplayAlert("Menú", "Escribe un nombre.", "OK"); return; }

		try
		{
			var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			var payload = JsonContent.Create(new { name, isActive = true });
			var resp = await http.PostAsync("/api/menus", payload);
			var body = await resp.Content.ReadAsStringAsync();
			if (!resp.IsSuccessStatusCode)
			{ await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body)); return; }

			TxtNewMenu.Text = "";
			await CargarMenusAsync();
		}
		catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Crear"); }
	}

	async void Abrir_Clicked(object sender, EventArgs e)
	{
		if ((sender as Button)?.BindingContext is not MenuDTO m) return;
		_lastOpened = m;
		TxtRename.Text = m.name;
		await Shell.Current.GoToAsync($"{nameof(MenuSectionsPage)}?menuId={m.id}&menuName={Uri.EscapeDataString(m.name)}");
	}

async void Renombrar_Clicked(object sender, EventArgs e)
{
    if (_lastOpened is null) { await DisplayAlert("Menú", "Selecciona un menú (tócala) o presiona Abrir.", "OK"); return; }
    var newName = TxtRename.Text?.Trim();
    if (string.IsNullOrWhiteSpace(newName)) { await DisplayAlert("Menú", "Escribe un nombre.", "OK"); return; }

    var oldName = _lastOpened.name;

    // UI optimista + reorden por nombre
    _lastOpened.name = newName;
    MoveKeepingSort(_lastOpened);

    try
    {
        var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
        var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
        using var http = NewAuthClient(baseUrl, token);

        var resp = await http.PatchAsync($"/api/menus/{_lastOpened.id}", JsonContent.Create(new { name = newName }));
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            // revertir
            _lastOpened.name = oldName;
            MoveKeepingSort(_lastOpened);
            await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
            return;
        }
        await DisplayAlert("Listo", "Nombre actualizado.", "OK");
    }
    catch (Exception ex)
    {
        // revertir
        _lastOpened.name = oldName;
        MoveKeepingSort(_lastOpened);
        await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Renombrar");
    }
}

async void ToggleActivo_Toggled(object sender, ToggledEventArgs e)
{
    if (_silenceSwitch) return;
    if (sender is not Switch sw) return;
    if (sw.BindingContext is not MenuDTO m) return;

    var nuevo = e.Value;
    var anterior = m.isActive;

    // Evitar dobles toques/llamadas
    if (_busyToggles.Contains(m.id))
    {
        _silenceSwitch = true; sw.IsToggled = anterior; _silenceSwitch = false;
        return;
    }
    _busyToggles.Add(m.id);

    // UI optimista + reorden
    m.isActive = nuevo;
    MoveKeepingSort(m);

    sw.IsEnabled = false;
    try
    {
        var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
        var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
        using var http = NewAuthClient(baseUrl, token);

        var resp = await http.PatchAsync($"/api/menus/{m.id}", JsonContent.Create(new { isActive = nuevo }));
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            // Revertir si falla
            _silenceSwitch = true;
            m.isActive = anterior;
            sw.IsToggled = anterior;
            _silenceSwitch = false;
            MoveKeepingSort(m);

            await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
        }
    }
    catch (Exception ex)
    {
        // Revertir en error
        _silenceSwitch = true;
        m.isActive = anterior;
        sw.IsToggled = anterior;
        _silenceSwitch = false;
        MoveKeepingSort(m);

        await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Activar/Inactivar");
    }
    finally
    {
        _busyToggles.Remove(m.id);
        sw.IsEnabled = true;
    }
}

	async void Eliminar_Clicked(object sender, EventArgs e)
	{
		if ((sender as ImageButton)?.BindingContext is not MenuDTO m) return;
		var hard = await DisplayAlert("Eliminar", $"¿Eliminar “{m.name}” definitivamente?", "Hard delete", "Soft delete");
		var url = hard ? $"/api/menus/{m.id}?hard=true" : $"/api/menus/{m.id}";

		try
		{
			var token = await GetTokenAsync(); if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			var resp = await http.DeleteAsync(url);
			if (!resp.IsSuccessStatusCode)
			{
				var body = await resp.Content.ReadAsStringAsync();
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}
			await CargarMenusAsync();
		}
		catch (Exception ex) { await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Eliminar"); }
	}
}
