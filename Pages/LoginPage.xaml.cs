using System.Threading.Tasks;
//using Imdeliceapp.Model;
using Imdeliceapp.Generic;
using System.Runtime.Intrinsics.Arm;
using Microsoft.Maui.Storage;
using System.Text.Json;
using Imdeliceapp.Model;
using CommunityToolkit.Mvvm.Messaging;
using Imdeliceapp.Helpers;
using System.Net.Http;
using Imdeliceapp.Services; // 👈 para Perms
using System.Net.Http.Headers;

using System.Net;
using System.Text;
using MauiFrame = Microsoft.Maui.Controls.Frame;
using System.Linq;                            // por All(...)
using Microsoft.Maui.ApplicationModel;        // por AppInfo
using Microsoft.Maui.Networking;              // por Connectivity


namespace Imdeliceapp.Pages;
#region DTOs de Login (locales a esta página)

class LoginData
{
    public UserDTO user { get; set; } = new();
    public string token { get; set; } = "";
    public long   expiresAt { get; set; }   // epoch ms
}
class ApiErr { public int code { get; set; } }

#endregion

public partial class LoginPage : ContentPage
{
    static readonly System.Text.Json.JsonSerializerOptions _json
    = new() { PropertyNameCaseInsensitive = true };

bool EsPin(string s) => s?.Length == 4 && s.All(char.IsDigit);


static List<string> TryGetPermsFromToken(string jwt)
{
    try
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return new();

        // base64url -> base64
        string payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("perms", out var a) && a.ValueKind == JsonValueKind.Array)
            return a.EnumerateArray()
                    .Select(x => x.GetString() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

        if (root.TryGetProperty("permissions", out var b) && b.ValueKind == JsonValueKind.Array)
            return b.EnumerateArray()
                    .Select(x => x.GetString() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

        return new();
    }
    catch
    {
        return new();
    }
}

    static async Task SyncPermsFromServerAsync(string baseUrl, string token)
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await http.GetAsync("/api/auth/me");
            if (!resp.IsSuccessStatusCode) return;

            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var perms = doc.RootElement
                           .GetProperty("data")
                           .GetProperty("permissions")
                           .EnumerateArray()
                           .Select(x => x.GetString() ?? "")
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .ToList();

            Perms.Set(perms);

            // guarda por conveniencia (para precargar UI en próximos arranques)
            Preferences.Default.Set("perms_json", JsonSerializer.Serialize(perms));
        }
        catch { /* ignora errores de red; ya tendrás los del token */ }
    }

async Task<LoginData> LoginConApiAsync(string entrada, string password)
{
    string baseUrl = App.Current.Resources["urlbase"].ToString().TrimEnd('/');
    string path, url;
    object body;
    
       

    if (EsCorreoValido(entrada))
        {
            path = App.Current.Resources["urlloginemail"].ToString().TrimStart('/');
            body = new { email = entrada, password };
        }
        else if (EsPin(entrada))
        {
            path = App.Current.Resources["urlloginpin"].ToString().TrimStart('/');
            body = new { pinCode = entrada, password };
        }
        else
        {
            throw new InvalidOperationException("Ingresa un correo válido o un PIN numérico.");
        }

    url = $"{baseUrl}/{path}";
    // await DisplayAlert("URL", url, "OK");

    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");


    var jsonBody = System.Text.Json.JsonSerializer.Serialize(body);
    using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

    var resp = await http.PostAsync(url, content);
    var text = await resp.Content.ReadAsStringAsync();
        var env = System.Text.Json.JsonSerializer.Deserialize<ApiEnvelope<LoginData>>(text, _json);
    

    if (!resp.IsSuccessStatusCode)
        throw new InvalidOperationException(env?.message ?? "Credenciales inválidas");

    if (env?.data is null)
        throw new InvalidOperationException("Respuesta inesperada del servidor.");

    return env.data;
}

    static async Task GuardarSesionAsync(LoginData d)
    {
        
        await SecureStorage.SetAsync("token", d.token);
        Preferences.Default.Set("token", d.token);
        Preferences.Default.Set("logeado", true);

        var expUtc = DateTimeOffset.FromUnixTimeMilliseconds(d.expiresAt).UtcDateTime;
        Preferences.Default.Set("expiracion", expUtc.ToString("o"));

        Preferences.Default.Set("user_id", d.user.id);
        Preferences.Default.Set("usuario_nombre", d.user.name ?? "");
        Preferences.Default.Set("usuario_mail", d.user.email ?? "");
        Preferences.Default.Set("roleId", d.user.roleId);
        Preferences.Default.Set("role", d.user.role?.name ?? "");

    var permsFromJwt = TryGetPermsFromToken(d.token); // ya es List<string>
Perms.Set(permsFromJwt);
Preferences.Default.Set("perms_json", JsonSerializer.Serialize(permsFromJwt));


    
        
    }

	static bool SinInternet =>
