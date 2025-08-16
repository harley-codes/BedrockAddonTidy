using System.Text.Json;
using System.Text.Json.Serialization;

namespace BedrockAddonTidy.ObjectModels;

public class BehaviorPackManifestModel : ManifestBaseModel
{
	[JsonPropertyName("dependencies")]
	public List<DependencyData> Dependencies { get; set; } = [];

	public class DependencyData
	{
		[JsonExtensionData]
		public Dictionary<string, JsonElement>? ExtensionData { get; set; }

		[JsonPropertyName("uuid")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? Uuid { get; set; }

		[JsonPropertyName("module_name")]
		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? ModuleName { get; set; }

		[JsonPropertyName("version")]
		public required object Version { get; set; }
	}
}
