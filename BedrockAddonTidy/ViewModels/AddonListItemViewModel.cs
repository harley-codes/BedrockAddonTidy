using BedrockAddonTidy.ObjectModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BedrockAddonTidy.ViewModels;

public partial class AddonListItemViewModel : ObservableObject
{
	[ObservableProperty]
	public partial Guid AddonId { get; set; }

	[ObservableProperty]
	public partial AddonFileModel AddonFile { get; set; }

	[ObservableProperty]
	public partial string[] AddonWarnings { get; set; } = [];
}
