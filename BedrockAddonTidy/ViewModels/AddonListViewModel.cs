using System.Collections.ObjectModel;
using BedrockAddonTidy.Services.AddonFileService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BedrockAddonTidy.ViewModels;

public partial class AddonListViewModel : ObservableObject
{
	private readonly AddonFileService _addonFileService;

	private static readonly string[] _addonFileExtensions = [".mcaddon", ".mcpack"];

	[ObservableProperty]
	public partial AddonListItemViewModel? SelectedItem { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(AddonFileListSorted))]
	private partial ObservableCollection<AddonListItemViewModel> AddonFileList { get; set; } = [];

	public List<AddonListItemViewModel> AddonFileListSorted => [.. AddonFileList.OrderByDescending(x => x.AddonFile?.UpdateDate)];

	public AddonListViewModel(AddonFileService addonFileService)
	{
		_addonFileService = addonFileService;
		_addonFileService.AddonFilePropertiesChanged += AddonFileService_OnAddonFilePropertiesChanged;
		_addonFileService.AddonFileValidationWarningsChanged += AddonFileService_OnAddonFileValidationWarningsChanged;
		_addonFileService.AddonFileSelected += AddonFileService_AddonFileSelected;
	}

	private void AddonFileService_AddonFileSelected(object? sender, Guid? guid)
	{
		if (guid.HasValue && guid.Value == SelectedItem?.AddonId) return;

		if (!guid.HasValue)
		{
			SelectedItem = null;
			return;
		}

		var addonFile = _addonFileService.GetAddonFile(guid.Value);
		SelectedItem = new AddonListItemViewModel
		{
			AddonId = addonFile.Id,
			AddonFile = addonFile,
			AddonWarnings = _addonFileService.GetAddonFileValidationWarnings(guid.Value)
		};
	}

	private void AddonFileService_OnAddonFilePropertiesChanged(object? sender, AddonFileEventTypes.AddonFilePropertiesChangedEventArgs e)
	{
		switch (e.ChangeType)
		{
			case AddonFileEventTypes.EventChangeType.Created:
			case AddonFileEventTypes.EventChangeType.Updated:
				if (AddonFileList.Any(x => x.AddonId == e.AddonId))
				{
					var existingAddon = AddonFileList.First(x => x.AddonId == e.AddonId);
					existingAddon.AddonFile = _addonFileService.GetAddonFile(e.AddonId);
				}
				else
				{
					AddonFileList.Add(new AddonListItemViewModel
					{
						AddonId = e.AddonId,
						AddonFile = _addonFileService.GetAddonFile(e.AddonId),
					});
				}
				break;
			case AddonFileEventTypes.EventChangeType.Deleted:
				if (AddonFileList.Any(x => x.AddonId == e.AddonId))
				{
					var addonToRemove = AddonFileList.First(x => x.AddonId == e.AddonId);
					AddonFileList.Remove(addonToRemove);
				}
				break;
		}
		OnPropertyChanged(nameof(AddonFileList));
		OnPropertyChanged(nameof(AddonFileListSorted));
	}

	private void AddonFileService_OnAddonFileValidationWarningsChanged(object? sender, AddonFileEventTypes.AddonFileValidationWarningsChangedEventArgs e)
	{
		switch (e.ChangeType)
		{
			case AddonFileEventTypes.EventChangeType.Created:
			case AddonFileEventTypes.EventChangeType.Updated:
				if (AddonFileList.Any(x => x.AddonId == e.AddonId))
				{
					var existingAddon = AddonFileList.First(x => x.AddonId == e.AddonId);
					existingAddon.AddonWarnings = _addonFileService.GetAddonFileValidationWarnings(e.AddonId);
				}
				else
				{
					AddonFileList.Add(new AddonListItemViewModel
					{
						AddonId = e.AddonId,
						AddonWarnings = _addonFileService.GetAddonFileValidationWarnings(e.AddonId)
					});
				}
				break;
			case AddonFileEventTypes.EventChangeType.Deleted:
				{
					var existingAddon = AddonFileList.First(x => x.AddonId == e.AddonId);
					existingAddon.AddonWarnings = [];
				}
				break;
		}
		OnPropertyChanged(nameof(AddonFileList));
		OnPropertyChanged(nameof(AddonFileListSorted));
	}

	[RelayCommand]
	private async Task ImportAddonHandler()
	{
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
			_addonFileService.ImportNewAddon(fileResult.FullPath);
		}
	}

	partial void OnSelectedItemChanged(AddonListItemViewModel? oldValue, AddonListItemViewModel? newValue)
	{
		if (newValue is null) return;
		_addonFileService.SelectAddonFile(newValue.AddonId);
	}
}