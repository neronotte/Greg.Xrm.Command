using Microsoft.Identity.Client.Extensions.Msal;

namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// Tells whether the OS secure storage used by the MSAL token cache works on this machine (issue #217).
	/// </summary>
	/// <remarks>
	/// Behind an interface so that tests can force either answer without touching the real OS storage.
	/// </remarks>
	public interface ISecureTokenStorageProbe
	{
		/// <summary>
		/// Returns <c>true</c> if the MSAL token cache can be persisted securely on this machine.
		/// </summary>
		bool IsAvailable();
	}


	/// <summary>
	/// Real <see cref="ISecureTokenStorageProbe"/> based on <see cref="MsalCacheHelper.VerifyPersistence"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// WHY A PROBE IS NEEDED. The SDK never reports whether it could persist its cache: when the secure storage
	/// is missing it silently keeps tokens in memory only (verified on headless Linux on 2026-10-18: no error,
	/// no file, no keyring entry). PACX must know it up front to decide between the secure SDK cache and the
	/// legacy fallback (see <see cref="TokenCacheMode"/>).
	/// </para>
	/// <para>
	/// HOW. <see cref="MsalCacheHelper.VerifyPersistence"/> writes, reads back and deletes a dummy value with
	/// the same mechanism MSAL uses for real tokens: DPAPI-protected file on Windows, Secret Service keyring on
	/// Linux, Keychain on macOS. No token is involved. It uses a validation-only location, and this probe also
	/// uses its own file name and keyring attributes, so it can never touch the SDK cache. It is not required to
	/// mirror the SDK attributes: only the availability of the mechanism matters.
	/// </para>
	/// <para>
	/// The result is computed once per process (lazily, and only when an OAuth profile is used) because the
	/// answer does not change during a command and, on desktop Linux, probing a locked keyring shows an unlock
	/// prompt: the user must see it at most once.
	/// </para>
	/// </remarks>
	public sealed class MsalSecureTokenStorageProbe : ISecureTokenStorageProbe
	{
		private const string ProbeFileName = "pacx_secure_storage_probe.bin";
		private const string LinuxSchemaName = "pacx.securestorage.probe";
		private const string LinuxLabel = "PACX secure storage probe";
		private const string MacServiceName = "pacx_secure_storage_probe_service";
		private const string MacAccountName = "pacx_secure_storage_probe_account";

		private readonly Lazy<bool> isAvailable;

		/// <summary>
		/// Creates the probe.
		/// </summary>
		/// <param name="getCacheDirectory">
		/// Returns the PACX token cache directory. Resolved lazily so that constructing the repository never
		/// creates folders or probes the OS storage for commands that do not connect to Dataverse.
		/// </param>
		public MsalSecureTokenStorageProbe(Func<string> getCacheDirectory)
		{
			ArgumentNullException.ThrowIfNull(getCacheDirectory);
			this.isAvailable = new Lazy<bool>(() => Probe(getCacheDirectory()));
		}

		/// <inheritdoc/>
		public bool IsAvailable() => this.isAvailable.Value;


		private static bool Probe(string cacheDirectory)
		{
			try
			{
				var properties = new StorageCreationPropertiesBuilder(ProbeFileName, cacheDirectory)
					.WithLinuxKeyring(
						LinuxSchemaName,
						MsalCacheHelper.LinuxKeyRingDefaultCollection,
						LinuxLabel,
						new KeyValuePair<string, string>("ProductGroup", "pacx"),
						new KeyValuePair<string, string>("Purpose", "probe"))
					.WithMacKeyChain(MacServiceName, MacAccountName)
					.Build();

				var helper = MsalCacheHelper.CreateAsync(properties).ConfigureAwait(false).GetAwaiter().GetResult();

				// Throws MsalCachePersistenceException when the storage cannot be used (e.g. no D-Bus/keyring).
				helper.VerifyPersistence();
				return true;
			}
			catch (Exception)
			{
				// Any failure means "not usable": PACX then keeps its pre-#217 behavior, which is the safe default.
				return false;
			}
		}
	}
}
