using Greg.Xrm.Command.Commands.Table.Builders;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System.Diagnostics;
using System.ServiceModel;
using System.Text;

namespace Greg.Xrm.Command.Commands.Table
{
    public class ScriptCommandExecutor : ICommandExecutor<ScriptCommand>
    {
        private readonly IOutput output;
        private readonly IOrganizationServiceRepository organizationServiceRepository;
        private readonly IAttributeMetadataScriptBuilderFactory attributeMetadataScriptBuilderFactory;


        public ScriptCommandExecutor(IOutput output,
                                        IOrganizationServiceRepository organizationServiceRepository,
                                        IAttributeMetadataScriptBuilderFactory attributeMetadataScriptBuilderFactory)
        {
            this.output = output;
            this.organizationServiceRepository = organizationServiceRepository;
            this.attributeMetadataScriptBuilderFactory = attributeMetadataScriptBuilderFactory;
        }

        public async Task<CommandResult> ExecuteAsync(ScriptCommand command, CancellationToken cancellationToken)
        {
            //command > includeDefault (skipDefault)
            this.output.Write($"Connecting to the current dataverse environment...");
            var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
            this.output.WriteLine("Done", ConsoleColor.Green);

            string text;
            try
            {
                var request = new RetrieveEntityRequest
                {
                    LogicalName = command.SchemaName,
                    EntityFilters = EntityFilters.All
                };

                var response = (RetrieveEntityResponse)await crm.ExecuteAsync(request);

                var resultScript = new StringBuilder();
                if (command.IncludeTable)
				{
					resultScript.Append(GenerateTableScript(response));
				}

				var customAttributes = response.EntityMetadata.Attributes
								.Where(x => x.IsCustomAttribute.HasValue && x.IsCustomAttribute.Value
									&& x.IsPrimaryName.HasValue && !x.IsPrimaryName.Value
									&& (x.AttributeType.HasValue && x.AttributeType.Value != AttributeTypeCode.Virtual
										|| x.AttributeType == AttributeTypeCode.Virtual && x.AttributeTypeName.Value == "MultiSelectPicklistType")
									&& !x.LogicalName.ToLower().Contains("_base"))
								.ToList();

				customAttributes.ForEach(x => resultScript.Append(GenerateColumnScript(x)));
                text = resultScript.ToString();
                output.WriteLine(text);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                output.WriteLine()
                    .Write("Error: ", ConsoleColor.Red)
                    .WriteLine(ex.Message, ConsoleColor.Red);

                if (ex.InnerException != null)
                {
                    output.Write("  ").WriteLine(ex.InnerException.Message, ConsoleColor.Red);
                }
                return CommandResult.Fail(ex.Message, ex);
            }

            #region file management
            var folder = command.OutputFilePath;
            if (string.IsNullOrWhiteSpace(folder))
            {
                folder = Environment.CurrentDirectory;
            }

            if (!Directory.Exists(folder))
            {
                var msg = $"The folder '{folder}' does not exist";
                output.WriteLine()
                    .Write("Error: ", ConsoleColor.Red)
                    .WriteLine(msg, ConsoleColor.Red);
                return CommandResult.Fail(msg);
            }

            var fileName = $"{command.SchemaName}.pacx";
            var filePath = Path.Combine(folder, fileName);

            try
            {
                File.WriteAllText(filePath, text);

                if (command.AutoOpenFile)
                {
                    Process.Start(new ProcessStartInfo(filePath)
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                output.WriteLine()
                    .Write("Error while trying to write on the generated file: ", ConsoleColor.Red)
                    .WriteLine(ex.Message, ConsoleColor.Red);
                return CommandResult.Fail(ex.Message, ex);

            }
            #endregion

            return CommandResult.Success();
        }

		private static string GenerateTableScript(RetrieveEntityResponse response)
		{
            var sb = new StringBuilder();
			var tableBuilder = new TableMetadataScriptBuilder();
			sb.Append(tableBuilder.GetTableScript(response.EntityMetadata));
#pragma warning disable S6602 // "Find" method should be used instead of the "FirstOrDefault" extension
			var primaryKeyAttribute = response.EntityMetadata.Attributes
										.FirstOrDefault(x => x.IsPrimaryName.HasValue && x.IsPrimaryName.Value);
#pragma warning restore S6602 // "Find" method should be used instead of the "FirstOrDefault" extension

			if (primaryKeyAttribute != null)
				sb.Append(tableBuilder.GetColumnScript(primaryKeyAttribute));

            return sb.ToString();
		}

		public string GenerateColumnScript(AttributeMetadata attributeMetadata)
        {
            if (!attributeMetadata.AttributeType.HasValue)
                throw new ArgumentNullException(nameof(attributeMetadata), "The attributeMetadata.AttributeType is null.");

            var builder = attributeMetadataScriptBuilderFactory.CreateFor(attributeMetadata.AttributeType.Value);

            var sb = new StringBuilder();
            sb.Append(builder.GetColumnScript(attributeMetadata));
            sb.AppendLine("");

            return sb.ToString();
        }
    }
}
