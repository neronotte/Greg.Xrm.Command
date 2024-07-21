using Greg.Xrm.Command.Services.Encryption;
using Newtonsoft.Json;

namespace Greg.Xrm.Command.Services.Connection
{
	/// <summary>
	/// This object is used as storage for connection strings and connection-related configurations (such as default solutions).
	/// </summary>
	public class ConnectionSetting
	{
		[JsonProperty("ConnectionStrings")]
		private Dictionary<string, string> connectionStrings = new();


		/// <summary>
		/// Gets or sets the key of the currently default connection.
		/// </summary>
		public string? CurrentConnectionStringKey { get; set; }


		/// <summary>
		/// Gets the number of connection strings stored in this object.
		/// </summary>
		[JsonIgnore]
		public int Count => this.connectionStrings.Count;


		/// <summary>
		/// Gets the list of keys of the connection strings stored in this object.
		/// </summary>
		[JsonIgnore]
		public IReadOnlyCollection<string> ConnectionStringKeys => this.connectionStrings.Keys;


		/// <summary>
		/// Gets or sets the names of the default solutions for each connection.
		/// </summary>
		public Dictionary<string, string> DefaultSolutions { get; set; } = new Dictionary<string, string>();


		/// <summary>
		/// Indicates whether the connection strings are secured or not.
		/// If not, on the first access, the connection strings will be encrypted using AES.
		/// </summary>
		public bool? IsSecured { get; set; }


		/// <summary>
		/// Returns <c>true</c> if the connection string with the given name exists.
		/// </summary>
		/// <param name="connectionName">The name of the connection to look for.</param>
		/// <returns>
		/// <c>True</c> if the connection string exists, <c>false</c> otherwise.
		/// </returns>
		public bool Exists(string connectionName)
		{
			return this.connectionStrings.ContainsKey(connectionName);
		}


		/// <summary>
		/// Checks if a connection string with the given name exists and returns it, decrypted with the given key and IV.
		/// </summary>
		/// <param name="name">The name of the connection</param>
		/// <param name="key">The AES key to be used to decrypt</param>
		/// <param name="iv">The AES IV to be used to decrypt</param>
		/// <param name="connectionString">The connection string</param>
		/// <returns><c>True</c> if a connection string with the given name exists, <c>false</c> otherwise </returns>
		/// <exception cref="ArgumentNullException">If no name, key or IV is provided</exception>
		/// <exception cref="ArgumentException">If the key is not a 32 byte array</exception>
		/// <exception cref="ArgumentException">If the IV is not a 16 byte array</exception>
		public bool TryGetConnectionString(string name, byte[] key, byte[] iv, out string? connectionString)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
			if (key == null || key.Length == 0) throw new ArgumentNullException(nameof(key));
			if (iv == null || iv.Length == 0) throw new ArgumentNullException(nameof(iv));
			if (key.Length != 32) throw new ArgumentException("The key must be 32 bytes long.", nameof(key));
			if (iv.Length != 16) throw new ArgumentException("The IV must be 16 bytes long.", nameof(iv));

			if (!this.connectionStrings.TryGetValue(name, out connectionString))
			{
				connectionString = null;
				return false;
			}

			if (!IsSecured.HasValue || !IsSecured.Value)
			{
				return true;
			}

			connectionString = AesEncryption.Decrypt(connectionString ?? string.Empty, key, iv);
			return true;
		}

		/// <summary>
		/// Checks if a default connection string is configured and returns it, decrypted with the given key and IV.
		/// </summary>
		/// <param name="key">The AES key to be used to decrypt</param>
		/// <param name="iv">The AES IV to be used to decrypt</param>
		/// <param name="connectionString">The connection string</param>
		/// <returns><c>True</c> if a default connection string is configured, <c>false</c> otherwise </returns>
		/// <exception cref="ArgumentNullException">If no key or IV is provided</exception>
		/// <exception cref="ArgumentException">If the key is not a 32 byte array</exception>
		/// <exception cref="ArgumentException">If the IV is not a 16 byte array</exception>
		public bool TryGetCurrentConnectionString(byte[] key, byte[] iv, out string? connectionString)
		{
			if (key == null || key.Length == 0) throw new ArgumentNullException(nameof(key));
			if (iv == null || iv.Length == 0) throw new ArgumentNullException(nameof(iv));
			if (key.Length != 32) throw new ArgumentException("The key must be 32 bytes long.", nameof(key));
			if (iv.Length != 16) throw new ArgumentException("The IV must be 16 bytes long.", nameof(iv));

			if (string.IsNullOrWhiteSpace(this.CurrentConnectionStringKey))
			{
				connectionString = null;
				return false;
			}

			return TryGetConnectionString(this.CurrentConnectionStringKey, key, iv, out connectionString);
		}


