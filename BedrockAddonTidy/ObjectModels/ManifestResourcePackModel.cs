using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BedrockAddonTidy.ObjectModels;

public class ResourcePackManifestModel : ManifestBaseModel
{
	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; }

	[JsonPropertyName("dependencies")]
	public List<DependencyData> Dependencies { get; set; } = [];

	public class DependencyData
	{
		[JsonPropertyName("uuid")]
		public string? Uuid { get; set; }

		[JsonPropertyName("version")]
		public required object Version { get; set; }
	}
}
