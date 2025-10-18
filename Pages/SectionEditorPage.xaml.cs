using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Imdeliceapp.Helpers;
using Imdeliceapp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(MenuId), "menuId")]
[QueryProperty(nameof(MenuName), "menuName")]
[QueryProperty(nameof(SectionId), "sectionId")]
[QueryProperty(nameof(InitialName), "name")]
[QueryProperty(nameof(InitialPosition), "position")]
[QueryProperty(nameof(InitialCategory), "categoryId")]
[QueryProperty(nameof(InitialActive), "isActive")]
public partial class SectionEditorPage : ContentPage
{
	static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
	readonly MenusApi _menusApi = new();

	public string Mode { get; set; } = "create";
	public int MenuId { get; set; }
	public string? MenuName { get; set; }
	public int SectionId { get; set; }

	public string? InitialName { get; set; }
	public string? InitialPosition { get; set; }
	public string? InitialCategory { get; set; }
	public string? InitialActive { get; set; }
    bool CanRead   => Perms.MenusRead;
    bool CanCreate => Perms.MenusCreate || Perms.MenusUpdate; // permite cualquiera de los dos
	bool CanUpdate => Perms.MenusUpdate;
	
	bool _isSaving;
	public SectionEditorPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		        var isEdit = Mode?.Equals("edit", StringComparison.OrdinalIgnoreCase) ?? false;
		if (!CanRead)
		{
			await DisplayAlert("Acceso restringido", "No tienes permiso para ver secciones.", "OK");
			await Shell.Current.GoToAsync("..");
			return;
		}
		if (MenuId <= 0)
        {
            await DisplayAlert("Parámetros", "Menú inválido.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

		if (isEdit)
		{
			if (!CanUpdate)
			{
				await DisplayAlert("Acceso restringido", "No puedes editar secciones.", "OK");
				await Shell.Current.GoToAsync("..");
				return;
			}
			if (SectionId <= 0)
			{
				await DisplayAlert("Parámetros", "Sección inválida.", "OK");
				await Shell.Current.GoToAsync("..");
				return;
			}
		}
		else
		{
			if (!CanCreate)
			{
				await DisplayAlert("Acceso restringido", "No puedes crear secciones.", "OK");
				await Shell.Current.GoToAsync("..");
				return;
			}
		}
		
		await EnsureCategoriesAsync();
		PopulateForm();
		
	}

	async Task EnsureCategoriesAsync()
	{
		try
		{
			BusyIndicator.IsRunning = BusyIndicator.IsVisible = true;
			var cats = await _menusApi.GetCategoriesAsync(isActive: true);
			var list = cats
				.OrderBy(c => c.name)
				.Select(c => new CategoryItem(c.id, c.name ?? $"Categoría {c.id}"))
				.ToList();

			list.Insert(0, new CategoryItem(null, "Sin categoría"));

			CategoryPicker.ItemsSource = list;

			if (int.TryParse(InitialCategory, out var catId))
			{
				var idx = list.FindIndex(c => c.Id == catId);
				if (idx >= 0) CategoryPicker.SelectedIndex = idx;
			}
			else
			{
				CategoryPicker.SelectedIndex = 0;
			}
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, "Sección – Categorías");
			CategoryPicker.ItemsSource = new[] { new CategoryItem(null, "Sin categoría") };
			CategoryPicker.SelectedIndex = 0;
		}
		finally
		{
			BusyIndicator.IsRunning = BusyIndicator.IsVisible = _isSaving;
		}
	}

	void PopulateForm()
	{
		var isEdit = Mode?.Equals("edit", StringComparison.OrdinalIgnoreCase) ?? false;
		HeaderLabel.Text = isEdit ? "Editar sección" : "Crear sección";
		MenuLabel.Text = string.IsNullOrWhiteSpace(MenuName)
			? $"Menú #{MenuId}"
			: $"Menú: {MenuName}";

		if (isEdit)
		{
			NameEntry.Text = InitialName ?? string.Empty;
			PositionEntry.Text = InitialPosition ?? string.Empty;

			if (bool.TryParse(InitialActive, out var active))
				ActiveSwitch.IsToggled = active;
			else
				ActiveSwitch.IsToggled = true;
		}
		else
		{
			NameEntry.Text = string.Empty;
			PositionEntry.Text = string.Empty;
			ActiveSwitch.IsToggled = true;
			CategoryPicker.SelectedIndex = 0;
		}

		NameEntry.Focus();
	}

	async void Save_Clicked(object sender, EventArgs e)
	{
		if (_isSaving) return;
		var isEdit = Mode?.Equals("edit", StringComparison.OrdinalIgnoreCase) ?? false;

        // 🔒 Re-chequeo por si forzaron el UI
        if (isEdit && !CanUpdate)
        {
            await DisplayAlert("Acceso restringido", "No puedes editar secciones.", "OK");
            return;
        }
        if (!isEdit && !CanCreate)
        {
            await DisplayAlert("Acceso restringido", "No puedes crear secciones.", "OK");
            return;
        }
		var name = NameEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			await DisplayAlert("Sección", "Escribe un nombre.", "OK");
			return;
		}

		int? position = null;
		if (int.TryParse(PositionEntry.Text, out var pos))
			position = pos;

		int? categoryId = (CategoryPicker.SelectedItem as CategoryItem)?.Id;

		var isActive = ActiveSwitch.IsToggled;

		try
		{
			var token = await SecureStorage.GetAsync("token");
			if (string.IsNullOrWhiteSpace(token))
			{
				var pref = Preferences.Default.Get("token", string.Empty);
				token = string.IsNullOrWhiteSpace(pref) ? null : pref;
			}

			if (string.IsNullOrWhiteSpace(token))
			{
				await AuthHelper.VerificarYRedirigirSiExpirado(this);
				return;
			}

			var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
			using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
			http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			BusyIndicator.IsVisible = BusyIndicator.IsRunning = _isSaving = true;

			HttpResponseMessage resp;
			if (isEdit)
			{
				var payload = JsonContent.Create(new
				{
					name,
					position,
					categoryId,
					isActive
				});
				resp = await http.PatchAsync($"/api/menus/sections/{SectionId}", payload);
			}
			else
			{
				var payload = JsonContent.Create(new
				{
					menuId = MenuId,
					name,
					position,
					categoryId,
					isActive
				});
				resp = await http.PostAsync("/api/menus/sections", payload);
			}

			if (!resp.IsSuccessStatusCode)
			{
				var body = await resp.Content.ReadAsStringAsync();
				await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
				return;
			}

			await DisplayAlert("Listo", isEdit ? "Sección actualizada." : "Sección creada.", "OK");
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await ErrorHandler.MostrarErrorTecnico(ex, "Sección – Guardar");
		}
		finally
		{
			BusyIndicator.IsRunning = BusyIndicator.IsVisible = _isSaving = false;
		}
	}

	void ClearCategory_Clicked(object sender, EventArgs e)
	{
		if (CategoryPicker.ItemsSource is IList<CategoryItem> list)
		{
			var noneIndex = list.ToList().FindIndex(c => c.Id == null);
			if (noneIndex >= 0)
				CategoryPicker.SelectedIndex = noneIndex;
		}
		else
		{
			CategoryPicker.SelectedIndex = 0;
		}
	}

	record CategoryItem(int? Id, string Name)
	{
		public override string ToString() => Name;
	}
}
