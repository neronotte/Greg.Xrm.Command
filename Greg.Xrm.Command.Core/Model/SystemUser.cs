using Greg.Xrm.Command.Services;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Greg.Xrm.Command.Model
{
	public sealed class SystemUser
	{
		public const string EntityName = "systemuser";

		public SystemUser(Guid id, string? domainName = null, string? firstName = null, string? lastName = null, string? fullName = null, EntityReference? businessUnit = null, bool isDisabled = false, Guid? applicationId = null)
		{
			Id = id;
			DomainName = domainName ?? string.Empty;
			FirstName = firstName ?? string.Empty;
			LastName = lastName ?? string.Empty;
			FullName = fullName ?? string.Empty;
			BusinessUnit = businessUnit;
			IsDisabled = isDisabled;
			ApplicationId = applicationId;
		}

		public SystemUser(Entity entity) : this(
			entity.Id,
			entity.GetAttributeValue<string>("domainname"),
			entity.GetAttributeValue<string>("firstname"),
			entity.GetAttributeValue<string>("lastname"),
			entity.GetAttributeValue<string>("fullname"),
			entity.GetAttributeValue<EntityReference>("businessunitid"),
			entity.GetAttributeValue<bool?>("isdisabled") ?? false,
			entity.GetAttributeValue<Guid?>("applicationid"))
		{
		}

		public Guid Id { get; }
		public string DomainName { get; }
		public string FirstName { get; }
		public string LastName { get; }
		public string FullName { get; }
		public EntityReference? BusinessUnit { get; }
		public string BusinessUnitName => BusinessUnit?.Name ?? string.Empty;
		public bool IsDisabled { get; }
		public Guid? ApplicationId { get; }
		public bool IsApplicationUser => ApplicationId.HasValue && ApplicationId.Value != Guid.Empty;

		/// <summary>
		/// Returns the full name if available, otherwise the domain name, otherwise the id.
		/// </summary>
		public string DisplayName =>
			!string.IsNullOrWhiteSpace(FullName) ? FullName
			: !string.IsNullOrWhiteSpace(DomainName) ? DomainName
			: Id.ToString();

		public class Repository : ISystemUserRepository
		{
			public async Task<SystemUser?> GetByIdAsync(IOrganizationServiceAsync2 crm, Guid userId, CancellationToken cancellationToken = default)
			{
				var query = CreateQuery();
				query.TopCount = 1;
				query.Criteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(e => new SystemUser(e)).FirstOrDefault();
			}

			public async Task<SystemUser?> GetByDomainNameAsync(IOrganizationServiceAsync2 crm, string domainName, CancellationToken cancellationToken = default)
			{
				var query = CreateQuery();
				query.TopCount = 1;
				query.Criteria.AddCondition("domainname", ConditionOperator.Equal, domainName);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(e => new SystemUser(e)).FirstOrDefault();
			}

			public async Task<IReadOnlyList<SystemUser>> GetByDomainNameOrEmailAsync(IOrganizationServiceAsync2 crm, string value, int top, CancellationToken cancellationToken = default)
			{
				var query = CreateQuery();
				query.TopCount = top;
				query.Criteria.FilterOperator = LogicalOperator.Or;
				query.Criteria.AddCondition("domainname", ConditionOperator.Equal, value);
				query.Criteria.AddCondition("internalemailaddress", ConditionOperator.Equal, value);

				var result = await crm.RetrieveMultipleAsync(query, cancellationToken);
				return result.Entities.Select(e => new SystemUser(e)).ToList();
			}

			public async Task<IReadOnlyList<SystemUser>> SearchAsync(IOrganizationServiceAsync2 crm, string? query, SystemUserSearchOptions? options = null, CancellationToken cancellationToken = default)
			{
				options ??= SystemUserSearchOptions.Default;

				var qe = CreateQuery();
				qe.Orders.Add(new OrderExpression("lastname", OrderType.Ascending));
				qe.Orders.Add(new OrderExpression("firstname", OrderType.Ascending));
				qe.Orders.Add(new OrderExpression("systemuserid", OrderType.Ascending));

				var text = query?.Trim();
				if (!string.IsNullOrEmpty(text) && Guid.TryParse(text, out var userId))
				{
					qe.Criteria.AddCondition("systemuserid", ConditionOperator.Equal, userId);
				}
				else
				{
					if (!string.IsNullOrEmpty(text))
					{
						var pattern = LikeExpression.Contains(text);
						var filter = new FilterExpression(LogicalOperator.Or);
						filter.AddCondition("firstname", ConditionOperator.Like, pattern);
						filter.AddCondition("lastname", ConditionOperator.Like, pattern);
						filter.AddCondition("domainname", ConditionOperator.Like, pattern);
						qe.Criteria.AddFilter(filter);
					}
					if (!options.IncludeDisabled)
					{
						qe.Criteria.AddCondition("isdisabled", ConditionOperator.Equal, false);
					}
					if (!options.IncludeApplicationUsers)
					{
						qe.Criteria.AddCondition("applicationid", ConditionOperator.Null);
					}
				}

				IEnumerable<SystemUser> users;
				if (options.Top.HasValue)
				{
					qe.TopCount = options.Top.Value;
					var result = await crm.RetrieveMultipleAsync(qe, cancellationToken);
					users = result.Entities.Select(e => new SystemUser(e));
				}
				else
				{
					users = await crm.RetrieveAllAsync(qe, e => new SystemUser(e), cancellationToken);
				}

				return users
					.OrderBy(x => x.LastName, StringComparer.OrdinalIgnoreCase)
					.ThenBy(x => x.FirstName, StringComparer.OrdinalIgnoreCase)
					.ThenBy(x => x.DomainName, StringComparer.OrdinalIgnoreCase)
					.ToList();
			}

			private static QueryExpression CreateQuery()
			{
				return new QueryExpression(EntityName)
				{
					NoLock = true,
					ColumnSet = new ColumnSet("systemuserid", "domainname", "firstname", "lastname", "fullname", "businessunitid", "isdisabled", "applicationid")
				};
			}
		}
	}
}
