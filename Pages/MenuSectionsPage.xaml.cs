using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Imdeliceapp.Helpers;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(MenuId), "menuId")]
[QueryProperty(nameof(MenuName), "menuName")]
public partial class MenuSectionsPage : ContentPage
{
	public int MenuId { get; set; }

	string? _menuName;
	public string? MenuName
	{
		get => _menuName;
		set
		{
			_menuName = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(Titulo));
		}
	}
	public string Titulo => $"Secciones – {MenuName}";

	public bool CanRead => Perms.MenusRead;
	public bool CanCreate => Perms.MenusUpdate;
	public bool CanUpdate => Perms.MenusUpdate;
	public bool CanDelete => Perms.MenusDelete;
	public bool ShowTrashFab => (CanUpdate || CanDelete) && _trash.Count > 0;

	#region DTOs
	class ApiEnvelope<T>
	{
		public object? error { get; set; }
		public T? data { get; set; }
		public string? message { get; set; }
	}

	class SectionItemDto
	{
		public int id { get; set; }
		public string? refType { get; set; }
		public int refId { get; set; }
		public string? displayName { get; set; }
		public int position { get; set; }
		public bool isActive { get; set; }
	}

	class SectionDto
	{
		public int id { get; set; }
		public int menuId { get; set; }
		public string name { get; set; } = string.Empty;
		public int position { get; set; }
		public bool isActive { get; set; }
		public int? categoryId { get; set; }
		public DateTime? deletedAt { get; set; }
		public List<SectionItemDto> items { get; set; } = new();
	}

	class SectionVM : INotifyPropertyChanged
	{
		int _id;
		int _menuId;
		string _name = string.Empty;
		int _position;
		bool _isActive;
		int? _categoryId;
		int _itemsCount;
		DateTime? _deletedAt;

		public int id { get => _id; set => SetField(ref _id, value); }
		public int menuId { get => _menuId; set => SetField(ref _menuId, value); }
		public string name { get => _name; set => SetField(ref _name, value); }
		public int position { get => _position; set => SetField(ref _position, value); }
		public bool isActive { get => _isActive; set => SetField(ref _isActive, value); }
		public int? categoryId { get => _categoryId; set => SetField(ref _categoryId, value); }
		public int itemsCount { get => _itemsCount; set => SetField(ref _itemsCount, value); }
		public DateTime? deletedAt { get => _deletedAt; set => SetField(ref _deletedAt, value); }

		public string PositionDisplay => $"Posición: {position}";
		public string ItemsBadge => itemsCount == 1 ? "1 item" : $"{itemsCount} items";
		public bool HasCategory => categoryId.HasValue;
		public string CategoryBadge => categoryId.HasValue ? $"catId {categoryId}" : string.Empty;
		public string TrashDisplay => deletedAt?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "Desconocido";

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

	readonly List<SectionVM> _all = new();
	readonly List<SectionVM> _trash = new();

	bool _silenceSwitch;
	readonly HashSet<int> _toggling = new();
	bool _isLoading;
	bool _navigatingItems;
	CancellationTokenSource? _loadCts;

	public MenuSectionsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (!CanRead)
		{
			await DisplayAlert("Acceso restringido", "No tienes permiso para ver secciones.", "OK");
			await Shell.Current.GoToAsync("..");
			return;
		}

		if (_isLoading) return;

		_isLoading = true;
		_loadCts?.Cancel();
		_loadCts = new CancellationTokenSource();
		_all.Clear();
		SectionsCV.ItemsSource = null;
		try
		{
			await CargarTodoAsync(_loadCts.Token);
		}
		finally
		{
			_isLoading = false;
		}
	}

	protected override void OnDisappearing()
	{
		_loadCts?.Cancel();
		base.OnDisappearing();
	}

	#region Helpers comunes
	static async Task<string?> GetTokenAsync()
	{
		var secure = await SecureStorage.GetAsync("token");
		if (!string.IsNullOrWhiteSpace(secure)) return secure;
		var stored = Preferences.Default.Get("token", string.Empty);
		return string.IsNullOrWhiteSpace(stored) ? null : stored;
	}

	static HttpClient NewAuthClient(string baseUrl, string token)
	{
		var cli = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
		cli.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		return cli;
	}

	static SectionVM ToVM(SectionDto dto)
		=> new()
		{
			id = dto.id,
			menuId = dto.menuId,
			name = dto.name,
			position = dto.position,
			isActive = dto.isActive,
			categoryId = dto.categoryId,
			itemsCount = dto.items?.Count ?? 0,
			deletedAt = dto.deletedAt
		};

	void ApplyFilter(string? query = null)
	{
		query = (query ?? string.Empty).Trim().ToLowerInvariant();

		IEnumerable<SectionVM> src = _all;

		if (!string.IsNullOrEmpty(query))
		{
			src = src.Where(s =>
				s.name.ToLowerInvariant().Contains(query) ||
				(s.CategoryBadge?.ToLowerInvariant().Contains(query) ?? false));
		}

		var list = src
			.OrderBy(s => s.position)
			.ThenBy(s => s.name, StringComparer.CurrentCultureIgnoreCase)
			.ToList();

		SectionsCV.ItemsSource = list;
	}
	#endregion

	async Task CargarTodoAsync(CancellationToken ct = default)
	{
		try
		{
			ct.ThrowIfCancellationRequested();

			if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
			{
				await ErrorHandler.MostrarErrorUsuario("Sin conexión a Internet.");
				return;
			}

			ct.ThrowIfCancellationRequested();
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token))
			{
				await AuthHelper.VerificarYRedirigirSiExpirado(this);
				return;
			}

			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');

			await CargarSeccionesAsync(token, baseUrl, ct);
			await CargarPapelerasAsync(token, baseUrl, ct);

			OnPropertyChanged(nameof(ShowTrashFab));
		}
		catch (Exception ex)
		{
			if (ex is TaskCanceledException) return;
			await ErrorHandler.MostrarErrorTecnico(ex, "Secciones – Cargar todo");
		}
	}

	async Task CargarSeccionesAsync(string token, string baseUrl, CancellationToken ct = default)
	{
		try
		{
			ct.ThrowIfCancellationRequested();
			using var http = NewAuthClient(baseUrl, token);
			var resp = await http.GetAsync($"/api/menus/{MenuId}/sections", ct);
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

			var env = JsonSerializer.Deserialize<ApiEnvelope<List<SectionDto>>>(body, _json);
			_all.Clear();
			foreach (var dto in env?.data ?? new())
				_all.Add(ToVM(dto));

			ApplyFilter(SearchBox?.Text);
		}
		catch (TaskCanceledException)
		{
			// cancel silencioso para no mostrar alertas al navegar atrás
		}
		catch (HttpRequestException)
		{
			await ErrorHandler.MostrarErrorUsuario("No se pudo contactar al servidor.");
		}
	}

	async Task CargarPapelerasAsync(string token, string baseUrl, CancellationToken ct = default)
	{
		try
		{
			ct.ThrowIfCancellationRequested();
			using var http = NewAuthClient(baseUrl, token);
			var resp = await http.GetAsync($"/api/menus/{MenuId}/sections/trash", ct);
			var body = await resp.Content.ReadAsStringAsync();

			if (!resp.IsSuccessStatusCode)
			{
				if (resp.StatusCode == HttpStatusCode.Unauthorized)
				{
					await AuthHelper.VerificarYRedirigirSiExpirado(this);
					return;
				}
				_trash.Clear();
				return;
			}

			var env = JsonSerializer.Deserialize<ApiEnvelope<List<SectionDto>>>(body, _json);
			_trash.Clear();
			foreach (var dto in env?.data ?? new())
				_trash.Add(ToVM(dto));
		}
		catch
		{
			_trash.Clear();
		}
	}

	#region UI handlers
	void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		=> ApplyFilter(e.NewTextValue);

	async void Refresh_Refreshing(object sender, EventArgs e)
	{
		try
		{
			_loadCts?.Cancel();
			_loadCts = new CancellationTokenSource();
			await CargarTodoAsync(_loadCts.Token);
		}
		finally
		{
			if (sender is RefreshView rv)
				rv.IsRefreshing = false;
		}
	}

	async void New_Clicked(object sender, EventArgs e)
	{
		if (!CanCreate)
		{
			await DisplayAlert("Acceso restringido", "No puedes crear secciones.", "OK");
			return;
		}

		var menuName = Uri.EscapeDataString(MenuName ?? string.Empty);
		await Shell.Current.GoToAsync($"{nameof(SectionEditorPage)}?mode=create&menuId={MenuId}&menuName={menuName}");
	}

	async void Edit_Clicked(object sender, EventArgs e)
	{
		if (!CanUpdate)
		{
			await DisplayAlert("Acceso restringido", "No puedes editar secciones.", "OK");
			return;
		}

		if ((sender as BindableObject)?.BindingContext is not SectionVM vm) return;

		var menuName = Uri.EscapeDataString(MenuName ?? string.Empty);
		var sectionName = Uri.EscapeDataString(vm.name ?? string.Empty);
		var cat = vm.categoryId.HasValue ? vm.categoryId.Value.ToString() : string.Empty;
		await Shell.Current.GoToAsync(
			$"{nameof(SectionEditorPage)}?mode=edit&sectionId={vm.id}&menuId={MenuId}&menuName={menuName}&name={sectionName}&position={vm.position}&categoryId={cat}&isActive={vm.isActive}");
	}

	async void Items_Clicked(object sender, EventArgs e)
	{
		if (_navigatingItems) return;
		if ((sender as Button)?.CommandParameter is not SectionVM vm)
			return;

		_navigatingItems = true;
		try
		{
			await Shell.Current.GoToAsync(
				$"{nameof(SectionItemsPage)}?menuId={MenuId}" +
				$"&menuName={Uri.EscapeDataString(MenuName ?? string.Empty)}" +
				$"&sectionId={vm.id}" +
				$"&sectionName={Uri.EscapeDataString(vm.name)}");
		}
		finally { _navigatingItems = false; }
	}

	async void Delete_Clicked(object sender, EventArgs e)
	{
		if (!CanDelete)
		{
			await DisplayAlert("Acceso restringido", "No puedes eliminar secciones.", "OK");
			return;
		}

		if ((sender as BindableObject)?.BindingContext is not SectionVM vm)
			return;

		var confirm = await DisplayAlert("Enviar a papelera", $"¿Enviar “{vm.name}” a la papelera?", "Sí, enviar", "Cancelar");
		if (!confirm) return;

		await DeleteSectionAsync(vm.id, hard: false);
	}

	async void More_Clicked(object sender, EventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not SectionVM vm) return;

		var actions = new List<string>();
		if (CanUpdate) actions.Add("Editar");
		if (CanDelete) actions.Add("Eliminar");

		if (actions.Count == 0) return;

		var choice = await DisplayActionSheet(vm.name, "Cancelar", null, actions.ToArray());
		if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar") return;

		switch (choice)
		{
			case "Editar":
				Edit_Clicked(sender, EventArgs.Empty);
				break;
			case "Eliminar":
				Delete_Clicked(sender, EventArgs.Empty);
				break;
		}
	}

	async void ToggleActivo_Toggled(object sender, ToggledEventArgs e)
	{
		if (_silenceSwitch) return;
		if (sender is not Switch toggle || toggle.BindingContext is not SectionVM vm) return;

		if (!CanUpdate)
		{
			_silenceSwitch = true;
			toggle.IsToggled = vm.isActive;
			_silenceSwitch = false;
			return;
		}

		if (_toggling.Contains(vm.id))
		{
			_silenceSwitch = true;
			toggle.IsToggled = !e.Value;
			_silenceSwitch = false;
			return;
		}

		_toggling.Add(vm.id);
		var nuevo = e.Value;
		var anterior = vm.isActive;
		vm.isActive = nuevo;

		try
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			var resp = await http.PatchAsync($"/api/menus/sections/{vm.id}", JsonContent.Create(new { isActive = nuevo }));
			var body = await resp.Content.ReadAsStringAsync();
			if (!resp.IsSuccessStatusCode)
			{
				_silenceSwitch = true;
				vm.isActive = anterior;
				toggle.IsToggled = anterior;
				_silenceSwitch = false;
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
			}
		}
		catch (Exception ex)
		{
			_silenceSwitch = true;
			vm.isActive = anterior;
			toggle.IsToggled = anterior;
			_silenceSwitch = false;
			await ErrorHandler.MostrarErrorTecnico(ex, "Secciones – Activar/Inactivar");
		}
		finally
		{
			_toggling.Remove(vm.id);
		}
	}

	async void TrashFab_Clicked(object sender, EventArgs e)
	{
		if (_trash.Count == 0) return;

		var options = _trash
			.Select(s => $"{s.name} • {s.TrashDisplay}")
			.ToArray();

		var choice = await DisplayActionSheet("Papelera de secciones", "Cerrar", null, options);
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
			await DisplayAlert("Papelera", "No tienes permisos para administrar la papelera.", "OK");
			return;
		}

		var action = await DisplayActionSheet(selected.name, "Cancelar", null, actions.ToArray());
		if (string.IsNullOrWhiteSpace(action) || action == "Cancelar")
			return;

		switch (action)
		{
			case "Restaurar":
				await RestaurarAsync(selected.id);
				break;
			case "Eliminar definitivamente":
				await DeleteSectionAsync(selected.id, hard: true);
				break;
		}
	}
	#endregion

	async Task DeleteSectionAsync(int id, bool hard)
	{
		try
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

            var url = hard
                ? $"/api/menus/sections/{id}/hard"
                : $"/api/menus/sections/{id}";
                        
			var resp = await http.DeleteAsync(url);
			var body = await resp.Content.ReadAsStringAsync();
			if (!resp.IsSuccessStatusCode)
			{
			System.Diagnostics.Debug.WriteLine($"[MenuSections] DELETE {(hard ? "hard" : "soft")} id={id}: {body}");
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}

			await CargarTodoAsync();
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, hard ? "Secciones – Eliminar definitivo" : "Secciones – Eliminar");
		}
	}

	async Task RestaurarAsync(int id)
	{
		try
		{
			var token = await GetTokenAsync();
			if (string.IsNullOrWhiteSpace(token)) { await AuthHelper.VerificarYRedirigirSiExpirado(this); return; }
			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = NewAuthClient(baseUrl, token);

			using var emptyContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");
			var resp = await http.PatchAsync($"/api/menus/sections/{id}/restore", emptyContent);
			if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.NoContent)
			{
				var body = await resp.Content.ReadAsStringAsync();
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}

			await CargarTodoAsync();
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, "Secciones – Restaurar");
		}
	}
}
