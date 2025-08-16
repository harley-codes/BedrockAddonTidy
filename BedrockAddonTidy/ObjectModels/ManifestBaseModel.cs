using System.Text.Json;
using System.Text.Json.Serialization;

namespace BedrockAddonTidy.ObjectModels;

public class ManifestBaseModel
{
	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; }

	[JsonPropertyName("header")]
	public required HeaderData Header { get; set; }

	[JsonPropertyName("modules")]
	public required ModulesModel[] Modules { get; set; }

	public class HeaderData
	{
		[JsonExtensionData]
		public Dictionary<string, JsonElement>? ExtensionData { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string Description { get; set; } = string.Empty;

		[JsonPropertyName("uuid")]
		public string Uuid { get; set; } = string.Empty;

		[JsonPropertyName("version")]
		public required object Version { get; set; }
	}

	public class ModulesModel
	{
		[JsonExtensionData]
		public Dictionary<string, JsonElement>? ExtensionData { get; set; }

		[JsonPropertyName("type")]
		public required string Type { get; set; }

		[JsonPropertyName("uuid")]
		public required string UUID { get; set; }

		[JsonPropertyName("version")]
		public required object Version { get; set; }
	}
}
