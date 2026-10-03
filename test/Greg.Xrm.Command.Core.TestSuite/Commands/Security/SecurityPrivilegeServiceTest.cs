using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Moq;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Security
{
	[TestClass]
	public class SecurityPrivilegeServiceTest
	{
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();
		private readonly SecurityPrivilegeService service = new(new Organization.Repository(), new BusinessUnit.Repository());
		private readonly List<Entity> businessUnits = [];

		[TestInitialize]
		public void Initialize()
		{
			this.SetupOrganization(false);
		}

		private void SetupOrganization(bool enabled)
		{
			this.crmMock.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken _) => Task.FromResult(((QueryExpression)query).EntityName == "organization"
					? new EntityCollection([new Entity("organization", Guid.NewGuid())
					{
						["orgdborgsettings"] = $"<OrgSettings><EnableOwnershipAcrossBusinessUnits>{enabled.ToString().ToLowerInvariant()}</EnableOwnershipAcrossBusinessUnits></OrgSettings>"
					}])
					: new EntityCollection(this.businessUnits)));
		}

		private static SecurityPrivilegeMetadata Privilege(Guid id, PrivilegeType type)
		{
			var p = (SecurityPrivilegeMetadata)Activator.CreateInstance(typeof(SecurityPrivilegeMetadata), nonPublic: true)!; SetProperty(p, nameof(SecurityPrivilegeMetadata.PrivilegeId), id); SetProperty(p, nameof(SecurityPrivilegeMetadata.PrivilegeType), type); SetProperty(p, nameof(SecurityPrivilegeMetadata.Name), $"prv{type}account"); return p;
		}

		private static void SetProperty(object target, string name, object? value) => target.GetType().GetProperty(name)!.SetValue(target, value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);

		private static EntityMetadata Table(string logicalName, params SecurityPrivilegeMetadata[] privileges)
		{
			var metadata = new EntityMetadata { LogicalName = logicalName };
			typeof(EntityMetadata).GetProperty(nameof(EntityMetadata.Privileges))!
				.SetValue(metadata, privileges, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
			return metadata;
		}

		private void SetupTable(EntityMetadata metadata, Action<RetrieveEntityRequest>? callback = null)
		{
			this.crmMock
				.Setup(x => x.ExecuteAsync(It.IsAny<RetrieveEntityRequest>(), It.IsAny<CancellationToken>()))
				.Returns((OrganizationRequest r, CancellationToken _) =>
				{
					callback?.Invoke((RetrieveEntityRequest)r);
					var response = new RetrieveEntityResponse();
					response.Results["EntityMetadata"] = metadata;
					return Task.FromResult<OrganizationResponse>(response);
				});
		}

		private void SetupUserPrivileges(params RolePrivilege[] privileges)
		{
			this.crmMock
				.Setup(x => x.ExecuteAsync(It.IsAny<RetrieveUserPrivilegesRequest>(), It.IsAny<CancellationToken>()))
				.Returns(() =>
				{
					var response = new RetrieveUserPrivilegesResponse();
					response.Results["RolePrivileges"] = privileges;
					return Task.FromResult<OrganizationResponse>(response);
				});
		}

		[TestMethod]
		[DataRow(false, 1)]
		[DataRow(true, 2)]
		public async Task CrossBusinessUnitSettingShouldControlPrivilegeAggregation(bool enabled, int expectedCount)
		{
			this.SetupOrganization(enabled);
			var read = Guid.NewGuid();
			var europe = Guid.NewGuid();
			var america = Guid.NewGuid();
			this.businessUnits.Add(new Entity("businessunit", europe) { ["name"] = "Europe" });
			this.businessUnits.Add(new Entity("businessunit", america) { ["name"] = "America" });
			this.SetupTable(Table("account", Privilege(read, PrivilegeType.Read)));
			this.SetupUserPrivileges(
				new RolePrivilege { PrivilegeId = read, BusinessUnitId = europe, Depth = PrivilegeDepth.Basic },
				new RolePrivilege { PrivilegeId = read, BusinessUnitId = europe, Depth = PrivilegeDepth.Local },
				new RolePrivilege { PrivilegeId = read, BusinessUnitId = america, Depth = PrivilegeDepth.Deep });

			var result = await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), "account", null, CancellationToken.None);

			Assert.AreEqual(expectedCount, result.Count);
			if (enabled)
			{
				CollectionAssert.AreEquivalent(new[] { PrivilegeDepth.Local, PrivilegeDepth.Deep }, result.Select(privilege => privilege.Depth!.Value).ToArray());
				Assert.AreEqual("Europe", result.Single(privilege => privilege.BusinessUnitId == europe).BusinessUnitName);
				Assert.AreEqual("America", result.Single(privilege => privilege.BusinessUnitId == america).BusinessUnitName);
				this.crmMock.Verify(client => client.RetrieveMultipleAsync(It.Is<QueryBase>(query =>
					((QueryExpression)query).EntityName == "businessunit" &&
					((QueryExpression)query).Criteria.Conditions.Single().Operator == ConditionOperator.In &&
					((QueryExpression)query).Criteria.Conditions.Single().Values.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
			}
			else
			{
				Assert.AreEqual(PrivilegeDepth.Deep, result.Single().Depth);
				Assert.IsNull(result.Single().BusinessUnitId);
				this.crmMock.Verify(client => client.RetrieveMultipleAsync(It.Is<QueryBase>(query =>
					((QueryExpression)query).EntityName == "businessunit"), It.IsAny<CancellationToken>()), Times.Never);
			}
		}

		[TestMethod]
		public async Task MissingBusinessUnitNameShouldPreserveBusinessUnitIdentifier()
		{
			this.SetupOrganization(true);
			var read = Guid.NewGuid();
			var businessUnit = Guid.NewGuid();
			this.SetupTable(Table("account", Privilege(read, PrivilegeType.Read)));
			this.SetupUserPrivileges(new RolePrivilege { PrivilegeId = read, BusinessUnitId = businessUnit, Depth = PrivilegeDepth.Local });
			var result = await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), "account", null, CancellationToken.None);
			Assert.AreEqual(businessUnit, result.Single().BusinessUnitId);
			Assert.IsNull(result.Single().BusinessUnitName);
		}

		[TestMethod]
		public async Task RecordCheckShouldNotReadBusinessUnitSettingsEvenWhenEnabled()
		{
			this.SetupOrganization(true);
			this.SetupTable(Table("account", Privilege(Guid.NewGuid(), PrivilegeType.Read)));
			this.crmMock.Setup(client => client.ExecuteAsync(It.IsAny<RetrievePrincipalAccessRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new RetrievePrincipalAccessResponse { Results = { ["AccessRights"] = AccessRights.ReadAccess } });
			var result = await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), "account", Guid.NewGuid(), CancellationToken.None);
			Assert.AreEqual("Read", result.Single().Privilege);
			Assert.IsNull(result.Single().BusinessUnitId);
			this.crmMock.Verify(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task TableCheckShouldNotFailWhenSamePrivilegeIsGrantedByMultipleRolesAndShouldKeepWidestDepth()
		{
			var read = Guid.NewGuid();
			SetupTable(Table("account", Privilege(read, PrivilegeType.Read)));
			SetupUserPrivileges(
				new RolePrivilege { PrivilegeId = read, Depth = PrivilegeDepth.Basic },
				new RolePrivilege { PrivilegeId = read, Depth = PrivilegeDepth.Global },
				new RolePrivilege { PrivilegeId = read, Depth = PrivilegeDepth.Local });

			var result = await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), "account", null, CancellationToken.None);

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("Read", result[0].Privilege);
			Assert.AreEqual(PrivilegeDepth.Global, result[0].Depth);
		}

		[TestMethod]
		public async Task TableCheckShouldIgnorePrivilegesOfOtherTablesWithSimilarNames()
		{
			var accountRead = Guid.NewGuid();
			var customerAccountRead = Guid.NewGuid();
			SetupTable(Table("account", Privilege(accountRead, PrivilegeType.Read)));
			SetupUserPrivileges(
				new RolePrivilege { PrivilegeId = accountRead, Depth = PrivilegeDepth.Local },
				new RolePrivilege { PrivilegeId = customerAccountRead, Depth = PrivilegeDepth.Global });

			var result = await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), "account", null, CancellationToken.None);

			Assert.AreEqual(1, result.Count);
			Assert.AreEqual(PrivilegeDepth.Local, result[0].Depth);
		}

		[TestMethod]
		public async Task TableCheckShouldDistinguishAppendFromAppendTo()
		{
			var append = Guid.NewGuid();
			var appendTo = Guid.NewGuid();
			SetupTable(Table("account", Privilege(append, PrivilegeType.Append), Privilege(appendTo, PrivilegeType.AppendTo)));
			SetupUserPrivileges(
				new RolePrivilege { PrivilegeId = append, Depth = PrivilegeDepth.Basic },
				new RolePrivilege { PrivilegeId = appendTo, Depth = PrivilegeDepth.Basic });

			var result = await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), "account", null, CancellationToken.None);

			CollectionAssert.AreEqual(new[] { "Append", "Append To" }, result.Select(x => x.Privilege).ToArray());
		}

		[TestMethod]
		public async Task TableCheckShouldReturnPrivilegesInCanonicalOrder()
		{
			var types = new[] { PrivilegeType.Share, PrivilegeType.Assign, PrivilegeType.AppendTo, PrivilegeType.Append, PrivilegeType.Delete, PrivilegeType.Write, PrivilegeType.Read, PrivilegeType.Create };
			var privileges = types.Select(t => Privilege(Guid.NewGuid(), t)).ToArray();
			SetupTable(Table("account", privileges));
			SetupUserPrivileges(privileges.Select(p => new RolePrivilege { PrivilegeId = p.PrivilegeId, Depth = PrivilegeDepth.Basic }).ToArray());

			var result = await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), "account", null, CancellationToken.None);

			CollectionAssert.AreEqual(
				new[] { "Create", "Read", "Write", "Delete", "Append", "Append To", "Assign", "Share" },
				result.Select(x => x.Privilege).ToArray());
		}

		[TestMethod]
		public async Task RecordCheckShouldReturnAccessRightsInCanonicalOrder()
		{
			SetupTable(Table("account"));
			this.crmMock
				.Setup(x => x.ExecuteAsync(It.IsAny<RetrievePrincipalAccessRequest>(), It.IsAny<CancellationToken>()))
				.Returns(() =>
				{
					var response = new RetrievePrincipalAccessResponse();
					response.Results["AccessRights"] = AccessRights.ReadAccess | AccessRights.WriteAccess | AccessRights.CreateAccess | AccessRights.DeleteAccess
						| AccessRights.AppendAccess | AccessRights.AppendToAccess | AccessRights.AssignAccess | AccessRights.ShareAccess;
					return Task.FromResult<OrganizationResponse>(response);
				});

			var result = await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), "account", Guid.NewGuid(), CancellationToken.None);

			CollectionAssert.AreEqual(
				new[] { "Create", "Read", "Write", "Delete", "Append", "Append To", "Assign", "Share" },
				result.Select(x => x.Privilege).ToArray());
		}

		[TestMethod]
		public async Task TableNameShouldBeNormalized()
		{
			RetrieveEntityRequest? captured = null;
			SetupTable(Table("account"), r => captured = r);

			await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), " Account ", null, CancellationToken.None);

			Assert.AreEqual("account", captured?.LogicalName);
		}

		[TestMethod]
		public async Task RecordCheckShouldReturnAccessRightsWithoutDepth()
		{
			SetupTable(Table("account"));
			OrganizationRequest? captured = null;
			this.crmMock
				.Setup(x => x.ExecuteAsync(It.IsAny<RetrievePrincipalAccessRequest>(), It.IsAny<CancellationToken>()))
				.Returns((OrganizationRequest r, CancellationToken _) =>
				{
					captured = r;
					var response = new RetrievePrincipalAccessResponse();
					response.Results["AccessRights"] = AccessRights.ReadAccess | AccessRights.AppendToAccess;
					return Task.FromResult<OrganizationResponse>(response);
				});

			var result = await this.service.CheckPrivilegesAsync(this.crmMock.Object, Guid.NewGuid(), "Account", Guid.NewGuid(), CancellationToken.None);

			Assert.AreEqual("account", ((RetrievePrincipalAccessRequest)captured!).Target.LogicalName);
			CollectionAssert.AreEqual(new[] { "Read", "Append To" }, result.Select(x => x.Privilege).ToArray());
			Assert.IsTrue(result.All(x => x.Depth is null));
		}
	}
}
