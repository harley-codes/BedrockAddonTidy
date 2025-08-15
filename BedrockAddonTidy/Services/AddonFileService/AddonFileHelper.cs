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
			var json = JsonSerializer.Serialize(addonFile);
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
		var manifestFiles = Directory.GetFiles(srcDirectory, "manifest.json", SearchOption.AllDirectories);

		if (manifestFiles.Length == 0)
		{
			throw new InvalidOperationException("No manifest files found in the addon source directory.");
		}

		var addonFile = new AddonFileModel
		{
			Id = addonId,
			AddonType = addonType,
			ResourcePackGuid = null,
			UpdateDate = DateTime.UtcNow,
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
			var manifestBase = JsonSerializer.Deserialize<ManifestBaseModel>(manifestFileContent, AddonFileConstants.SERIALIZE_OPTIONS);

			if (manifestBase is null || !manifestBase.Modules.Any(x => x.Type == "resources" || x.Type == "data"))
				throw new InvalidOperationException($"No required modules found in manifest file: {manifestFileInfo.FullName}");

			foreach (var module in manifestBase.Modules)
			{
				switch (module.Type.ToLowerInvariant())
				{
					case "resources":
						var resourceManifest = JsonSerializer.Deserialize<ResourcePackManifestModel>(manifestFileContent, AddonFileConstants.SERIALIZE_OPTIONS)
							?? throw new InvalidOperationException($"Failed to deserialize resource pack manifest: {manifestFileInfo.FullName}");
						addonFile.ResourcePackGuid = resourceManifest.Header.Uuid;
						addonFile.ResourcePackVersion = resourceManifest.Header.Version;
						addonFile.ResourcePackFolderName = manifestParentDirectory;
						addonFile.ResourcePackNewFolderName = manifestParentDirectory.Replace(srcDirectory, "").Split(Path.DirectorySeparatorChar).LastOrDefault() ?? string.Empty;
						addonFile.ResourcePackDependencyEnabled = resourceManifest.Dependencies.Any(d => !string.IsNullOrWhiteSpace(d.Uuid));
						addonFile.Name ??= resourceManifest.Header.Name;
						addonFile.Description ??= resourceManifest.Header.Description;
						break;
					case "data":
						var behaviorManifest = JsonSerializer.Deserialize<BehaviorPackManifestModel>(manifestFileContent, AddonFileConstants.SERIALIZE_OPTIONS)
							?? throw new InvalidOperationException($"Failed to deserialize behavior pack manifest: {manifestFileInfo.FullName}");
						addonFile.BehaviorPackGuid = behaviorManifest.Header.Uuid;
						addonFile.BehaviorPackVersion = behaviorManifest.Header.Version;
						addonFile.BehaviorPackFolderName = manifestParentDirectory;
						addonFile.BehaviorPackNewFolderName = manifestParentDirectory.Replace(srcDirectory, "").Split(Path.DirectorySeparatorChar).LastOrDefault() ?? string.Empty;
						addonFile.BehaviorPackDependencyEnabled = behaviorManifest.Dependencies.Any(d => !string.IsNullOrWhiteSpace(d.Uuid));
						addonFile.BehaviorPackUsingExperimental = behaviorManifest.Dependencies.Any(x => !string.IsNullOrWhiteSpace(x.ModuleName) && x.ModuleName.Contains("beta"));
						addonFile.Name ??= behaviorManifest.Header.Name;
						addonFile.Description ??= behaviorManifest.Header.Description;
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
}