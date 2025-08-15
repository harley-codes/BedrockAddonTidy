using System.Text.Json;

namespace BedrockAddonTidy.Services.AddonFileService;

public class AddonFileConstants
{
	public static readonly JsonSerializerOptions SERIALIZE_OPTIONS = new()
	{
		ReadCommentHandling = JsonCommentHandling.Skip
	};

	public static readonly string ADDON_PROPERTY_FILE_NAME = "bedrock-addon-tidy.json";

	public static readonly string DIRECTORY_PATH_ADDONS = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"BedrockAddonTidy",
		System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
			?? throw new InvalidOperationException("Unable to determine application version."),
		"Addons"
	);

	public static readonly string DIRECTORY_PATH_MINECRAFT = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"Packages",
		"Microsoft.MinecraftUWP_8wekyb3d8bbwe",
		"LocalState",
		"games",
		"com.mojang"
	);
}
