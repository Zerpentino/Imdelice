using Microsoft.Maui;            // IView
using Microsoft.Maui.Controls;   // FileResult
using Microsoft.Maui.Graphics;
using Imdeliceapp.Pages;
// using Imdeliceapp.Model;
using Microsoft.Maui.Storage;
using Microsoft.Maui;              // IView
using Microsoft.Maui.Controls;     // FileResult
using System.IO;                   // Path, File
using System.Globalization;
#if ANDROID
using Android.Graphics;
using Android.Views;
#elif IOS
using UIKit;
#endif

namespace Imdeliceapp.Helpers;

public static class ViewCaptureExtensions
{
    /// <summary>Captura la vista a PNG, la guarda en caché y devuelve la ruta.</summary>
    public static async Task<string?> CaptureToPngFileAsync(this IView view, string? prefix = null)
    {
        var image = await view.CaptureAsync();               // built-in MAUI ✅
        if (image is null) return null;

        await using var src = await image.OpenReadAsync();
        var fileName = $"{(prefix ?? "recibo")}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var fullPath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);

        await using (var fs = System.IO.File.Create(fullPath))
            await src.CopyToAsync(fs);

        return fullPath;                                     // ruta lista
    }
}