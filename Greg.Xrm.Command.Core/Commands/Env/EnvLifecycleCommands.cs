using Greg.Xrm.Command.Parsing;

namespace Greg.Xrm.Command.Commands.Env
{
	[Command("env", "reset", HelpText = "Reset an environment to a clean state — removes customizations, data, or both.")]
	public class EnvResetCommand
	{
		[Option("id", Order = 1, Required = true, HelpText = "Environment ID to reset.")]
		public string EnvironmentId { get; set; } = "";

		[Option("type", "t", Order = 2, DefaultValue = "full", HelpText = "Reset type: full (data + customizations), customizations-only, data-only.")]
		public string ResetType { get; set; } = "full";

		[Option("force", "y", Order = 3, HelpText = "Skip confirmation prompt.")]
		public bool Force { get; set; }

		[Option("wait", Order = 4, HelpText = "Wait for reset operation to complete.")]
		public bool Wait { get; set; }

		[Option("format", "f", Order = 5, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("env", "backup", HelpText = "Create a backup of an environment for safe-keeping before major changes.")]
	public class EnvBackupCommand
	{
		[Option("id", Order = 1, Required = true, HelpText = "Environment ID to backup.")]
		public string EnvironmentId { get; set; } = "";

		[Option("name", "n", Order = 2, HelpText = "Backup label/name. Defaults to timestamp.")]
		public string? BackupName { get; set; }

		[Option("include-data", Order = 3, HelpText = "Include data in backup (default: schema only).")]
		public bool IncludeData { get; set; }

		[Option("wait", Order = 4, HelpText = "Wait for backup to complete.")]
		public bool Wait { get; set; }

		[Option("format", "f", Order = 5, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}

	[Command("env", "restore", HelpText = "Restore an environment from a previous backup.")]
	public class EnvRestoreCommand
	{
		[Option("id", Order = 1, Required = true, HelpText = "Environment ID to restore.")]
		public string EnvironmentId { get; set; } = "";

		[Option("backup-id", Order = 2, Required = true, HelpText = "Backup operation ID to restore from.")]
		public string BackupId { get; set; } = "";

		[Option("force", "y", Order = 3, HelpText = "Skip confirmation prompt.")]
		public bool Force { get; set; }

		[Option("wait", Order = 4, HelpText = "Wait for restore to complete.")]
		public bool Wait { get; set; }

		[Option("format", "f", Order = 5, DefaultValue = "table", HelpText = "Output format: table, json.")]
		public string Format { get; set; } = "table";
	}
}
