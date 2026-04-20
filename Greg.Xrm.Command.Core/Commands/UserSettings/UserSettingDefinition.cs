using System.Globalization;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	public enum UserSettingFieldType
	{
		/// <summary>Windows LCID — validated against CultureInfo, then against Dataverse available languages.</summary>
		Language,
		/// <summary>Integer chosen from a fixed set of labelled options.</summary>
		Picklist,
		/// <summary>Unconstrained integer with optional min/max bounds.</summary>
		Integer,
		/// <summary>Boolean — accepts true/false, 1/0, yes/no.</summary>
		Boolean,
		/// <summary>Free-text string with optional max-length constraint.</summary>
		String,
		/// <summary>HH:mm time string.</summary>
		Time
	}

	public class UserSettingDefinition
	{
		public required string FieldName { get; init; }
		public required string DisplayName { get; init; }
		public required UserSettingFieldType FieldType { get; init; }
		public required string HelpText { get; init; }
		public IReadOnlyDictionary<int, string>? AllowedValues { get; init; }
		public int? MinValue { get; init; }
		public int? MaxValue { get; init; }
		public int? MaxLength { get; init; }

		/// <summary>
		/// Validates <paramref name="rawValue"/> according to the field definition.
		/// Returns (true, null, parsedValue) on success or (false, errorMessage, null) on failure.
		/// Note: Language fields are validated locally here; the Dataverse availability check is
		/// performed separately in the executor.
		/// </summary>
		public (bool IsValid, string? ErrorMessage, object? Value) Validate(string rawValue)
		{
			return FieldType switch
			{
				UserSettingFieldType.Language => ValidateLanguage(rawValue),
				UserSettingFieldType.Picklist => ValidatePicklist(rawValue),
				UserSettingFieldType.Integer => ValidateInteger(rawValue),
				UserSettingFieldType.Boolean => ValidateBoolean(rawValue),
				UserSettingFieldType.String => ValidateString(rawValue),
				UserSettingFieldType.Time => ValidateTime(rawValue),
				_ => (false, $"Unsupported field type '{FieldType}'.", null)
			};
		}

		private (bool, string?, object?) ValidateLanguage(string rawValue)
		{
			if (!int.TryParse(rawValue, out int lcid))
				return (false, $"The value '{rawValue}' for '{DisplayName}' ({FieldName}) must be an integer LCID (e.g. 1033 for English, 1040 for Italian).", null);

			try
			{
				_ = CultureInfo.GetCultureInfo(lcid);
				return (true, null, lcid);
			}
			catch (CultureNotFoundException)
			{
				return (false, BuildInvalidLcidError(lcid), null);
			}
			catch (ArgumentOutOfRangeException)
			{
				return (false, BuildInvalidLcidError(lcid), null);
			}
		}

		private string BuildInvalidLcidError(int lcid)
			=> $"The LCID {lcid} is not a recognised Windows culture identifier. " +
			   $"The field '{DisplayName}' ({FieldName}) requires a valid Windows LCID. " +
			   $"See https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid for the full list.";

		private (bool, string?, object?) ValidatePicklist(string rawValue)
		{
			if (!int.TryParse(rawValue, out int value))
				return (false, $"The value '{rawValue}' for '{DisplayName}' ({FieldName}) must be an integer.", null);

			if (AllowedValues is not null && !AllowedValues.ContainsKey(value))
			{
				var allowed = string.Join(", ", AllowedValues.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key} = {kv.Value}"));
				return (false, $"The value {value} is not valid for '{DisplayName}' ({FieldName}). Allowed values are: {allowed}.", null);
			}

			return (true, null, value);
		}

		private (bool, string?, object?) ValidateInteger(string rawValue)
		{
			if (!int.TryParse(rawValue, out int value))
				return (false, $"The value '{rawValue}' for '{DisplayName}' ({FieldName}) must be an integer.", null);

			if (MinValue.HasValue && value < MinValue.Value)
				return (false, $"The value {value} is below the minimum of {MinValue.Value} for '{DisplayName}' ({FieldName}).", null);

			if (MaxValue.HasValue && value > MaxValue.Value)
				return (false, $"The value {value} exceeds the maximum of {MaxValue.Value} for '{DisplayName}' ({FieldName}).", null);

			return (true, null, value);
		}

		private static (bool, string?, object?) ValidateBoolean(string rawValue)
		{
			if (bool.TryParse(rawValue, out bool boolValue)) return (true, null, boolValue);
			if (rawValue == "1") return (true, null, true);
			if (rawValue == "0") return (true, null, false);
			if (string.Equals(rawValue, "yes", StringComparison.OrdinalIgnoreCase)) return (true, null, true);
			if (string.Equals(rawValue, "no", StringComparison.OrdinalIgnoreCase)) return (true, null, false);
			return (false, $"The value '{rawValue}' is not a valid boolean. Use: true, false, 1, 0, yes, or no.", null);
		}

		private (bool, string?, object?) ValidateString(string rawValue)
		{
			if (MaxLength.HasValue && rawValue.Length > MaxLength.Value)
				return (false, $"The value for '{DisplayName}' ({FieldName}) must be at most {MaxLength} character(s) long, but '{rawValue}' is {rawValue.Length} characters.", null);
			return (true, null, rawValue);
		}

		private static (bool, string?, object?) ValidateTime(string rawValue)
		{
			string[] formats = ["hh\\:mm", "h\\:mm", "HH\\:mm", "H\\:mm"];
			if (TimeSpan.TryParseExact(rawValue, formats, CultureInfo.InvariantCulture, out _))
				return (true, null, rawValue);
			return (false, $"The value '{rawValue}' is not a valid time. Use HH:mm format (e.g. 09:00 for 9 AM, 17:30 for 5:30 PM).", null);
		}
	}

	/// <summary>
	/// Registry of all writable usersettings fields exposed by the 'pacx usersettings set' command.
	/// Field descriptions are based on:
	/// https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/usersettings
	/// </summary>
	public static class UserSettingRegistry
	{
		public static readonly IReadOnlyDictionary<string, UserSettingDefinition> Fields =
			new Dictionary<string, UserSettingDefinition>(StringComparer.OrdinalIgnoreCase)
			{
				// ── Language / locale ─────────────────────────────────────────────────────
				["uilanguageid"] = new()
				{
					FieldName = "uilanguageid",
					DisplayName = "UI Language",
					FieldType = UserSettingFieldType.Language,
					HelpText = "LCID of the language shown in the user interface (e.g. 1033=English, 1040=Italian). The language must be provisioned in Dataverse."
				},
				["helplanguageid"] = new()
				{
					FieldName = "helplanguageid",
					DisplayName = "Help Language",
					FieldType = UserSettingFieldType.Language,
					HelpText = "LCID of the language used for help content. The language must be provisioned in Dataverse."
				},
				["localeid"] = new()
				{
					FieldName = "localeid",
					DisplayName = "Locale",
					FieldType = UserSettingFieldType.Language,
					HelpText = "LCID that controls number, date and currency formatting. The language must be provisioned in Dataverse."
				},

				// ── Date / time ───────────────────────────────────────────────────────────
				["dateformatcode"] = new()
				{
					FieldName = "dateformatcode",
					DisplayName = "Date Format",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 0, "MM/DD/YYYY" },
						{ 1, "DD/MM/YYYY" },
						{ 2, "DD-MMM-YYYY" },
						{ 3, "DD/MM/YYYY (locale-aware)" },
						{ 4, "YYYY/MM/DD" },
						{ 5, "YYYY-MM-DD" }
					},
					HelpText = "Short date format: 0=MM/DD/YYYY, 1=DD/MM/YYYY, 2=DD-MMM-YYYY, 3=DD/MM/YYYY (locale), 4=YYYY/MM/DD, 5=YYYY-MM-DD."
				},
				["timeformatcode"] = new()
				{
					FieldName = "timeformatcode",
					DisplayName = "Time Format",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 0, "12-hour (AM/PM)" },
						{ 1, "24-hour" }
					},
					HelpText = "Time display format: 0=12-hour (AM/PM), 1=24-hour."
				},
				["timezonecode"] = new()
				{
					FieldName = "timezonecode",
					DisplayName = "Time Zone",
					FieldType = UserSettingFieldType.Integer,
					MinValue = 0,
					HelpText = "Windows time zone code. Common values: 85=UTC, 110=Romance (Paris/Rome/Madrid), 35=Eastern (US). See https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/default-time-zones for codes."
				},
				["workdaysstarttime"] = new()
				{
					FieldName = "workdaysstarttime",
					DisplayName = "Work Day Start Time",
					FieldType = UserSettingFieldType.Time,
					HelpText = "Start of the user's work day in HH:mm format (e.g. 08:00)."
				},
				["workdaystoptime"] = new()
				{
					FieldName = "workdaystoptime",
					DisplayName = "Work Day Stop Time",
					FieldType = UserSettingFieldType.Time,
					HelpText = "End of the user's work day in HH:mm format (e.g. 17:00)."
				},

				// ── Full-name convention ──────────────────────────────────────────────────
				["fullnameconventioncode"] = new()
				{
					FieldName = "fullnameconventioncode",
					DisplayName = "Full Name Convention",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 0, "Last Name, First Name" },
						{ 1, "First Name Last Name" },
						{ 2, "Last Name, First Name MI" },
						{ 3, "First Name MI Last Name" },
						{ 4, "Last Name, First Name MI (alt)" },
						{ 5, "FI. Last Name" },
						{ 6, "Last Name FI." }
					},
					HelpText = "How full names are displayed: 0=Last First, 1=First Last, 2=Last First MI, 3=First MI Last, 4=Last First MI2, 5=FI Last, 6=Last FI."
				},

				// ── Number formatting ─────────────────────────────────────────────────────
				["numberformatcode"] = new()
				{
					FieldName = "numberformatcode",
					DisplayName = "Number Format",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 0, "1,234 (comma thousands, period decimal)" },
						{ 1, "1.234 (period thousands, comma decimal)" },
						{ 2, "1 234 (space thousands, comma decimal)" },
						{ 3, "1'234 (apostrophe thousands, period decimal)" }
					},
					HelpText = "Number grouping style: 0=1,234  1=1.234  2=1 234  3=1'234."
				},
				["negativenumberformatcode"] = new()
				{
					FieldName = "negativenumberformatcode",
					DisplayName = "Negative Number Format",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 0, "(X) — parentheses" },
						{ 1, "-X — minus prefix" },
						{ 2, "- X — minus with space" },
						{ 3, "X- — minus suffix" },
						{ 4, "X - — minus suffix with space" }
					},
					HelpText = "Negative number display: 0=(X)  1=-X  2=- X  3=X-  4=X -."
				},
				["decimalsymbol"] = new()
				{
					FieldName = "decimalsymbol",
					DisplayName = "Decimal Symbol",
					FieldType = UserSettingFieldType.String,
					MaxLength = 5,
					HelpText = "Character used as the decimal separator (e.g. '.' or ',')."
				},
				["numberseparator"] = new()
				{
					FieldName = "numberseparator",
					DisplayName = "Number Separator (Thousands)",
					FieldType = UserSettingFieldType.String,
					MaxLength = 5,
					HelpText = "Character used as the thousands separator (e.g. ',' or '.')."
				},
				["pricingdecimalprecision"] = new()
				{
					FieldName = "pricingdecimalprecision",
					DisplayName = "Pricing Decimal Precision",
					FieldType = UserSettingFieldType.Integer,
					MinValue = 0,
					MaxValue = 4,
					HelpText = "Number of decimal places used for prices (0–4)."
				},

				// ── Currency formatting ───────────────────────────────────────────────────
				["currencyformatcode"] = new()
				{
					FieldName = "currencyformatcode",
					DisplayName = "Currency Format",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 0, "$X — symbol before amount" },
						{ 1, "X$ — symbol after amount" },
						{ 2, "$ X — symbol before with space" },
						{ 3, "X $ — symbol after with space" }
					},
					HelpText = "Currency symbol position: 0=$X  1=X$  2=$ X  3=X $."
				},
				["negativecurrencyformatcode"] = new()
				{
					FieldName = "negativecurrencyformatcode",
					DisplayName = "Negative Currency Format",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 0, "($X)" },
						{ 1, "-$X" },
						{ 2, "$-X" },
						{ 3, "$X-" }
					},
					HelpText = "Negative currency display: 0=($X)  1=-$X  2=$-X  3=$X-."
				},
				["currencysymbol"] = new()
				{
					FieldName = "currencysymbol",
					DisplayName = "Currency Symbol",
					FieldType = UserSettingFieldType.String,
					MaxLength = 13,
					HelpText = "Currency symbol (e.g. '$', '€', '£'). Maximum 13 characters."
				},

				// ── Paging ────────────────────────────────────────────────────────────────
				["paginglimit"] = new()
				{
					FieldName = "paginglimit",
					DisplayName = "Paging Limit",
					FieldType = UserSettingFieldType.Integer,
					MinValue = 1,
					HelpText = "Number of records per page (common values: 50, 100, 250, 500)."
				},

				// ── Email / tracking ─────────────────────────────────────────────────────
				["incomingemailfilteringmethod"] = new()
				{
					FieldName = "incomingemailfilteringmethod",
					DisplayName = "Incoming Email Filtering Method",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 0, "All email messages" },
						{ 1, "Email messages in response to Dynamics 365 email" },
						{ 2, "Email messages from Dynamics 365 leads, contacts, and accounts" },
						{ 3, "No email messages" }
					},
					HelpText = "Which incoming emails to track: 0=All, 1=InResponse, 2=FromLeadsContactsAccounts, 3=None."
				},
				["ignoreunsolicitedemail"] = new()
				{
					FieldName = "ignoreunsolicitedemail",
					DisplayName = "Ignore Unsolicited Email",
					FieldType = UserSettingFieldType.Boolean,
					HelpText = "Whether to ignore unsolicited (non-tracked) incoming email. Accepts true/false, 1/0, yes/no."
				},

				// ── Script errors ─────────────────────────────────────────────────────────
				["reportscripterrors"] = new()
				{
					FieldName = "reportscripterrors",
					DisplayName = "Report Script Errors",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 1, "No error reported" },
						{ 2, "Ask me for permission to send" },
						{ 3, "Send an error report to Microsoft automatically" }
					},
					HelpText = "How script errors are handled: 1=NoError, 2=AskMe, 3=SendReport."
				},

				// ── Advanced Find ─────────────────────────────────────────────────────────
				["advancedfindstartupmode"] = new()
				{
					FieldName = "advancedfindstartupmode",
					DisplayName = "Advanced Find Startup Mode",
					FieldType = UserSettingFieldType.Picklist,
					AllowedValues = new Dictionary<int, string>
					{
						{ 1, "Simple" },
						{ 2, "Advanced" }
					},
					HelpText = "Default view when opening Advanced Find: 1=Simple, 2=Advanced."
				},

				// ── Sync intervals ────────────────────────────────────────────────────────
				["offlinesyncinterval"] = new()
				{
					FieldName = "offlinesyncinterval",
					DisplayName = "Offline Sync Interval",
					FieldType = UserSettingFieldType.Integer,
					MinValue = 0,
					HelpText = "Offline synchronization interval in minutes (0 = no sync)."
				},
				["outlooksyncinterval"] = new()
				{
					FieldName = "outlooksyncinterval",
					DisplayName = "Outlook Sync Interval",
					FieldType = UserSettingFieldType.Integer,
					MinValue = 0,
					HelpText = "Outlook synchronization interval in minutes (0 = no sync)."
				},

				// ── Boolean settings ──────────────────────────────────────────────────────
				["showweeknumber"] = new()
				{
					FieldName = "showweeknumber",
					DisplayName = "Show Week Number",
					FieldType = UserSettingFieldType.Boolean,
					HelpText = "Whether to display ISO week numbers in calendars. Accepts true/false, 1/0, yes/no."
				},
				["synccontactcompany"] = new()
				{
					FieldName = "synccontactcompany",
					DisplayName = "Sync Contact Company",
					FieldType = UserSettingFieldType.Boolean,
					HelpText = "Whether to sync the parent account name to the Outlook contact Company field. Accepts true/false, 1/0, yes/no."
				},
				["usecrmformforappointment"] = new()
				{
					FieldName = "usecrmformforappointment",
					DisplayName = "Use Dynamics 365 Form for Appointment",
					FieldType = UserSettingFieldType.Boolean,
					HelpText = "Open appointments using the Dynamics 365 form instead of the Outlook form. Accepts true/false, 1/0, yes/no."
				},
				["usecrmformforcontact"] = new()
				{
					FieldName = "usecrmformforcontact",
					DisplayName = "Use Dynamics 365 Form for Contact",
					FieldType = UserSettingFieldType.Boolean,
					HelpText = "Open contacts using the Dynamics 365 form instead of the Outlook form. Accepts true/false, 1/0, yes/no."
				},
				["usecrmformforemail"] = new()
				{
					FieldName = "usecrmformforemail",
					DisplayName = "Use Dynamics 365 Form for Email",
					FieldType = UserSettingFieldType.Boolean,
					HelpText = "Open emails using the Dynamics 365 form instead of the Outlook form. Accepts true/false, 1/0, yes/no."
				},
				["usecrmformfortask"] = new()
				{
					FieldName = "usecrmformfortask",
					DisplayName = "Use Dynamics 365 Form for Task",
					FieldType = UserSettingFieldType.Boolean,
					HelpText = "Open tasks using the Dynamics 365 form instead of the Outlook form. Accepts true/false, 1/0, yes/no."
				},
				["createcontactonresolve"] = new()
				{
					FieldName = "createcontactonresolve",
					DisplayName = "Create Contact on Email Resolve",
					FieldType = UserSettingFieldType.Boolean,
					HelpText = "Automatically create a contact when resolving an email from an unknown sender. Accepts true/false, 1/0, yes/no."
				},
				["autocreaterecontactsonresolve"] = new()
				{
					FieldName = "autocreaterecontactsonresolve",
					DisplayName = "Auto-create Related Contacts on Resolve",
					FieldType = UserSettingFieldType.Boolean,
					HelpText = "Automatically create related contacts when resolving an email from an unknown sender. Accepts true/false, 1/0, yes/no."
				},
			};
	}
}
