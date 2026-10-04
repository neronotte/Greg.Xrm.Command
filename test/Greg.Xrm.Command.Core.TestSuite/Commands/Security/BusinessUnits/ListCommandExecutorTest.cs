using Autofac;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.ServiceModel;

namespace Greg.Xrm.Command.Commands.Security.BusinessUnits
{
	[TestClass]
	public class ListCommandExecutorTest
	{
		private readonly Mock<IOrganizationServiceAsync2> crm = new(MockBehavior.Strict);
		private readonly Mock<IOrganizationServiceRepository> connections = new(MockBehavior.Strict);
		private readonly Mock<IBusinessUnitRepository> units = new(MockBehavior.Strict);
		private readonly OutputToMemory output = new();
		private readonly StringWriter treeOutput = new();
		private readonly IAnsiConsole console;

		public ListCommandExecutorTest()
		{
			console = AnsiConsole.Create(new AnsiConsoleSettings
			{
				Out = new AnsiConsoleOutput(treeOutput), Ansi = AnsiSupport.No, ColorSystem = ColorSystemSupport.NoColors
			});
			connections.Setup(repository => repository.GetCurrentConnectionAsync()).ReturnsAsync(crm.Object);
		}

		private ListCommandExecutor Executor() => new(output, connections.Object, units.Object, console);

		private async Task SetupUnitsAsync(params Entity[] entities)
		{
			var mapper = new Mock<IOrganizationServiceAsync2>();
			mapper.Setup(client => client.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new EntityCollection(entities.ToList()));
			var mapped = await new BusinessUnit.Repository().GetAllAsync(mapper.Object, CancellationToken.None);
			units.Setup(repository => repository.GetAllAsync(crm.Object, It.IsAny<CancellationToken>())).ReturnsAsync(mapped);
		}

		private static Entity Unit(Guid id, string name, Guid? parentId = null)
		{
			var entity = new Entity("businessunit", id) { ["name"] = name };
			if (parentId.HasValue) entity["parentbusinessunitid"] = new EntityReference("businessunit", parentId.Value);
			return entity;
		}

