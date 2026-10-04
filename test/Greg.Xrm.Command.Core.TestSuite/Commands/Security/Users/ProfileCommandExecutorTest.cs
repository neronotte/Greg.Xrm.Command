using Autofac;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using Spectre.Console;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	[TestClass]
	public class ProfileCommandExecutorTest
	{
		private readonly SecurityTeamTestContext context = new();
		private ProfileCommandExecutor Executor() => new(context.Output, context.Connections.Object, context.Profiles, context.Console);

		[TestMethod]
		public async Task JsonShouldIncludeBusinessUnitMergedRolesAndTeamsWithoutProgress()
		{
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = "john@contoso.com", Format = ProfileOutputFormat.Json }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsEmpty(result);
			var json = JObject.Parse(context.Output.ToString());
			Assert.AreEqual(context.UserId.ToString(), json["UserId"]!.ToString());
			Assert.AreEqual("Europe", json["BusinessUnit"]!["Name"]!.ToString());
			Assert.AreEqual(context.BusinessUnitId.ToString(), json["BusinessUnit"]!["Id"]!.ToString());
			var roles = (JArray)json["Roles"]!;
			Assert.HasCount(3, roles);
			var shared = roles.Single(assignment => assignment["Role"]!["RoleId"]!.ToString() == context.SharedRoleId.ToString());
			CollectionAssert.AreEqual(new[] { "Direct", "Team" }, shared["Sources"]!.Values<string>().ToArray());
			var teams = (JArray)json["Teams"]!;
			Assert.HasCount(2, teams);
			Assert.HasCount(0, (JArray)teams.Single(team => team["TeamId"]!.ToString() == context.AccessTeamId.ToString())["Roles"]!);
			Assert.HasCount(2, (JArray)teams.Single(team => team["TeamId"]!.ToString() == context.TeamId.ToString())["Roles"]!);
			Assert.AreEqual(string.Empty, context.TreeOutput.ToString());
			var query = context.Queries.Single(query => query.EntityName == "role" && query.LinkEntities.Single().LinkToEntityName == "teamroles");
			CollectionAssert.AreEquivalent(new[] { context.TeamId, context.AccessTeamId }, query.LinkEntities.Single().LinkCriteria.Conditions.Single().Values.Cast<Guid>().ToArray());
			Assert.IsTrue(query.Orders.Any(order => order.EntityName == "assignment" && order.AttributeName == "teamid"));
		}

		[TestMethod]
		public async Task RoleSharedByMultipleTeamsShouldRemainUnderEachTeamWithoutDuplicatingUserRoles()
		{
			context.Membership.Add(context.Teams[2]);
			context.TeamRoles.Add(context.TeamRole(context.SharedRoleId, "Salesperson", context.GroupTeamId));
			context.TeamRoles.Add(context.TeamRoles[0]);
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = "john@contoso.com", Format = ProfileOutputFormat.Json }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var json = JObject.Parse(context.Output.ToString());
			Assert.HasCount(3, (JArray)json["Roles"]!);
			var teams = (JArray)json["Teams"]!;
			Assert.HasCount(3, teams);
			Assert.HasCount(2, (JArray)teams.Single(team => team["TeamId"]!.ToString() == context.TeamId.ToString())["Roles"]!);
			var groupRoles = (JArray)teams.Single(team => team["TeamId"]!.ToString() == context.GroupTeamId.ToString())["Roles"]!;
			Assert.HasCount(1, groupRoles);
			Assert.AreEqual(context.SharedRoleId.ToString(), groupRoles[0]["RoleId"]!.ToString());
			Assert.AreEqual(1, context.Queries.Count(query => query.EntityName == "role" && query.LinkEntities.Single().LinkToEntityName == "teamroles"));
		}

		[TestMethod]
		public async Task MembershipShouldReadAllPagesAndUseSingleRoleBatch()
		{
			var pages = new List<int>();
			context.Crm.Setup(client => client.RetrieveMultipleAsync(It.Is<QueryBase>(query =>
				((QueryExpression)query).EntityName == "team" && ((QueryExpression)query).LinkEntities.Count > 0), It.IsAny<CancellationToken>()))
				.Returns((QueryBase query, CancellationToken _) =>
				{
					var expression = (QueryExpression)query;
					Assert.AreEqual(context.UserId, expression.LinkEntities.Single().LinkCriteria.Conditions.Single().Values[0]);
					var page = expression.PageInfo.PageNumber;
					pages.Add(page);
					return Task.FromResult(new EntityCollection([context.Membership[page - 1]]) { MoreRecords = page == 1, PagingCookie = "cookie" });
				});
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = "john@contoso.com", Format = ProfileOutputFormat.Json }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.HasCount(2, (JArray)JObject.Parse(context.Output.ToString())["Teams"]!);
			CollectionAssert.AreEqual(new[] { 1, 2 }, pages);
			Assert.AreEqual(1, context.Queries.Count(query => query.EntityName == "role" && query.LinkEntities.Single().LinkToEntityName == "teamroles"));
		}

		[TestMethod]
		public async Task MissingUserBusinessUnitShouldBeExplicitAndNotInvented()
		{
			context.Users.Setup(repository => repository.GetByDomainNameOrEmailAsync(context.Crm.Object, "john@contoso.com", 2, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new[] { new Greg.Xrm.Command.Model.SystemUser(context.UserId, "john@contoso.com", fullName: "John Doe") });
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = "john@contoso.com", Format = ProfileOutputFormat.Json }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(JTokenType.Null, JObject.Parse(context.Output.ToString())["BusinessUnit"]!.Type);
			Assert.IsFalse(context.Queries.Any(query => query.EntityName == "businessunit"));
		}

		[TestMethod]
		public async Task TreeShouldRenderEscapedNamesAndBothRoleSources()
		{
			context.Teams[0]["name"] = "[red]Sales[/]";
			context.DirectRoles[0]["name"] = "[blue]Role[/]";
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = context.UserId.ToString() }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(3, result["RoleCount"]);
			Assert.AreEqual(2, result["TeamCount"]);
			var tree = context.TreeOutput.ToString();
			StringAssert.Contains(tree, "John Doe");
			StringAssert.Contains(tree, "Europe");
			StringAssert.Contains(tree, "Direct, Team");
			StringAssert.Contains(tree, "[red]Sales[/]");
			StringAssert.Contains(tree, "[blue]Role[/]");
			StringAssert.Contains(tree, "No roles");
		}

		[TestMethod]
		public async Task OmittedUserShouldResolveCurrentUser()
		{
			var result = await Executor().ExecuteAsync(new ProfileCommand { Format = ProfileOutputFormat.Json }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			context.Crm.Verify(client => client.ExecuteAsync(It.IsAny<WhoAmIRequest>(), It.IsAny<CancellationToken>()), Times.Once);
			Assert.AreEqual(context.UserId.ToString(), JObject.Parse(context.Output.ToString())["UserId"]!.ToString());
		}

		[TestMethod]
		public async Task SameRoleNameInDifferentBusinessUnitsShouldRemainDistinct()
		{
			var different = context.TeamRole(Guid.NewGuid(), "Salesperson", context.TeamId);
			var businessUnitId = Guid.NewGuid();
			different["businessunitid"] = new EntityReference("businessunit", businessUnitId) { Name = "America" };
			context.TeamRoles.Add(different);
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = "john@contoso.com", Format = ProfileOutputFormat.Json }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var roles = (JArray)JObject.Parse(context.Output.ToString())["Roles"]!;
			Assert.HasCount(4, roles);
			Assert.AreEqual(businessUnitId.ToString(), roles.Single(role => role["Role"]!["RoleId"]!.ToString() == different.Id.ToString())["Role"]!["BusinessUnitId"]!.ToString());
		}

		[TestMethod]
		[DataRow(ProfileOutputFormat.Tree)]
		[DataRow(ProfileOutputFormat.Json)]
		public async Task EmptyMembershipAndRolesShouldSucceed(ProfileOutputFormat format)
		{
			context.Membership.Clear();
			context.DirectRoles.Clear();
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = "john@contoso.com", Format = format }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsFalse(context.Queries.Any(query => query.EntityName == "role" && query.LinkEntities.Single().LinkToEntityName == "teamroles"));
			if (format == ProfileOutputFormat.Json)
			{
				var json = JObject.Parse(context.Output.ToString());
				Assert.IsEmpty((JArray)json["Roles"]!);
				Assert.IsEmpty((JArray)json["Teams"]!);
			}
			else
			{
				StringAssert.Contains(context.TreeOutput.ToString(), "No roles");
				StringAssert.Contains(context.TreeOutput.ToString(), "No teams");
			}
		}

		[TestMethod]
		public async Task UnknownUserShouldFailBeforeProfileQueries()
		{
			context.Users.Setup(repository => repository.GetByDomainNameOrEmailAsync(context.Crm.Object, "missing", 2, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Greg.Xrm.Command.Model.SystemUser>());
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = "missing" }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			Assert.IsEmpty(context.Queries);
		}

		[TestMethod]
		public async Task ReadFailureShouldNotEmitPartialJson()
		{
			context.Crm.Setup(client => client.RetrieveMultipleAsync(It.Is<QueryBase>(query => ((QueryExpression)query).EntityName == "role"), It.IsAny<CancellationToken>()))
				.ThrowsAsync(new InvalidOperationException("Roles denied"));
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = "john@contoso.com", Format = ProfileOutputFormat.Json }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Roles denied");
			Assert.AreEqual(string.Empty, context.Output.ToString());
		}

		[TestMethod]
		public async Task CancellationShouldPropagateBeforeConnecting()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(() => Executor().ExecuteAsync(new ProfileCommand(), cancellation.Token));
			context.Connections.Verify(repository => repository.GetCurrentConnectionAsync(), Times.Never);
		}

		[TestMethod]
		public async Task CancellationTokenShouldReachAllProfileQueries()
		{
			using var cancellation = new CancellationTokenSource();
			var result = await Executor().ExecuteAsync(new ProfileCommand { User = "john@contoso.com", Format = ProfileOutputFormat.Json }, cancellation.Token);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			context.Crm.Verify(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), cancellation.Token), Times.Exactly(4));
		}

		[TestMethod]
		public void SecurityDependenciesShouldBeInjectedThroughInterfaces()
		{
			var types = typeof(ProfileCommandExecutor).Assembly.GetTypes()
				.Where(type => type.Namespace?.StartsWith("Greg.Xrm.Command.Commands.Security", StringComparison.Ordinal) == true
					|| type.Namespace == "Greg.Xrm.Command.Services.Security")
				.Where(type => type.IsClass && !type.IsAbstract && (type.Name.EndsWith("CommandExecutor", StringComparison.Ordinal)
					|| type.Name.EndsWith("Service", StringComparison.Ordinal) || type.Name.EndsWith("Resolver", StringComparison.Ordinal)))
				.ToArray();
			Assert.IsNotEmpty(types);
			foreach (var type in types)
				foreach (var constructor in type.GetConstructors())
					foreach (var parameter in constructor.GetParameters())
						Assert.IsTrue(parameter.ParameterType.IsInterface,
							$"{type.FullName}.{parameter.Name} must be injected through an interface, not {parameter.ParameterType.FullName}.");
		}

		[TestMethod]
		public void SecurityServicesAndContractsShouldNotLiveInCommandNamespaces()
		{
			var misplaced = typeof(ProfileCommandExecutor).Assembly.GetTypes()
				.Where(type => type.Namespace?.StartsWith("Greg.Xrm.Command.Commands.Security", StringComparison.Ordinal) == true)
				.Where(type => type.IsInterface || type.Name.EndsWith("Service", StringComparison.Ordinal)
					|| type.Name.EndsWith("Resolver", StringComparison.Ordinal))
				.Select(type => type.FullName).ToArray();
			Assert.IsEmpty(misplaced, string.Join(", ", misplaced));
		}

		[TestMethod]
		public async Task ProfileServiceShouldUseMockedDependenciesIncludingDirectRoleReads()
		{
			var crm = new Mock<IOrganizationServiceAsync2>(MockBehavior.Strict);
			var resolver = new Mock<ISecurityUserResolver>(MockBehavior.Strict);
			var teams = new Mock<ITeamRepository>(MockBehavior.Strict);
			var roles = new Mock<ISecurityRoleService>(MockBehavior.Strict);
			var businessUnits = new Mock<IBusinessUnitRepository>(MockBehavior.Strict);
			var userId = Guid.NewGuid();
			var role = new SecurityRoleInfo(Guid.NewGuid(), "Salesperson", "Europe", false);
			using var cancellation = new CancellationTokenSource();
			resolver.Setup(service => service.ResolveAsync(crm.Object, "john", cancellation.Token))
				.ReturnsAsync(new SecurityUserInfo(userId, "John Doe", "john"));
			teams.Setup(repository => repository.GetByUserAsync(crm.Object, userId, cancellation.Token))
				.ReturnsAsync(Array.Empty<Team>());
			roles.Setup(service => service.GetDirectRolesByUserAsync(crm.Object, userId, cancellation.Token))
				.ReturnsAsync(new[] { role });
			roles.Setup(service => service.GetRolesByTeamsAsync(crm.Object, It.Is<IEnumerable<Guid>>(ids => !ids.Any()), cancellation.Token))
				.ReturnsAsync(new Dictionary<Guid, IReadOnlyList<SecurityRoleInfo>>());
			var service = new SecurityUserProfileService(resolver.Object, teams.Object, roles.Object, businessUnits.Object);

			var profile = await service.GetAsync(crm.Object, "john", cancellation.Token);

			Assert.AreEqual(userId, profile.UserId);
			Assert.HasCount(1, profile.Roles);
			Assert.AreEqual(role, profile.Roles[0].Role);
			CollectionAssert.AreEqual(new[] { "Direct" }, profile.Roles[0].Sources.ToArray());
			Assert.IsEmpty(profile.Teams);
			resolver.VerifyAll();
			teams.VerifyAll();
			roles.VerifyAll();
			businessUnits.VerifyNoOtherCalls();
			crm.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void NewExecutorsShouldResolveUsingCoreModule()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new IoCModule());
			builder.RegisterInstance(context.Connections.Object).As<IOrganizationServiceRepository>();
			builder.RegisterInstance(context.Output).As<IOutput>();
			builder.RegisterInstance(context.Console).As<IAnsiConsole>();
			var executors = typeof(ProfileCommandExecutor).Assembly.GetTypes()
				.Where(type => type.Namespace?.StartsWith("Greg.Xrm.Command.Commands.Security", StringComparison.Ordinal) == true)
				.Where(type => type.IsClass && !type.IsAbstract && type.GetInterfaces().Any(contract =>
					contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ICommandExecutor<>)))
				.ToArray();
			Assert.IsNotEmpty(executors);
			builder.RegisterTypes(executors).AsSelf().AsImplementedInterfaces();
			using var container = builder.Build();
			foreach (var executor in executors)
				Assert.IsNotNull(container.Resolve(executor), executor.FullName);
		}
	}
}