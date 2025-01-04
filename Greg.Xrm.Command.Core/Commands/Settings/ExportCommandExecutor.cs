using Greg.Xrm.Command.Commands.Settings.Model;
using Greg.Xrm.Command.Model;
using Greg.Xrm.Command.Services.Connection;
using Greg.Xrm.Command.Services.Output;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Diagnostics;
using System.Drawing;

namespace Greg.Xrm.Command.Commands.Settings
{
    public class ExportCommandExecutor : ICommandExecutor<ExportCommand>
	{
		private readonly IOutput output;
		private readonly IOrganizationServiceRepository organizationServiceRepository;
		private readonly ISolutionRepository solutionRepository;
		private readonly ISettingDefinitionRepository settingRepository;
		private readonly IOrganizationSettingRepository organizationSettingRepository;
		private readonly IAppSettingRepository appSettingRepository;

		public ExportCommandExecutor(
			IOutput output,
			IOrganizationServiceRepository organizationServiceRepository,
			ISolutionRepository solutionRepository,
			ISettingDefinitionRepository settingRepository,
			IOrganizationSettingRepository organizationSettingRepository,
			IAppSettingRepository appSettingRepository)
        {
			this.output = output ?? throw new ArgumentNullException(nameof(output));
			this.organizationServiceRepository = organizationServiceRepository ?? throw new ArgumentNullException(nameof(organizationServiceRepository));
			this.solutionRepository = solutionRepository ?? throw new ArgumentNullException(nameof(solutionRepository));
			this.settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));
			this.organizationSettingRepository = organizationSettingRepository ?? throw new ArgumentNullException(nameof(organizationSettingRepository));
			this.appSettingRepository = appSettingRepository ?? throw new ArgumentNullException(nameof(appSettingRepository));
		}


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
				rowHeaders: () => new[] { "Unique Name", "Default Value", "Env. Value", "Type", "Visible", "Overridable on" },
				rowData: setting => new[] {
					setting.uniquename,
					setting.defaultvalue?.Trim() ?? string.Empty,
					organizationSettingList.FirstOrDefault(x => x.settingdefinitionid?.Id == setting.Id)?.value ?? string.Empty,
					setting.FormattedDataType ?? string.Empty,
					setting.ishidden ? string.Empty : "X",
					setting.FormattedOverridableLevel ?? string.Empty});
		}

		private void WriteJson(ExportCommand command, IReadOnlyList<SettingDefinition> settingList, IReadOnlyList<OrganizationSetting> organizationSettingList, IReadOnlyList<AppSetting> appSettingList)
		{
			var jsonItems = settingList.Select(x => new JsonSettingDefinition(
				x,
				organizationSettingList.FirstOrDefault(y => y.settingdefinitionid?.Id == x.Id),
				appSettingList.Where(y => y.settingdefinitionid?.Id == x.Id).ToArray()))
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
				using var package = new ExcelPackage();
				var ws = package.Workbook.Worksheets.Add("Settings");
				ws.View.ShowGridLines = false;


				var appList = appSettingList
					.Select(x => x.parentappmoduleid)
					.Where(x => x != null)
					.DistinctBy(x => x.Id)
					.OrderBy(x => x.Name)
					.ToList();


				var row = 1;
				var col = 0;
				ws.Cells[row, 1].Title().SetValue("Settings list");

				row++;

				if (appList.Count > 0)
				{
					col = 5;
					var range = ws.Cells[row, col, row, col + appList.Count - 1];
					range.Merge = true;
					range.Value = "App value";
					range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
					range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
					range.Style.Fill.PatternType = ExcelFillStyle.Solid;
					range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
					range.Style.Font.Color.SetColor(Color.White);
					range.Style.Font.Bold = true;
					range.Style.Font.Size = 13;
					range.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.White);
				}

				row++;
				col = 0;

				ws.Cells[row, ++col].Value = "Unique Name";
				ws.Cells[row, ++col].Value = "Description";
				ws.Cells[row, ++col].Value = "Default Value";
				ws.Cells[row, ++col].Value = "Env. value";
				foreach (var app in appList)
				{
					ws.Cells[row, ++col].Value = app.Name;
				}
				ws.Cells[row, ++col].Value = "Type";
				ws.Cells[row, ++col].Value = "Visible";
				ws.Cells[row, ++col].Value = "Overridable on";

				foreach (var setting in settingList)
				{
					row++;
					col = 0;
					ws.Cells[row, ++col].SetValue(setting.uniquename).Bold();
					ws.Cells[row, ++col].Value = setting.description;
					ws.Cells[row, col].Style.WrapText = true;
					ws.Cells[row, ++col]
						.SetFormat(setting.datatype, setting.defaultvalue)
						.ApplyValidation(setting.datatype)
						.Unlocked();
					ws.Cells[row, ++col]
						.SetFormat(setting.datatype, organizationSettingList.FirstOrDefault(x => x.settingdefinitionid?.Id == setting.Id)?.value)
						.ApplyValidation(setting.datatype)
						.Unlocked(setting.overridablelevel, SettingDefinitionOverridableLevel.Organization, SettingDefinitionOverridableLevel.AppAndOrganization);

					foreach (var app in appList)
					{
						var appSetting = appSettingList.FirstOrDefault(x => x.settingdefinitionid?.Id == setting.Id && x.parentappmoduleid?.Id == app.Id);
						ws.Cells[row, ++col]
							.SetFormat(setting.datatype, appSetting?.value)
							.ApplyValidation(setting.datatype)
							.Unlocked(setting.overridablelevel, SettingDefinitionOverridableLevel.App, SettingDefinitionOverridableLevel.AppAndOrganization);
					}

					ws.Cells[row, ++col].Value = setting.FormattedDataType;
					ws.Cells[row, ++col].Value = setting.ishidden ? string.Empty : "X";
					ws.Cells[row, ++col].Value = setting.FormattedOverridableLevel;
				}

				var tableRange = ws.Cells[3, 1, row, col];
				tableRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
				var table = ws.Tables.Add(tableRange, "Settings");
				table.TableStyle = OfficeOpenXml.Table.TableStyles.Medium2;

				var colWidth = 25;
				ws.Column(1).AutoFit();
				ws.Column(2).Width = colWidth * 3;
				for (var i = 3; i <= col; i++)
				{
					ws.Column(i).Width = colWidth;

					if (i >= col-2)
					{ 
						ws.Column(i).AutoFit();
						ws.Column(i).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
					}
				}

				ws.View.FreezePanes(4, 3);

				ws.Protection.IsProtected = true;
				ws.Protection.AllowSort = true;
				ws.Protection.AllowAutoFilter = true;
				ws.Protection.SetPassword("ciaociao");

				package.Workbook.Protection.LockStructure = true;
				await package.SaveAsAsync(new FileInfo(command.OutputFileName));

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

    public static class ExcelExtensions
    {
		public static ExcelRange SetFormat(this ExcelRange range, OptionSetValue? type, string? value)
		{
			if (type?.Value == (int)SettingDefinitionDataType.Boolean)
			{
				return range.SetFormatBoolean(value);
			}
			if (type?.Value == (int)SettingDefinitionDataType.Number)
			{
				return range.SetFormatNumber(value);
			}

			return range.SetValue(value);
		}

		public static ExcelRange SetFormatBoolean(this ExcelRange range, string? value)
		{
			return range.TextAlign(ExcelHorizontalAlignment.Center).SetValue(value?.ToString().ToUpperInvariant());
		}

		public static ExcelRange SetFormatNumber(this ExcelRange range, string? value)
		{
			range.TextAlign(ExcelHorizontalAlignment.Right);
			range.Style.Numberformat.Format = "#,##0.0";

			if (value != null && double.TryParse(value, out var number))
			{
				return range.SetValue(number);
			}

			return range;
		}


		public static ExcelRange ApplyValidation(this ExcelRange range, OptionSetValue? value)
		{
			if (value == null) return range;
			if (value.Value == (int)SettingDefinitionDataType.Boolean)
			{
				return range.ApplyValidationBoolean();
			}
			if (value.Value == (int)SettingDefinitionDataType.Number)
			{
				return range.ApplyValidationNumber();
			}

			return range;
		}

		public static ExcelRange ApplyValidationNumber(this ExcelRange range)
		{
			var list = range.DataValidation.AddDecimalDataValidation();
			list.AllowBlank = true;
			list.ShowErrorMessage = true;
			list.Error = "Invalid value, only numbers are supported!";
			list.ErrorStyle = OfficeOpenXml.DataValidation.ExcelDataValidationWarningStyle.stop;
			list.ErrorTitle = "Invalid value";
			return range;
		}

        public static ExcelRange ApplyValidationBoolean(this ExcelRange range)
		{
			var list = range.DataValidation.AddListDataValidation();
			list.Formula.Values.Add("TRUE");
			list.Formula.Values.Add("FALSE");
			list.AllowBlank = true;
			list.ShowErrorMessage = true;
			list.Error = "Invalid value, only 'true' and 'false' are supported!";
			list.ErrorStyle = OfficeOpenXml.DataValidation.ExcelDataValidationWarningStyle.stop;
			list.ErrorTitle = "Invalid value";
			return range;
		}

		public static ExcelRange Unlocked(this ExcelRange range, OptionSetValue? overridableLevel = null, params SettingDefinitionOverridableLevel[] validLevels)
		{
			var isLocked = overridableLevel != null && !validLevels.Contains((SettingDefinitionOverridableLevel)overridableLevel.Value);
			range.Style.Locked = isLocked;

			if (isLocked)
			{
				range.Style.Fill.PatternType = ExcelFillStyle.Solid;
				range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
			}
			else
			{
				range.Input();
			}

			return range;
		}
	}
}
