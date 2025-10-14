// Helpers/StatusBarHelper.cs  (o Platforms/Android/, donde prefieras)
#if ANDROID
using Android.Views;
using Microsoft.Maui.Graphics;          // Color, GetLuminosity

namespace Imdeliceapp.Platforms.Android;

public static class StatusBarHelper
{
    public static void ApplyColor(Color mauiColor)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var window   = activity?.Window;          // <- puede ser null al inicio
        if (window is null)
            return;                               // simplemente salimos


        // --- MAUI.Color  →  Android.Graphics.Color ---------------------------
        var a = (int)(mauiColor.Alpha * 255);
        var r = (int)(mauiColor.Red   * 255);
        var g = (int)(mauiColor.Green * 255);
        var b = (int)(mauiColor.Blue  * 255);

        var androidColor = global::Android.Graphics.Color.Argb(a, r, g, b);

        window.SetStatusBarColor(androidColor);
        window.SetNavigationBarColor(androidColor);

        // --- íconos claros / oscuros -----------------------------------------
        bool fondoClaro = mauiColor.GetLuminosity() > 0.5;

        var flags = (SystemUiFlags)window.DecorView.SystemUiVisibility;
        const SystemUiFlags DarkIcons =
            SystemUiFlags.LightStatusBar | SystemUiFlags.LightNavigationBar;

        flags = fondoClaro ? flags |  DarkIcons     // fondo claro → iconos oscuros
                           : flags & ~DarkIcons;    // fondo oscuro → iconos claros

        window.DecorView.SystemUiVisibility = (StatusBarVisibility)flags;
    }
}
#endif
