using Greg.Xrm.Command.Commands.Security.Roles;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.Extensions.DependencyInjection;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security.Users
{
	[TestClass]
	public class GetRolesCommandExecutorTest
	{
		[TestMethod]
		public void AutomaticRegistrationShouldResolveDelegatedExecutor()
		{
			var services = new ServiceCollection();
			services.RegisterCommandExecutors(typeof(GetRolesCommand).Assembly);
			services.AddSingleton<IOutput>(new OutputToMemory());
			services.AddSingleton(new Mock<IOrganizationServiceRepository>().Object);
			services.AddSingleton(new Mock<ISecurityUserResolver>().Object);
			services.AddSingleton(new Mock<ISecurityRoleService>().Object);
			using var provider = services.BuildServiceProvider();
			Assert.IsInstanceOfType<GetRolesCommandExecutor>(provider.GetRequiredService<ICommandExecutor<GetRolesCommand>>());
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("john.doe@contoso.com")]
		[DataRow("00000000-0000-0000-0000-000000000001")]
		public async Task ShouldForwardUserAndCancellationToken(string? user)
		{
			using var cancellation = new CancellationTokenSource();
			var expected = CommandResult.Success();
			expected["Count"] = 2;
			expected["Roles"] = "Salesperson, System Administrator";
			var delegated = new Mock<ICommandExecutor<GetByUserCommand>>();
			delegated.Setup(executor => executor.ExecuteAsync(It.IsAny<GetByUserCommand>(), cancellation.Token)).ReturnsAsync(expected);
			var executor = new GetRolesCommandExecutor(delegated.Object);

			var result = await executor.ExecuteAsync(new GetRolesCommand { User = user }, cancellation.Token);

			Assert.AreSame(expected, result);
			delegated.Verify(executor => executor.ExecuteAsync(It.Is<GetByUserCommand>(command => command.User == user), cancellation.Token), Times.Once);
			delegated.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnDelegatedFailureUnchanged()
		{
			var expected = CommandResult.Fail("User not found");
			var delegated = new Mock<ICommandExecutor<GetByUserCommand>>();
			delegated.Setup(executor => executor.ExecuteAsync(It.IsAny<GetByUserCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
			var result = await new GetRolesCommandExecutor(delegated.Object).ExecuteAsync(new GetRolesCommand { User = "missing" }, CancellationToken.None);
			Assert.AreSame(expected, result);
		}

		[TestMethod]
		public async Task ShouldPropagateDelegatedCancellation()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			var delegated = new Mock<ICommandExecutor<GetByUserCommand>>();
			delegated.Setup(executor => executor.ExecuteAsync(It.IsAny<GetByUserCommand>(), cancellation.Token))
				.Returns(Task.FromCanceled<CommandResult>(cancellation.Token));
			await Assert.ThrowsAsync<OperationCanceledException>(() =>
				new GetRolesCommandExecutor(delegated.Object).ExecuteAsync(new GetRolesCommand(), cancellation.Token));
		}
	}
}