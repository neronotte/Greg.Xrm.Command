using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

using Greg.Xrm.Command.Services.Security;

namespace Greg.Xrm.Command.Commands.Security
{
	[TestClass]
	public class SecurityUserResolverTest
	{
		private readonly Mock<IOrganizationServiceAsync2> crmMock = new();
		private readonly Mock<ISystemUserRepository> userRepoMock = new();

		[TestMethod]
		[DataRow(null)]
		[DataRow("  ")]
		public async Task ResolveAsyncWithoutUserShouldUseCurrentUser(string? user)
		{
			var currentUserId = Guid.NewGuid();
			var whoAmI = new WhoAmIResponse();
			whoAmI.Results["UserId"] = currentUserId;
			this.crmMock
				.Setup(c => c.ExecuteAsync(It.IsAny<WhoAmIRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(whoAmI);
			this.userRepoMock
				.Setup(r => r.GetByIdAsync(this.crmMock.Object, currentUserId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new SystemUser(currentUserId, @"DOMAIN\me", fullName: "Me"));

			var resolver = new SecurityUserResolver(this.userRepoMock.Object);
			var result = await resolver.ResolveAsync(this.crmMock.Object, user, CancellationToken.None);

			Assert.AreEqual(currentUserId, result.UserId);
			Assert.AreEqual("Me", result.FullName);
		}

		[TestMethod]
		public async Task ResolveAsyncWithDomainNameShouldNotCallWhoAmI()
		{
			var userId = Guid.NewGuid();
			this.userRepoMock
				.Setup(r => r.GetByDomainNameOrEmailAsync(this.crmMock.Object, @"DOMAIN\john", 2, It.IsAny<CancellationToken>()))
				.ReturnsAsync([new SystemUser(userId, @"DOMAIN\john", fullName: "John")]);

			var resolver = new SecurityUserResolver(this.userRepoMock.Object);
			var result = await resolver.ResolveAsync(this.crmMock.Object, @"DOMAIN\john", CancellationToken.None);

			Assert.AreEqual(userId, result.UserId);
			this.crmMock.Verify(c => c.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[TestMethod]
		public async Task ResolveAsyncWithUnknownUserShouldThrow()
		{
			this.userRepoMock
				.Setup(r => r.GetByDomainNameOrEmailAsync(this.crmMock.Object, "ghost@contoso.com", 2, It.IsAny<CancellationToken>()))
				.ReturnsAsync([]);

			var resolver = new SecurityUserResolver(this.userRepoMock.Object);
			var ex = await Assert.ThrowsExactlyAsync<CommandException>(() => resolver.ResolveAsync(this.crmMock.Object, "ghost@contoso.com", CancellationToken.None));

			StringAssert.Contains(ex.Message, "No user found");
		}

		[TestMethod]
		public async Task ResolveAsyncWithUnknownUserIdShouldThrow()
		{
			var resolver = new SecurityUserResolver(this.userRepoMock.Object);
			var ex = await Assert.ThrowsExactlyAsync<CommandException>(() => resolver.ResolveAsync(this.crmMock.Object, Guid.NewGuid().ToString(), CancellationToken.None));

			StringAssert.Contains(ex.Message, "No user found");
		}

		[TestMethod]
		public async Task ResolveAsyncWithMultipleMatchesShouldThrow()
		{
			this.userRepoMock
				.Setup(r => r.GetByDomainNameOrEmailAsync(this.crmMock.Object, "shared@contoso.com", 2, It.IsAny<CancellationToken>()))
				.ReturnsAsync([new SystemUser(Guid.NewGuid(), "a"), new SystemUser(Guid.NewGuid(), "b")]);

			var resolver = new SecurityUserResolver(this.userRepoMock.Object);
			var ex = await Assert.ThrowsExactlyAsync<CommandException>(() => resolver.ResolveAsync(this.crmMock.Object, "shared@contoso.com", CancellationToken.None));

			StringAssert.Contains(ex.Message, "More than one user");
		}

		[TestMethod]
		public async Task ResolveAsyncWithoutFullNameShouldFallBackToDomainName()
		{
			var userId = Guid.NewGuid();
			this.userRepoMock
				.Setup(r => r.GetByIdAsync(this.crmMock.Object, userId, It.IsAny<CancellationToken>()))
				.ReturnsAsync(new SystemUser(userId, @"DOMAIN\nofullname"));

			var resolver = new SecurityUserResolver(this.userRepoMock.Object);
			var result = await resolver.ResolveAsync(this.crmMock.Object, userId.ToString(), CancellationToken.None);

			Assert.AreEqual(@"DOMAIN\nofullname", result.FullName);
		}
	}
}
