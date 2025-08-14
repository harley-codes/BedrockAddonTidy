using System;
using System.IO.Compression;
using System.Text.Json;
using BedrockAddonTidy.Enums;
using BedrockAddonTidy.ObjectModels;

namespace BedrockAddonTidy.Utils;

public class AddonFilesHandler
{
	private static readonly JsonSerializerOptions serializerOptions = new()
	{
		ReadCommentHandling = JsonCommentHandling.Skip
	};

	public static AddonFileModel ImportAddon(string addonPath)
	{
		if (!File.Exists(addonPath))
		{
			throw new FileNotFoundException("Addon file not found.", addonPath);
		}

		var fileExtension = new FileInfo(addonPath).Extension.ToLowerInvariant();
		var addonType = fileExtension switch
		{
			".mcaddon" => AddonType.McAddon,
			".mcpack" => AddonType.McPack,
			_ => throw new InvalidOperationException("Invalid addon file type. Only .mcaddon and .mcpack files are supported.")
		};

		var addonId = Guid.NewGuid();

		// Get directory paths for addon storage
		var addonDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BedrockAddonTidy", "Addons", addonId.ToString());
		var srcDirectory = Path.Combine(addonDirectory, "src");

		// Create the directories exist
		try
		{
			Directory.CreateDirectory(addonDirectory);
			Directory.CreateDirectory(srcDirectory);
		}
		catch (IOException ex)
		{
			throw new IOException("Failed to create addon directories.", ex);
		}

		// Extract the addon file (zip) into the src directory
		try
		{
			using var archive = ZipFile.OpenRead(addonPath);
			archive.ExtractToDirectory(srcDirectory);
		}
		catch
		{
			throw new InvalidOperationException("Failed to extract the addon file. Ensure it is a valid .mcaddon or .mcpack file.");
		}

		var addonFile = ProcessAddonSrcFiles(srcDirectory, addonType, addonId);

		// Serialize the addon file information
		try
		{
			var addonFilePath = Path.Combine(addonDirectory, "addon.json");
			var json = JsonSerializer.Serialize(addonFile);
			File.WriteAllText(addonFilePath, json);
		}
		catch (Exception ex)
		{
			throw new IOException("Failed to write addon file information.", ex);
		}

		// return the addon ID
		return addonFile;
	}

	private static AddonFileModel ProcessAddonSrcFiles(string srcDirectory, AddonType addonType, Guid addonId)
	{
		var expectedManifestFileCount = addonType == AddonType.McAddon ? 2 : 1;

		// Find all manifest files in the src directory
		var manifestFiles = Directory.GetFiles(srcDirectory, "manifest.json", SearchOption.AllDirectories);

		if (manifestFiles.Length != expectedManifestFileCount)
		{
			throw new InvalidOperationException(
				$"Expected {expectedManifestFileCount} manifest files, but found {manifestFiles.Length}. " +
				"Make sure the addon is structured correctly, and using the right file extension.");
		}

		var addonFile = new AddonFileModel
		{
			Id = addonId,
			AddonType = addonType,
			ResourcePackGuid = null,
			ResourcePackFolderName = null,
			ResourcePackDependencyEnabled = false,
			BehaviorPackGuid = null,
			BehaviorPackFolderName = null,
			BehaviorPackDependencyEnabled = false,
			BehaviorPackUsingExperimental = false
		};

		for (var i = 0; i < manifestFiles.Length; i++)
		{
			var manifestFileInfo = new FileInfo(manifestFiles[i]);

			if (!manifestFileInfo.Exists)
				throw new FileNotFoundException($"Manifest file not found: {manifestFileInfo.FullName}");

			var manifestParentDirectory = manifestFileInfo.Directory?.FullName
				?? throw new InvalidOperationException("Manifest file directory is null.");

			// Get any pack icon if exists
			if (string.IsNullOrEmpty(addonFile.ImagePath))
			{
				var imagePath = Path.Combine(manifestFileInfo.Directory?.FullName ?? string.Empty, "pack_icon.png");
				if (File.Exists(imagePath)) addonFile.ImagePath = imagePath;
			}

			var manifestFileContent = File.ReadAllText(manifestFileInfo.FullName);
			var manifestBase = JsonSerializer.Deserialize<ManifestBaseModel>(manifestFileContent, serializerOptions);

			if (manifestBase is null || !manifestBase.Modules.Any(x => x.Type == "resources" || x.Type == "data"))
				throw new InvalidOperationException($"No required modules found in manifest file: {manifestFileInfo.FullName}");

			foreach (var module in manifestBase.Modules)
			{
				switch (module.Type.ToLowerInvariant())
				{
					case "resources":
						var resourceManifest = JsonSerializer.Deserialize<ResourcePackManifestModel>(manifestFileContent, serializerOptions)
							?? throw new InvalidOperationException($"Failed to deserialize resource pack manifest: {manifestFileInfo.FullName}");
						addonFile.ResourcePackGuid = resourceManifest.Header.Uuid;
						addonFile.ResourcePackVersion = resourceManifest.Header.Version;
						addonFile.ResourcePackFolderName = manifestParentDirectory;
						addonFile.ResourcePackDependencyEnabled = resourceManifest.Dependencies.Any(d => !string.IsNullOrWhiteSpace(d.Uuid));
						addonFile.Name = resourceManifest.Header.Name;
						addonFile.Description = resourceManifest.Header.Description;
						break;
					case "data":
						var behaviorManifest = JsonSerializer.Deserialize<BehaviorPackManifestModel>(manifestFileContent, serializerOptions)
							?? throw new InvalidOperationException($"Failed to deserialize behavior pack manifest: {manifestFileInfo.FullName}");
						addonFile.BehaviorPackGuid = behaviorManifest.Header.Uuid;
						addonFile.BehaviorPackVersion = behaviorManifest.Header.Version;
						addonFile.BehaviorPackFolderName = manifestParentDirectory;
						addonFile.BehaviorPackDependencyEnabled = behaviorManifest.Dependencies.Any(d => !string.IsNullOrWhiteSpace(d.Uuid));
						addonFile.BehaviorPackUsingExperimental = behaviorManifest.Dependencies.Any(x => !string.IsNullOrWhiteSpace(x.ModuleName) && x.ModuleName.Contains("beta"));
						addonFile.Name = behaviorManifest.Header.Name;
						addonFile.Description = behaviorManifest.Header.Description;
						break;
					default:
						break; // Ignore other module types
				}
			}
		}

		return addonFile;
	}
}