Connectivity.Current.NetworkAccess != NetworkAccess.Internet;
	static async Task MostrarSinConexionAsync()
	{
		await ErrorHandler.MostrarErrorUsuario(
			"Sin conexión a Internet. Revisa tu red e inténtalo de nuevo.");
	}

	    private LoginModel oLoginModel;

	public LoginPage()
	{
		InitializeComponent();
		if (Application.Current.Resources.TryGetValue("EntrySinLinea", out var entryStyle))
		{
			System.Diagnostics.Debug.WriteLine($"EntrySinLinea encontrado: {entryStyle}");
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("EntrySinLinea NO se resolvió en Resources");
		}

		string versionName = AppInfo.VersionString;   // e.g. "1.0"
		string versionCode = AppInfo.BuildString;     // e.g. "7"
													  // Muestra: "Version: 1.0_7"
		LabelVersion.Text = $"Version: {versionName}_{versionCode}";
		NavigationPage.SetHasNavigationBar(this, false);

		oLoginModel = new LoginModel();
		BindingContext = oLoginModel;
	}
	

    private async void btnIngresar_Clicked(object sender, EventArgs e)
    {
        
SetError(frameCorreo, labelCorreoError.IsVisible);

        overlay.IsVisible = true;


        loader.IsVisible = true;
        loader.IsRunning = true;
        // urlbase = App.Current.Resources["urlbase"].ToString();
        // urllogin = App.Current.Resources["urllogin"].ToString();

        await Task.Delay(1000); // simula espera si lo deseas
                                //  Application.Current.MainPage = new AppShell();
        if (SinInternet)
        {
            await MostrarSinConexionAsync();
            loader.IsRunning = false;
            loader.IsVisible = false;
            overlay.IsVisible = false;
            return;
        }
        var entrada = oLoginModel.dvcMail?.Trim();
        var contrasenia = oLoginModel.dvcContrasenia?.Trim();
        if (string.IsNullOrWhiteSpace(entrada) || string.IsNullOrWhiteSpace(contrasenia))
        {
            labelCorreoError.IsVisible = string.IsNullOrWhiteSpace(entrada);
            labelContraseniaError.IsVisible = string.IsNullOrWhiteSpace(contrasenia);
SetError(frameCorreo, labelCorreoError.IsVisible);
SetError(frameContrasena, labelContraseniaError.IsVisible);

            await ErrorHandler.MostrarErrorUsuario("Completa todos los campos.");
            loader.IsRunning = false;
            loader.IsVisible = false;
            overlay.IsVisible = false;
            return;
        }

        var datosLogin = new LoginModel { dvcContrasenia = contrasenia };


       


        try
        {
            if (!EsCorreoValido(entrada) && !EsPin(entrada))
            {
                await ErrorHandler.MostrarErrorUsuario("Ingresa correo válido o PIN de 4 dígitos.");
                FinalizarCarga();
                return;
            }


            var data = await LoginConApiAsync(entrada, contrasenia);
            await GuardarSesionAsync(data);
            // Refresca desde el servidor (por si cambió algo en backend)
var baseUrl = App.Current.Resources["urlbase"].ToString().TrimEnd('/');
_ = SyncPermsFromServerAsync(baseUrl, data.token); // fire-and-forget está bien



            // ➜ Entrar a la app
            Application.Current.MainPage = new AppShell();
        }
        catch (InvalidOperationException ex)       // mensajes del backend o validaciones
        {
            await ErrorHandler.MostrarErrorUsuario(ex.Message);
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Login – Error inesperado");
        }
        finally
        {
            FinalizarCarga();
        }


        // Aquí va tu lógica de login...
        /* urlbase = App.Current.Resources["urlbase"].ToString();
         urllogin = App.Current.Resources["urllogin"].ToString();
         oLoginModel.iscargando = true;

         string body = System.Text.Json.JsonSerializer.Serialize(oLoginModel);
         await Application.Current.MainPage.DisplayAlert("Body que se enviaría", body, "OK");

         try
         {

             var respuesta = await ClientHttp.Post<LoginModel, RespuestaAutenticacionDTO>(
                 urlbase,
                 urllogin,
                 oLoginModel
             );
             oLoginModel.iscargando = false;
             string respuestaDebug = System.Text.Json.JsonSerializer.Serialize(respuesta);
             await Application.Current.MainPage.DisplayAlert("Respuesta Deserializada", respuestaDebug ?? "nulo", "OK");

             if (respuesta is not null && !string.IsNullOrWhiteSpace(respuesta.token))
             {
                 await SecureStorage.SetAsync("token", respuesta.token);
                 Preferences.Default.Set("logeado", true);
                 Preferences.Default.Set("token", respuesta.token);
                 Preferences.Default.Set("expiracion", respuesta.expiracion.ToString("o")); // formato ISO8601
                 //Preferences.Default.Set("expiracion", DateTime.UtcNow.AddSeconds(20).ToString("o"));

 */


        /*

}
else
{
    // Aquí entra si el backend regresó un 400 o no se obtuvo un token
    await Application.Current.MainPage.DisplayAlert("Error", "Usuario o contraseña incorrectos", "OK");
    Preferences.Default.Set("logeado", false);
}
}
catch (HttpRequestException ex)
{
await Application.Current.MainPage.DisplayAlert("Error de conexión", "No se pudo conectar al servidor o error HTTP: " + ex.Message, "OK");
}
catch (Exception ex)
{
await Application.Current.MainPage.DisplayAlert("Error inesperado", ex.Message, "OK");
}*/
    }


    private async void OlvidasteContrasena_Tapped(object sender, EventArgs e)
    {
        //  await DisplayAlert("Info", "Se abrirá el cambio de contraseña como prueba", "OK");
        // await Navigation.PushAsync(new ForgotPwdEmailPage());
    }

    private bool EsCorreoValido(string correo)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(correo);
            return addr.Address == correo;
        }
        catch
        {
            return false;
        }

    }

    private void FinalizarCarga()
    {
        loader.IsRunning = false;
        loader.IsVisible = false;
        overlay.IsVisible = false;
    }
    private bool mostrarContrasena = false;

    private void TogglePasswordVisibility_Clicked(object sender, EventArgs e)
    {

        mostrarContrasena = !mostrarContrasena;
        entryContrasenia.IsPassword = !mostrarContrasena;

        var imageButton = sender as ImageButton;
        var esModoOscuro = Application.Current.RequestedTheme == AppTheme.Dark;
        if (mostrarContrasena)
        {
            imageButton.Source = esModoOscuro ? "ojo_abierto_blanco.png" : "ojo_abierto.png";
        }
        else
        {
            imageButton.Source = esModoOscuro ? "ojo_cerrado_blanco.png" : "ojo_cerrado.png";
        }

    }

    private void EntryCorreo_TextChanged(object sender, TextChangedEventArgs e)
    {
        bool vacio = string.IsNullOrWhiteSpace(entryCorreo.Text);

           SetError(frameCorreo, vacio);
    labelCorreoError.IsVisible = vacio;
    }

    private void EntryContrasenia_TextChanged(object sender, TextChangedEventArgs e)
    {
        bool vacio = string.IsNullOrWhiteSpace(entryContrasenia.Text);

        SetError(frameContrasena, vacio);
    labelContraseniaError.IsVisible = vacio;
    }
private async void ProblemasIniciarSesion_Tapped(object sender, EventArgs e)
    {
        //  await DisplayAlert("Info", "Se abrirá el cambio de contraseña como prueba", "OK");
        // await Navigation.PushAsync(new ComunicacionEmailPage());
    }


private void SetError(Border border, bool isError)
{
    if (isError)
        border.Stroke = Colors.Red;
    else
        border.ClearValue(Border.StrokeProperty);
}


}