namespace BedrockAddonTidy.Helpers;

public class ServiceProviderHelper
{
	public static TService GetService<TService>() where TService : class
	{
		if (ServiceProvider.GetService(typeof(TService)) is TService service)
		{
			return service;
		}
		throw new InvalidOperationException($"Service of type {typeof(TService).Name} not found.");
	}

	private static IServiceProvider ServiceProvider => MauiWinUIApplication.Current.Services;
}
