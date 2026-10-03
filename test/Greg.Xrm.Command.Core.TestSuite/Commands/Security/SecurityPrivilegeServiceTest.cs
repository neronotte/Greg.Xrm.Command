using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Moq;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.Security
{
	[TestClass]
	public class SecurityPrivilegeServiceTest
	{
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();
		private readonly SecurityPrivilegeService service = new();

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
