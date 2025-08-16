using System.Text.Json;

namespace BedrockAddonTidy.ObjectModels;

public class AddonVersion
{
	public string? Major { get; set; }
	public string? Minor { get; set; }
	public string? Patch { get; set; }

	public AddonVersion() { }

	public AddonVersion(string major, string minor, string patch)
	{
		Major = major;
		Minor = minor;
		Patch = patch;
	}

	public AddonVersion(object version)
	{
		if (version is JsonElement jsonStringElement && jsonStringElement.ValueKind == JsonValueKind.String)
		{
			var parts = jsonStringElement.GetString()?.Split('.') ?? [];
			if (parts.Length > 0) Major = parts[0];
			if (parts.Length > 1) Minor = parts[1];
			if (parts.Length > 2) Patch = parts[2];
			return;
		}

		if (version is JsonElement jsonArrayElement && jsonArrayElement.ValueKind == JsonValueKind.Array)
		{
			var arrayVersion = jsonArrayElement.EnumerateArray().Select(e => e.ToString()).ToArray();
			if (arrayVersion.Length > 0) Major = arrayVersion[0];
			if (arrayVersion.Length > 1) Minor = arrayVersion[1];
			if (arrayVersion.Length > 2) Patch = arrayVersion[2];
			return;
		}

		throw new ArgumentException("Invalid version format. Expected string or int array.");
	}

	public int[] ToArray()
	{
		_ = int.TryParse(Major, out int major);
		_ = int.TryParse(Minor, out int minor);
		_ = int.TryParse(Patch, out int patch);
		return [major, minor, patch];
	}

	public override string ToString()
	{
		return $"{Major}.{Minor}.{Patch}";
	}
}
