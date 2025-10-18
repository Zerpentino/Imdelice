using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Imdeliceapp.Helpers;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Pages;

public partial class AdminMenuPage : ContentPage
{
	public bool CanRead => Perms.MenusRead;
	public bool CanCreate => Perms.MenusCreate;
	public bool CanUpdate => Perms.MenusUpdate;
	public bool CanDelete => Perms.MenusDelete;
	public bool ShowTrashFab => (CanUpdate || CanDelete) && _trash.Count > 0;
	public bool FormCardVisible => CanCreate || CanUpdate;

	#region DTOs
	class ApiEnvelope<T>
	{
		public object? error { get; set; }
		public T? data { get; set; }
		public string? message { get; set; }
	}

	class MenuDTO : INotifyPropertyChanged
	{
		int _id;
		string _name = string.Empty;
		bool _isActive;
		DateTime? _publishedAt;
		int? _version;
		DateTime _createdAt;
		DateTime _updatedAt;
		DateTime? _deletedAt;

		public int id { get => _id; set => SetField(ref _id, value); }
		public string name { get => _name; set => SetField(ref _name, value); }
		public bool isActive { get => _isActive; set => SetField(ref _isActive, value); }
		public DateTime? publishedAt { get => _publishedAt; set => SetField(ref _publishedAt, value); }
		public int? version { get => _version; set => SetField(ref _version, value); }
		public DateTime createdAt { get => _createdAt; set => SetField(ref _createdAt, value); }
		public DateTime updatedAt { get => _updatedAt; set => SetField(ref _updatedAt, value); }
		public DateTime? deletedAt { get => _deletedAt; set => SetField(ref _deletedAt, value); }

		[JsonIgnore]
		public string VersionDisplay => version.HasValue ? $"Versión {version}" : "Sin versión";

		[JsonIgnore]
		public string DeletedAtDisplay => deletedAt?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "Fecha no disponible";

		public event PropertyChangedEventHandler? PropertyChanged;

		void OnPropertyChanged([CallerMemberName] string? name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
				return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}
	}
	#endregion

	static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

	readonly ObservableCollection<MenuDTO> _menus = new();
	readonly ObservableCollection<MenuDTO> _trash = new();

	MenuDTO? _lastOpened;
	bool _silenceSwitch;
	readonly HashSet<int> _busyToggles = new();

	public AdminMenuPage()
	{
		InitializeComponent();
		MenusCV.ItemsSource = _menus;
		_trash.CollectionChanged += (_, __) => OnPropertyChanged(nameof(ShowTrashFab));
		UpdateFormMode();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		OnPropertyChanged(nameof(CanRead));
		OnPropertyChanged(nameof(CanCreate));
		OnPropertyChanged(nameof(CanUpdate));
		OnPropertyChanged(nameof(CanDelete));
		OnPropertyChanged(nameof(FormCardVisible));
		OnPropertyChanged(nameof(ShowTrashFab));
		UpdateFormMode();

		if (!CanRead)
		{
			await DisplayAlert("Acceso restringido", "No tienes permiso para ver menús.", "OK");
			await Shell.Current.GoToAsync("..");
			return;
		}

		await CargarTodoAsync();
	}

	#region Helpers comunes
	static async Task<string?> GetTokenAsync()
	{
		var secure = await SecureStorage.GetAsync("token");
		if (!string.IsNullOrWhiteSpace(secure))
			return secure;

		var stored = Preferences.Default.Get("token", string.Empty);
		return string.IsNullOrWhiteSpace(stored) ? null : stored;
	}

	static HttpClient NewAuthClient(string baseUrl, string token)
	{
		var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
		cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		return cli;
	}

	void MostrarServidorNoDisponible() => EmptyLbl.Text = "Servidor no disponible. Reintenta.";

