using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Spectre.Console;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security
{
	internal sealed class SecurityTeamTestContext
	{
		public Guid UserId { get; } = Guid.NewGuid();
		public Guid BusinessUnitId { get; } = Guid.NewGuid();
		public Guid TeamId { get; } = Guid.NewGuid();
		public Guid AccessTeamId { get; } = Guid.NewGuid();
		public Guid GroupTeamId { get; } = Guid.NewGuid();
		public Guid SharedRoleId { get; } = Guid.NewGuid();
		public Mock<IOrganizationServiceAsync2> Crm { get; } = new();
		public Mock<IOrganizationServiceRepository> Connections { get; } = new();
		public OutputToMemory Output { get; } = new();
		public StringWriter TreeOutput { get; } = new();
		public IAnsiConsole Console { get; }
		public List<Entity> Teams { get; } = [];
		public List<Entity> Membership { get; } = [];
		public List<Entity> DirectRoles { get; } = [];
		public List<Entity> TeamRoles { get; } = [];
		public List<EntityCollection> ListPages { get; } = [];
		public List<QueryExpression> Queries { get; } = [];
		public List<int> ListPageNumbers { get; } = [];
		public Mock<ISystemUserRepository> Users { get; } = new();
		public Team.Repository TeamRepository { get; } = new();
		public SecurityRoleService Roles { get; } = new();
		public SecurityUserProfileService Profiles { get; }

		public SecurityTeamTestContext()
		{
			Console = AnsiConsole.Create(new AnsiConsoleSettings
			{
				Out = new AnsiConsoleOutput(TreeOutput), Ansi = AnsiSupport.No, ColorSystem = ColorSystemSupport.NoColors
			});
			Teams.Add(Team(TeamId, "Sales", 0));
			Teams.Add(Team(AccessTeamId, "Access Team", 1));
			Teams.Add(Team(GroupTeamId, "Entra Group", 2));
			Membership.AddRange(Teams.Take(2));
			DirectRoles.Add(Role(SharedRoleId, "Salesperson"));
			DirectRoles.Add(Role(Guid.NewGuid(), "Personal"));
			TeamRoles.Add(TeamRole(SharedRoleId, "Salesperson", TeamId));
			TeamRoles.Add(TeamRole(Guid.NewGuid(), "Regional", TeamId));
			Connections.Setup(repository => repository.GetCurrentConnectionAsync()).ReturnsAsync(Crm.Object);
			var user = new SystemUser(UserId, "john@contoso.com", fullName: "John Doe", businessUnit: new EntityReference("businessunit", BusinessUnitId));
			Users.Setup(repository => repository.GetByIdAsync(Crm.Object, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
			Users.Setup(repository => repository.GetByDomainNameOrEmailAsync(Crm.Object, "john@contoso.com", 2, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { user });
			Crm.Setup(client => client.ExecuteAsync(It.IsAny<WhoAmIRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new WhoAmIResponse { Results = { ["UserId"] = UserId } });
			Crm.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken token) =>
				{
					token.ThrowIfCancellationRequested();
					var expression = (QueryExpression)query;
					Queries.Add(expression);
					if (expression.EntityName == "businessunit")
						return Task.FromResult(new EntityCollection([new Entity("businessunit", BusinessUnitId) { ["name"] = "Europe" }]));
					if (expression.EntityName == "team")
					{
						if (expression.LinkEntities.Count > 0) return Task.FromResult(new EntityCollection(Membership));
						if (expression.TopCount == 2)
						{
							var condition = expression.Criteria.Conditions.Single();
							var matches = Teams.Where(team => Equals(condition.AttributeName == "teamid" ? team.Id : team["name"], condition.Values[0])).Take(2).ToList();
							return Task.FromResult(new EntityCollection(matches));
						}
						ListPageNumbers.Add(expression.PageInfo.PageNumber);
						return Task.FromResult(ListPages.Count > 0 ? ListPages[expression.PageInfo.PageNumber - 1] : new EntityCollection(Teams));
					}
					var link = expression.LinkEntities.Single();
					if (link.LinkToEntityName == "systemuserroles") return Task.FromResult(new EntityCollection(DirectRoles));
					var ids = link.LinkCriteria.Conditions.Single().Values.Cast<Guid>().ToHashSet();
					return Task.FromResult(new EntityCollection(TeamRoles.Where(role => ids.Contains((Guid)role.GetAttributeValue<AliasedValue>("assignment.teamid").Value)).ToList()));
				});
			Profiles = new SecurityUserProfileService(new SecurityUserResolver(Users.Object), TeamRepository, Roles, new BusinessUnit.Repository());
		}

		public Entity Team(Guid id, string name, int type) => new("team", id)
		{
			["name"] = name, ["teamtype"] = new OptionSetValue(type),
			["businessunitid"] = new EntityReference("businessunit", BusinessUnitId) { Name = "Europe" }
		};

		public Entity Role(Guid id, string name) => new("role", id)
		{
			["name"] = name, ["ismanaged"] = true,
			["businessunitid"] = new EntityReference("businessunit", BusinessUnitId) { Name = "Europe" }
		};

		public Entity TeamRole(Guid id, string name, Guid teamId)
		{
			var role = Role(id, name);
			role["assignment.teamid"] = new AliasedValue("teamroles", "teamid", teamId);
			return role;
		}
	}
}