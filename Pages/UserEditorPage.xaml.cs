using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Imdeliceapp.Helpers;
using Imdeliceapp.Model;
using Microsoft.Maui.Storage;
using System.Text.RegularExpressions;
using Microsoft.Maui.Networking;  // Connectivity
using System.Threading.Tasks;      // TaskCanceledException
using Imdeliceapp.Services;

namespace Imdeliceapp.Pages;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(UserId), "id")]
[QueryProperty(nameof(PrefillName), "name")]     // <-- NUEVO
[QueryProperty(nameof(PrefillEmail), "email")]   // <-- NUEVO
[QueryProperty(nameof(PrefillRoleId), "roleId")] // <-- NUEVO
public partial class UserEditorPage : ContentPage
{
    public string? Mode { get; set; }   // "create" | "edit"
    public int UserId { get; set; }
    // NUEVO: valores que llegan desde la lista
    public string? PrefillName { get; set; }
    public string? PrefillEmail { get; set; }
    public int PrefillRoleId { get; set; }

    string? _origEmail, _origName;
    int _origRoleId;


    private readonly JsonSerializerOptions _jsonRoles = new() { PropertyNameCaseInsensitive = true };
    private List<RoleDTO> _roles = new();
    static readonly JsonSerializerOptions _jsonWrite = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
    bool EsCorreoValido(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return false;
        try { var _ = new System.Net.Mail.MailAddress(correo); return true; }
        catch { return false; }
    }
    string ExtraerMensajeApi(string body)
    {
        try
        {
            var env = JsonSerializer.Deserialize<ApiEnvelope<object>>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (!string.IsNullOrWhiteSpace(env?.message)) return env!.message!;
        }
        catch { /* ignorar */ }
        return string.IsNullOrWhiteSpace(body) ? "Solicitud inválida" : body;
    }