	int CompareMenus(MenuDTO a, MenuDTO b)
	{
		int c = b.isActive.CompareTo(a.isActive); // Activos primero
		if (c != 0) return c;
		return string.Compare(a.name, b.name, StringComparison.CurrentCultureIgnoreCase);
	}

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
		Dispatcher.Dispatch(() =>
		{
			if (_menus.Count == 0) return;

			var old = _menus.IndexOf(item);
			if (old < 0) return;

			var target = FindInsertIndexSkipping(old, item);
			if (target > old) target--;

			target = Math.Clamp(target, 0, _menus.Count - 1);

			if (target != old)
				_menus.Move(old, target);
		});
	}
	#endregion

	async Task CargarTodoAsync()
	{
		try
		{
			if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
			{
				MostrarServidorNoDisponible();
				_trash.Clear();
				OnPropertyChanged(nameof(ShowTrashFab));
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

			ExitEditMode();

			await CargarMenusAsync(token, baseUrl);
			await CargarPapeleraAsync(token, baseUrl);
			OnPropertyChanged(nameof(ShowTrashFab));
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Cargar todo");
		}
	}

	async Task CargarMenusAsync(string token, string baseUrl)
	{
		try
		{
			_menus.Clear();
			EmptyLbl.Text = "No hay menús";

			using var http = NewAuthClient(baseUrl, token);

			var resp = await http.GetAsync("/api/menus");
			if (resp.StatusCode == HttpStatusCode.Forbidden)
			{
				await DisplayAlert("Acceso restringido", "No tienes permiso para ver menús.", "OK");
				await Shell.Current.GoToAsync("..");
				return;
			}

			var body = await resp.Content.ReadAsStringAsync();

			if (!resp.IsSuccessStatusCode)
			{
				if (resp.StatusCode == HttpStatusCode.Unauthorized)
				{
					await AuthHelper.VerificarYRedirigirSiExpirado(this);
					return;
				}

				if (resp.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
					MostrarServidorNoDisponible();

				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}

			var env = JsonSerializer.Deserialize<ApiEnvelope<List<MenuDTO>>>(body, _json);
			foreach (var m in (env?.data ?? new())
			         .OrderByDescending(x => x.isActive)
			         .ThenBy(x => x.name, StringComparer.CurrentCultureIgnoreCase))
			{
				_menus.Add(m);
			}
		}
		catch (TaskCanceledException)
		{
			MostrarServidorNoDisponible();
			await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado.");
		}
		catch (HttpRequestException)
		{
			MostrarServidorNoDisponible();
			await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
		}
		catch (Exception ex)
		{
			MostrarServidorNoDisponible();
			await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Cargar");
		}
	}

	async Task CargarPapeleraAsync(string token, string baseUrl)
	{
		try
		{
			using var http = NewAuthClient(baseUrl, token);
			var resp = await http.GetAsync("/api/menus/trash");
			var body = await resp.Content.ReadAsStringAsync();

			if (resp.StatusCode == HttpStatusCode.Forbidden)
			{
				_trash.Clear();
				return;
			}

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

			var env = JsonSerializer.Deserialize<ApiEnvelope<List<MenuDTO>>>(body, _json);
			_trash.Clear();
			foreach (var item in (env?.data ?? new())
			         .OrderByDescending(x => x.deletedAt ?? DateTime.MinValue))
			{
				_trash.Add(item);
			}
			OnPropertyChanged(nameof(ShowTrashFab));
		}
		catch (TaskCanceledException)
		{
			await ErrorHandler.MostrarErrorUsuario("Tiempo de espera agotado al cargar la papelera.");
		}
		catch (HttpRequestException)
		{
			await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Papelera");
		}
	}

	void MenusCV_SelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (!CanUpdate)
			return;

		var selected = e.CurrentSelection?.FirstOrDefault() as MenuDTO;
		if (selected is null)
		{
			ExitEditMode();
			return;
		}

		EnterEditMode(selected);
	}

	async void Refresh_Refreshing(object sender, EventArgs e)
	{
		try
		{
			await CargarTodoAsync();
		}
		finally
		{
			Refresh.IsRefreshing = false;
		}
	}

	async void Retry_Clicked(object sender, EventArgs e) => await CargarTodoAsync();

	async void SubmitMenu_Clicked(object sender, EventArgs e)
	{
		var name = TxtMenuName.Text?.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			await DisplayAlert("Menú", "Escribe un nombre.", "OK");
			return;
		}

		if (name.Length < 2)
		{
			await DisplayAlert("Menú", "El nombre debe tener al menos 2 caracteres.", "OK");
			return;
		}

		if (_lastOpened is null)
		{
			if (!CanCreate)
			{
				await DisplayAlert("Acceso restringido", "No puedes crear menús.", "OK");
				return;
			}

			if (NameExists(name))
			{
				await DisplayAlert("Duplicado", "Ya existe un menú con ese nombre.", "OK");
				return;
			}

			await CrearMenuAsync(name);
		}
		else
		{
			if (!CanUpdate)
			{
				await DisplayAlert("Acceso restringido", "No puedes renombrar menús.", "OK");
				return;
			}

			if (string.Equals(name, _lastOpened.name, StringComparison.OrdinalIgnoreCase))
			{
				await DisplayAlert("Menú", "Usa un nombre distinto para actualizar.", "OK");
				return;
			}

			if (NameExists(name, _lastOpened.id))
			{
				await DisplayAlert("Duplicado", "Ya existe otro menú con ese nombre.", "OK");
				return;
			}

			await RenombrarMenuAsync(_lastOpened, name);
		}
	}

	async void Abrir_Clicked(object sender, EventArgs e)
	{
		if ((sender as Button)?.BindingContext is not MenuDTO m) return;
		if (CanUpdate)
			EnterEditMode(m);

		await Shell.Current.GoToAsync($"{nameof(MenuSectionsPage)}?menuId={m.id}&menuName={Uri.EscapeDataString(m.name)}");
	}

	async void ToggleActivo_Toggled(object sender, ToggledEventArgs e)
	{
		if (_silenceSwitch) return;
		if (sender is not Switch sw) return;
		if (sw.BindingContext is not MenuDTO m) return;

		var nuevo = e.Value;
		var anterior = m.isActive;

		if (!CanUpdate)
		{
			_silenceSwitch = true;
			sw.IsToggled = anterior;
			_silenceSwitch = false;
			await DisplayAlert("Acceso restringido", "No puedes actualizar menús.", "OK");
			return;
		}

		if (_busyToggles.Contains(m.id))
		{
			_silenceSwitch = true;
			sw.IsToggled = anterior;
			_silenceSwitch = false;
			return;
		}

		_busyToggles.Add(m.id);

		m.isActive = nuevo;
		MoveKeepingSort(m);

		sw.IsEnabled = false;
		try
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			var resp = await http.PatchAsync($"/api/menus/{m.id}", JsonContent.Create(new { isActive = nuevo }));
			var body = await resp.Content.ReadAsStringAsync();

			if (resp.StatusCode == HttpStatusCode.Forbidden)
			{
				_silenceSwitch = true;
				m.isActive = anterior;
				sw.IsToggled = anterior;
				_silenceSwitch = false;
				MoveKeepingSort(m);
				await DisplayAlert("Acceso restringido", "No tienes permiso para actualizar menús.", "OK");
				return;
			}

			if (!resp.IsSuccessStatusCode)
			{
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
		if (!CanDelete) { await DisplayAlert("Acceso restringido", "No puedes eliminar menús.", "OK"); return; }
		if ((sender as ImageButton)?.BindingContext is not MenuDTO m) return;

		var confirm = await DisplayAlert("Enviar a papelera",
			$"¿Quieres enviar “{m.name}” a la papelera?",
			"Sí, enviar",
			"Cancelar");
		if (!confirm) return;

		try
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			var resp = await http.DeleteAsync($"/api/menus/{m.id}");
			if (resp.StatusCode == HttpStatusCode.Forbidden)
			{
				await DisplayAlert("Acceso restringido", "No tienes permiso para eliminar menús.", "OK");
				return;
			}

			if (!resp.IsSuccessStatusCode)
			{
				var body = await resp.Content.ReadAsStringAsync();
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}

			await CargarTodoAsync();
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Eliminar");
		}
	}

	async void TrashFab_Clicked(object sender, EventArgs e)
	{
		if (_trash.Count == 0) return;

		var options = _trash
			.Select(m => $"{m.name} • {m.DeletedAtDisplay}")
			.ToArray();

		var choice = await DisplayActionSheet("Papelera de menús", "Cerrar", null, options);
		if (string.IsNullOrWhiteSpace(choice) || choice == "Cerrar")
			return;

		var index = Array.IndexOf(options, choice);
		if (index < 0) return;
		var selected = _trash[index];

		var actions = new List<string>();
		if (CanUpdate) actions.Add("Restaurar");
		if (CanDelete) actions.Add("Eliminar definitivamente");

		if (actions.Count == 0)
		{
			await DisplayAlert("Papelera", "No puedes administrar la papelera con tu rol actual.", "OK");
			return;
		}

		var action = await DisplayActionSheet(selected.name, "Cancelar", null, actions.ToArray());
		if (string.IsNullOrWhiteSpace(action) || action == "Cancelar")
			return;

		switch (action)
		{
			case "Restaurar":
				await RestaurarDesdePapeleraAsync(selected);
				break;
			case "Eliminar definitivamente":
				await EliminarDefinitivoAsync(selected);
				break;
		}
	}

	void CancelEdit_Clicked(object sender, EventArgs e) => ExitEditMode();

	bool NameExists(string name, int? ignoreId = null)
	{
		bool ExistsIn(IEnumerable<MenuDTO> src) =>
			src.Any(m => m.id != ignoreId && string.Equals(m.name, name, StringComparison.OrdinalIgnoreCase));

		return ExistsIn(_menus) || ExistsIn(_trash);
	}

	void EnterEditMode(MenuDTO menu)
	{
		_lastOpened = menu;
		TxtMenuName.Text = menu.name;
		MenusCV.SelectedItem = menu;
		UpdateFormMode();
	}

	void ExitEditMode()
	{
		_lastOpened = null;
		TxtMenuName.Text = string.Empty;
		MenusCV.SelectedItem = null;
		UpdateFormMode();
	}

	void UpdateFormMode()
	{
		var editing = _lastOpened is not null;
		if (editing)
		{
			FormTitle.Text = "Renombrar menú";
			TxtMenuName.Placeholder = "Nuevo nombre del menú";
		}
		else
		{
			FormTitle.Text = CanCreate ? "Crear nuevo menú" : "Selecciona un menú para renombrar";
			TxtMenuName.Placeholder = CanCreate ? "Nombre del nuevo menú" : "Selecciona un menú para editar";
		}

		BtnSubmit.Text = editing ? "Guardar" : "Crear";
		BtnCancelEdit.IsVisible = editing && CanUpdate;
		var canInteract = editing ? CanUpdate : CanCreate;
		BtnSubmit.IsEnabled = canInteract;
		TxtMenuName.IsEnabled = canInteract;
	}

	async Task CrearMenuAsync(string name)
	{
		try
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			var payload = JsonContent.Create(new { name, isActive = true });
			var resp = await http.PostAsync("/api/menus", payload);
			var body = await resp.Content.ReadAsStringAsync();

			if (resp.StatusCode == HttpStatusCode.Forbidden)
			{
				await DisplayAlert("Acceso restringido", "No tienes permiso para crear menús.", "OK");
				return;
			}

			if (!resp.IsSuccessStatusCode)
			{
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}

			await CargarTodoAsync();
			await DisplayAlert("Listo", "Menú creado correctamente.", "OK");
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Crear");
		}
	}

	async Task RenombrarMenuAsync(MenuDTO menu, string newName)
	{
		var oldName = menu.name;
		menu.name = newName;
		MoveKeepingSort(menu);

		try
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			var resp = await http.PatchAsync($"/api/menus/{menu.id}", JsonContent.Create(new { name = newName }));
			var body = await resp.Content.ReadAsStringAsync();

			if (resp.StatusCode == HttpStatusCode.Forbidden)
			{
				menu.name = oldName;
				MoveKeepingSort(menu);
				TxtMenuName.Text = menu.name;
				UpdateFormMode();
				await DisplayAlert("Acceso restringido", "No tienes permiso para actualizar menús.", "OK");
				return;
			}

			if (!resp.IsSuccessStatusCode)
			{
				menu.name = oldName;
				MoveKeepingSort(menu);
				TxtMenuName.Text = menu.name;
				UpdateFormMode();
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}

			await DisplayAlert("Listo", "Nombre actualizado.", "OK");
			ExitEditMode();
		}
		catch (Exception ex)
		{
			menu.name = oldName;
			MoveKeepingSort(menu);
			TxtMenuName.Text = menu.name;
			UpdateFormMode();
			await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Renombrar");
		}
	}

	async Task RestaurarDesdePapeleraAsync(MenuDTO m)
	{
		if (!CanUpdate)
		{
			await DisplayAlert("Acceso restringido", "No puedes restaurar menús.", "OK");
			return;
		}

		try
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
			var resp = await http.PatchAsync($"/api/menus/{m.id}/restore", content);
			if (resp.StatusCode == HttpStatusCode.NoContent || resp.IsSuccessStatusCode)
			{
				await CargarTodoAsync();
				return;
			}

			var body = await resp.Content.ReadAsStringAsync();
			await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Restaurar");
		}
	}

	async Task EliminarDefinitivoAsync(MenuDTO m)
	{
		if (!CanDelete)
		{
			await DisplayAlert("Acceso restringido", "No puedes eliminar menús.", "OK");
			return;
		}

		var confirm = await DisplayAlert("Eliminar definitivamente",
			$"Esto eliminará “{m.name}” de forma permanente. ¿Continuar?",
			"Eliminar",
			"Cancelar");
		if (!confirm) return;

		try
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			var resp = await http.DeleteAsync($"/api/menus/{m.id}?hard=true");
			if (!resp.IsSuccessStatusCode)
			{
				var body = await resp.Content.ReadAsStringAsync();
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}

			await CargarTodoAsync();
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, "Menús – Eliminar definitivo");
		}
	}
}
