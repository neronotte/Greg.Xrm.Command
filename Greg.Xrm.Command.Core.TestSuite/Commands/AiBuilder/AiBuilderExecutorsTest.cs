using Greg.Xrm.Command.Services.AiBuilder;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.PowerPlatform.Dataverse.Client;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Greg.Xrm.Command.Commands.AiBuilder
{
	[TestClass]
	public class AiModelListCommandExecutorTest
	{
		private Mock<IOutput>? outputMock;
		private Mock<IOrganizationServiceRepository>? orgRepoMock;
		private Mock<IAiBuilderApiClientFactory>? factoryMock;
		private Mock<IAiBuilderApiClient>? clientMock;
		private Mock<IOrganizationServiceAsync2>? serviceMock;

		[TestInitialize]
		public void Setup()
		{
			outputMock = new Mock<IOutput>();
			orgRepoMock = new Mock<IOrganizationServiceRepository>();
			factoryMock = new Mock<IAiBuilderApiClientFactory>();
			clientMock = new Mock<IAiBuilderApiClient>();
			serviceMock = new Mock<IOrganizationServiceAsync2>();

			orgRepoMock
				.Setup(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(serviceMock.Object);

			factoryMock
				.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(clientMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithModels_ShouldSucceedAndOutputModels()
		{
			var models = new List<AiModelInfo>
			{
				new() { Id = "guid-1", Name = "Invoice Model", Status = "Published", CreatedOn = DateTime.Now },
				new() { Id = "guid-2", Name = "Receipt Model", Status = "Training", CreatedOn = DateTime.Now.AddDays(-1) }
			};

			clientMock!
				.Setup(c => c.ListModelsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(models);

			var executor = new AiModelListCommandExecutor(
				outputMock!.Object,
				orgRepoMock!.Object,
				factoryMock!.Object);

			var result = await executor.ExecuteAsync(
				new AiModelListCommand { Format = "table" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			outputMock.Verify(o => o.WriteLine(It.IsAny<string>(), It.IsAny<ConsoleColor>()), Times.AtLeastOnce);
		}

		[TestMethod]
		public async Task ExecuteAsync_NoModels_ShouldSucceedWithNoModelsMessage()
		{
			clientMock!
				.Setup(c => c.ListModelsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(new List<AiModelInfo>());

			var executor = new AiModelListCommandExecutor(
				outputMock!.Object,
				orgRepoMock!.Object,
				factoryMock!.Object);

			var result = await executor.ExecuteAsync(
				new AiModelListCommand(),
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			outputMock.Verify(o => o.WriteLine("No AI Builder models found.", ConsoleColor.Yellow), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_JsonFormat_ShouldOutputJson()
		{
			var models = new List<AiModelInfo>
			{
				new() { Id = "guid-1", Name = "Test Model", Status = "Published" }
			};

			clientMock!
				.Setup(c => c.ListModelsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(models);

			var executor = new AiModelListCommandExecutor(
				outputMock!.Object,
				orgRepoMock!.Object,
				factoryMock!.Object);

			var result = await executor.ExecuteAsync(
				new AiModelListCommand { Format = "json" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			outputMock.Verify(o => o.WriteLine(It.Is<string>(s => s.Contains("Test Model"))), Times.Once);
		}
	}

	[TestClass]
	public class AiModelTrainCommandExecutorTest
	{
		private Mock<IOutput>? outputMock;
		private Mock<IOrganizationServiceRepository>? orgRepoMock;
		private Mock<IAiBuilderApiClientFactory>? factoryMock;
		private Mock<IAiBuilderApiClient>? clientMock;
		private Mock<IOrganizationServiceAsync2>? serviceMock;

		[TestInitialize]
		public void Setup()
		{
			outputMock = new Mock<IOutput>();
			orgRepoMock = new Mock<IOrganizationServiceRepository>();
			factoryMock = new Mock<IAiBuilderApiClientFactory>();
			clientMock = new Mock<IAiBuilderApiClient>();
			serviceMock = new Mock<IOrganizationServiceAsync2>();

			orgRepoMock
				.Setup(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(serviceMock.Object);

			factoryMock
				.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(clientMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_TrainWithoutWait_ShouldSucceed()
		{
			clientMock!
				.Setup(c => c.TrainModelAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var executor = new AiModelTrainCommandExecutor(
				outputMock!.Object,
				orgRepoMock!.Object,
				factoryMock!.Object);

			var result = await executor.ExecuteAsync(
				new AiModelTrainCommand { ModelId = "test-model-id", Wait = false },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			clientMock.Verify(c => c.TrainModelAsync("test-model-id", false, It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_TrainWithWait_ShouldSucceed()
		{
			clientMock!
				.Setup(c => c.TrainModelAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var executor = new AiModelTrainCommandExecutor(
				outputMock!.Object,
				orgRepoMock!.Object,
				factoryMock!.Object);

			var result = await executor.ExecuteAsync(
				new AiModelTrainCommand { ModelId = "test-model-id", Wait = true },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			clientMock.Verify(c => c.TrainModelAsync("test-model-id", true, It.IsAny<CancellationToken>()), Times.Once);
			outputMock.Verify(o => o.WriteLine("Training completed successfully!", ConsoleColor.Green), Times.Once);
		}
	}

	[TestClass]
	public class AiModelPublishCommandExecutorTest
	{
		private Mock<IOutput>? outputMock;
		private Mock<IOrganizationServiceRepository>? orgRepoMock;
		private Mock<IAiBuilderApiClientFactory>? factoryMock;
		private Mock<IAiBuilderApiClient>? clientMock;
		private Mock<IOrganizationServiceAsync2>? serviceMock;

		[TestInitialize]
		public void Setup()
		{
			outputMock = new Mock<IOutput>();
			orgRepoMock = new Mock<IOrganizationServiceRepository>();
			factoryMock = new Mock<IAiBuilderApiClientFactory>();
			clientMock = new Mock<IAiBuilderApiClient>();
			serviceMock = new Mock<IOrganizationServiceAsync2>();

			orgRepoMock
				.Setup(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(serviceMock.Object);

			factoryMock
				.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(clientMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_Publish_ShouldSucceed()
		{
			clientMock!
				.Setup(c => c.PublishModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var executor = new AiModelPublishCommandExecutor(
				outputMock!.Object,
				orgRepoMock!.Object,
				factoryMock!.Object);

			var result = await executor.ExecuteAsync(
				new AiModelPublishCommand { ModelId = "test-model-id" },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			clientMock.Verify(c => c.PublishModelAsync("test-model-id", It.IsAny<CancellationToken>()), Times.Once);
			outputMock.Verify(o => o.WriteLine("AI model published successfully!", ConsoleColor.Green), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_DryRun_ShouldNotPublish()
		{
			var executor = new AiModelPublishCommandExecutor(
				outputMock!.Object,
				orgRepoMock!.Object,
				factoryMock!.Object);

			var result = await executor.ExecuteAsync(
				new AiModelPublishCommand { ModelId = "test-model-id", DryRun = true },
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			clientMock.Verify(c => c.PublishModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
		}
	}

	[TestClass]
	public class AiFormProcessorConfigureCommandExecutorTest
	{
		private Mock<IOutput>? outputMock;
		private Mock<IOrganizationServiceRepository>? orgRepoMock;
		private Mock<IAiBuilderApiClientFactory>? factoryMock;
		private Mock<IAiBuilderApiClient>? clientMock;
		private Mock<IOrganizationServiceAsync2>? serviceMock;

		[TestInitialize]
		public void Setup()
		{
			outputMock = new Mock<IOutput>();
			orgRepoMock = new Mock<IOrganizationServiceRepository>();
			factoryMock = new Mock<IAiBuilderApiClientFactory>();
			clientMock = new Mock<IAiBuilderApiClient>();
			serviceMock = new Mock<IOrganizationServiceAsync2>();

			orgRepoMock
				.Setup(r => r.GetCurrentConnectionAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(serviceMock.Object);

			factoryMock
				.Setup(f => f.CreateAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(clientMock.Object);
		}

		[TestMethod]
		public async Task ExecuteAsync_Configure_ShouldSucceed()
		{
			clientMock!
				.Setup(c => c.ConfigureFormProcessorAsync(
					It.IsAny<string>(),
					It.IsAny<string>(),
					It.IsAny<string[]>(),
					It.IsAny<string[]>(),
					It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			var executor = new AiFormProcessorConfigureCommandExecutor(
				outputMock!.Object,
				orgRepoMock!.Object,
				factoryMock!.Object);

			var result = await executor.ExecuteAsync(
				new AiFormProcessorConfigureCommand
				{
					ModelId = "test-model-id",
					DocumentType = "Invoice",
					Fields = new[] { "TotalAmount", "InvoiceDate" },
					Tables = new[] { "LineItems" }
				},
				CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);
			clientMock.Verify(c => c.ConfigureFormProcessorAsync(
				"test-model-id",
				"Invoice",
				new[] { "TotalAmount", "InvoiceDate" },
				new[] { "LineItems" },
				It.IsAny<CancellationToken>()), Times.Once);
			outputMock.Verify(o => o.WriteLine("Form processor configured successfully!", ConsoleColor.Green), Times.Once);
		}
	}
}
