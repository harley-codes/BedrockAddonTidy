using System;
using BedrockAddonTidy.Services.AddonFileService;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BedrockAddonTidy.ViewModels;

public partial class AddonEditorViewModel : ObservableObject
{
	private readonly AddonFileService _addonFileService;

	[ObservableProperty]
	public partial AddonListItemViewModel? SelectedAddon { get; set; }

	public AddonEditorViewModel(AddonFileService addonFileService)
	{
		_addonFileService = addonFileService;
		_addonFileService.AddonFileSelected += AddonFileService_AddonFileSelected;
		_addonFileService.AddonFilePropertiesChanged += AddonFileService_AddonFilePropertiesChanged;
		_addonFileService.AddonFileValidationWarningsChanged += AddonFileService_AddonFileValidationWarningsChanged;
	}

	private void AddonFileService_AddonFileValidationWarningsChanged(object? sender, AddonFileEventTypes.AddonFileValidationWarningsChangedEventArgs e)
	{
		if (SelectedAddon is null || SelectedAddon.AddonId != e.AddonId)
			return;
		SelectedAddon.AddonWarnings = _addonFileService.GetAddonFileValidationWarnings(e.AddonId);
		OnPropertyChanged(nameof(SelectedAddon));
	}

	private void AddonFileService_AddonFilePropertiesChanged(object? sender, AddonFileEventTypes.AddonFilePropertiesChangedEventArgs e)
	{
		if (SelectedAddon is null || SelectedAddon.AddonId != e.AddonId || e.ChangeType != AddonFileEventTypes.EventChangeType.Updated)
			return;

		var addonFile = _addonFileService.GetAddonFile(e.AddonId);
		SelectedAddon = new AddonListItemViewModel
		{
			AddonId = addonFile.Id,
			AddonFile = addonFile,
			AddonWarnings = _addonFileService.GetAddonFileValidationWarnings(e.AddonId)
		};
	}

	private void AddonFileService_AddonFileSelected(object? sender, Guid? guid)
	{
		if (guid.HasValue)
		{
			var addonFile = _addonFileService.GetAddonFile(guid.Value);
			if (addonFile != null)
			{
				SelectedAddon = new AddonListItemViewModel
				{
					AddonId = addonFile.Id,
					AddonFile = addonFile,
					AddonWarnings = _addonFileService.GetAddonFileValidationWarnings(guid.Value)
				};
			}
			else
			{
				SelectedAddon = null;
			}
		}
		else
		{
			SelectedAddon = null;
		}
	}

	[RelayCommand]
	private async Task SaveChangesHandler()
	{
		if (SelectedAddon is null) return;
		var page = Application.Current!.Windows[0].Page!;
		var result = await page.DisplayAlert("Warning", "Are you sure you want to save this addon?", "Continue", "Cancel");
		if (!result) return;
		_addonFileService.UpdateAddonFile(SelectedAddon.AddonFile);
	}

	[RelayCommand]
	private void DeselectAddonHandler()
	{
		_addonFileService.SelectAddonFile(null);
	}

	[RelayCommand]
	private async Task DeleteAddonHandler()
	{
		if (SelectedAddon is null) return;
		var page = Application.Current!.Windows[0].Page!;
		var result = await page.DisplayAlert("Warning", "Are you sure you want to delete this addon?", "Continue", "Cancel");
		if (!result) return;
		_addonFileService.RemoveAddonFile(SelectedAddon.AddonId);
		DeselectAddonHandler();
	}

	[RelayCommand]
	private async Task NewResourcePackGuidHandler()
	{
		if (SelectedAddon is null) return;
		var page = Application.Current!.Windows[0].Page!;
		var result = await page.DisplayAlert("Warning", "Are you sure you want to generate a new resource pack id?", "Continue", "Cancel");
		if (!result) return;
		SelectedAddon.AddonFile.ResourcePackGuid = Guid.NewGuid().ToString();
		_addonFileService.UpdateAddonFile(SelectedAddon.AddonFile);
		// OnPropertyChanged(nameof(SelectedAddon));
	}

	[RelayCommand]
	private async Task NewBehaviorPackGuidHandler()
	{
		if (SelectedAddon is null) return;
		var page = Application.Current!.Windows[0].Page!;
		var result = await page.DisplayAlert("Warning", "Are you sure you want to generate a new behavior pack id?", "Continue", "Cancel");
		if (!result) return;
		SelectedAddon.AddonFile.BehaviorPackGuid = Guid.NewGuid().ToString();
		_addonFileService.UpdateAddonFile(SelectedAddon.AddonFile);
		// OnPropertyChanged(nameof(SelectedAddon));
	}
}
