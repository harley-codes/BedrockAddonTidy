
using BedrockAddonTidy.Enums;

namespace BedrockAddonTidy.ObjectModels;

public class AddonFileModel
{
	public Guid Id { get; set; }
	public AddonType AddonType { get; set; }

	public string? Name { get; set; }
	public string? Description { get; set; }
	public string? Author { get; set; }
	public string? ImagePath { get; set; }

	public string? ResourcePackGuid { get; set; }
	public object? ResourcePackVersion { get; set; }
	public string? ResourcePackFolderName { get; set; }
	public bool ResourcePackDependencyEnabled { get; set; }

	public string? BehaviorPackGuid { get; set; }
	public object? BehaviorPackVersion { get; set; }
	public string? BehaviorPackFolderName { get; set; }
	public bool BehaviorPackDependencyEnabled { get; set; }
	public bool BehaviorPackUsingExperimental { get; set; }
}
