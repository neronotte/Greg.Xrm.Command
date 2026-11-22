using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Table
{
	[TestClass]
	public class UpdateCommandTests
	{
		[TestMethod]
		public void ParseWithoutDisplayNameShouldWork()
		{
			var command = Utility.TestParseCommand<UpdateCommand>("table", "update", "--schemaName", "new_table", "--audit", "false");

			Assert.IsNull(command.DisplayName);
			Assert.AreEqual(false, command.IsAuditEnabled);
		}

		[TestMethod]
		public void ParseNullableBooleanSwitchesWithOrWithoutValues()
		{
			var command = Utility.TestParseCommand<UpdateCommand>("table", "update", "--schemaName", "new_table",
				"--activities", "--audit", "--quickCreate", "false", "--offline", "true", "--connections");

			Assert.AreEqual(true, command.Activities);
			Assert.AreEqual(true, command.IsAuditEnabled);
			Assert.AreEqual(false, command.IsQuickCreateEnabled);
			Assert.AreEqual(true, command.Offline);
			Assert.AreEqual(true, command.Connections);
			Assert.IsNull(command.HasNotes);
		}

		[TestMethod]
		public void ParseNewOptionsShouldPreserveOmittedValues()
		{
			var command = Utility.TestParseCommand<UpdateCommand>("table", "update", "--schemaName", "new_table",
				"--color", "#336699", "--primaryImage", "new_image", "--duplicateDetection", "false",
				"--customHelp", "true", "--helpUrl", "https://example.com/help", "--retention", "true",
				"--activities", "true", "--mailMerge", "false", "--documentManagement", "true",
				"--connections", "true", "--accessTeams", "true", "--offline", "false",
				"--queue", "true", "--autoRouteToOwnerQueue", "true", "--solution", "MySolution");

			Assert.AreEqual("#336699", command.Color);
			Assert.AreEqual("new_image", command.PrimaryImage);
			Assert.AreEqual(false, command.DuplicateDetection);
			Assert.AreEqual(true, command.CustomHelp);
			Assert.AreEqual("https://example.com/help", command.HelpUrl);
			Assert.AreEqual(true, command.Retention);
			Assert.AreEqual(true, command.Activities);
			Assert.AreEqual(false, command.MailMerge);
			Assert.AreEqual(true, command.DocumentManagement);
			Assert.AreEqual(true, command.Connections);
			Assert.AreEqual(true, command.AccessTeams);
			Assert.AreEqual(false, command.Offline);
			Assert.AreEqual(true, command.Queue);
			Assert.AreEqual(true, command.AutoRouteToOwnerQueue);
			Assert.AreEqual("MySolution", command.SolutionName);
			Assert.IsNull(command.IsAuditEnabled);
			Assert.IsNull(command.HasNotes);
		}

		[TestMethod]
		public void ValidateRejectsInvalidValuesAndIrreversibleDisabling()
		{
			var command = new UpdateCommand
			{
				SchemaName = "new_table",
				Color = "red",
				HelpUrl = "invalid",
				HasNotes = false,
				HasFeedback = false,
				HasEmailAddresses = false,
				Activities = false
			};
			var results = new List<ValidationResult>();

			Assert.IsFalse(Validator.TryValidateObject(command, new ValidationContext(command), results, true));
			CollectionAssert.AreEquivalent(
				new[] { nameof(command.Color), nameof(command.HelpUrl), nameof(command.HasNotes), nameof(command.HasFeedback), nameof(command.HasEmailAddresses), nameof(command.Activities) },
				results.SelectMany(result => result.MemberNames).ToArray());
		}
	}
}