    bool EsMensajeCorreoDuplicado(string? msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return false;
        // Backend manda algo como: "Email ya está registrado"
        return msg.Contains("Email ya está registrado", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("correo ya está registrado", StringComparison.OrdinalIgnoreCase);
    }


    // estados para ojitos
    bool _showPwd = false;
    bool _showPwd2 = false;

    public UserEditorPage()
    {
        InitializeComponent();
    }

    #region Helpers
    void SetSaving(bool v)
    {
        BtnGuardar.IsEnabled = !v;
        BtnCancelar.IsEnabled = !v;
    }

    Task MostrarServidorNoDisponibleAsync(string causa = "")
    {
        var msg = causa switch
        {
            "sin_internet" => "Sin conexión a Internet. Revisa tu red e inténtalo de nuevo.",
            "timeout" => "Tiempo de espera agotado. El servidor no responde.",
            _ => "No pudimos contactar al servidor. Inténtalo más tarde."
        };
        return DisplayAlert("Servidor no disponible", msg, "OK");
    }

    static int RoleIdFromName(string? name)
        => string.Equals(name, "Admin", StringComparison.OrdinalIgnoreCase) ? 1 : 2;

    static string RoleNameFromId(int id) => id == 1 ? "Admin" : "Mesero";

    static async Task<string?> GetTokenAsync()
    {
        var s = await SecureStorage.GetAsync("token");
        if (!string.IsNullOrWhiteSpace(s)) return s;

        var p = Preferences.Default.Get<string>("token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }
    void SetError(Border b, Label l, bool isError, string? msg = null)
    {
        if (isError)
        {
            b.Stroke = Colors.Red;
            if (msg != null) l.Text = msg;
            l.IsVisible = true;
        }
        else
        {
            b.ClearValue(Border.StrokeProperty);
            l.IsVisible = false;
        }
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

    void SetEye(ImageButton btn, bool visible)
    {
        var dark = Application.Current.RequestedTheme == AppTheme.Dark;
        btn.Source = visible
            ? (dark ? "ojo_abierto_blanco.png" : "ojo_abierto.png")
            : (dark ? "ojo_cerrado_blanco.png" : "ojo_cerrado.png");
    }
    #endregion
    async Task CargarRolesAsync()
    {
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

            var resp = await http.GetAsync("/api/roles");
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthHelper.VerificarYRedirigirSiExpirado(this);
                    return;
                }

                var apiMsg = ExtraerMensajeApi(body);

                // 409 por correo duplicado => pinta rojo el correo y muestra mensaje claro
                if (resp.StatusCode == HttpStatusCode.Conflict && EsMensajeCorreoDuplicado(apiMsg))
                {
                    SetError(BdrEmail, ErrEmail, true, "Este correo ya está registrado.");
                    await DisplayAlert("Correo duplicado", "Ese correo ya está registrado. Usa otro diferente.", "OK");
                    return;
                }

                // Cualquier otro error => mensaje del backend o genérico
                await ErrorHandler.MostrarErrorUsuario(apiMsg);
                return;
            }


            var env = JsonSerializer.Deserialize<ApiEnvelope<List<RoleDTO>>>(body, _jsonRoles);
            _roles = env?.data ?? new List<RoleDTO>();

            PkRole.ItemsSource = _roles; // ItemDisplayBinding="name"
        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "Roles – CargarRoles");
        }
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
        if (esEdicion && !Perms.UsersUpdate) { await DisplayAlert("Acceso restringido", "No puedes editar usuarios.", "OK"); return; }
        if (!esEdicion && !Perms.UsersCreate) { await DisplayAlert("Acceso restringido", "No puedes crear usuarios.", "OK"); return; }

        TitleLabel.Text = esEdicion ? "Editar usuario" : "Crear usuario";
        HintEditPassword.IsVisible = esEdicion;
        HintEditGeneral.IsVisible = esEdicion;

        SetEye(BtnTogglePwd, _showPwd);
        SetEye(BtnTogglePwd2, _showPwd2);

        if (Perms.RolesRead)
        {
            if (_roles.Count == 0) await CargarRolesAsync();
            PkRole.IsEnabled = true;
            PkRole.IsVisible = true;
        }
        else
        {
            PkRole.IsEnabled = false;
            PkRole.IsVisible = false;
        }

        // === PREFILL desde la lista (sin llamar GET) ===
        if (esEdicion && UserId > 0 && (!string.IsNullOrEmpty(PrefillEmail) || !string.IsNullOrEmpty(PrefillName)))
        {
            _origName = PrefillName ?? "";
            _origEmail = PrefillEmail ?? "";
            _origRoleId = PrefillRoleId;

            TxtName.Text = _origName;
            TxtEmail.Text = _origEmail;
            if (Perms.RolesRead)
                PkRole.SelectedItem = _roles.FirstOrDefault(r => r.id == _origRoleId);

            return; // IMPORTANTÍSIMO: no intentes GET si ya hay datos
        }

        // Fallback solo si de verdad no llegaron datos y SÍ tienes permiso de ver
        if (esEdicion && UserId > 0 && Perms.UsersRead)
            await CargarUsuarioAsync(UserId);
    }


    async Task CargarUsuarioAsync(int id)
    {
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

            var resp = await http.GetAsync($"/api/users/{id}");
            if (resp.StatusCode == HttpStatusCode.NotFound || resp.StatusCode == HttpStatusCode.Forbidden)
            {
                var msg = resp.StatusCode == HttpStatusCode.NotFound
                    ? $"El usuario #{id} no existe o no tienes permiso para verlo."
                    : "No tienes permiso para ver este usuario (falta users.read).";

                await DisplayAlert("No se puede cargar", msg, "OK");
                // Nos quedamos en la pantalla para permitir edición parcial.
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
                await ErrorHandler.MostrarErrorUsuario(ErrorHandler.ObtenerMensajeHttp(resp, body));
                await Shell.Current.GoToAsync("..");
                return;
            }

            var env = JsonSerializer.Deserialize<ApiEnvelope<UserDTO>>(body, _jsonWrite);
            var u = env?.data;
            if (u is null)
            {
                await ErrorHandler.MostrarErrorUsuario("Usuario no encontrado.");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Guarda “originales”
            _origEmail = u.email ?? "";

            _origName = u.name ?? "";

            _origRoleId = u.roleId;


            // **Prefill** en las entradas
            TxtName.Text = _origName;

            TxtEmail.Text = _origEmail;



            PkRole.SelectedItem = _roles.FirstOrDefault(r => r.id == _origRoleId);


        }
        catch (Exception ex)
        {
            await ErrorHandler.MostrarErrorTecnico(ex, "UserEditor – CargarUsuario");
        }
    }

