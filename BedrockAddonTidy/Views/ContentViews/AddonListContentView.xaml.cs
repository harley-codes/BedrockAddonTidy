using BedrockAddonTidy.ViewModels;

namespace BedrockAddonTidy.Views.ContentViews;

public partial class AddonListContentView : ContentView
{

	public AddonListContentView()
	{
		InitializeComponent();
		BindingContext = new AddonListContentViewModel();
	}
}