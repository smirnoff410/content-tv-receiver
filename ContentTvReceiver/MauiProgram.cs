using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace ContentTvReceiver;

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
			});
		var a = Assembly.GetExecutingAssembly();
		using var stream = a.GetManifestResourceStream("ContentTvReceiver.appsettings.json");

		var config = new ConfigurationBuilder()
					.AddJsonStream(stream)
					.Build();


		builder.Configuration.AddConfiguration(config);

		builder.Services.AddTransient<MainPage>();

		return builder.Build();
	}
}

