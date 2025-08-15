using BedrockAddonTidy.Services.AddonFileService;
using BedrockAddonTidy.ViewModels;
using BedrockAddonTidy.Views.ContentViews;
using Microsoft.Extensions.Logging;

namespace BedrockAddonTidy;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("minecraft-five.otf", "MinecraftFive");
			});

		// Services
		builder.Services.AddSingleton<AddonFileService>();

		// Views
		builder.Services.AddTransient<MainPage>();

		builder.Services.AddTransient<AddonListViewModel>();
		builder.Services.AddTransient<AddonListContentView>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
