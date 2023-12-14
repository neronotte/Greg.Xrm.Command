using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	}
}
