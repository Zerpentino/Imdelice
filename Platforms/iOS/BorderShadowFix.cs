#if IOS
using CoreGraphics;
using UIKit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;   // ToCGColor(), etc.

namespace Imdeliceapp.Platforms.iOS;

public static class BorderShadowFix
{
    public static void Init()
    {
        // Se ejecuta una vez para TODOS los <Border>
        BorderHandler.Mapper.AppendToMapping("ShadowFix", (handler, view) =>
        {
            var layer = handler.PlatformView.Layer;

            /* ---- 1. Ajustes básicos de la capa ---- */
            layer.MasksToBounds = false;   // Deja que la sombra salga
            layer.CornerRadius  = 12;      // Igual que tu XAML
            layer.BackgroundColor = UIColor.White.CGColor; // O usa un color fijo #FFF

            /* ---- 2. Sombra consistente ---- */
            layer.ShadowColor   = UIColor.Black.CGColor;
            layer.ShadowOpacity = 0.25f;   // 25 % opacidad
            layer.ShadowRadius  = 10f;     // Difuminado
            layer.ShadowOffset  = new CGSize(0, 4); // 4 pt abajo
            // ⚠️  No establecemos ShadowPath; así se recalcula cuando cambie el tamaño
        });
    }
}
#endif
