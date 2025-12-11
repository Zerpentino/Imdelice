using System;
using System.Threading;
using System.Threading.Tasks;
using Imdeliceapp.Generic;
using Imdeliceapp.Pages;
using Imdeliceapp.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Imdeliceapp.Helpers
{
    public static class AuthHelper
    {
        static CancellationTokenSource? _watcherCts;
        static bool _signingOut;

        /// <summary>
        /// Verifica si el token ha expirado usando la fecha almacenada en Preferences["expiracion"]
        /// </summary>
        public static bool TokenExpirado()
        {
            string expiracionStr = Preferences.Default.Get<string>("expiracion", null);

            // Si no existe la preferencia, o está vacía, asumimos que ya expiró
            if (string.IsNullOrWhiteSpace(expiracionStr))
                return true;

            // Intentamos convertir la cadena a DateTime (formato "o")
            if (!DateTime.TryParseExact(expiracionStr, "o", null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime expiracion))
                return true;

            // Comparamos con la hora actual UTC
            return DateTime.UtcNow > expiracion;
        }
        

        /// <summary>
        /// Limpia sesión y redirige al login si el token está vencido
        /// </summary>
        public static async Task<bool> VerificarYRedirigirSiExpirado(Page page)
        {
            if (TokenExpirado())
            {
                await ForzarLogoutAsync("Tu sesión ha caducado. Vuelve a iniciar sesión.");
                return true;            //  ⬅️  ¡expiró!
            }
            return false;
        }

        /// <summary>
        /// Limpia sesión y reemplaza MainPage por LoginPage. Seguro para llamar varias veces.
        /// </summary>
        public static async Task ForzarLogoutAsync(string? mensaje = null)
        {
            if (_signingOut) return;
            _signingOut = true;

            try
            {
                _watcherCts?.Cancel();
                Preferences.Clear();
                SecureStorage.Remove("token");
                Perms.Set(Array.Empty<string>()); // limpia permisos en memoria

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Application.Current.MainPage = new NavigationPage(new LoginPage());
                    if (!string.IsNullOrWhiteSpace(mensaje))
                        await Application.Current.MainPage.DisplayAlert("Sesión expirada", mensaje, "OK");
                });
            }
            finally
            {
                _signingOut = false;
            }
        }

        /// <summary>
        /// Inicia un watcher en background que cierra sesión cuando el token expira.
        /// </summary>
        public static void IniciarWatcherExpiracion()
        {
            // Si ya hay un watcher vivo, no hacemos nada
            if (_watcherCts is { IsCancellationRequested: false }) return;

            _watcherCts = new CancellationTokenSource();
            var ct = _watcherCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), ct);
                        if (ct.IsCancellationRequested) break;

                        if (TokenExpirado())
                        {
                            await ForzarLogoutAsync("Tu sesión expiró. Ingresa de nuevo.");
                            break;
                        }
                    }
                }
                catch (TaskCanceledException) { }
            }, ct);
        }

        public static void ReiniciarWatcherExpiracion()
        {
            _watcherCts?.Cancel();
            IniciarWatcherExpiracion();
        }

        public static void DetenerWatcher()
        {
            _watcherCts?.Cancel();
            _watcherCts = null;
        }

        /// <summary>
        /// Devuelve el token si existe y no está expirado; de lo contrario cierra sesión y devuelve null.
        /// </summary>
        public static async Task<string?> ObtenerTokenValidoAsync(Page? page = null)
        {
            if (TokenExpirado())
            {
                await ForzarLogoutAsync("Tu sesión expiró. Ingresa de nuevo.");
                return null;
            }

            var secure = await SecureStorage.GetAsync("token");
            if (!string.IsNullOrWhiteSpace(secure)) return secure;

            var stored = Preferences.Default.Get("token", string.Empty);
            if (!string.IsNullOrWhiteSpace(stored)) return stored;

            await ForzarLogoutAsync("Tu sesión expiró. Ingresa de nuevo.");
            return null;
        }
    }
}