		[TestMethod]
		public async Task JsonShouldContainSortedNestedHierarchyWithoutProgressOrDataverseCalls()
		{
			var rootId = Guid.NewGuid();
			var branchId = Guid.NewGuid();
			var leafId = Guid.NewGuid();
			var alphaId = Guid.Parse("00000000-0000-0000-0000-000000000001");
			var otherAlphaId = Guid.Parse("00000000-0000-0000-0000-000000000002");
			await SetupUnitsAsync(Unit(leafId, "Leaf", branchId), Unit(branchId, "Zulu", rootId),
				Unit(otherAlphaId, "alpha", rootId), Unit(rootId, "Root"), Unit(alphaId, "Alpha", rootId));
			using var cancellation = new CancellationTokenSource();

			var result = await Executor().ExecuteAsync(new ListCommand { Format = BusinessUnitOutputFormat.Json }, cancellation.Token);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.IsEmpty(result);
			var roots = (JArray)JObject.Parse(output.ToString())["BusinessUnits"]!;
			Assert.HasCount(1, roots);
			Assert.AreEqual(rootId.ToString(), roots[0]["Id"]!.ToString());
			Assert.AreEqual(JTokenType.Null, roots[0]["ParentId"]!.Type);
			var children = (JArray)roots[0]["Children"]!;
			CollectionAssert.AreEqual(new[] { alphaId.ToString(), otherAlphaId.ToString(), branchId.ToString() },
				children.Select(child => child["Id"]!.ToString()).ToArray());
			Assert.AreEqual(leafId.ToString(), children[2]["Children"]![0]!["Id"]!.ToString());
			Assert.AreEqual(branchId.ToString(), children[2]["Children"]![0]!["ParentId"]!.ToString());
			Assert.IsEmpty((JArray)children[0]["Children"]!);
			Assert.AreEqual(string.Empty, treeOutput.ToString());
			units.Verify(repository => repository.GetAllAsync(crm.Object, cancellation.Token), Times.Once);
			connections.Verify(repository => repository.GetCurrentConnectionAsync(), Times.Once);
			crm.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task JsonShouldNotWrapLongNamesWithAnsiConsoleOutput()
		{
			const string longName = "A business unit name that exceeds the console width";
			await SetupUnitsAsync(Unit(Guid.NewGuid(), longName));
			using var rendered = new StringWriter();
			var narrowConsole = AnsiConsole.Create(new AnsiConsoleSettings
			{
				Out = new AnsiConsoleOutput(rendered), Ansi = AnsiSupport.No, ColorSystem = ColorSystemSupport.NoColors
			});
			narrowConsole.Profile.Width = 20;
			var executor = new ListCommandExecutor(new OutputToAnsiConsole(narrowConsole), connections.Object, units.Object, narrowConsole);

			var result = await executor.ExecuteAsync(new ListCommand { Format = BusinessUnitOutputFormat.Json }, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(longName, JObject.Parse(rendered.ToString())["BusinessUnits"]![0]!["Name"]!.ToString());
		}

		[TestMethod]
		public async Task TreeShouldRenderEscapedNamesAndCounts()
		{
			var rootId = Guid.NewGuid();
			var childId = Guid.NewGuid();
			await SetupUnitsAsync(Unit(childId, "[blue]Child[/]", rootId), Unit(rootId, "[red]Root[/]"));

			var result = await Executor().ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			Assert.AreEqual(2, result["Count"]);
			Assert.AreEqual(1, result["RootCount"]);
			var tree = treeOutput.ToString();
			StringAssert.Contains(tree, "[red]Root[/]");
			StringAssert.Contains(tree, "[blue]Child[/]");
			StringAssert.Contains(tree, rootId.ToString());
			StringAssert.Contains(tree, childId.ToString());
			Assert.IsTrue(tree.IndexOf("Root", StringComparison.Ordinal) < tree.IndexOf("Child", StringComparison.Ordinal));
		}

		[TestMethod]
		public async Task TreeShouldUsePaletteColorsAndStartWithBlankLine()
		{
			await SetupUnitsAsync(Unit(Guid.NewGuid(), "Europe"));
			using var rendered = new StringWriter();
			var coloredConsole = AnsiConsole.Create(new AnsiConsoleSettings
			{
				Out = new AnsiConsoleOutput(rendered), Ansi = AnsiSupport.Yes, ColorSystem = ColorSystemSupport.TrueColor
			});
			coloredConsole.Profile.Width = 240;
			var executor = new ListCommandExecutor(output, connections.Object, units.Object, coloredConsole);

			var result = await executor.ExecuteAsync(new ListCommand(), CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var text = rendered.ToString();
			var labelAnsi = "\u001b[38;5;111m";
			var valueAnsi = "\u001b[38;5;215m";
			Assert.IsTrue(text.StartsWith(Environment.NewLine, StringComparison.Ordinal));
			StringAssert.Contains(text, $"{labelAnsi}Business units\u001b[0m");
			StringAssert.Contains(text, $"{valueAnsi}Europe\u001b[0m");
		}

		[TestMethod]
		public async Task MissingParentShouldRetainUnitAsSortedRoot()
		{
			var missingId = Guid.NewGuid();
			await SetupUnitsAsync(Unit(Guid.NewGuid(), "Zulu"), Unit(Guid.NewGuid(), "Alpha", missingId));
			var result = await Executor().ExecuteAsync(new ListCommand { Format = BusinessUnitOutputFormat.Json }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			var roots = (JArray)JObject.Parse(output.ToString())["BusinessUnits"]!;
			Assert.HasCount(2, roots);
			Assert.AreEqual("Alpha", roots[0]["Name"]!.ToString());
			Assert.AreEqual(missingId.ToString(), roots[0]["ParentId"]!.ToString());
		}

		[TestMethod]
		[DataRow(BusinessUnitOutputFormat.Tree)]
		[DataRow(BusinessUnitOutputFormat.Json)]
		public async Task EmptyHierarchyShouldSucceed(BusinessUnitOutputFormat format)
		{
			await SetupUnitsAsync();
			var result = await Executor().ExecuteAsync(new ListCommand { Format = format }, CancellationToken.None);
			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			if (format == BusinessUnitOutputFormat.Json)
				Assert.IsEmpty((JArray)JObject.Parse(output.ToString())["BusinessUnits"]!);
			else
			{
				Assert.AreEqual(0, result["Count"]);
				StringAssert.Contains(treeOutput.ToString(), "No business units");
			}
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task CyclesShouldFailWithoutPartialJson(bool selfReference)
		{
			var first = Guid.NewGuid();
			var second = selfReference ? first : Guid.NewGuid();
			var entities = new List<Entity> { Unit(first, "First", second), Unit(Guid.NewGuid(), "Root") };
			if (!selfReference) entities.Add(Unit(second, "Second", first));
			await SetupUnitsAsync(entities.ToArray());
			var result = await Executor().ExecuteAsync(new ListCommand { Format = BusinessUnitOutputFormat.Json }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "cycle");
			Assert.AreEqual(string.Empty, output.ToString());
		}

		[TestMethod]
		public async Task DataverseFailureShouldNotEmitPartialJson()
		{
			units.Setup(repository => repository.GetAllAsync(crm.Object, It.IsAny<CancellationToken>()))
				.ThrowsAsync(new FaultException<OrganizationServiceFault>(new OrganizationServiceFault(), "Access denied"));
			var result = await Executor().ExecuteAsync(new ListCommand { Format = BusinessUnitOutputFormat.Json }, CancellationToken.None);
			Assert.IsFalse(result.IsSuccess);
			StringAssert.Contains(result.ErrorMessage, "Access denied");
			Assert.AreEqual(string.Empty, output.ToString());
		}

		[TestMethod]
		public async Task CancellationShouldPropagateBeforeConnecting()
		{
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(() => Executor().ExecuteAsync(new ListCommand(), cancellation.Token));
			connections.Verify(repository => repository.GetCurrentConnectionAsync(), Times.Never);
			units.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task CancellationDuringReadShouldPropagate()
		{
			using var cancellation = new CancellationTokenSource();
			units.Setup(repository => repository.GetAllAsync(crm.Object, cancellation.Token))
				.Returns(() =>
				{
					cancellation.Cancel();
					return Task.FromCanceled<IReadOnlyList<BusinessUnit>>(cancellation.Token);
				});
			await Assert.ThrowsAsync<OperationCanceledException>(() => Executor().ExecuteAsync(
				new ListCommand { Format = BusinessUnitOutputFormat.Json }, cancellation.Token));
			Assert.AreEqual(string.Empty, output.ToString());
		}

		[TestMethod]
		public void ExecutorShouldResolveUsingCoreModule()
		{
			var builder = new ContainerBuilder();
			builder.RegisterModule(new IoCModule());
			builder.RegisterInstance(output).As<IOutput>();
			builder.RegisterInstance(connections.Object).As<IOrganizationServiceRepository>();
			builder.RegisterInstance(console).As<IAnsiConsole>();
			builder.RegisterType<ListCommandExecutor>();
			using var container = builder.Build();
			Assert.IsNotNull(container.Resolve<ListCommandExecutor>());
		}
	}
}