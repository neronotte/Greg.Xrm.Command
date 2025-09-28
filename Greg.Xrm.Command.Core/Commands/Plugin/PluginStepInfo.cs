using static Greg.Xrm.Command.Services.Plugin.PluginRegistrationToolkit;

namespace Greg.Xrm.Command.Commands.Plugin
{
	public class PluginStepInfo
	{
		public string Name { get; set; } = string.Empty;
		public string PluginTypeName { get; set; } = string.Empty;
		public Guid PluginTypeId { get; set; }
		public string AssemblyName { get; set; } = string.Empty;
		public Guid AssemblyId { get; set; }
		public string Message { get; set; } = string.Empty;
		public string Table { get; set; } = string.Empty;
		public string Stage { get; set; } = string.Empty;
		public string Mode { get; set; } = string.Empty;
		public int Rank { get; set; }
		public bool HasPreImage { get; set; }
		public bool HasPostImage { get; set; }
		public string Status { get; set; } = string.Empty;
		public Guid StepId { get; set; }
		public bool IsInSolution { get; set; }

		public string Images
		{
			get
			{
				if (HasPreImage && HasPostImage)
					return "pre/post";
				if (HasPreImage)
					return "pre";
				if (HasPostImage)
					return "post";
				return "";
			}
		}

		public static string GetStageDisplayName(int? stageCode)
		{
			return stageCode switch
			{
				10 => "PreValidation",
				20 => "PreOperation",
				30 => "MainOperation", // Internal stage - should not normally appear due to filtering
				40 => "PostOperation",
				_ => stageCode?.ToString() ?? "Unknown"
			};
		}

		public static string GetModeDisplayName(int? modeCode)
		{
			return modeCode switch
			{
				0 => "Sync",
				1 => "Async",
				_ => modeCode?.ToString() ?? "Unknown"
			};
		}

		public static string GetStatusDisplayName(int? statusCode)
		{
			return statusCode switch
			{
				1 => "Active",
				2 => "Inactive",
				_ => statusCode?.ToString() ?? "Unknown"
			};
		}
	}
}