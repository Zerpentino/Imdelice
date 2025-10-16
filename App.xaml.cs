using System.Globalization;
using Microsoft.Maui.Controls;
using Imdeliceapp.Pages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.ApplicationModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Imdeliceapp.Services; // ← Perms

#if ANDROID            // ⬅️ solo Android
using Imdeliceapp.Platforms.Android;
using Android.Views;
using Microsoft.Maui.Platform;
#endif

namespace Imdeliceapp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // ——— 1. Sincronizar la barra de estado / navegación ———
#if ANDROID



		// Y cada vez que el usuario cambie el tema
		Current.RequestedThemeChanged += (_, args) =>
		{
			var clr = args.RequestedTheme == AppTheme.Dark ? Colors.Black : Colors.White;
			StatusBarHelper.ApplyColor(clr);
		};
#endif
        // ——— 2. Tu suscripción a ThemeChanged para tus propias páginas ———
        Application.Current.RequestedThemeChanged += (s, e) =>
            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage(e.RequestedTheme));

            Resources ??= new ResourceDictionary();
    if (!Resources.ContainsKey("BoolToIconConverter"))
        Resources.Add("BoolToIconConverter", new BoolToIconConverter());
    try
    {
        var saved = Preferences.Default.Get("perms_json", "");
        if (!string.IsNullOrWhiteSpace(saved))
        {
            var list = JsonSerializer.Deserialize<List<string>>(saved) ?? new();
            Perms.Set(list);
        }
        else
        {
            Perms.Set(Array.Empty<string>());
        }
    }
    catch
    {
        Perms.Set(Array.Empty<string>());
    }

    }

    protected override Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState)
    {
        var firstPage = DecidePaginaInicial();
        return new Microsoft.Maui.Controls.Window(firstPage);
    }

    private Page DecidePaginaInicial()
    {
        try
        {
            bool logeado = Preferences.Default.Get("logeado", false);
            string token = Preferences.Default.Get("token", "");
            string expiracionStr = Preferences.Default.Get("expiracion", "");

            System.Diagnostics.Debug.WriteLine($"📌 logeado={logeado}, token={(string.IsNullOrWhiteSpace(token) ? "vacio" : "ok")}, expiracion={expiracionStr}");

            if (logeado && !string.IsNullOrWhiteSpace(token))
            {
                if (DateTime.TryParseExact(expiracionStr, "o", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime expiracion) &&
                    expiracion > DateTime.UtcNow)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Token válido, cargando AppShell");
                    //                 #if ANDROID
                    //                 _ = EnviarTokenFCMAlBackend(); // ← Aquí la llamas (sin await si no estás en método async)
                    // #endif
                    var baseUrl = Application.Current.Resources["urlbase"].ToString().TrimEnd('/');
                    _ = Task.Run(() => SyncPermsFromServerAsync(baseUrl, token));
                
                    return new AppShell();
                }

                System.Diagnostics.Debug.WriteLine("⚠️ Token expirado o inválido, limpiando sesión");
                Preferences.Default.Clear();
                SecureStorage.Remove("token");
                Perms.Set(Array.Empty<string>()); // ← limpia permisos en memoria
            }

            System.Diagnostics.Debug.WriteLine("🔐 Mostrando LoginPage");
            return new NavigationPage(new LoginPage());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ERROR en DecidePaginaInicial: {ex.Message}");
            return new NavigationPage(new LoginPage()); // fallback
        }
    }static async Task SyncPermsFromServerAsync(string baseUrl, string token)
{
    try
    {
        using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await http.GetAsync("/api/auth/me");
        if (!resp.IsSuccessStatusCode) return;

        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("permissions", out var arr) &&
            arr.ValueKind == JsonValueKind.Array)
        {
            var perms = arr.EnumerateArray()
                           .Select(x => x.GetString() ?? "")
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .ToList();

            Perms.Set(perms);
            Preferences.Default.Set("perms_json", JsonSerializer.Serialize(perms));
        }
    }
    catch { /* ignora errores de red */ }
}

    public class BoolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? "−" : "+";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

}