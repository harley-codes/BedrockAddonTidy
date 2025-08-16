using System.IO.Compression;
using System.Text.Json;
using BedrockAddonTidy.Enums;
using BedrockAddonTidy.ObjectModels;

namespace BedrockAddonTidy.Services.AddonFileService;

public class AddonFileHelper
{
	public static AddonFileModel[] GetExistingAddonFiles()
	{
		if (!Directory.Exists(AddonFileConstants.DIRECTORY_PATH_ADDONS))
			Directory.CreateDirectory(AddonFileConstants.DIRECTORY_PATH_ADDONS);

		var addonFilePropertyPaths = Directory.GetFiles(
			AddonFileConstants.DIRECTORY_PATH_ADDONS,
			AddonFileConstants.ADDON_PROPERTY_FILE_NAME,
			SearchOption.AllDirectories);

		var addonFiles = new List<AddonFileModel>();

		foreach (var filePath in addonFilePropertyPaths)
		{
			try
			{
				var fileContents = File.ReadAllText(filePath);
				var addonFile = JsonSerializer.Deserialize<AddonFileModel>(
					fileContents,
					AddonFileConstants.SERIALIZE_OPTIONS);

				if (addonFile is not null) addonFiles.Add(addonFile);
			}
			catch (JsonException ex)
			{
				Console.WriteLine($"Error deserializing addon file {filePath}: {ex.Message}");
			}
			catch (IOException ex)
			{
				Console.WriteLine($"Error reading addon file {filePath}: {ex.Message}");
			}
		}

		return [.. addonFiles];
	}

	public static Dictionary<Guid, string[]> GetAddonFileValidationWarnings(Dictionary<Guid, AddonFileModel> addonFiles)
	{
		var validationWarnings = new Dictionary<Guid, string[]>();

		foreach (var addonId in addonFiles.Keys)
		{
			var warnings = new List<string>();
			var addonFile = addonFiles[addonId];

			if (string.IsNullOrWhiteSpace(addonFile.Name))
				warnings.Add("Addon name is missing.");

			if (string.IsNullOrWhiteSpace(addonFile.Description))
				warnings.Add("Addon description is missing.");

			if (string.IsNullOrWhiteSpace(addonFile.ImagePath))
				warnings.Add("Addon image is missing.");

			if (!string.IsNullOrEmpty(addonFile.BehaviorPackGuid))
			{
				var isInvalid = addonFiles.Values
					.Any(x => x.BehaviorPackGuid == addonFile.BehaviorPackGuid && x.Id != addonFile.Id);
				if (isInvalid)
					warnings.Add($"Behavior pack GUID '{addonFile.BehaviorPackGuid}' is not unique.");
			}

			if (!string.IsNullOrEmpty(addonFile.ResourcePackGuid))
			{
				var isInvalid = addonFiles.Values
					.Any(x => x.ResourcePackGuid == addonFile.ResourcePackGuid && x.Id != addonFile.Id);
				if (isInvalid)
					warnings.Add($"Resource pack GUID '{addonFile.ResourcePackGuid}' is not unique.");
			}

			if (!string.IsNullOrEmpty(addonFile.ResourcePackNewFolderName) && addonFile.ResourcePackNewFolderName.Length > 32)
				warnings.Add("Resource pack folder name exceeds 32 characters. This can lead to issues in Minecraft.");

			if (!string.IsNullOrEmpty(addonFile.BehaviorPackNewFolderName) && addonFile.BehaviorPackNewFolderName.Length > 32)
				warnings.Add("Behavior pack folder name exceeds 32 characters. This can lead to issues in Minecraft.");

			if (addonFile.ResourcePackDependencyEnabled && string.IsNullOrEmpty(addonFile.BehaviorPackGuid))
				warnings.Add("Resource pack dependency is enabled but Behavior Pack is missing.");

			if (addonFile.BehaviorPackDependencyEnabled && string.IsNullOrEmpty(addonFile.ResourcePackGuid))
				warnings.Add("Behavior pack dependency is enabled but Resource Pack is missing.");

			if (warnings.Count > 0)
			{
				validationWarnings[addonId] = [.. warnings];
			}
		}

		return validationWarnings;
	}

