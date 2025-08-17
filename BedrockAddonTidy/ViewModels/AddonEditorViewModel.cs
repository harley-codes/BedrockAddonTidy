using System;
using BedrockAddonTidy.Services.AddonFileService;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage.Pickers;

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
		_addonFileService.UpdateAddonSrc(SelectedAddon.AddonFile);
	}

	[RelayCommand]
	private async Task DownloadAddonHandler()
	{
		if (SelectedAddon is null) return;
		var page = Application.Current!.Windows[0].Page!;
		var result = await page.DisplayAlert("Warning", "Are you sure you want download the addon, this will aso save current changes?", "Continue", "Cancel");
		if (!result) return;
		_addonFileService.UpdateAddonFile(SelectedAddon.AddonFile);

		var warnings = _addonFileService.GetAddonFileValidationWarnings(SelectedAddon.AddonId);
		if (warnings.Length > 0)
		{
			await page.DisplayAlert("Error", $"Cannot download addon while there are errors.", "OK");
			return;
		}
		var newFileName = $"{SelectedAddon.AddonFile.Name} - {SelectedAddon.AddonFile.Author}.{SelectedAddon.AddonFile.AddonType.ToString().ToLower()}";
		var fileResult = await FileSaver.Default.SaveAsync(newFileName, Stream.Null, new CancellationToken());
		if (fileResult.IsSuccessful)
		{
			_addonFileService.UpdateAddonSrc(SelectedAddon.AddonFile);
			_addonFileService.DownloadAddonFile(SelectedAddon.AddonId, fileResult.FilePath);
			if (File.Exists(fileResult.FilePath))
			{
				var parentFolder = Path.GetDirectoryName(fileResult.FilePath);
				if (parentFolder != null)
				{
					await Launcher.Default.OpenAsync(parentFolder);
				}
			}
		}
		else
		{
			await page.DisplayAlert("Error", "Failed to pick the addon file location.", "OK");
			return;
		}
	}

	[RelayCommand]
	private async Task MinecraftImportAddonHandler()
	{
		if (SelectedAddon is null) return;
		var page = Application.Current!.Windows[0].Page!;
		var result = await page.DisplayAlert("Warning", "Are you sure you want import the addon into minecraft, this will aso save current changes?", "Continue", "Cancel");
		if (!result) return;
		_addonFileService.UpdateAddonFile(SelectedAddon.AddonFile);

		var warnings = _addonFileService.GetAddonFileValidationWarnings(SelectedAddon.AddonId);
		if (warnings.Length > 0)
		{
			await page.DisplayAlert("Error", $"Cannot import addon while there are errors.", "OK");
			return;
		}
		var newFileName = $"{SelectedAddon.AddonFile.Name} - {SelectedAddon.AddonFile.Author} - {Guid.NewGuid()}.{SelectedAddon.AddonFile.AddonType.ToString().ToLower()}";

		var tempFilePath = Path.Combine(Path.GetTempPath(), newFileName);

		_addonFileService.UpdateAddonSrc(SelectedAddon.AddonFile);
		_addonFileService.DownloadAddonFile(SelectedAddon.AddonId, tempFilePath);
		if (File.Exists(tempFilePath))
		{
			await Launcher.Default.OpenAsync(tempFilePath);
		}
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
	}

	[RelayCommand]
	private async Task OpenAddonFolderHandler()
	{
		if (SelectedAddon is null) return;
		await Launcher.Default.OpenAsync(SelectedAddon.AddonFile.SrcPath);
	}
}
