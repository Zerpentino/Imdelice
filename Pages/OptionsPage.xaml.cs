using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Storage;           // Preferences, SecureStorage
using Imdeliceapp.Services; // ← Perms
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Storage;
using System;
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

        // Visibilidad según permisos
        BtnUsuarios.IsVisible = Perms.UsersRead;       // ver listado de usuarios
        BtnRoles.IsVisible = Perms.RolesRead;       // ver listado de roles
        BtnCategorias.IsVisible = Perms.CategoriesRead;  // ver categorías
        BtnModifierGroups.IsVisible = Perms.ModifiersRead;   // ver grupos de modificadores
        BtnTables.IsVisible = Perms.TablesRead; // ver mesas
        BtnMenu.IsVisible = Perms.MenusRead;      // editar/publicar menú
        BtnChannelConfig.IsVisible = Perms.OrdersUpdate;
        BtnPaymentsReport.IsVisible = Perms.OrdersRead;
    }

    private async void BtnUsuarios_Clicked(object sender, EventArgs e)
    {
        if (!Perms.UsersRead) { await DisplayAlert("Acceso restringido", "No puedes ver usuarios.", "OK"); return; }
        // Navega dentro del mismo Tab (el TabBar no desaparece)
        await Shell.Current.GoToAsync(nameof(UsersPage));
    }
    private async void OpenRoles_Clicked(object sender, EventArgs e)
    {
        if (!Perms.RolesRead) { await DisplayAlert("Acceso restringido", "No puedes ver roles.", "OK"); return; }

        // Navega dentro del mismo Tab (el TabBar no desaparece)
        await Shell.Current.GoToAsync(nameof(RolesPage));
    }
    private async void EditarMenu_Clicked(object sender, EventArgs e)
    {
        if (!Perms.MenusRead) { await DisplayAlert("Acceso restringido", "No puedes editar el menú.", "OK"); return; }

        // Navega dentro del mismo Tab (el TabBar no desaparece)
        await Shell.Current.GoToAsync(nameof(AdminMenuPage));
    }
    private async void OpeCategorias_Clicked(object sender, EventArgs e)
    {
        if (!Perms.CategoriesRead) { await DisplayAlert("Acceso restringido", "No puedes ver categorías.", "OK"); return; }

        // Navega dentro del mismo Tab (el TabBar no desaparece)
        await Shell.Current.GoToAsync(nameof(CategoriesPage));
    }
    private async void OpenModifierGroups_Clicked(object sender, EventArgs e)
    {
        if (!Perms.ModifiersRead) { await DisplayAlert("Acceso restringido","No puedes ver modificadores.","OK"); return; }

        await Shell.Current.GoToAsync(nameof(ModifierGroupsPage));
    }

    private async void OpenTables_Clicked(object sender, EventArgs e)
    {
        if (!Perms.TablesRead) { await DisplayAlert("Acceso restringido", "No puedes ver mesas.", "OK"); return; }
        await Shell.Current.GoToAsync(nameof(TablesPage));
    }

    private async void OpenChannelConfig_Clicked(object sender, EventArgs e)
    {
        if (!Perms.OrdersUpdate)
        {
            await DisplayAlert("Acceso restringido", "No puedes editar la configuración de plataformas.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(ChannelConfigPage));
    }

    private async void OpenPaymentsReport_Clicked(object sender, EventArgs e)
    {
        if (!Perms.OrdersRead)
        {
            await DisplayAlert("Acceso restringido", "No puedes ver el reporte de pagos.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(PaymentsReportPage));
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
            Perms.Set(Array.Empty<string>());

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