		/// <summary>
		/// Secures all the connection strings using AES encryption.
		/// </summary>
		/// <param name="key">The AES key to be used to decrypt</param>
		/// <param name="iv">The AES IV to be used to decrypt</param>
		/// <exception cref="ArgumentNullException">If no key or IV is provided</exception>
		/// <exception cref="ArgumentException">If the key is not a 32 byte array</exception>
		/// <exception cref="ArgumentException">If the IV is not a 16 byte array</exception>
		public void SecureSettings(byte[] key, byte[] iv)
		{
			if (key == null || key.Length == 0) throw new ArgumentNullException(nameof(key));
			if (iv == null || iv.Length == 0) throw new ArgumentNullException(nameof(iv));
			if (key.Length != 32) throw new ArgumentException("The key must be 32 bytes long.", nameof(key));
			if (iv.Length != 16) throw new ArgumentException("The IV must be 16 bytes long.", nameof(iv));

			foreach (var name in this.connectionStrings.Keys)
			{
				var connectionString = this.connectionStrings[name];
				connectionString = AesEncryption.Encrypt(connectionString, key, iv);
				this.connectionStrings[name] = connectionString;
			}
			this.IsSecured = true;
		}


		/// <summary>
		/// Adds or updates a connection string to the current list.
		/// </summary>
		/// <param name="name">The name of the connection string to add</param>
		/// <param name="connectionString">The connection string </param>
		/// <param name="key">The AES key to be used to encrypt</param>
		/// <param name="iv">The AES IV to be used to encrypt</param>
		/// <exception cref="ArgumentNullException">If no connection name is provided</exception>
		/// <exception cref="ArgumentNullException">If no connection string is provided</exception>
		/// <exception cref="ArgumentNullException">If no key or IV is provided</exception>
		/// <exception cref="ArgumentException">If the key is not a 32 byte array</exception>
		/// <exception cref="ArgumentException">If the IV is not a 16 byte array</exception>
		public void UpsertConnectionString(string name, string connectionString, byte[] key, byte[] iv)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
			if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
			if (key == null || key.Length == 0) throw new ArgumentNullException(nameof(key));
			if (iv == null || iv.Length == 0) throw new ArgumentNullException(nameof(iv));
			if (key.Length != 32) throw new ArgumentException("The key must be 32 bytes long.", nameof(key));
			if (iv.Length != 16) throw new ArgumentException("The IV must be 16 bytes long.", nameof(iv));


			this.connectionStrings[name] = AesEncryption.Encrypt( connectionString, key, iv);
		}


		/// <summary>
		/// Renames a connection string.
		/// </summary>
		/// <param name="oldName">The old name</param>
		/// <param name="newName">The new name</param>
		/// <exception cref="ArgumentNullException">If the oldName received is null or whitespace</exception>
		/// <exception cref="ArgumentNullException">If the newName received is null or whitespace</exception>
		/// <exception cref="ArgumentException">If a connection with the given oldName does not exists</exception>
		/// <exception cref="ArgumentException">If a connection with the given newName already exists</exception>
		public void Rename(string oldName, string newName)
		{
			if (string.IsNullOrWhiteSpace(oldName)) throw new ArgumentNullException(nameof(oldName));
			if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentNullException(nameof(newName));
			if (!Exists(oldName)) throw new ArgumentException("The old connection name does not exist.", nameof(oldName));
			if (Exists(newName)) throw new ArgumentException("The new connection name does exists already.", nameof(newName));

			this.connectionStrings[newName] = this.connectionStrings[oldName];
			this.Remove(oldName);
		}

		/// <summary>
		/// Deletes a connection string.
		/// </summary>
		/// <param name="name">The name of the connection string to delete</param>
		public void Remove(string name)
		{
			this.connectionStrings.Remove(name);
			this.DefaultSolutions.Remove(name);
			if (this.CurrentConnectionStringKey == name)
			{
				this.CurrentConnectionStringKey = null;
			}
		}
	}
}
