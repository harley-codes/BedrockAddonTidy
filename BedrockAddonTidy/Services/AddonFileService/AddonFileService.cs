using System;
using System.IO.Compression;
using System.Text.Json;
using BedrockAddonTidy.Enums;
using BedrockAddonTidy.ObjectModels;

namespace BedrockAddonTidy.Services.AddonFileService;

public class AddonFileService
{
	private bool _isInitialized = false;
	private readonly Dictionary<Guid, AddonFileModel> addonFileProperties;
	private readonly Dictionary<Guid, string[]> addonFileValidationWarnings;

	public delegate void AddonFilePropertiesChangedHandler(object? sender, AddonFileEventTypes.AddonFilePropertiesChangedEventArgs e);
	public event AddonFilePropertiesChangedHandler? AddonFilePropertiesChanged;

	public delegate void AddonFileValidationWarningsChangedHandler(object? sender, AddonFileEventTypes.AddonFileValidationWarningsChangedEventArgs e);
	public event AddonFileValidationWarningsChangedHandler? AddonFileValidationWarningsChanged;

	public delegate void AddonFileSelectedHandler(object? sender, Guid? guid);
	public event AddonFileSelectedHandler? AddonFileSelected;

	public AddonFileService()
	{
		addonFileProperties = [];
		addonFileValidationWarnings = [];
		AddonFilePropertiesChanged += AddonFileService_AddonFilePropertiesChanged;
	}

	~AddonFileService()
	{
		AddonFilePropertiesChanged -= AddonFileService_AddonFilePropertiesChanged;
	}

	private void AddonFileService_AddonFilePropertiesChanged(object? sender, AddonFileEventTypes.AddonFilePropertiesChangedEventArgs e)
	{
		var currentWarnings = AddonFileHelper.GetAddonFileValidationWarnings(addonFileProperties);

		// Check removed warnings
		var removedWarnings = addonFileValidationWarnings
			.Where(kvp => !currentWarnings.ContainsKey(kvp.Key))
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		foreach (var removedWarning in removedWarnings)
		{
			addonFileValidationWarnings.Remove(removedWarning.Key);
			AddonFileValidationWarningsChanged?.Invoke(this, new AddonFileEventTypes.AddonFileValidationWarningsChangedEventArgs(removedWarning.Key, AddonFileEventTypes.EventChangeType.Deleted));
		}

		// Check created warnings
		var createdWarnings = currentWarnings
			.Where(kvp => !addonFileValidationWarnings.ContainsKey(kvp.Key))
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		foreach (var createdWarning in createdWarnings)
		{
			addonFileValidationWarnings[createdWarning.Key] = createdWarning.Value;
			AddonFileValidationWarningsChanged?.Invoke(this, new AddonFileEventTypes.AddonFileValidationWarningsChangedEventArgs(createdWarning.Key, AddonFileEventTypes.EventChangeType.Created));
		}

		// Check updated warnings
		var updatedWarnings = currentWarnings
			.Where(kvp => addonFileValidationWarnings.ContainsKey(kvp.Key))
			.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		foreach (var updatedWarning in updatedWarnings)
		{
			addonFileValidationWarnings[updatedWarning.Key] = updatedWarning.Value;
			AddonFileValidationWarningsChanged?.Invoke(this, new AddonFileEventTypes.AddonFileValidationWarningsChangedEventArgs(updatedWarning.Key, AddonFileEventTypes.EventChangeType.Updated));
		}
	}

	public void SelectAddonFile(Guid? addonId)
	{
		if (addonId.HasValue && !addonFileProperties.ContainsKey(addonId.Value))
			throw new KeyNotFoundException($"Addon file with ID {addonId} not found.");
		AddonFileSelected?.Invoke(this, addonId);
	}

	public void InitializeAddonFiles()
	{
		if (_isInitialized)
			return;
		_isInitialized = true;

		foreach (var addon in AddonFileHelper.GetExistingAddonFiles())
		{
			addonFileProperties[addon.Id] = addon;
			AddonFilePropertiesChanged?.Invoke(this, new AddonFileEventTypes.AddonFilePropertiesChangedEventArgs(addon.Id, AddonFileEventTypes.EventChangeType.Created));
		}
	}

	public AddonFileModel GetAddonFile(Guid addonId)
	{
		addonFileProperties.TryGetValue(addonId, out var addonFile);
		return addonFile ?? throw new KeyNotFoundException($"Addon file with ID {addonId} not found.");
	}

	public string[] GetAddonFileValidationWarnings(Guid addonId)
	{
		addonFileValidationWarnings.TryGetValue(addonId, out var warnings);
		return warnings ?? [];
	}

	public AddonFileModel ImportNewAddon(string addonPath)
	{
		var addonFile = AddonFileHelper.ImportNewAddon(addonPath)
			?? throw new InvalidOperationException("Failed to import addon.");

		addonFileProperties[addonFile.Id] = addonFile;
		AddonFilePropertiesChanged?.Invoke(this, new AddonFileEventTypes.AddonFilePropertiesChangedEventArgs(addonFile.Id, AddonFileEventTypes.EventChangeType.Created));

		return addonFile;
	}

	public void RemoveAddonFile(Guid addonId)
	{
		if (!addonFileProperties.ContainsKey(addonId))
		{
			throw new KeyNotFoundException($"Addon file with ID {addonId} not found.");
		}

		AddonFileHelper.DeleteAddonFiles(addonId);
		addonFileProperties.Remove(addonId);
		AddonFilePropertiesChanged?.Invoke(this, new AddonFileEventTypes.AddonFilePropertiesChangedEventArgs(addonId, AddonFileEventTypes.EventChangeType.Deleted));
	}

	public void UpdateAddonFile(AddonFileModel updatedAddonFile)
	{
		var addonId = updatedAddonFile.Id;

		if (!addonFileProperties.ContainsKey(addonId))
		{
			throw new KeyNotFoundException($"Addon file with ID {addonId} not found.");
		}

		AddonFileHelper.SaveAddonFileProperties(updatedAddonFile);
		addonFileProperties[addonId] = updatedAddonFile;
		AddonFilePropertiesChanged?.Invoke(this, new AddonFileEventTypes.AddonFilePropertiesChangedEventArgs(addonId, AddonFileEventTypes.EventChangeType.Updated));
	}
}
