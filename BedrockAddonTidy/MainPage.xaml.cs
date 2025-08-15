using BedrockAddonTidy.Services.AddonFileService;

namespace BedrockAddonTidy;

public partial class MainPage : ContentPage
{
	public MainPage(AddonFileService addonFileService)
	{
		InitializeComponent();
		addonFileService.InitializeAddonFiles();
	}
}
