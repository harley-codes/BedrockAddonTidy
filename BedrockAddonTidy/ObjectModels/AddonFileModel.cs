
using BedrockAddonTidy.Enums;

namespace BedrockAddonTidy.ObjectModels;

public class AddonFileModel
{
	public Guid Id { get; set; }
	public AddonType AddonType { get; set; }
	public required string SrcPath { get; set; }

	public string? Name { get; set; }
	public string? Description { get; set; }
	public string? Author { get; set; }
	public string? ImagePath { get; set; }
	public DateTime UpdateDate { get; set; }
	public string VersionTextRp => ResourcePackVersion is not null ? $"[{ResourcePackVersion}]" : "N/A";
	public string VersionTextBp => BehaviorPackVersion is not null ? $"[{BehaviorPackVersion}]" : "N/A";
	public string? ResourcePackGuid { get; set; }
	public AddonVersion? ResourcePackVersion { get; set; }
	public string? ResourcePackFolderName { get; set; }
	public string? ResourcePackNewFolderName { get; set; }
	public bool ResourcePackDependencyEnabled { get; set; }

	public string? BehaviorPackGuid { get; set; }
	public AddonVersion? BehaviorPackVersion { get; set; }
	public string? BehaviorPackFolderName { get; set; }
	public string? BehaviorPackNewFolderName { get; set; }
	public bool BehaviorPackDependencyEnabled { get; set; }
	public AddonVersion? BehaviorPackServerVersion { get; set; }
	public AddonVersion? BehaviorPackServerUiVersion { get; set; }

	public bool BehaviorPackUsingExperimental =>
		(BehaviorPackServerVersion is not null && BehaviorPackServerVersion.ToString().Contains("beta")) ||
		(BehaviorPackServerUiVersion is not null && BehaviorPackServerUiVersion.ToString().Contains("beta"));
}
