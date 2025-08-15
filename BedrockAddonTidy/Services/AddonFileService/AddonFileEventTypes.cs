using System;

namespace BedrockAddonTidy.Services.AddonFileService;

public class AddonFileEventTypes
{
	public enum EventChangeType
	{
		Created,
		Updated,
		Deleted
	}

	public class AddonFilePropertiesChangedEventArgs(Guid addonId, EventChangeType changeType) : EventArgs
	{
		public Guid AddonId { get; } = addonId;
		public EventChangeType ChangeType { get; } = changeType;
	}

	public class AddonFileValidationWarningsChangedEventArgs(Guid addonId, EventChangeType changeType) : EventArgs
	{
		public Guid AddonId { get; } = addonId;
		public EventChangeType ChangeType { get; } = changeType;
	}
}