	public static AddonFileModel ImportNewAddon(string addonPath)
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
		var addonDirectory = Path.Combine(AddonFileConstants.DIRECTORY_PATH_ADDONS, addonId.ToString());
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
			var addonFilePath = Path.Combine(addonDirectory, AddonFileConstants.ADDON_PROPERTY_FILE_NAME);
			var json = JsonSerializer.Serialize(addonFile, AddonFileConstants.SERIALIZE_OPTIONS);
			File.WriteAllText(addonFilePath, json);
		}
		catch (Exception ex)
		{
			throw new IOException("Failed to write addon file information.", ex);
		}

		return addonFile;
	}

	private static AddonFileModel ProcessAddonSrcFiles(string srcDirectory, AddonType addonType, Guid addonId)
	{

		// Find all manifest files in the src directory
		var manifestFiles = Directory.GetFiles(srcDirectory, AddonFileConstants.MANIFEST_FILE_NAME, SearchOption.AllDirectories);

		if (manifestFiles.Length == 0)
		{
			throw new InvalidOperationException("No manifest files found in the addon source directory.");
		}

		var addonFile = new AddonFileModel
		{
			Id = addonId,
			AddonType = addonType,
			SrcPath = srcDirectory,
			ResourcePackGuid = null,
			UpdateDate = DateTime.UtcNow,
			ResourcePackFolderName = null,
			ResourcePackDependencyEnabled = false,
			BehaviorPackGuid = null,
			BehaviorPackFolderName = null,
			BehaviorPackDependencyEnabled = false
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
				var imagePath = Path.Combine(manifestFileInfo.Directory?.FullName ?? string.Empty, AddonFileConstants.PACK_ICON_FILE_NAME);
				if (File.Exists(imagePath)) addonFile.ImagePath = imagePath;
			}

			var manifestFileContent = File.ReadAllText(manifestFileInfo.FullName);
			var manifestBase = JsonSerializer.Deserialize<ManifestBaseModel>(manifestFileContent, AddonFileConstants.SERIALIZE_OPTIONS);

			if (manifestBase is null || !manifestBase.Modules.Any(x => x.Type == "resources" || x.Type == "data"))
				throw new InvalidOperationException($"No required modules found in manifest file: {manifestFileInfo.FullName}");

			foreach (var module in manifestBase.Modules)
			{
				switch (module.Type.ToLowerInvariant())
				{
					case "resources":
						{
							var resourceManifest = JsonSerializer.Deserialize<ResourcePackManifestModel>(manifestFileContent, AddonFileConstants.SERIALIZE_OPTIONS)
							?? throw new InvalidOperationException($"Failed to deserialize resource pack manifest: {manifestFileInfo.FullName}");
							addonFile.ResourcePackGuid = resourceManifest.Header.Uuid;
							addonFile.ResourcePackVersion = new AddonVersion(resourceManifest.Header.Version);
							addonFile.ResourcePackFolderName = manifestParentDirectory;
							addonFile.ResourcePackNewFolderName = manifestParentDirectory.Replace(srcDirectory, "").Split(Path.DirectorySeparatorChar).LastOrDefault() ?? string.Empty;
							addonFile.ResourcePackDependencyEnabled = resourceManifest.Dependencies.Any(d => !string.IsNullOrWhiteSpace(d.Uuid));
							addonFile.Name ??= resourceManifest.Header.Name;
							addonFile.Description ??= resourceManifest.Header.Description;
						}
						break;
					case "data":
						{
							var behaviorManifest = JsonSerializer.Deserialize<BehaviorPackManifestModel>(manifestFileContent, AddonFileConstants.SERIALIZE_OPTIONS)
							?? throw new InvalidOperationException($"Failed to deserialize behavior pack manifest: {manifestFileInfo.FullName}");
							addonFile.BehaviorPackGuid = behaviorManifest.Header.Uuid;
							addonFile.BehaviorPackVersion = new AddonVersion(behaviorManifest.Header.Version);
							addonFile.BehaviorPackFolderName = manifestParentDirectory;
							addonFile.BehaviorPackNewFolderName = manifestParentDirectory.Replace(srcDirectory, "").Split(Path.DirectorySeparatorChar).LastOrDefault() ?? string.Empty;
							addonFile.BehaviorPackDependencyEnabled = behaviorManifest.Dependencies.Any(d => !string.IsNullOrWhiteSpace(d.Uuid));
							addonFile.Name ??= behaviorManifest.Header.Name;
							addonFile.Description ??= behaviorManifest.Header.Description;
							var serverDep = behaviorManifest.Dependencies.FirstOrDefault(x => x.ModuleName == "@minecraft/server");
							if (serverDep is not null)
								addonFile.BehaviorPackServerVersion = new AddonVersion(serverDep.Version);
							var serverUiDep = behaviorManifest.Dependencies.FirstOrDefault(x => x.ModuleName == "@minecraft/server-ui");
							if (serverUiDep is not null)
								addonFile.BehaviorPackServerUiVersion = new AddonVersion(serverUiDep.Version);
						}
						break;
					default:
						break; // Ignore other module types
				}
			}
		}

		return addonFile;
	}

	public static void DeleteAddonFiles(Guid addonId)
	{
		var addonDirectory = Path.Combine(AddonFileConstants.DIRECTORY_PATH_ADDONS, addonId.ToString());

		if (Directory.Exists(addonDirectory))
		{
			try
			{
				Directory.Delete(addonDirectory, true);
			}
			catch (IOException ex)
			{
				throw new IOException($"Failed to delete addon directory {addonDirectory}.", ex);
			}
		}
		else
		{
			throw new DirectoryNotFoundException($"Addon directory {addonDirectory} not found.");
		}
	}

	public static void SaveAddonFileProperties(AddonFileModel addonFile)
	{
		if (addonFile is null)
		{
			throw new ArgumentNullException(nameof(addonFile), "Addon file cannot be null.");
		}

		var addonDirectory = Path.Combine(AddonFileConstants.DIRECTORY_PATH_ADDONS, addonFile.Id.ToString());

		if (!Directory.Exists(addonDirectory))
		{
			throw new DirectoryNotFoundException($"Addon directory {addonDirectory} does not exist.");
		}

		// Serialize the addon file information
		try
		{
			var addonFilePath = Path.Combine(addonDirectory, AddonFileConstants.ADDON_PROPERTY_FILE_NAME);
			var json = JsonSerializer.Serialize(addonFile, AddonFileConstants.SERIALIZE_OPTIONS);
			File.WriteAllText(addonFilePath, json);
		}
		catch (Exception ex)
		{
			throw new IOException("Failed to write addon file information.", ex);
		}
	}

	public static AddonFileModel UpdateAddonFileSrc(AddonFileModel addonFile)
	{
		if (addonFile is null)
		{
			throw new ArgumentNullException(nameof(addonFile), "Addon file cannot be null.");
		}

		if (!string.IsNullOrWhiteSpace(addonFile.ResourcePackFolderName))
		{
			// Move folder if needed
			if (!string.IsNullOrWhiteSpace(addonFile.ResourcePackNewFolderName))
			{
				var newPathSplit = addonFile.ResourcePackFolderName.Split(Path.DirectorySeparatorChar)[..^1];
				var newPath = Path.Combine(string.Join(Path.DirectorySeparatorChar, newPathSplit), addonFile.ResourcePackNewFolderName);
				if (newPath != addonFile.ResourcePackFolderName)
				{
					Directory.Move(addonFile.ResourcePackFolderName, newPath);
					addonFile.ResourcePackFolderName = newPath;
				}
			}

			var resourceManifestPath = Path.Combine(addonFile.ResourcePackFolderName, AddonFileConstants.MANIFEST_FILE_NAME);
			if (File.Exists(resourceManifestPath))
			{
				var resourcePackManifest = JsonSerializer.Deserialize<ResourcePackManifestModel>(
					File.ReadAllText(resourceManifestPath),
					AddonFileConstants.SERIALIZE_OPTIONS);

				if (resourcePackManifest is not null)
				{
					resourcePackManifest.Header.Name = addonFile.Name ?? resourcePackManifest.Header.Name;
					resourcePackManifest.Header.Description = addonFile.Description ?? resourcePackManifest.Header.Description;
					resourcePackManifest.Header.Uuid = addonFile.ResourcePackGuid ?? resourcePackManifest.Header.Uuid;
					resourcePackManifest.Header.Version = resourcePackManifest.Header.Version is JsonElement versionJsonElement && versionJsonElement.ValueKind == JsonValueKind.String
						? addonFile.ResourcePackVersion?.ToString() ?? resourcePackManifest.Header.Version
						: addonFile.ResourcePackVersion?.ToArray() ?? resourcePackManifest.Header.Version;

					if (addonFile.ResourcePackDependencyEnabled && !string.IsNullOrEmpty(addonFile.BehaviorPackGuid))
					{
						var dependency = resourcePackManifest.Dependencies.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Uuid));
						if (dependency is not null)
						{
							dependency.Uuid = addonFile.BehaviorPackGuid;
							dependency.Version = dependency.Version is string
								? addonFile.BehaviorPackVersion?.ToString() ?? dependency.Version
								: addonFile.BehaviorPackVersion?.ToArray() ?? dependency.Version;
						}
						else
						{
							resourcePackManifest.Dependencies.Add(new ResourcePackManifestModel.DependencyData
							{
								Uuid = addonFile.BehaviorPackGuid,
								Version = addonFile.BehaviorPackVersion?.ToArray().AsEnumerable() ?? [1, 0, 0],
							});
						}
					}
					if (!addonFile.ResourcePackDependencyEnabled)
					{
						var dependency = resourcePackManifest.Dependencies.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Uuid));
						if (dependency is not null)
						{
							resourcePackManifest.Dependencies.Remove(dependency);
						}
					}

					if (string.IsNullOrEmpty(addonFile.ImagePath) || !File.Exists(addonFile.ImagePath))
					{
						var imagePath = Path.Combine(addonFile.ResourcePackFolderName, AddonFileConstants.PACK_ICON_FILE_NAME);
						if (File.Exists(imagePath)) addonFile.ImagePath = imagePath;
					}

					// Update behavior pack file
					File.WriteAllText(
						Path.Combine(addonFile.ResourcePackFolderName, AddonFileConstants.MANIFEST_FILE_NAME),
						JsonSerializer.Serialize(resourcePackManifest, AddonFileConstants.SERIALIZE_OPTIONS));
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(addonFile.BehaviorPackFolderName))
		{
			// Move folder if needed
			if (!string.IsNullOrWhiteSpace(addonFile.BehaviorPackNewFolderName))
			{
				var newPathSplit = addonFile.BehaviorPackFolderName.Split(Path.DirectorySeparatorChar)[..^1];
				var newPath = Path.Combine(string.Join(Path.DirectorySeparatorChar, newPathSplit), addonFile.BehaviorPackNewFolderName);
				if (newPath != addonFile.BehaviorPackFolderName)
				{
					Directory.Move(addonFile.BehaviorPackFolderName, newPath);
					addonFile.BehaviorPackFolderName = newPath;
				}
			}

			var behaviorManifestPath = Path.Combine(addonFile.BehaviorPackFolderName, AddonFileConstants.MANIFEST_FILE_NAME);
			if (File.Exists(behaviorManifestPath))
			{
				var behaviorPackManifest = JsonSerializer.Deserialize<BehaviorPackManifestModel>(
					File.ReadAllText(behaviorManifestPath),
					AddonFileConstants.SERIALIZE_OPTIONS);

				if (behaviorPackManifest is not null)
				{
					behaviorPackManifest.Header.Name = addonFile.Name ?? behaviorPackManifest.Header.Name;
					behaviorPackManifest.Header.Description = addonFile.Description ?? behaviorPackManifest.Header.Description;
					behaviorPackManifest.Header.Uuid = addonFile.BehaviorPackGuid ?? behaviorPackManifest.Header.Uuid;

					behaviorPackManifest.Header.Version = behaviorPackManifest.Header.Version is JsonElement versionJsonElement && versionJsonElement.ValueKind == JsonValueKind.String
						? addonFile.BehaviorPackVersion?.ToString() ?? behaviorPackManifest.Header.Version
						: addonFile.BehaviorPackVersion?.ToArray() ?? behaviorPackManifest.Header.Version;

					var serverDep = behaviorPackManifest.Dependencies.FirstOrDefault(x => x.ModuleName == "@minecraft/server");
					serverDep?.Version = serverDep.Version is JsonElement serverDepJsonElement && serverDepJsonElement.ValueKind == JsonValueKind.String
							? addonFile.BehaviorPackServerVersion?.ToString() ?? serverDep.Version
							: addonFile.BehaviorPackServerVersion?.ToArray() ?? serverDep.Version;

					var serverUiDep = behaviorPackManifest.Dependencies.FirstOrDefault(x => x.ModuleName == "@minecraft/server-ui");
					serverUiDep?.Version = serverUiDep.Version is JsonElement serverDepUiJsonElement && serverDepUiJsonElement.ValueKind == JsonValueKind.String
							? addonFile.BehaviorPackServerUiVersion?.ToString() ?? serverUiDep.Version
							: addonFile.BehaviorPackServerUiVersion?.ToArray() ?? serverUiDep.Version;

					if (addonFile.BehaviorPackDependencyEnabled && !string.IsNullOrEmpty(addonFile.ResourcePackGuid))
					{
						var dependency = behaviorPackManifest.Dependencies.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Uuid));
						if (dependency is not null)
						{
							dependency.Uuid = addonFile.ResourcePackGuid;
							dependency.Version = dependency.Version is string
								? addonFile.ResourcePackVersion?.ToString() ?? dependency.Version
								: addonFile.ResourcePackVersion?.ToArray() ?? dependency.Version;
						}
						else
						{
							behaviorPackManifest.Dependencies.Add(new BehaviorPackManifestModel.DependencyData
							{
								Uuid = addonFile.ResourcePackGuid,
								Version = addonFile.ResourcePackVersion?.ToArray().AsEnumerable() ?? [1, 0, 0],
							});
						}
					}
					if (!addonFile.BehaviorPackDependencyEnabled)
					{
						var dependency = behaviorPackManifest.Dependencies.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Uuid));
						if (dependency is not null)
						{
							behaviorPackManifest.Dependencies.Remove(dependency);
						}
					}

					if (string.IsNullOrEmpty(addonFile.ImagePath) || !File.Exists(addonFile.ImagePath))
					{
						var imagePath = Path.Combine(addonFile.BehaviorPackFolderName, AddonFileConstants.PACK_ICON_FILE_NAME);
						if (File.Exists(imagePath)) addonFile.ImagePath = imagePath;
					}

					// Update behavior pack file
					File.WriteAllText(
						Path.Combine(addonFile.BehaviorPackFolderName, AddonFileConstants.MANIFEST_FILE_NAME),
						JsonSerializer.Serialize(behaviorPackManifest, AddonFileConstants.SERIALIZE_OPTIONS));
				}
			}
		}

		return addonFile;
	}
}