using System.Collections.ObjectModel;
using BedrockAddonTidy.ObjectModels;
using BedrockAddonTidy.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BedrockAddonTidy.ViewModels;

public partial class AddonListContentViewModel : ObservableObject
{
	private static readonly string[] _addonFileExtensions = [".mcaddon", ".mcpack"];

	[ObservableProperty]
	public partial string? Name { get; set; }

	[ObservableProperty]
	private partial ObservableCollection<AddonFileModel> AddonFiles { get; set; } = [];

	[ObservableProperty]
	public partial bool DisableImportButton { get; set; }

	[RelayCommand]
	private async Task ImportAddonHandler()
	{
		DisableImportButton = true;

		var pickOptions = new PickOptions
		{
			FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
			{
				{ DevicePlatform.WinUI, _addonFileExtensions},
			}),
			PickerTitle = "Select Addon File"
		};

		var fileResult = await FilePicker.Default.PickAsync(pickOptions);

		if (fileResult is not null)
		{
			var addonFile = AddonFilesHandler.ImportAddon(fileResult.FullPath);

			AddonFiles.Insert(0, addonFile);
		}

		DisableImportButton = false;
	}
}