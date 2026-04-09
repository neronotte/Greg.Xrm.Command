using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Greg.Xrm.Command.Services.OptionSet;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Moq;

namespace Greg.Xrm.Command.Commands.Column.Create
{
	[TestClass]
	public class CreatePicklistCommandExecutorTest : CommandExecutorTestBase
	{
		private readonly CreatePicklistCommandExecutor executor;
		private readonly Mock<IOptionSetParser> optionSetParserMock;

		public CreatePicklistCommandExecutorTest()
		{
			this.optionSetParserMock = new Mock<IOptionSetParser>();
			this.executor = new CreatePicklistCommandExecutor(this.Output, this.OrganizationServiceRepositoryMock.Object, this.optionSetParserMock.Object);
		}

		private void SetupMocks()
		{
			// Language code mock (organization)
			var organization = new Entity("organization");
			organization["languagecode"] = 1033;
			var orgCollection = new EntityCollection(new List<Entity> { organization });

			// Solution and Publisher mock
			var solution = new Entity("solution");
			solution["ismanaged"] = false;
			solution["publisher.customizationprefix"] = new AliasedValue("publisher", "customizationprefix", "new");
			solution["publisher.customizationoptionvalueprefix"] = new AliasedValue("publisher", "customizationoptionvalueprefix", 10000);
			var solCollection = new EntityCollection(new List<Entity> { solution });

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((QueryBase q, CancellationToken ct) => 
				{
					if (q is QueryExpression qe)
					{
						if (qe.EntityName == "organization") return orgCollection;
						if (qe.EntityName == "solution") return solCollection;
					}
					return new EntityCollection();
				});

			this.OrganizationServiceMock
				.Setup(x => x.RetrieveMultipleAsync(It.IsAny<QueryBase>()))
				.ReturnsAsync((QueryBase q) => 
				{
					if (q is QueryExpression qe)
					{
						if (qe.EntityName == "organization") return orgCollection;
						if (qe.EntityName == "solution") return solCollection;
					}
					return new EntityCollection();
				});

			this.optionSetParserMock
				.Setup(x => x.Parse(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
				.Returns(new List<OptionMetadata>());

			this.OrganizationServiceMock
				.Setup(x => x.ExecuteAsync(It.IsAny<OrganizationRequest>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(new Microsoft.Xrm.Sdk.Messages.CreateAttributeResponse { });
		}

		[TestMethod]
		public async Task ExecuteAsync_WithSuppressCodeSuffixTrue_ShouldNotAddCodeSuffix()
		{
			SetupMocks();

			var command = new CreatePicklistCommand
			{
				EntityName = "account",
				DisplayName = "My Picklist",
				SchemaName = "new_mypicklist",
				Options = "A,B",
				SuppressCodeSuffix = true,
				SolutionName = "MySolution"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);

			this.OrganizationServiceMock.Verify(x => x.ExecuteAsync(It.Is<Microsoft.Xrm.Sdk.Messages.CreateAttributeRequest>(r => 
				r.Attribute.SchemaName == "new_mypicklist"
			), It.IsAny<CancellationToken>()), Times.Once);
		}

		[TestMethod]
		public async Task ExecuteAsync_WithSuppressCodeSuffixFalse_ShouldAddCodeSuffix()
		{
			SetupMocks();

			var command = new CreatePicklistCommand
			{
				EntityName = "account",
				DisplayName = "My Picklist",
				SchemaName = "new_mypicklist",
				Options = "A,B",
				SuppressCodeSuffix = false,
				SolutionName = "MySolution"
			};

			var result = await executor.ExecuteAsync(command, CancellationToken.None);

			Assert.IsTrue(result.IsSuccess, result.ErrorMessage);

			this.OrganizationServiceMock.Verify(x => x.ExecuteAsync(It.Is<Microsoft.Xrm.Sdk.Messages.CreateAttributeRequest>(r => 
				r.Attribute.SchemaName == "new_mypicklistcode"
			), It.IsAny<CancellationToken>()), Times.Once);
		}
	}
}
