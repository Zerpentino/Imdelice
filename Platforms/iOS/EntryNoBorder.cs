#if IOS
using Microsoft.Maui.Handlers;
using UIKit;

namespace Imdeliceapp.Platforms.iOS;

public static class EntryNoBorder
{
    public static void Init()
    {
        EntryHandler.Mapper.AppendToMapping("NoBorder", (handler, view) =>
        {
            // Quita el borde nativo
            handler.PlatformView.BorderStyle = UITextBorderStyle.None;
            // Asegura fondo transparente para que se vea el Frame
            handler.PlatformView.BackgroundColor = UIColor.Clear;
        });
    }
}
#endif
