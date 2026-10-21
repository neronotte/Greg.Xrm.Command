using Greg.Xrm.Command.Model;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Services.Ribbon
{
	[TestClass]
	public class RibbonComponentRepositoryTest
	{
		private readonly Mock<IOrganizationServiceAsync2> crm = new();
		private readonly IRibbonComponentRepository repository = new RibbonComponent.Repository();

		[TestMethod]
		public async Task RetrievesOnlySevenSolutionComponentsAndPreservesLookupReferences()
		{
			var solutionId = Guid.NewGuid(); var rootId = Guid.NewGuid();
			var entity = new Entity("solutioncomponent", Guid.NewGuid())
			{
				["componenttype"] = new OptionSetValue((int)ComponentType.Attribute), ["objectid"] = Guid.NewGuid(),
				["rootsolutioncomponentid"] = new EntityReference("solutioncomponent", rootId)
			};
			QueryExpression? captured = null;
			using var cancellation = new CancellationTokenSource();
			crm.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>(), cancellation.Token))
				.Callback<QueryBase, CancellationToken>((q, _) => captured = (QueryExpression)q)
				.ReturnsAsync(new EntityCollection([entity]));
			var components = await repository.GetSolutionComponentsAsync(crm.Object, solutionId, 7, cancellation.Token);
			Assert.IsNotNull(captured);
			Assert.AreEqual(7, captured.TopCount);
			Assert.AreEqual("solutioncomponent", captured.EntityName);
			Assert.IsTrue(captured.Criteria.Conditions.Any(c => c.AttributeName == "solutionid" && (Guid)c.Values.Single() == solutionId));
			CollectionAssert.IsSubsetOf(new[] { "componenttype", "objectid", "rootsolutioncomponentid", "rootcomponentbehavior" }, captured.ColumnSet.Columns.ToArray());
			Assert.AreEqual(rootId, components.Single().RootSolutionComponentId);
		}

		[TestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public async Task FiltersApplicationRibbonsByScopeAndOptionalId(bool activeOnly)
		{
			QueryExpression? captured = null; var id = Guid.NewGuid();
			crm.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Callback<QueryBase, CancellationToken>((q, _) => captured = (QueryExpression)q)
				.ReturnsAsync(new EntityCollection([new Entity("ribboncustomization", id)]));
			var result = await repository.GetApplicationRibbonIdsAsync(crm.Object, activeOnly, activeOnly ? null : id, CancellationToken.None);
			Assert.AreEqual(id, result.Single());
			Assert.IsNotNull(captured);
			Assert.AreEqual("ribboncustomization", captured.EntityName);
			Assert.IsTrue(captured.Criteria.Conditions.Any(c => c.AttributeName == "entity" && c.Operator == ConditionOperator.Null));
			Assert.AreEqual(activeOnly, captured.Criteria.Conditions.Any(c => c.AttributeName == "solutionid" && (Guid)c.Values.Single() == new Guid("FD140AAE-4DF4-11DD-BD17-0019B9312238")));
			Assert.AreEqual(!activeOnly, captured.Criteria.Conditions.Any(c => c.AttributeName == "ribboncustomizationid" && (Guid)c.Values.Single() == id));
		}

		[TestMethod]
		[DataRow(ComponentType.SystemForm, "systemform", "formid", "objecttypecode")]
		[DataRow(ComponentType.SavedQuery, "savedquery", "savedqueryid", "returnedtypecode")]
		[DataRow(ComponentType.SavedQueryVisualization, "savedqueryvisualization", "savedqueryvisualizationid", "primaryentitytypecode")]
		public async Task RetrievesComponentDiagnosticNames(ComponentType type, string entityName, string idColumn, string tableColumn)
		{
			var id = Guid.NewGuid(); QueryExpression? captured = null;
			crm.Setup(c => c.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Callback<QueryBase, CancellationToken>((q, _) => captured = (QueryExpression)q)
				.ReturnsAsync(new EntityCollection([new Entity(entityName, id) { [tableColumn] = "account", ["name"] = "Example" }]));
			Assert.AreEqual(("account", "Example"), await repository.GetComponentNamesAsync(crm.Object, type, id, CancellationToken.None));
			Assert.IsNotNull(captured);
			Assert.AreEqual(entityName, captured.EntityName);
			Assert.IsTrue(captured.Criteria.Conditions.Any(c => c.AttributeName == idColumn && (Guid)c.Values.Single() == id));
		}
	}
}
