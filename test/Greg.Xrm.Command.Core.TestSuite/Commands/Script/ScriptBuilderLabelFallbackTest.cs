using Greg.Xrm.Command.Commands.Script.Models;
using Greg.Xrm.Command.Commands.Script.Service;
using Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script
{
	/// <summary>
	/// On environments where the user language differs from the language the labels
	/// have been authored in, UserLocalizedLabel is null on the affected options.
	/// The script generation must fall back to another label instead of failing.
	/// </summary>
	[TestClass]
	public class ScriptBuilderLabelFallbackTest
	{
		[TestMethod]
		public void GetTextOrDefault_ShouldPreferUserLocalizedLabel()
		{
			var label = new Label("User language", 1033) { UserLocalizedLabel = new LocalizedLabel("User language", 1033) };

			Assert.AreEqual("User language", label.GetTextOrDefault("fallback"));
		}

		[TestMethod]
		public void GetTextOrDefault_ShouldPreferEnglish_WhenTheUserLanguageHasNoLabel()
		{
			var label = new Label();
			label.LocalizedLabels.Add(new LocalizedLabel("Chaud", 1036));
			label.LocalizedLabels.Add(new LocalizedLabel("Hot", 1033));

			Assert.AreEqual("Hot", label.GetTextOrDefault("fallback"));
		}

		[TestMethod]
		public void GetTextOrDefault_ShouldFallBackToAnyAvailableLabel()
		{
			var label = new Label();
			label.LocalizedLabels.Add(new LocalizedLabel("Other language", 1031));

			Assert.AreEqual("Other language", label.GetTextOrDefault("fallback"));
		}

		[TestMethod]
		public void GetTextOrDefault_ShouldFallBackToDefault_WhenNoLabelIsAvailable()
		{
			Assert.AreEqual("fallback", new Label().GetTextOrDefault("fallback"));
			Assert.AreEqual("fallback", ((Label?)null).GetTextOrDefault("fallback"));
		}

		[TestMethod]
		public void GeneratePacxScript_ShouldNotFail_WhenPicklistOptionHasNoUserLocalizedLabel()
		{
			var optionSet = new OptionSetMetadata { IsGlobal = false };
			optionSet.Options.Add(new OptionMetadata(new Label(), 1));

			var otherLanguageOnly = new Label();
			otherLanguageOnly.LocalizedLabels.Add(new LocalizedLabel("Andere Sprache", 1031));
			optionSet.Options.Add(new OptionMetadata(otherLanguageOnly, 2));

			var picklist = new PicklistAttributeMetadata
			{
				LogicalName = "categorycode",
				SchemaName = "CategoryCode",
				OptionSet = optionSet
			};

			var entity = new Extractor_EntityMetadata
			{
				SchemaName = "myprefix_table",
				DisplayName = "My Table",
				PluralName = "My Tables",
				IsCustomEntity = true,
				Fields = [picklist]
			};

			var script = new ScriptBuilder().GeneratePacxScript([entity], [], ["myprefix"]);

			StringAssert.Contains(script, "1:1", "The option value must be used when no label is available at all.");
			StringAssert.Contains(script, "Andere Sprache:2", "A label of another language must be used when the user language has none.");
		}

		[TestMethod]
		public void GeneratePacxScript_ShouldNotFail_WhenStateOrStatusOptionHasNoUserLocalizedLabel()
		{
			var stateOptions = new OptionSetMetadata();
			stateOptions.Options.Add(new StateOptionMetadata { Value = 0, DefaultStatus = 1, Label = new Label() });
			var state = new StateAttributeMetadata { LogicalName = "statecode", OptionSet = stateOptions };

			var statusLabel = new Label();
			statusLabel.LocalizedLabels.Add(new LocalizedLabel("Aktiv", 1031));
			var statusOptions = new OptionSetMetadata();
			statusOptions.Options.Add(new StatusOptionMetadata { Value = 1, State = 0, Label = statusLabel });
			var status = new StatusAttributeMetadata { LogicalName = "statuscode", OptionSet = statusOptions };

			var entity = new Extractor_EntityMetadata
			{
				SchemaName = "myprefix_table",
				DisplayName = "My Table",
				PluralName = "My Tables",
				IsCustomEntity = true,
				Fields = [state, status]
			};

			var script = new ScriptBuilder().GeneratePacxScript([entity], [], ["myprefix"]);

			StringAssert.Contains(script, "## STATE: 0", "The state value must be used when the state has no label at all.");
			StringAssert.Contains(script, "--label Aktiv", "A label of another language must be used for the status option.");
		}
	}
}
