using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;
using QuestPDF.Infrastructure;
using Microsoft.Maui.Handlers;
#if ANDROID
using AndroidX.AppCompat.Widget;
#endif


namespace Imdeliceapp;

public static class MauiProgram
{
	
	public static MauiApp CreateMauiApp()
	{
#if IOS
			Imdeliceapp.Platforms.iOS.EntryNoBorder.Init();
			Imdeliceapp.Platforms.iOS.BorderShadowFix.Init();   // ⬅️ nuevo
#endif
#if ANDROID
			Imdeliceapp.Platforms.Android.BorderShadowFix.Init();
#endif

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
