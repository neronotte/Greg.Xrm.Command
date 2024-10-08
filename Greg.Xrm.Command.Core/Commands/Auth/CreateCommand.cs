﻿using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Auth
{
	[Command("auth", "create", HelpText = "Create and store authentication profiles on this computer. Can be also used to update an existing authentication profile.")]
	public class CreateCommand
	{
		[Option("name", "n", HelpText = "The name you want to give to this authentication profile (maximum 30 characters).")]
		[Required]
		public string? Name { get; set; }

		[Option("conn", "cs", HelpText = "The [connection string](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect) that will be used to connect to the dataverse.")]
		[Required]
		public string? ConnectionString { get; set; }
	}
}
