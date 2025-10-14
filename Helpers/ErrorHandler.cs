using System;
using Microsoft.Maui.Storage;
using System.Threading.Tasks;
using Imdeliceapp.Model;
using Imdeliceapp.Generic;
using System.Runtime.Intrinsics.Arm;
using Microsoft.Maui.Storage;
using System.Text.Json;
using Imdeliceapp.Pages;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Net;

namespace Imdeliceapp.Helpers;

public static class ErrorHandler
{
    public static async Task MostrarErrorUsuario(string mensaje )
    {
        await Application.Current.MainPage.DisplayAlert("Aviso", mensaje, "OK");
    }

    public static async Task MostrarErrorTecnico(Exception ex, string contexto = "")
    {
#if DEBUG
        await Application.Current.MainPage.DisplayAlert("Error técnico", $"{contexto}\n{ex.Message}", "OK");
#else
        Console.WriteLine($"[ERROR] {contexto}: {ex.Message}");
        //await MostrarErrorUsuario();
#endif
    }

 public static string ObtenerMensajeHttp(HttpResponseMessage response,
                                        string body = "")
{
    if (!string.IsNullOrWhiteSpace(body))
    {
        try
        {
            using var doc = JsonDocument.Parse(body);

            string? msg    = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;
            string? detail = null;

            // acepta "details" o "error"
            if (doc.RootElement.TryGetProperty("details", out var d))
                detail = d.GetString();
            else if (doc.RootElement.TryGetProperty("error", out var e))
                detail = e.GetString();

            if (!string.IsNullOrWhiteSpace(msg))
                return string.IsNullOrWhiteSpace(detail) ? msg : $"{msg}. {detail}";
        }
        catch { /* body no era JSON */ }
    }
    return GenericFallback(response.StatusCode);
}

    private static string GenericFallback(HttpStatusCode code) => code switch
    {
        HttpStatusCode.Unauthorized      => "Tu sesión expiró.",
        HttpStatusCode.Forbidden         => "No tienes permisos para esta acción.",
        HttpStatusCode.NotFound          => "Recurso no encontrado.",
        HttpStatusCode.InternalServerError=> "El servidor presentó un error. Intenta más tarde.",
        HttpStatusCode.Conflict         => "Conflicto de datos. Verifica e intenta nuevamente.",
        _                                => "No se pudo completar la operación. Intenta nuevamente."
    };
  public static string Friendly(string raw)
{
    if (raw.Contains("contraseña", StringComparison.OrdinalIgnoreCase) &&
        raw.Contains("correcta",   StringComparison.OrdinalIgnoreCase))
        return "La contraseña de la llave privada es incorrecta.";

    if (raw.Contains("No es PEM ni DER", StringComparison.OrdinalIgnoreCase))
        return "La llave privada (.key) parece estar dañada o en formato no válido.";

    if (raw.Contains("Certificado expirado", StringComparison.OrdinalIgnoreCase))
        return "El certificado (.cer) está vencido.";

    return raw;
}

    public static async Task MostrarMensajeExito(string mensaje)
    {
        await Application.Current.MainPage.DisplayAlert("✔️ Éxito", mensaje, "OK");
    }


}

