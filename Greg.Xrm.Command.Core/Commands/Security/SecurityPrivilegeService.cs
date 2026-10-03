using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Security
{
	/// <summary>
	/// A privilege granted to a user. <see cref="Depth"/> is populated only for table-level checks.
	/// </summary>
	public sealed record SecurityPrivilegeInfo(string Privilege, PrivilegeDepth? Depth = null);

	public sealed class SecurityPrivilegeService
	{
		private static readonly (AccessRights Flag, string Label)[] RecordAccessRights =
		[
			(AccessRights.CreateAccess, "Create"),
			(AccessRights.ReadAccess, "Read"),
			(AccessRights.WriteAccess, "Write"),
			(AccessRights.DeleteAccess, "Delete"),
			(AccessRights.AppendAccess, "Append"),
			(AccessRights.AppendToAccess, "Append To"),
			(AccessRights.AssignAccess, "Assign"),
			(AccessRights.ShareAccess, "Share"),
		];

		private static readonly PrivilegeType[] PrivilegeOrder =
		[
			PrivilegeType.Create,
			PrivilegeType.Read,
			PrivilegeType.Write,
			PrivilegeType.Delete,
			PrivilegeType.Append,
			PrivilegeType.AppendTo,
			PrivilegeType.Assign,
			PrivilegeType.Share,
		];

		private static readonly IReadOnlyDictionary<PrivilegeType, string> PrivilegeLabels = new Dictionary<PrivilegeType, string>
		{
			[PrivilegeType.Create] = "Create",
			[PrivilegeType.Read] = "Read",
			[PrivilegeType.Write] = "Write",
			[PrivilegeType.Delete] = "Delete",
			[PrivilegeType.Assign] = "Assign",
			[PrivilegeType.Share] = "Share",
			[PrivilegeType.Append] = "Append",
			[PrivilegeType.AppendTo] = "Append To",
		};

		public async Task<IReadOnlyList<SecurityPrivilegeInfo>> CheckPrivilegesAsync(IOrganizationServiceAsync2 crm, Guid userId, string tableName, Guid? recordId, CancellationToken cancellationToken)
		{
			var table = await RetrieveTableAsync(crm, tableName, cancellationToken);

			if (recordId.HasValue)
			{
				return await GetRecordPrivilegesAsync(crm, userId, table.LogicalName, recordId.Value, cancellationToken);
			}

			return await GetTablePrivilegesAsync(crm, userId, table, cancellationToken);
		}

		private static async Task<EntityMetadata> RetrieveTableAsync(IOrganizationServiceAsync2 crm, string tableName, CancellationToken cancellationToken)
		{
			var logicalName = tableName.Trim().ToLowerInvariant();
			try
			{
				var response = (RetrieveEntityResponse)await crm.ExecuteAsync(new RetrieveEntityRequest
				{
					LogicalName = logicalName,
					EntityFilters = EntityFilters.Privileges
				}, cancellationToken);

				return response.EntityMetadata;
			}
			catch (FaultException<OrganizationServiceFault> ex)
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"Table '{tableName}' not found: {ex.Message}", ex);
			}
		}

		private static async Task<IReadOnlyList<SecurityPrivilegeInfo>> GetRecordPrivilegesAsync(IOrganizationServiceAsync2 crm, Guid userId, string tableName, Guid recordId, CancellationToken cancellationToken)
		{
			var request = new RetrievePrincipalAccessRequest
			{
				Principal = new EntityReference("systemuser", userId),
				Target = new EntityReference(tableName, recordId)
			};

			var response = (RetrievePrincipalAccessResponse)await crm.ExecuteAsync(request, cancellationToken);

			return RecordAccessRights
				.Where(x => response.AccessRights.HasFlag(x.Flag))
				.Select(x => new SecurityPrivilegeInfo(x.Label))
				.ToList();
		}

		private static async Task<IReadOnlyList<SecurityPrivilegeInfo>> GetTablePrivilegesAsync(IOrganizationServiceAsync2 crm, Guid userId, EntityMetadata table, CancellationToken cancellationToken)
		{
			var tablePrivileges = (table.Privileges ?? []).ToDictionary(x => x.PrivilegeId);
			if (tablePrivileges.Count == 0)
			{
				return [];
			}

			var response = (RetrieveUserPrivilegesResponse)await crm.ExecuteAsync(new RetrieveUserPrivilegesRequest { UserId = userId }, cancellationToken);

			// the same privilege can be granted by multiple roles: keep the widest depth
			return (response.RolePrivileges ?? [])
				.Where(x => tablePrivileges.ContainsKey(x.PrivilegeId))
				.GroupBy(x => x.PrivilegeId)
				.Select(g =>
				{
					var type = tablePrivileges[g.Key].PrivilegeType;
					var label = PrivilegeLabels.TryGetValue(type, out var l) ? l : type.ToString();
					return (Type: type, Info: new SecurityPrivilegeInfo(label, g.Max(x => x.Depth)));
				})
				.OrderBy(x => GetPrivilegeSortIndex(x.Type))
				.ThenBy(x => x.Info.Privilege, StringComparer.OrdinalIgnoreCase)
				.Select(x => x.Info)
				.ToList();
		}

		private static int GetPrivilegeSortIndex(PrivilegeType type)
		{
			var index = Array.IndexOf(PrivilegeOrder, type);
			return index < 0 ? int.MaxValue : index;
		}
	}
}
