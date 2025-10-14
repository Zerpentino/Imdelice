using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Storage;           // Preferences, SecureStorage

namespace Imdeliceapp.Pages;

public partial class OptionsPage : ContentPage
{
    public OptionsPage()
    {
        InitializeComponent();
    }
   protected override void OnAppearing()
{
    base.OnAppearing();

    var roleId = Preferences.Default.Get("roleId", 0);
    var role = Preferences.Default.Get("role", string.Empty);
    bool isAdmin = roleId == 1 || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

    BtnUsuarios.IsVisible = isAdmin;
    BtnRoles.IsVisible = isAdmin;
    BtnMenu.IsVisible = isAdmin;
    BtnCategorias.IsVisible = isAdmin;
    BtnModifierGroups.IsVisible = isAdmin; // <-- NUEVO
}

     private async void BtnUsuarios_Clicked(object sender, EventArgs e)
    {
        // Navega dentro del mismo Tab (el TabBar no desaparece)
        await Shell.Current.GoToAsync(nameof(UsersPage));
    }
    private async void OpenRoles_Clicked(object sender, EventArgs e)
    {
        // Navega dentro del mismo Tab (el TabBar no desaparece)
        await Shell.Current.GoToAsync(nameof(RolesPage));
    }
    private async void EditarMenu_Clicked(object sender, EventArgs e)
    {
        // Navega dentro del mismo Tab (el TabBar no desaparece)
    await Shell.Current.GoToAsync(nameof(AdminMenuPage));
    }
    private async void OpeCategorias_Clicked(object sender, EventArgs e)
    {
          // Navega dentro del mismo Tab (el TabBar no desaparece)
    await Shell.Current.GoToAsync(nameof(CategoriesPage));
    }
    private async void OpenModifierGroups_Clicked(object sender, EventArgs e)
{
    await Shell.Current.GoToAsync(nameof(ModifierGroupsPage));
}


    private async void BtnLogout_Clicked(object sender, EventArgs e)
    {
        var confirmar = await DisplayAlert("Cerrar sesión",
            "¿Seguro que quieres cerrar sesión?", "Sí, salir", "Cancelar");

        if (!confirmar) return;

        try
        {
            // Limpia credenciales y sesión
            SecureStorage.Remove("token");
            Preferences.Default.Clear();

            // Reemplaza la raíz por LoginPage (como en DecidePaginaInicial)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cerrar sesión: {ex.Message}", "OK");
        }
    }
}