    private async void Guardar_Clicked(object sender, EventArgs e)
    {
        var name = TxtName.Text?.Trim();
        var email = TxtEmail.Text?.Trim();
        // var role = PkRole.SelectedItem?.ToString();

        var pwd = TxtPassword.Text?.Trim();
        var pwd2 = TxtPasswordConfirm.Text?.Trim();
        var pin = TxtPin.Text?.Trim();
        // Validaciones password/confirm
        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
        if (esEdicion && !Perms.UsersUpdate) { await DisplayAlert("Acceso restringido", "No puedes editar usuarios.", "OK"); return; }
        if (!esEdicion && !Perms.UsersCreate) { await DisplayAlert("Acceso restringido", "No puedes crear usuarios.", "OK"); return; }
        var selRole = PkRole.SelectedItem as RoleDTO;

        if (esEdicion)
        {
            // Email: si lo escriben, debe ser válido
            if (!string.IsNullOrWhiteSpace(email) && !EsCorreoValido(email))
            {
                SetError(BdrEmail, ErrEmail, true);
                await DisplayAlert("Correo", "Escribe un correo válido.", "OK");
                return;
            }

            // si quieren cambiarla: mínima 3 y debe coincidir
            if (!string.IsNullOrEmpty(pwd) || !string.IsNullOrEmpty(pwd2))
            {
                if ((pwd ?? "").Length < 3)
                {
                    SetError(BdrPwd, ErrPwd, true);
                    await DisplayAlert("Contraseña", "Mínimo 3 caracteres.", "OK");
                    return;
                }
                if (pwd != pwd2)
                {
                    SetError(BdrPwd2, ErrPwd2, true);
                    await DisplayAlert("Contraseña", "La contraseña y la confirmación no coinciden.", "OK");
                    return;
                }
            }
            if (!string.IsNullOrWhiteSpace(pin) && pin.Length != 4)
            {
                SetError(BdrPin, ErrPin, true);
                await DisplayAlert("PIN", "El PIN debe tener 4 dígitos.", "OK");
                return;
            }


        }

        else
        {
            if (string.IsNullOrWhiteSpace(name) ||
           string.IsNullOrWhiteSpace(email) ||
           string.IsNullOrWhiteSpace(selRole?.name))
            {
                await DisplayAlert("Campos incompletos", "Llena nombre, correo y rol.", "OK");
                return;
            }
            // === validaciones base ===
            if (string.IsNullOrWhiteSpace(name))
            {
                SetError(BdrName, ErrName, true);
                await DisplayAlert("Nombre", "Escribe el nombre.", "OK");
                return;
            }
            if (!EsCorreoValido(email))
            {
                SetError(BdrEmail, ErrEmail, true);
                await DisplayAlert("Correo", "Escribe un correo válido.", "OK");
                return;
            }
            if (selRole is null)
            {
                await DisplayAlert("Rol", "Selecciona un rol.", "OK");
                return;
            }


            // crear: obligatoria, mínima 3 y coincidir
            if (string.IsNullOrWhiteSpace(pwd) || string.IsNullOrWhiteSpace(pwd2))
            {
                SetError(BdrPwd, ErrPwd, true);
                SetError(BdrPwd2, ErrPwd2, true, "Las contraseñas no coinciden.");
                await DisplayAlert("Contraseña", "Escribe y confirma la contraseña.", "OK");
                return;
            }

            if (pwd.Length < 3)
            {
                SetError(BdrPwd, ErrPwd, true);
                await DisplayAlert("Contraseña", "Mínimo 3 caracteres.", "OK");
                return;
            }

            if (pwd != pwd2)
            {
                SetError(BdrPwd2, ErrPwd2, true);
                await DisplayAlert("Contraseña", "Las contraseñas no coinciden.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4)
            {
                SetError(BdrPin, ErrPin, true);
                await DisplayAlert("PIN", "Ingresa el PIN de 4 dígitos.", "OK");
                return;
            }



        }


        if (!BtnGuardar.IsEnabled) return;
        SetSaving(true);

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await MostrarServidorNoDisponibleAsync("sin_internet");
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

            HttpResponseMessage resp;
            string body;

            if (esEdicion && UserId > 0)
            {
                // solo mandamos password/pin si procede
                string? sendEmail = string.IsNullOrWhiteSpace(email) ? null
                        : (email != _origEmail ? email : null);

                string? sendName = string.IsNullOrWhiteSpace(name) ? null
                                    : (name != _origName ? name : null);

                int? sendRole = null;
                if (Perms.RolesRead) // sólo si puede ver/escoger roles
                {
                    selRole = PkRole.SelectedItem as RoleDTO;
                    var selId = (selRole?.id) ?? _origRoleId;
                    if (selRole != null && selId != _origRoleId)
                        sendRole = selRole.id;
                }


                string? sendPwd = (!string.IsNullOrWhiteSpace(pwd) && pwd == pwd2 && pwd.Length >= 3) ? pwd : null;
                string? sendPin = string.IsNullOrWhiteSpace(pin) ? null : pin; // ya validado si viene

                var hayCambios = sendEmail != null || sendName != null || sendRole != null || sendPwd != null || sendPin != null;
                if (!hayCambios)
                {
                    await DisplayAlert("Sin cambios", "No hay cambios por guardar.", "OK");
                    return;
                }



                var payload = new
                {
                    email = sendEmail,
                    name = sendName,
                    roleId = sendRole,
                    password = sendPwd,
                    pinCode = sendPin
                };

                var json = JsonSerializer.Serialize(payload, _jsonWrite);
                resp = await http.PutAsync($"/api/users/{UserId}", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                await DisplayAlert("Respuesta api", json, "OK");
                await DisplayAlert("Respuesta api", resp.StatusCode.ToString(), "OK");
                body = await resp.Content.ReadAsStringAsync();
                // Si el server está sobrecargado/caído:
                if (resp.StatusCode == HttpStatusCode.ServiceUnavailable ||  // 503
                    resp.StatusCode == HttpStatusCode.GatewayTimeout)        // 504
                {
                    await MostrarServidorNoDisponibleAsync();
                    return;
                }

            }
            else
            {
                selRole = Perms.RolesRead ? (PkRole.SelectedItem as RoleDTO) : null;
                // si no puede ver roles, puedes forzar un rol por defecto, o impedir crear
                if (selRole == null && Perms.RolesRead)
                {
                    await DisplayAlert("Rol", "Selecciona un rol.", "OK");
                    return;
                }

                var payload = new
                {
                    email,
                    name,
                    roleId = selRole?.id, // será null si no hay RolesRead; el backend decide si lo acepta
                    password = pwd,
                    pinCode = pin
                };


                var json = JsonSerializer.Serialize(payload, _jsonWrite);
                resp = await http.PostAsync("/api/users", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                body = await resp.Content.ReadAsStringAsync();
                // Si el server está sobrecargado/caído:
                if (resp.StatusCode == HttpStatusCode.ServiceUnavailable ||  // 503
                    resp.StatusCode == HttpStatusCode.GatewayTimeout)        // 504
                {
                    await MostrarServidorNoDisponibleAsync();
                    return;
                }


                await DisplayAlert("Respuesta api", json, "OK");
                await DisplayAlert("Respuesta api", resp.StatusCode.ToString(), "OK");
            }

            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await AuthHelper.VerificarYRedirigirSiExpirado(this);
                    return;
                }

                var apiMsg = ExtraerMensajeApi(body);

                // 409 por correo duplicado => pinta rojo el correo y muestra mensaje claro
                // if (resp.StatusCode == HttpStatusCode.Conflict && EsMensajeCorreoDuplicado(apiMsg))
                // {
                //     SetError(BdrEmail, ErrEmail, true, "Este correo ya está registrado.");
                //     await DisplayAlert("Correo duplicado", "Ese correo ya está registrado. Usa otro diferente.", "OK");
                //     return;
                // }

                // Cualquier otro error => mensaje del backend o genérico
                await ErrorHandler.MostrarErrorUsuario(apiMsg);
                return;
            }
            if (resp.StatusCode == HttpStatusCode.Forbidden || resp.StatusCode == HttpStatusCode.NotFound)
            {
                await DisplayAlert("Permisos insuficientes",
                    "No puedes ver los datos del usuario. Pide que te activen el permiso users.read. " +
                    "Si no necesitas cambiar el rol, también puedes editar sin ver roles.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }




            await DisplayAlert("Listo", "Cambios guardados.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (TaskCanceledException)            // timeout
        {
            await MostrarServidorNoDisponibleAsync("timeout");
        }
        catch (HttpRequestException)             // sin conexión / DNS / refused
        {
            await MostrarServidorNoDisponibleAsync();
        }
        catch (Exception ex)
        {
            // Cualquier otro error inesperado (mantén tu handler)
            await ErrorHandler.MostrarErrorTecnico(ex, "UserEditor – Guardar");
        }
        finally
        {
            SetSaving(false);
        }
    }

    private async void Cancelar_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // ==== Toggles de visibilidad ====
    private void TogglePwd_Clicked(object sender, EventArgs e)
    {
        _showPwd = !_showPwd;
        TxtPassword.IsPassword = !_showPwd;
        SetEye(BtnTogglePwd, _showPwd);
    }

    private void TogglePwd2_Clicked(object sender, EventArgs e)
    {
        _showPwd2 = !_showPwd2;
        TxtPasswordConfirm.IsPassword = !_showPwd2;
        SetEye(BtnTogglePwd2, _showPwd2);
    }
    // Solo letras y espacios (Unicode) en Nombre
    void TxtName_TextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = (Entry)sender;
        var filtered = Regex.Replace(e.NewTextValue ?? "", @"[^\p{L} ]+", "");
        if (filtered != entry.Text)
        {
            var pos = entry.CursorPosition;
            entry.Text = filtered;
            entry.CursorPosition = Math.Min(pos, filtered.Length);
        }
        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
        // en edición: vacío no es error; en crear: vacío sí
        var isEmpty = string.IsNullOrWhiteSpace(entry.Text);
        SetError(BdrName, ErrName, !esEdicion && isEmpty);
    }
    // Correo válido
    void TxtEmail_TextChanged(object sender, TextChangedEventArgs e)
    {
        var t = e.NewTextValue ?? "";
        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);

        bool showError =
            string.IsNullOrEmpty(t) ? !esEdicion       // crear: vacío = error; edición: vacío = ok
                                    : !EsCorreoValido(t);

        SetError(BdrEmail, ErrEmail, showError);

    }

    // Password: si hay texto, mínimo 3
    void TxtPassword_TextChanged(object sender, TextChangedEventArgs e)
    {
        var t = e.NewTextValue ?? "";
        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);
        var invalida = t.Length > 0 && t.Length < 3;
        // en crear, además marca error si está vacía (lo reforzamos al guardar)
        SetError(BdrPwd, ErrPwd, invalida);
        // revalida confirmación
        TxtPasswordConfirm_TextChanged(TxtPasswordConfirm, new TextChangedEventArgs(TxtPasswordConfirm.Text, TxtPasswordConfirm.Text));
    }

    // Confirmación: debe coincidir cuando ambas tengan algo (o en crear)
    void TxtPasswordConfirm_TextChanged(object sender, TextChangedEventArgs e)
    {
        var p1 = TxtPassword.Text ?? "";
        var p2 = TxtPasswordConfirm.Text ?? "";
        var esEdicion = string.Equals(Mode, "edit", StringComparison.OrdinalIgnoreCase);

        bool hayQueValidar = (!esEdicion && (p1.Length > 0 || p2.Length > 0)) ||  // crear
                             (esEdicion && (p1.Length > 0 || p2.Length > 0));      // editar (si quieren cambiar)

        bool error = hayQueValidar && (p1 != p2 || (p1.Length > 0 && p1.Length < 3));
        var msg = (p1.Length > 0 && p1.Length < 3) ? "Mínimo 3 caracteres."
                : "Las contraseñas no coinciden.";
        SetError(BdrPwd2, ErrPwd2, error, msg);
    }

    // PIN: solo 4 dígitos (marcamos error si tiene algo y no es 4)
    void TxtPin_TextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = (Entry)sender;
        var filtered = Regex.Replace(e.NewTextValue ?? "", @"\D+", ""); // solo dígitos
        if (filtered != entry.Text)
        {
            var pos = entry.CursorPosition;
            entry.Text = filtered;
            entry.CursorPosition = Math.Min(pos, filtered.Length);
        }
        var invalido = !string.IsNullOrEmpty(entry.Text) && entry.Text.Length != 4;
        SetError(BdrPin, ErrPin, invalido);
    }




}
