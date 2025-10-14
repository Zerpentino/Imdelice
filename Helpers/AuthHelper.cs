using System;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
//using Imdeliceapp.Model;
using Imdeliceapp.Generic;
using System.Runtime.Intrinsics.Arm;
using Microsoft.Maui.Storage;
using System.Text.Json;
using Imdeliceapp.Pages;
using CommunityToolkit.Mvvm.Messaging;

namespace Imdeliceapp.Helpers
{
    public static class AuthHelper
    {
        /// <summary>
        /// Verifica si el token ha expirado usando la fecha almacenada en Preferences["expiracion"]
        /// </summary>
        public static bool TokenExpirado()
        {
            string expiracionStr = Preferences.Default.Get<string>("expiracion", null);

            // Si no existe la preferencia, o está vacía, asumimos que ya expiró
            if (string.IsNullOrWhiteSpace(expiracionStr))
                return true;

            // Intentamos convertir la cadena a DateTime
            if (!DateTime.TryParse(expiracionStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiracion))
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
                await page.DisplayAlert("Sesión expirada", "Tu sesión ha caducado. Vuelve a iniciar sesión.", "OK");
                Preferences.Clear();
                Application.Current.MainPage = new LoginPage();
                return true;            //  ⬅️  ¡expiró!
            }
            return false;
        }
    }
}
