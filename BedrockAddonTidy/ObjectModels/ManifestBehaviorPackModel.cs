using System.Text.Json;
using System.Text.Json.Serialization;

namespace BedrockAddonTidy.ObjectModels;

public class BehaviorPackManifestModel : ManifestBaseModel
{
	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; }

	[JsonPropertyName("dependencies")]
	public List<DependencyData> Dependencies { get; set; } = [];

	public class DependencyData
	{
		[JsonPropertyName("uuid")]
		public string? Uuid { get; set; }

		[JsonPropertyName("module_name")]
		public string? ModuleName { get; set; }

		[JsonPropertyName("version")]
		public required object Version { get; set; }
	}
}
