using Microsoft.Identity.Client;
using System;

namespace Greg.Xrm.Command.Services.Forms
{
	public interface IFormsTokenManagerFactory
	{
		IFormsTokenManager Create(string tenantId, string clientId, string? clientSecret = null, string? username = null, string? password = null, bool useRopc = false);
	}

	public class FormsTokenManagerFactory : IFormsTokenManagerFactory
	{
		public IFormsTokenManager Create(string tenantId, string clientId, string? clientSecret = null, string? username = null, string? password = null, bool useRopc = false)
		{
			return new FormsTokenManager(tenantId, clientId, clientSecret, username, password, useRopc);
		}
	}
}
