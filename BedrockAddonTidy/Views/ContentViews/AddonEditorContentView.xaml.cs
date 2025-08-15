using BedrockAddonTidy.Helpers;
using BedrockAddonTidy.ViewModels;

namespace BedrockAddonTidy.Views.ContentViews;

public partial class AddonEditorContentView : ContentView
{
	public AddonEditorContentView()
	{
		InitializeComponent();
		BindingContext = ServiceProviderHelper.GetService<AddonEditorViewModel>();
	}
}