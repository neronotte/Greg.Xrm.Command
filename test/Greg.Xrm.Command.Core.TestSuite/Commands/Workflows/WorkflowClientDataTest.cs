namespace Greg.Xrm.Command.Commands.Workflows
{
	[TestClass]
	public class WorkflowClientDataTest
	{
		private const string ValidDefinition = "{\"properties\":{\"connectionReferences\":{},\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\",\"kind\":\"Button\"}},\"actions\":{\"Step1\":{\"type\":\"Compose\",\"runAfter\":{},\"inputs\":\"x\"},\"Step2\":{\"type\":\"Compose\",\"runAfter\":{\"Step1\":[\"Succeeded\"]},\"inputs\":\"y\"}}}},\"schemaVersion\":\"1.0.0.0\"}";

		[TestMethod]
		public void GetStructureErrors_ShouldAcceptAValidDefinition()
		{
			var errors = WorkflowClientData.GetStructureErrors(ValidDefinition);

			Assert.AreEqual(0, errors.Count, string.Join(" | ", errors));
		}

		[TestMethod]
		public void GetStructureErrors_ShouldDetectARunAfterOnAMissingAction()
		{
			// this is the exact case that crashed the flow designer during the live tests
			var definition = "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}},\"actions\":{\"Broken\":{\"type\":\"Compose\",\"runAfter\":{\"Does_Not_Exist\":[\"Succeeded\"]},\"inputs\":\"x\"}}}}}";

			var errors = WorkflowClientData.GetStructureErrors(definition);

			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains(errors[0], "Does_Not_Exist");
		}

		[TestMethod]
		public void GetStructureErrors_ShouldDetectAMissingTrigger()
		{
			var definition = "{\"properties\":{\"definition\":{\"actions\":{\"Only\":{\"type\":\"Compose\",\"runAfter\":{},\"inputs\":\"x\"}}}}}";

			var errors = WorkflowClientData.GetStructureErrors(definition);

			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains(errors[0], "trigger");
		}

		[TestMethod]
		public void GetStructureErrors_ShouldDetectMissingActions()
		{
			var definition = "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}},\"actions\":{}}}}";

			var errors = WorkflowClientData.GetStructureErrors(definition);

			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains(errors[0], "action");
		}

		[TestMethod]
		public void GetStructureErrors_ShouldCheckRunAfterInsideNestedScopes()
		{
			var definition = "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}},\"actions\":{\"Outer\":{\"type\":\"Scope\",\"runAfter\":{},\"actions\":{\"Inner\":{\"type\":\"Compose\",\"runAfter\":{\"Nope\":[\"Succeeded\"]},\"inputs\":\"x\"}}}}}}}";

			var errors = WorkflowClientData.GetStructureErrors(definition);

			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains(errors[0], "Nope");
		}

		[TestMethod]
		public void GetStructureErrors_ShouldCheckRunAfterInsideAnElseBranch()
		{
			var definition = "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}},\"actions\":{\"Check\":{\"type\":\"If\",\"runAfter\":{},\"expression\":{},\"actions\":{\"Ok\":{\"type\":\"Compose\",\"runAfter\":{},\"inputs\":\"x\"}},\"else\":{\"actions\":{\"Bad\":{\"type\":\"Compose\",\"runAfter\":{\"Missing_In_Else\":[\"Succeeded\"]},\"inputs\":\"x\"}}}}}}}}";

			var errors = WorkflowClientData.GetStructureErrors(definition);

			Assert.AreEqual(1, errors.Count);
			StringAssert.Contains(errors[0], "Missing_In_Else");
		}

		[TestMethod]
		public void GetStructureErrors_ShouldCheckRunAfterInsideSwitchCasesAndDefault()
		{
			var definition = "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}},\"actions\":{\"Branch\":{\"type\":\"Switch\",\"runAfter\":{},\"expression\":\"@1\",\"cases\":{\"CaseA\":{\"case\":\"A\",\"actions\":{\"InCase\":{\"type\":\"Compose\",\"runAfter\":{\"Missing_In_Case\":[\"Succeeded\"]},\"inputs\":\"x\"}}}},\"default\":{\"actions\":{\"InDefault\":{\"type\":\"Compose\",\"runAfter\":{\"Missing_In_Default\":[\"Succeeded\"]},\"inputs\":\"x\"}}}}}}}}";

			var errors = WorkflowClientData.GetStructureErrors(definition);

			Assert.AreEqual(2, errors.Count, string.Join(" | ", errors));
			StringAssert.Contains(string.Join(" ", errors), "Missing_In_Case");
			StringAssert.Contains(string.Join(" ", errors), "Missing_In_Default");
		}

		[TestMethod]
		public void GetStructureErrors_ShouldStayQuiet_WhenThereIsNoDefinitionObject()
		{
			// the executors warn about this shape, the activation probe rejects it
			Assert.AreEqual(0, WorkflowClientData.GetStructureErrors("{\"properties\":{\"definition\":\"foo\"}}").Count);
			Assert.AreEqual(0, WorkflowClientData.GetStructureErrors("{\"foo\":\"bar\"}").Count);
		}

		[TestMethod]
		public void GetStructureErrors_ShouldNotMixScopes()
		{
			// Step1 lives at the top level, the inner action may not reference it as sibling;
			// but referencing an inner sibling is fine
			var definition = "{\"properties\":{\"definition\":{\"triggers\":{\"manual\":{\"type\":\"Request\"}},\"actions\":{\"Step1\":{\"type\":\"Compose\",\"runAfter\":{},\"inputs\":\"x\"},\"Outer\":{\"type\":\"Scope\",\"runAfter\":{\"Step1\":[\"Succeeded\"]},\"actions\":{\"InnerA\":{\"type\":\"Compose\",\"runAfter\":{},\"inputs\":\"x\"},\"InnerB\":{\"type\":\"Compose\",\"runAfter\":{\"InnerA\":[\"Succeeded\"]},\"inputs\":\"x\"}}}}}}}";

			var errors = WorkflowClientData.GetStructureErrors(definition);

			Assert.AreEqual(0, errors.Count, string.Join(" | ", errors));
		}

		[TestMethod]
		public void GetConnectionReferenceLogicalNames_ShouldReturnTheDistinctNames()
		{
			var definition = "{\"properties\":{\"connectionReferences\":{\"shared_a\":{\"connection\":{\"connectionReferenceLogicalName\":\"new_a_123\"}},\"shared_b\":{\"connection\":{\"connectionReferenceLogicalName\":\"new_b_456\"}},\"shared_c\":{\"connection\":{\"connectionReferenceLogicalName\":\"NEW_A_123\"}}},\"definition\":{}}}";

			var names = WorkflowClientData.GetConnectionReferenceLogicalNames(definition);

			Assert.AreEqual(2, names.Count);
			CollectionAssert.Contains(names.ToList(), "new_a_123");
			CollectionAssert.Contains(names.ToList(), "new_b_456");
		}

		[TestMethod]
		public void GetConnectionReferenceLogicalNames_ShouldReturnEmpty_WhenThereAreNoReferences()
		{
			Assert.AreEqual(0, WorkflowClientData.GetConnectionReferenceLogicalNames(ValidDefinition).Count);
		}

		[TestMethod]
		public void AddTriggerMuzzle_ShouldMuzzleAutomaticTriggers()
		{
			var definition = "{\"properties\":{\"definition\":{\"triggers\":{\"Recurrence\":{\"type\":\"Recurrence\",\"recurrence\":{\"frequency\":\"Day\",\"interval\":1}}},\"actions\":{}}}}";

			var muzzled = WorkflowClientData.AddTriggerMuzzle(definition);

			StringAssert.Contains(muzzled, "@false", "An automatic trigger must not be able to fire while the probe is activated.");
			StringAssert.Contains(muzzled, "conditions");
		}

		[TestMethod]
		public void AddTriggerMuzzle_ShouldLeaveManualTriggersAlone()
		{
			var muzzled = WorkflowClientData.AddTriggerMuzzle(ValidDefinition);

			Assert.IsFalse(muzzled.Contains("@false"), "A manual trigger only fires on an explicit call, it needs no muzzle.");
		}

		[TestMethod]
		public void AddTriggerMuzzle_ShouldKeepExistingTriggerConditions()
		{
			var definition = "{\"properties\":{\"definition\":{\"triggers\":{\"OnChange\":{\"type\":\"OpenApiConnectionWebhook\",\"conditions\":[{\"expression\":\"@equals(1,1)\"}]}},\"actions\":{}}}}";

			var muzzled = WorkflowClientData.AddTriggerMuzzle(definition);

			StringAssert.Contains(muzzled, "@equals(1,1)", "The conditions of the user must stay part of the probed definition, so they are validated too.");
			StringAssert.Contains(muzzled, "@false");
		}

		[TestMethod]
		public void AddTriggerMuzzle_ShouldLeaveBrokenConditionsAlone_SoTheEngineSeesThem()
		{
			var definition = "{\"properties\":{\"definition\":{\"triggers\":{\"Odd\":{\"type\":\"Recurrence\",\"conditions\":\"garbage\"}},\"actions\":{}}}}";

			var muzzled = WorkflowClientData.AddTriggerMuzzle(definition);

			StringAssert.Contains(muzzled, "\"garbage\"", "A broken conditions value must reach the engine unchanged, so the probe validates what would actually be deployed.");
			Assert.IsFalse(muzzled.Contains("@false"), "Muzzling would replace the broken value and hide it from the validation.");
		}

		[TestMethod]
		public void AddTriggerMuzzle_ShouldTolerateShapesThatAreNotAFlow()
		{
			Assert.AreEqual("{\"properties\":{\"definition\":\"foo\"}}", WorkflowClientData.AddTriggerMuzzle("{\"properties\":{\"definition\":\"foo\"}}"));
		}

		[TestMethod]
		public void AddTriggerMuzzle_ShouldMuzzleATriggerWithANonStringType()
		{
			// a numeric type is not the manual trigger, so it gets the muzzle;
			// whether the engine accepts such a type is decided by the probe
			var muzzled = WorkflowClientData.AddTriggerMuzzle("{\"properties\":{\"definition\":{\"triggers\":{\"odd\":{\"type\":123}},\"actions\":{}}}}");

			StringAssert.Contains(muzzled, "@false");
		}

		[TestMethod]
		public void AddTriggerMuzzle_ShouldReturnTheInputUnchanged_WhenTheJsonCarriesDuplicateKeys()
		{
			// JsonDocument tolerates duplicate keys, JsonNode does not - the engine
			// has to see such a definition exactly as it is
			var duplicateKeys = "{\"properties\":{\"definition\":{\"triggers\":{\"a\":{\"type\":\"Recurrence\"},\"a\":{\"type\":\"Recurrence\"}},\"actions\":{}}}}";

			Assert.AreEqual(duplicateKeys, WorkflowClientData.AddTriggerMuzzle(duplicateKeys));
		}
	}
}
