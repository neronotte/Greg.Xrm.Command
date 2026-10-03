using Greg.Xrm.Command.Model;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Greg.Xrm.Command.Commands.Security
{
	public sealed record SecurityUserInfo(Guid UserId, string FullName, string DomainName);

	public sealed class SecurityUserResolver(ISystemUserRepository systemUserRepository)
	{
		/// <summary>
		/// Resolves the given user (id, domain name or primary email).
		/// If <paramref name="user"/> is null or empty, the current user (WhoAmI) is returned.
		/// </summary>
		public async Task<SecurityUserInfo> ResolveAsync(IOrganizationServiceAsync2 crm, string? user, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(user))
			{
				var whoAmI = (WhoAmIResponse)await crm.ExecuteAsync(new WhoAmIRequest(), cancellationToken);
				user = whoAmI.UserId.ToString();
			}

			user = user.Trim();

			IReadOnlyList<SystemUser> users;
			if (Guid.TryParse(user, out var userId))
			{
				var found = await systemUserRepository.GetByIdAsync(crm, userId, cancellationToken);
				users = found == null ? [] : [found];
			}
			else
			{
				users = await systemUserRepository.GetByDomainNameOrEmailAsync(crm, user, 2, cancellationToken);
			}

			if (users.Count == 0)
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"No user found matching '{user}'.");
			}
			if (users.Count > 1)
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, $"More than one user matches '{user}'. Please specify the user id.");
			}

			return new SecurityUserInfo(users[0].Id, users[0].DisplayName, users[0].DomainName);
		}
	}
}
