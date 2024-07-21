using System.Security.Cryptography;
using System.Text;

namespace Greg.Xrm.Command.Services.Encryption
{
    public class AesEncryption
	{
		public static string Encrypt(string plaintext, byte[] key, byte[] iv)
		{
			using Aes aesAlg = Aes.Create();
			aesAlg.Key = key;
			aesAlg.IV = iv;

			ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
			byte[] encryptedBytes;
			using (var msEncrypt = new System.IO.MemoryStream())
			{
				using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
				{
					byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
					csEncrypt.Write(plainBytes, 0, plainBytes.Length);
				}
				encryptedBytes = msEncrypt.ToArray();
			}
			return Convert.ToBase64String(encryptedBytes);
		}


		public static string Decrypt(string ciphertext, byte[] key, byte[] iv)
		{
			using Aes aesAlg = Aes.Create();
			
			aesAlg.Key = key;
			aesAlg.IV = iv;
			ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
			byte[] decryptedBytes;

			using var msDecrypt = new MemoryStream(Convert.FromBase64String(ciphertext));
			using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
			using var msPlain = new MemoryStream();

			csDecrypt.CopyTo(msPlain);
			decryptedBytes = msPlain.ToArray();


			return Encoding.UTF8.GetString(decryptedBytes);

		}
	}
}
