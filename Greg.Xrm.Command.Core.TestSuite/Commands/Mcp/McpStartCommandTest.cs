namespace Greg.Xrm.Command.Commands.Mcp
{
	[TestClass]
	public class McpStartCommandTest
	{
		[TestMethod]
		public void ParseWithDefaultsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<McpStartCommand>(
				"mcp", "start");

			Assert.AreEqual(3000, command.Port);
			Assert.AreEqual("stdio", command.Transport);
			Assert.AreEqual("localhost", command.Host);
		}

		[TestMethod]
		public void ParseWithAllOptionsShouldWork()
		{
			var command = Commands.Utility.TestParseCommand<McpStartCommand>(
				"mcp", "start",
				"-p", "5000",
				"-t", "http",
				"--host", "0.0.0.0");

			Assert.AreEqual(5000, command.Port);
			Assert.AreEqual("http", command.Transport);
			Assert.AreEqual("0.0.0.0", command.Host);
		}
	}

	[TestClass]
	public class CapturedOutputTest
	{
		[TestMethod]
		public void WriteShouldCaptureText()
		{
			var output = new CapturedOutput();
			output.Write("Hello");
			output.WriteLine(" World");

			Assert.AreEqual(2, output.Lines.Count);
			Assert.AreEqual("Hello", output.Lines[0]);
			Assert.AreEqual(" World", output.Lines[1]);
		}

		[TestMethod]
		public void WriteLineShouldCaptureWithNewline()
		{
			var output = new CapturedOutput();
			output.WriteLine("Line 1");
			output.WriteLine("Line 2");

			var captured = output.GetCapturedOutput();
			Assert.AreEqual("Line 1\nLine 2", captured);
		}

		[TestMethod]
		public void WriteTableShouldSerializeHeadersAndRows()
		{
			var output = new CapturedOutput();
			var data = new[]
			{
				new { Name = "Alice", Age = "30" },
				new { Name = "Bob", Age = "25" },
			};

			output.WriteTable(data, () => new[] { "Name", "Age" }, row => new[] { row.Name, row.Age });

			Assert.IsTrue(output.Lines.Count >= 3);
			Assert.AreEqual("Name | Age", output.Lines[0]);
			Assert.IsTrue(output.Lines[1].StartsWith("---"));
			Assert.AreEqual("Alice | 30", output.Lines[2]);
		}
	}
}
