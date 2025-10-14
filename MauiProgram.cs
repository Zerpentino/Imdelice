using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Microsoft.Maui.LifecycleEvents;
using QuestPDF.Infrastructure;



namespace Imdeliceapp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
#if IOS
		Imdeliceapp.Platforms.iOS.EntryNoBorder.Init();
		Imdeliceapp.Platforms.iOS.BorderShadowFix.Init();   // ⬅️ nuevo
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
