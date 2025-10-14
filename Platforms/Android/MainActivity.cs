using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Util;
using Imdeliceapp.Platforms.Android;   // helper
using MauiApp = Microsoft.Maui.Controls.Application;

namespace Imdeliceapp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {

        base.OnCreate(savedInstanceState);
        // Color inicial coherente con el tema
        var theme = Microsoft.Maui.Controls.Application.Current!.RequestedTheme;
        var initial = theme == AppTheme.Dark ? Colors.Black : Colors.White;
        StatusBarHelper.ApplyColor(initial);

        // HandleIntent(Intent);
        // CreateNotificationChannelIfNeeded();

    }
    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);

        // HandleIntent(intent);


    }
    //     private void HandleIntent(Intent intent)
    // {
    //     FirebaseCloudMessagingImplementation.OnNewIntent(intent);
    //     var TAG = "MiApp"; // Etiqueta común para la clase
    //     var uri = intent?.Data;   
    //     if (uri != null)
    //     {
    //     string path = uri.Path?.ToLower();

    //     Log.Debug(TAG, $"Deep link received: {uri}");

    //     // Esto es lo nuevo que falta:
    //     Microsoft.Maui.Controls.Application.Current?.Dispatcher.Dispatch(async () =>
    //     {
    //        var segments = uri.Path?.Split('/', StringSplitOptions.RemoveEmptyEntries);

    //             if (segments != null && segments.Length >= 2)
    //             {
    //                string accion = segments[0].ToLower();    // "pago-cancelado"
    //                 string idRecibo = segments[1];            // "258976"

    //                 Log.Debug(TAG, $"Acción: {accion}, ID Recibo: {idRecibo}");

    //                 string route = accion switch
    //                 {
    //                     "pago-exitoso" => $"pago-exitoso?reciboId={idRecibo}",
    //                     "pago-cancelado" => $"pago-cancelado?reciboId={idRecibo}",
    //                     _ => null
    //                 };

    //                 if (!string.IsNullOrWhiteSpace(route))
    //                 {
    //                     await Shell.Current.GoToAsync(route);
    //                 }
    //                 else
    //                 {
    //                     Log.Debug(TAG, "Ruta de acción no reconocida");
    //                 }
    //             }
    //             else
    //             {
    //                 Log.Debug(TAG, "URL malformada o sin ID de recibo");
    //             }

           
    //     });
        
    // }
    // else
    // {
    //     Log.Debug(TAG, "No URI found in the intent");
    // }
    


    //     var action = intent?.Action;
    //     var data = intent?.Data;

    //         if (Intent?.Action == action && data != null) {
    //         var itemID = data.LastPathSegment;
    //             // Procesar el numeroderecibo: "
    //         }
    //     // Maneja la lógica del deep link
        
        
    //     Log.Debug(TAG, $"Received deep link: {uri}");
        
       
    // }

}
