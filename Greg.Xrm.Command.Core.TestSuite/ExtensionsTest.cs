using System.Security.Cryptography;

namespace Greg.Xrm.Command
{
	[TestClass]
	public class ExtensionsTest
	{
		[TestMethod]
		public void SplitNameInPartsByCapitalLettersShouldWork()
		{
			Assert.AreEqual("Is BPF Entity", "IsBPFEntity".SplitNameInPartsByCapitalLetters());
			Assert.AreEqual("asdasdasd", "asdasdasd".SplitNameInPartsByCapitalLetters());
			Assert.AreEqual("asdas dasd", "asdas dasd".SplitNameInPartsByCapitalLetters());
			Assert.AreEqual("asdas da Sd", "asdas daSd".SplitNameInPartsByCapitalLetters());
		}


		[TestMethod]
		public void GenerateKeyIv()
		{
			var key = RandomNumberGenerator.GetBytes(32);
			var keyString = Convert.ToBase64String(key);

			var iv = RandomNumberGenerator.GetBytes(16);
			var ivString = Convert.ToBase64String(iv);

			Assert.IsNotNull(keyString);
			Assert.IsNotNull(ivString);
		}
	}
}
