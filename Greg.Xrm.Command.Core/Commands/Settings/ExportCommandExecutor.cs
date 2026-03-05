using ClosedXML.Excel;
using Greg.Xrm.Command.Commands.Settings.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Greg.Xrm.Command.Commands.Settings
{
    public class ExportCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISolutionRepository solutionRepository,
			ISettingDefinitionRepository settingRepository,
			IOrganizationSettingRepository organizationSettingRepository,
			IAppSettingRepository appSettingRepository) : ICommandExecutor<ExportCommand>
	{
		private readonly IOutput output = output ?? throw new ArgumentNullException(nameof(output));
		private readonly IOrganizationServiceRepository organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));
		private readonly ISolutionRepository solutionRepository = solutionRepository ?? throw new ArgumentNullException(nameof(solutionRepository));
		private readonly ISettingDefinitionRepository settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
		private readonly IOrganizationSettingRepository organizationSettingRepository = organizationSettingRepository ?? throw new ArgumentNullException(nameof(organizationSettingRepository));
		private readonly IAppSettingRepository appSettingRepository = appSettingRepository ?? throw new ArgumentNullException(nameof(appSettingRepository));

		public async Task<CommandResult> ExecuteAsync(ExportCommand command, CancellationToken cancellationToken)
		{
			this.output.Write($"Connecting to the current dataverse environment...");
			var crm = await this.organizationServiceRepository.GetCurrentConnectionAsync();
			this.output.WriteLine("Done", ConsoleColor.Green);

			Guid? solutionId = null;
			if (command.Origin == Origin.Solution)
			{
				var defaultSolutionName = command.SolutionName;
				if (string.IsNullOrWhiteSpace(defaultSolutionName))
				{
					defaultSolutionName = await this.organizationServiceRepository.GetCurrentDefaultSolutionAsync();
					if (string.IsNullOrWhiteSpace(defaultSolutionName))
					{
						return CommandResult.Fail("No solution name provided and no current solution name found in the settings.");
					}
				}

				var solution = await this.solutionRepository.GetByUniqueNameAsync(crm, defaultSolutionName);
				if (solution == null)
				{
					return CommandResult.Fail($"No solution found with uniquename <{defaultSolutionName}>.");
				}

				solutionId = solution.Id;
			}

			var onlyVisible = command.Filter == Which.Visible;
			var settingList = await this.settingRepository.GetAllAsync(crm, solutionId, onlyVisible);
			if (settingList.Count == 0)
			{
				this.output.WriteLine("No setting found!", ConsoleColor.Yellow);
				return CommandResult.Success();
			}

			var organizationSettingList = await this.organizationSettingRepository.GetByDefinitionsAsync(crm, settingList);

			var appSettingList = await this.appSettingRepository.GetByDefinitionsAsync(crm, settingList);


			// if the file name contains the {version} token, replace it with the current date time
			if (command.OutputFileName?.Contains("{version}") == true)
			{
				command.OutputFileName = command.OutputFileName.Replace("{version}", DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss"));
			}


			if (command.Format == Format.Text)
			{
				WriteText(settingList, organizationSettingList);
			}

			if (command.Format == Format.Json)
			{
				WriteJson(command, settingList, organizationSettingList, appSettingList);
			}

			if (command.Format == Format.Excel)
			{
				return await WriteExcelAsync(command, settingList, organizationSettingList, appSettingList);
			}


			return CommandResult.Success();
		}

		private void WriteText(IReadOnlyList<SettingDefinition> settingList, IReadOnlyList<OrganizationSetting> organizationSettingList)
		{
			this.output.WriteLine();
			this.output.WriteTable(
				settingList,
				rowHeaders: () => ["Unique Name", "Default Value", "Env. Value", "Type", "Visible", "Overridable on"],
				rowData: setting => [
					setting.uniquename,
					setting.defaultvalue?.Trim() ?? string.Empty,
					organizationSettingList.FirstOrDefault(x => x.settingdefinitionid?.Id == setting.Id)?.value ?? string.Empty,
					setting.FormattedDataType ?? string.Empty,
					setting.ishidden ? string.Empty : "X",
					setting.FormattedOverridableLevel ?? string.Empty]);
		}

		private void WriteJson(ExportCommand command, IReadOnlyList<SettingDefinition> settingList, IReadOnlyList<OrganizationSetting> organizationSettingList, IReadOnlyList<AppSetting> appSettingList)
		{
			var jsonItems = settingList.Select(x => new JsonSettingDefinition(
				x,
				organizationSettingList.FirstOrDefault(y => y.settingdefinitionid?.Id == x.Id),
				[.. appSettingList.Where(y => y.settingdefinitionid?.Id == x.Id)]))
			.OrderBy(x => x.uniquename)
			.ToList();

			var text = JsonConvert.SerializeObject(jsonItems, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented,
			});
			this.output.WriteLine(text);

			if (string.IsNullOrWhiteSpace(command.OutputFileName))
			{
				return;
			}


			try
			{
				File.WriteAllText(command.OutputFileName, text);
			}
			catch (Exception ex)
			{
				this.output.WriteLine($"Error writing to file <{command.OutputFileName}>: {ex.Message}", ConsoleColor.Red);
			}



			if (!command.AutoRun) return;

			Process.Start(new ProcessStartInfo
			{
				UseShellExecute = true,
				FileName = command.OutputFileName
			});
		}

		private async Task<CommandResult> WriteExcelAsync(ExportCommand command, IReadOnlyList<SettingDefinition> settingList, IReadOnlyList<OrganizationSetting> organizationSettingList, IReadOnlyList<AppSetting> appSettingList)
		{
			if (string.IsNullOrWhiteSpace(command.OutputFileName))
			{
				throw new CommandException(CommandException.CommandInvalidArgumentValue, "Output file name is required when exporting to Excel.");
			}

			this.output.Write("Creating Excel file...");

			try
			{
				using var workbook = new XLWorkbook();
				var ws = workbook.Worksheets.Add("Settings");
				ws.ShowGridLines = false;


				var appList = appSettingList
					.Select(x => x.parentappmoduleid)
					.Where(x => x != null)
					.DistinctBy(x => x.Id)
					.OrderBy(x => x.Name)
					.ToList();


				var row = 1;
				var col = 0;
				ws.Cell(row, 1).Title().SetValue("Settings list");

				row++;

				if (appList.Count > 0)
				{
					col = 5;
					var range = ws.Range(row, col, row, col + appList.Count - 1);
					range.Merge();
					range.SetValue("App value");
					range.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
					range.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
					range.Style.Fill.PatternType = XLFillPatternValues.Solid;
					range.Style.Fill.BackgroundColor = XLColor.FromArgb(68, 114, 196);
					range.Style.Font.FontColor = XLColor.White;
					range.Style.Font.Bold = true;
					range.Style.Font.FontSize = 13;
					range.Style
						.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
						.Border.SetOutsideBorderColor(XLColor.White);
				}

				row++;
				col = 0;

				ws.Cell(row, ++col).SetValue("Unique Name");
				ws.Cell(row, ++col).SetValue("Description");
				ws.Cell(row, ++col).SetValue("Default Value");
				ws.Cell(row, ++col).SetValue("Env. value");
				foreach (var app in appList)
				{
					ws.Cell(row, ++col).SetValue(app.Name);
				}
				ws.Cell(row, ++col).SetValue("Type");
				ws.Cell(row, ++col).SetValue("Visible");
				ws.Cell(row, ++col).SetValue("Overridable on");

				foreach (var setting in settingList)
				{
					row++;
					col = 0;
					ws.Cell(row, ++col).SetValue(setting.uniquename).Bold();
					ws.Cell(row, ++col).SetValue(setting.description);
					ws.Cell(row, col).Style.Alignment.SetWrapText(true);
					ws.Cell(row, ++col).AsRange()
						.SetFormat(setting.datatype, setting.defaultvalue)
						.ApplyValidation(setting.datatype)
						.Unlocked();
					ws.Cell(row, ++col).AsRange()
						.SetFormat(setting.datatype, organizationSettingList.FirstOrDefault(x => x.settingdefinitionid?.Id == setting.Id)?.value)
						.ApplyValidation(setting.datatype)
						.Unlocked(setting.overridablelevel, SettingDefinitionOverridableLevel.Organization, SettingDefinitionOverridableLevel.AppAndOrganization);

					foreach (var app in appList)
					{
						var appSetting = appSettingList.FirstOrDefault(x => x.settingdefinitionid?.Id == setting.Id && x.parentappmoduleid?.Id == app.Id);
						ws.Cell(row, ++col).AsRange()
							.SetFormat(setting.datatype, appSetting?.value)
							.ApplyValidation(setting.datatype)
							.Unlocked(setting.overridablelevel, SettingDefinitionOverridableLevel.App, SettingDefinitionOverridableLevel.AppAndOrganization);
					}

					ws.Cell(row, ++col).SetValue(setting.FormattedDataType);
					ws.Cell(row, ++col).SetValue(setting.ishidden ? string.Empty : "X");
					ws.Cell(row, ++col).SetValue(setting.FormattedOverridableLevel);
				}

				var tableRange = ws.Range(3, 1, row, col);
				tableRange.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

				
				var table = tableRange.CreateTable("Settings");
				
				var colWidth = 25;
				ws.Column(1).AdjustToContents();
				ws.Column(2).Width = colWidth * 3;
				for (var i = 3; i <= col; i++)
				{
					ws.Column(i).Width = colWidth;

					if (i >= col-2)
					{ 
						ws.Column(i).AdjustToContents();
						ws.Column(i).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
					}
				}
				ws.SheetView.Freeze(4, 3);

				var protection = ws.Protect("ciaociao");
				protection.AllowElement(XLSheetProtectionElements.Sort);
				protection.AllowElement(XLSheetProtectionElements.AutoFilter);


				workbook.Protect("ciaociao");
				workbook.LockStructure = true;
				workbook.LockWindows = true;

				workbook.SaveAs(command.OutputFileName);

				this.output.WriteLine("Done", ConsoleColor.Green);
			}
			catch(Exception ex)
			{
				this.output.WriteLine("ERROR", ConsoleColor.Red);
				return CommandResult.Fail($"Error creating Excel file: {ex.Message}");
			}

			if (command.AutoRun) 
			{
				Process.Start(new ProcessStartInfo
				{
					UseShellExecute = true,
					FileName = command.OutputFileName
				});
			}

			return CommandResult.Success();
		}
	}
}
