#if ANDROID
using Android.OS;
using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
// Alias para evitar ambigüedad:
using AColor = Android.Graphics.Color;
using Imdeliceapp.Controls;

namespace Imdeliceapp.Platforms.Android;

public static class BorderShadowFix
{
    // Sube/ajusta para más sombra
    const float ElevationDp    = 22f;  // 18–24 se ve “fuerte”
    const float TranslationZDp = 14f;

    public static void Init()
    {
        BorderHandler.Mapper.AppendToMapping(nameof(BorderShadowFix), (handler, view) =>
        {
            if (view is not LoginShadowBorder)
                return;

            var v = handler.PlatformView;
            if (v == null) return;

            // Da outline para que Android calcule la sombra.
            v.OutlineProvider = ViewOutlineProvider.PaddedBounds;
            v.ClipToOutline   = false; // deja que la sombra salga del borde

            var pxElev = v.Context?.ToPixels(ElevationDp) ?? 0f;
            var pxTZ   = v.Context?.ToPixels(TranslationZDp) ?? 0f;
            v.Elevation    = pxElev;
            v.TranslationZ = pxTZ;

            // Sombra negra (API 28+). Usar MÉTODOS, no propiedades.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                var black = AColor.Argb(255, 0, 0, 0);
                v.SetOutlineAmbientShadowColor(black);  // <- método
                v.SetOutlineSpotShadowColor(black);     // <- método
            }

            // Evita animaciones que bajan Z en estados presionado/disabled.
            v.StateListAnimator = null;
        });
    }
}
#endif
