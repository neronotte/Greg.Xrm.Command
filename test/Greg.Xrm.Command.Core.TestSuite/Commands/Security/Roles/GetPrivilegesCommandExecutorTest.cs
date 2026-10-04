using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.ServiceModel;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Roles
{
	[TestClass]
	public class GetPrivilegesCommandExecutorTest
	{
		private sealed class CapturingOutput : OutputToMemory, IOutput
		{
			public IReadOnlyList<RoleTablePrivileges> TableRows { get; set; } = [];
			public IReadOnlyList<RoleMiscellaneousPrivilege> MiscellaneousRows { get; set; } = [];

			IOutput IOutput.WriteTable<TRow>(IReadOnlyList<TRow> collection, Func<string[]> rowHeaders, Func<TRow, string[]> rowData, Func<int, TRow, ConsoleColor?>? colorPicker)
			{
				if (collection is IReadOnlyList<RoleTablePrivileges> tables) this.TableRows = tables;
				if (collection is IReadOnlyList<RoleMiscellaneousPrivilege> miscellaneous) this.MiscellaneousRows = miscellaneous;
				return base.WriteTable(collection, rowHeaders, rowData, colorPicker);
			}
		}

		private readonly Guid roleId = Guid.NewGuid();
		private readonly Mock<IOrganizationServiceAsync2> crm = new();
		private readonly Mock<IOrganizationServiceRepository> connections = new();
		private readonly CapturingOutput output = new();
		private readonly List<EntityMetadata> metadata = [];
		private readonly List<Entity> catalog = [];
		private readonly List<RolePrivilege> grants = [];
		private readonly List<OrganizationRequest> requests = [];
		private readonly List<QueryExpression> queries = [];
		private EntityCollection roles = new();
		private GetPrivilegesCommandExecutor executor = null!;
		private bool pagedCatalog;

		private static void SetProperty(object target, string name, object value) => target.GetType().GetProperty(name)!.SetValue(target, value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);

		private SecurityPrivilegeMetadata Action(PrivilegeType type, string name, PrivilegeDepth? depth = null)
		{
			var id = Guid.NewGuid();
			var action = (SecurityPrivilegeMetadata)Activator.CreateInstance(typeof(SecurityPrivilegeMetadata), nonPublic: true)!;
			SetProperty(action, nameof(SecurityPrivilegeMetadata.PrivilegeId), id);
			SetProperty(action, nameof(SecurityPrivilegeMetadata.PrivilegeType), type);
			SetProperty(action, nameof(SecurityPrivilegeMetadata.Name), name);
			this.catalog.Add(new Entity("privilege", id) { ["name"] = name });
			if (depth.HasValue) this.grants.Add(new RolePrivilege { PrivilegeId = id, Depth = depth.Value });
			return action;
		}

		private static EntityMetadata Table(string name, params SecurityPrivilegeMetadata[] actions)
		{
			var table = new EntityMetadata { LogicalName = name, SchemaName = "Schema_" + name };
			SetProperty(table, nameof(EntityMetadata.Privileges), actions);
			return table;
		}

		[TestInitialize]
		public void Initialize()
		{
			this.roles = new EntityCollection([new Entity("role", this.roleId) { ["name"] = "Salesperson", ["ismanaged"] = true }]);
			var read = Action(PrivilegeType.Read, "prvReadUnexpectedName", PrivilegeDepth.Basic);
			this.metadata.Add(Table("new_claims",
				Action(PrivilegeType.Create, "prvCreateClaims"), read,
				Action(PrivilegeType.Write, "prvWriteClaims", PrivilegeDepth.Local),
				Action(PrivilegeType.Delete, "prvDeleteClaims"),
				Action(PrivilegeType.Append, "prvAppendClaims", PrivilegeDepth.Global),
				Action(PrivilegeType.AppendTo, "prvAppendToClaims", PrivilegeDepth.Global),
				Action(PrivilegeType.Assign, "prvAssignClaims"),
				Action(PrivilegeType.Share, "prvShareClaims")));
			this.metadata.Add(Table("new_claimresponse", read));
			this.metadata.Add(Table("new_case", Action(PrivilegeType.Create, "prvCreateCase"), Action(PrivilegeType.Read, "prvReadCase")));
			this.metadata.Add(Table("no_security_actions"));
			Action(PrivilegeType.None, "prvExportToExcel", PrivilegeDepth.Global);
			Action(PrivilegeType.None, "prvOtherSetting");
			this.connections.Setup(repository => repository.GetCurrentConnectionAsync()).ReturnsAsync(this.crm.Object);
			this.crm.Setup(service => service.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken _) =>
				{
					var expression = (QueryExpression)query;
					this.queries.Add(expression);
					if (expression.EntityName == "role") return Task.FromResult(this.roles);
					var page = expression.PageInfo.PageNumber;
					var entities = this.pagedCatalog ? (page == 1 ? this.catalog.Take(3) : this.catalog.Skip(3)) : this.catalog;
					return Task.FromResult(new EntityCollection(entities.ToList()) { MoreRecords = this.pagedCatalog && page == 1, PagingCookie = "<cookie/>" });
				});
			this.crm.Setup(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.Returns((OrganizationRequest request, CancellationToken _) =>
				{
					this.requests.Add(request);
					OrganizationResponse response;
					if (request is RetrieveRolePrivilegesRoleRequest)
					{
						response = new RetrieveRolePrivilegesRoleResponse();
						response.Results["RolePrivileges"] = this.grants.ToArray();
					}
					else if (request is RetrieveAllEntitiesRequest)
					{
						response = new RetrieveAllEntitiesResponse();
						response.Results["EntityMetadata"] = this.metadata.ToArray();
					}
					else throw new InvalidOperationException("Unexpected request: " + request.RequestName);
					return Task.FromResult(response);
				});
			this.executor = new GetPrivilegesCommandExecutor(this.output, this.connections.Object, new SecurityRoleService(), new RolePrivilegeInspectionService(new Privilege.Repository()));
		}

		private Task<CommandResult> Run(RolePrivilegeViewMode mode = RolePrivilegeViewMode.Assigned, string format = "functional", string? table = null, string? privilege = null)
		{
			this.output.TableRows = [];
			this.output.MiscellaneousRows = [];
			return this.executor.ExecuteAsync(new GetPrivilegesCommand { Role = "Salesperson", Mode = mode, Format = format, Table = table, Privilege = privilege }, CancellationToken.None);
		}
		private RoleTablePrivileges[] Tables(CommandResult result)
		{
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			return this.output.TableRows.ToArray();
		}
		private RoleMiscellaneousPrivilege[] Miscellaneous(CommandResult result)
		{
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			return this.output.MiscellaneousRows.ToArray();
		}

		[TestMethod]
		public async Task TabularOutputShouldSeparateRoleFromGridWithoutModeOrSectionLogging()
		{
			var result = await Run();
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var lines = this.output.ToString().Split(Environment.NewLine);
			var roleIndex = Array.FindIndex(lines, line => line.StartsWith("Role: "));
			Assert.IsTrue(roleIndex >= 0);
			Assert.AreEqual(string.Empty, lines[roleIndex + 1]);
			StringAssert.StartsWith(lines[roleIndex + 2], "| Table");
			Assert.IsFalse(this.output.ToString().Contains("Mode:"));
			Assert.IsFalse(this.output.ToString().Contains("Table privileges"));
		}

		[TestMethod]
		[DataRow("jsontech", "Basic", "Local", "Global", "None")]
		[DataRow("jsontechnical", "Basic", "Local", "Global", "None")]
		[DataRow("jt", "Basic", "Local", "Global", "None")]
		[DataRow("json", "User", "Business Unit", "Organization", "None")]
		[DataRow("jsonfunc", "User", "Business Unit", "Organization", "None")]
		[DataRow("jsonfunctional", "User", "Business Unit", "Organization", "None")]
		[DataRow("jf", "User", "Business Unit", "Organization", "None")]
		[DataRow("jsonnumeric", "1", "2", "4", "0")]
		[DataRow("jn", "1", "2", "4", "0")]
		public async Task JsonFormatsShouldEmitOneObjectWithoutCommandLogging(string format, string read, string write, string global, string none)
		{
			var result = await Run(RolePrivilegeViewMode.All, format);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var payload = JObject.Parse(this.output.ToString());
			Assert.HasCount(12, payload.Properties().ToArray());
			Assert.AreEqual(read, payload["prvReadUnexpectedName"]!.ToString());
			Assert.AreEqual(write, payload["prvWriteClaims"]!.ToString());
			Assert.AreEqual(global, payload["prvExportToExcel"]!.ToString());
			Assert.AreEqual(none, payload["prvOtherSetting"]!.ToString());
			Assert.AreEqual(format is "jsonnumeric" or "jn" ? JTokenType.Integer : JTokenType.String, payload["prvReadUnexpectedName"]!.Type);
			Assert.AreEqual(format is "jsonnumeric" or "jn" ? JTokenType.Integer : JTokenType.String, payload["prvCreateCase"]!.Type);
			Assert.IsFalse(this.output.ToString().Contains("Role:"));
			Assert.IsEmpty(result);
			Assert.IsEmpty(this.output.TableRows);
			Assert.IsEmpty(this.output.MiscellaneousRows);
		}

		[TestMethod]
		[DataRow(RolePrivilegeViewMode.Assigned, null, null, 9)]
		[DataRow(RolePrivilegeViewMode.All, "claim", null, 8)]
		[DataRow(RolePrivilegeViewMode.Unassigned, null, null, 3)]
		[DataRow(RolePrivilegeViewMode.Assigned, "claim", "Create", 1)]
		[DataRow(RolePrivilegeViewMode.All, "not_found", null, 0)]
		public async Task JsonExportShouldRespectFiltersAndDeduplicateSharedPrivileges(RolePrivilegeViewMode mode, string? table, string? privilege, int count)
		{
			var result = await Run(mode, "jn", table, privilege);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var payload = JObject.Parse(this.output.ToString());
			Assert.HasCount(count, payload.Properties().ToArray());
			if (table != null) Assert.IsNull(payload["prvExportToExcel"]);
			if (mode == RolePrivilegeViewMode.Unassigned) Assert.IsTrue(payload.Properties().All(property => property.Value.Value<int>() == 0));
			if (privilege == "Create") Assert.AreEqual(0, payload["prvCreateClaims"]!.Value<int>());
		}

		[TestMethod]
		[DataRow(PrivilegeDepth.Basic, 1, "Basic", "User")]
		[DataRow(PrivilegeDepth.Local, 2, "Local", "Business Unit")]
		[DataRow(PrivilegeDepth.Deep, 3, "Deep", "Parent Child")]
		[DataRow(PrivilegeDepth.Global, 4, "Global", "Organization")]
		public async Task JsonExportsShouldMapEveryAssignedDepth(PrivilegeDepth depth, int numeric, string technical, string functional)
		{
			this.grants[0].Depth = depth;
			foreach (var format in new[] { "jt", "jf", "jn" })
			{
				var captured = new OutputToMemory();
				var executor = new GetPrivilegesCommandExecutor(captured, this.connections.Object, new SecurityRoleService(), new RolePrivilegeInspectionService(new Privilege.Repository()));
				var result = await executor.ExecuteAsync(new GetPrivilegesCommand { Role = "Salesperson", Table = "claimresponse", Format = format }, CancellationToken.None);
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				var payload = JObject.Parse(captured.ToString());
				Assert.HasCount(1, payload.Properties().ToArray());
				Assert.AreEqual(format switch { "jt" => technical, "jf" => functional, _ => numeric.ToString() }, payload["prvReadUnexpectedName"]!.ToString());
			}
		}

		[TestMethod]
		public async Task JsonUnknownDepthShouldBeNullInsteadOfAnInventedLevel()
		{
			this.grants.Add(new RolePrivilege { PrivilegeId = Guid.NewGuid(), PrivilegeName = "prvUnknown", Depth = (PrivilegeDepth)99 });
			var result = await Run(format: "jn");
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(JTokenType.Null, JObject.Parse(this.output.ToString())["prvUnknown"]!.Type);
		}

		[TestMethod]
		public async Task ResultShouldContainOnlyScalarValues()
		{
			var result = await Run(RolePrivilegeViewMode.All);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsFalse(result.Values.Any(value => value is System.Collections.IEnumerable && value is not string));
			Assert.IsFalse(result.ContainsKey("Tables"));
			Assert.IsFalse(result.ContainsKey("Miscellaneous"));
			Assert.IsFalse(result.ContainsKey("Warnings"));
			Assert.AreEqual(0, result["WarningCount"]);
		}

		[TestMethod]
		[DataRow("functional", "Organization")]
		[DataRow("technical", "Global")]
		[DataRow("number", "4")]
		[DataRow("compact", "4")]
		public async Task MiscellaneousShouldRenderThreeColumnsInEveryFormat(string format, string expectedLevel)
		{
			var result = await Run(format: format);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var section = this.output.ToString().Split("Miscellaneous privileges", StringSplitOptions.None)[1];
			var lines = section.Split(Environment.NewLine).Where(line => line.StartsWith("| ")).ToArray();
			CollectionAssert.AreEqual(new[] { "Label", "Level", "Technical name" }, lines[0].Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
			CollectionAssert.AreEqual(new[] { "Export To Excel", expectedLevel, "prvExportToExcel" }, lines[1].Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
		}

		[TestMethod]
		[DataRow("prvReadRecordAuditHistory", "Read Record Audit History")]
		[DataRow("prvUseOfficeApps", "Use Office Apps")]
		[DataRow("ReadRecordAuditHistory", "Read Record Audit History")]
		[DataRow("prvAB", "A B")]
		public async Task MiscellaneousLabelShouldRemoveOnlyLeadingPrefixAndSplitCapitals(string name, string expectedLabel)
		{
			Action(PrivilegeType.None, name, PrivilegeDepth.Global);
			var result = await Run(privilege: name);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var row = this.output.ToString().Split(Environment.NewLine).Single(line => line.StartsWith("| ") && line.Contains(name));
			CollectionAssert.AreEqual(new[] { expectedLabel, "Organization", name }, row.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
		}

		[TestMethod]
		[DataRow(RolePrivilegeViewMode.Assigned, 2, 1)]
		[DataRow(RolePrivilegeViewMode.All, 3, 2)]
		[DataRow(RolePrivilegeViewMode.Unassigned, 1, 1)]
		public async Task ModesShouldFilterTablesByAnyAssignmentNotMissingCells(RolePrivilegeViewMode mode, int tableCount, int miscellaneousCount)
		{
			var result = await Run(mode);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(tableCount, result["TableCount"]);
			Assert.AreEqual(miscellaneousCount, result["MiscellaneousCount"]);
			if (mode == RolePrivilegeViewMode.Unassigned) Assert.AreEqual("new_case", Tables(result).Single().LogicalName);
			Assert.IsFalse(Tables(result).Any(table => table.LogicalName == "no_security_actions"));
			Assert.HasCount(2, this.requests);
			Assert.AreEqual(this.roleId, ((RetrieveRolePrivilegesRoleRequest)this.requests[0]).RoleId);
			var metadataRequest = (RetrieveAllEntitiesRequest)this.requests[1];
			Assert.AreEqual(EntityFilters.Entity | EntityFilters.Privileges, metadataRequest.EntityFilters);
			Assert.IsFalse(metadataRequest.RetrieveAsIfPublished);
		}

		[TestMethod]
		[DataRow("functional", "User", "Business Unit", "Organization")]
		[DataRow("technical", "Basic", "Local", "Global")]
		[DataRow("number", "1", "2", "4")]
		[DataRow("compact", "01204400", "CRWDATaS", " 1      ")]
		public async Task FormatsShouldRenderLevelsAndPreserveUnderlyingCells(string format, string first, string second, string third)
		{
			var result = await Run(RolePrivilegeViewMode.All, format);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var claims = Tables(result).Single(table => table.LogicalName == "new_claims");
			CollectionAssert.AreEqual(new int?[] { 0, 1, 2, 0, 4, 4, 0, 0 }, claims.Cells.Select(cell => cell.Level).ToArray());
			var response = Tables(result).Single(table => table.LogicalName == "new_claimresponse");
			Assert.IsNull(response.Cells[0].PrivilegeId);
			Assert.IsNull(response.Cells[0].Level);
			Assert.AreEqual(claims.Cells[1].PrivilegeId, response.Cells[1].PrivilegeId);
			StringAssert.Contains(this.output.ToString(), first);
			StringAssert.Contains(this.output.ToString(), second);
			StringAssert.Contains(this.output.ToString(), third);
			Assert.AreEqual(format, result["Format"]);
			Assert.IsFalse(Miscellaneous(result).Any(privilege => privilege.Name == "prvReadUnexpectedName"));
		}

		[TestMethod]
		[DataRow(RolePrivilegeViewMode.Assigned, "functional", "claim", 2)]
		[DataRow(RolePrivilegeViewMode.All, "compact", "CLAIM", 2)]
		[DataRow(RolePrivilegeViewMode.Unassigned, "number", "claim", 0)]
		[DataRow(RolePrivilegeViewMode.All, "technical", "not_found", 0)]
		public async Task TableFilterShouldUseContainsAndSuppressMiscellaneous(RolePrivilegeViewMode mode, string format, string filter, int count)
		{
			var result = await Run(mode, format, filter);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(count, result["TableCount"]);
			Assert.AreEqual(0, result["MiscellaneousCount"]);
			Assert.IsEmpty(Miscellaneous(result));
			Assert.IsFalse(this.output.ToString().Contains("Miscellaneous privileges"));
			if (count == 2) CollectionAssert.AreEqual(new[] { "new_claimresponse", "new_claims" }, Tables(result).Select(table => table.LogicalName).ToArray());
		}

		[TestMethod]
		public async Task PrivilegeFilterShouldNotChangeModeAndShouldKeepCompactPositions()
		{
			var result = await Run(RolePrivilegeViewMode.Assigned, "compact", "claim", "Create");
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.HasCount(1, Tables(result));
			Assert.AreEqual(0, Tables(result)[0].Cells[0].Level);
			StringAssert.Contains(this.output.ToString(), "0-------");
			Assert.AreEqual(0, result["MiscellaneousCount"]);
		}

		[TestMethod]
		public async Task TechnicalPrivilegeFilterShouldMatchMetadataAndNotNamePrefixes()
		{
			var result = await Run(privilege: "prvReadUnexpectedName");
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.HasCount(2, Tables(result));
			Assert.IsEmpty(Miscellaneous(result));
			Assert.IsFalse(this.output.ToString().Contains("| Create"));
		}

		[TestMethod]
		public async Task CatalogShouldReadAllPagesAndNotClassifySharedIdsAsMiscellaneous()
		{
			this.pagedCatalog = true;
			var result = await Run(RolePrivilegeViewMode.All);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(2, this.queries.Count(query => query.EntityName == "privilege"));
			Assert.HasCount(2, Miscellaneous(result));
		}

		[TestMethod]
		public async Task DuplicateGrantsShouldKeepWidestLevel()
		{
			this.grants.Add(new RolePrivilege { PrivilegeId = this.grants[0].PrivilegeId, Depth = PrivilegeDepth.Global });
			var result = await Run();
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsTrue(Tables(result).All(table => table.Cells[1].Level == 4));
		}

		[TestMethod]
		public async Task UnknownAssignedPrivilegeAndDepthShouldBeVisibleWithWarnings()
		{
			this.grants.Add(new RolePrivilege { PrivilegeId = Guid.NewGuid(), PrivilegeName = "prvUnknown", Depth = (PrivilegeDepth)99 });
			var result = await Run();
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var unknown = Miscellaneous(result).Single(privilege => privilege.Name == "prvUnknown");
			Assert.IsTrue(unknown.IsAssigned);
			Assert.IsNull(unknown.Level);
			var row = this.output.ToString().Split(Environment.NewLine).Single(line => line.StartsWith("| ") && line.Contains("prvUnknown"));
			CollectionAssert.AreEqual(new[] { "Unknown", "?", "prvUnknown" }, row.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
			Assert.IsTrue((int)result["WarningCount"] > 0);
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task MissingOrAmbiguousRoleShouldFailBeforeMetadata(bool ambiguous)
		{
			if (ambiguous) this.roles.Entities.Add(new Entity("role", Guid.NewGuid()) { ["name"] = "Salesperson" });
			else this.roles = new EntityCollection();
			var result = await Run();
			Assert.IsFalse(result.IsSuccess);
			Assert.IsEmpty(this.requests);
		}

		[TestMethod]
		public async Task DataverseFaultShouldReturnFailure()
		{
			this.crm.Setup(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Metadata denied"));
			var result = await Run();
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Metadata denied");
		}

		[TestMethod]
		[DataRow(PrivilegeDepth.Basic, 1, "Basic", "User")]
		[DataRow(PrivilegeDepth.Local, 2, "Local", "Business Unit")]
		[DataRow(PrivilegeDepth.Deep, 3, "Deep", "Parent Child")]
		[DataRow(PrivilegeDepth.Global, 4, "Global", "Organization")]
		public async Task EveryAssignedDepthShouldUseExplicitNumericAndTextualMapping(PrivilegeDepth depth, int number, string technical, string functional)
		{
			this.grants[0].Depth = depth;
			foreach (var format in new[] { "technical", "functional", "number", "compact" })
			{
				var captured = new CapturingOutput();
				var executor = new GetPrivilegesCommandExecutor(captured, this.connections.Object, new SecurityRoleService(), new RolePrivilegeInspectionService(new Privilege.Repository()));
				var result = await executor.ExecuteAsync(new GetPrivilegesCommand { Role = "Salesperson", Table = "claimresponse", Format = format }, CancellationToken.None);
				Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
				Assert.AreEqual(number, captured.TableRows.Single().Cells[1].Level);
				StringAssert.Contains(captured.ToString(), format switch
				{
					"technical" => technical,
					"functional" => functional,
					"compact" => " " + number + "      ",
					_ => number.ToString()
				});
			}
		}

		[TestMethod]
		public async Task UnassignedMiscellaneousShouldUseFunctionalNone()
		{
			var result = await Run(RolePrivilegeViewMode.Unassigned);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var row = this.output.ToString().Split(Environment.NewLine).Single(line => line.StartsWith("| ") && line.Contains("prvOtherSetting"));
			CollectionAssert.AreEqual(new[] { "Other Setting", "None", "prvOtherSetting" }, row.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
			Assert.IsFalse(Miscellaneous(result).Any(privilege => privilege.IsAssigned));
		}

		[TestMethod]
		public async Task TableFilterShouldMatchSchemaAndDisplayNames()
		{
			var label = new Label();
			SetProperty(label, nameof(Label.UserLocalizedLabel), new LocalizedLabel("Insurance document", 1033));
			this.metadata[0].DisplayName = label;
			var display = await Run(table: "insurance");
			Assert.IsTrue(display.IsSuccess, display.ErrorMessage);
			Assert.AreEqual("new_claims", Tables(display).Single().LogicalName);
			var schema = await Run(table: "Schema_new_claim");
			Assert.IsTrue(schema.IsSuccess, schema.ErrorMessage);
			Assert.HasCount(2, Tables(schema));
		}

		[TestMethod]
		public async Task ConnectionFailureShouldReturnFailure()
		{
			this.connections.Setup(repository => repository.GetCurrentConnectionAsync()).ThrowsAsync(new InvalidOperationException("No connection"));
			var result = await Run();
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "No connection");
		}

		[TestMethod]
		public async Task CancellationTokenShouldReachEveryRead()
		{
			using var source = new CancellationTokenSource();
			var result = await this.executor.ExecuteAsync(new GetPrivilegesCommand { Role = this.roleId.ToString() }, source.Token);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			this.crm.Verify(service => service.ExecuteAsync(It.IsAny<OrganizationRequest>(), source.Token), Times.Exactly(2));
			this.crm.Verify(service => service.RetrieveMultipleAsync(It.IsAny<QueryBase>(), source.Token), Times.Exactly(2));
		}
	}
}