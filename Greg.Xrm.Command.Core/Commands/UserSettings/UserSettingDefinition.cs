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
/// Registry of all usersettings fields exposed by the 'pacx usersettings set' and 'pacx usersettings list' commands.
/// Field names match the official Dataverse usersettings entity schema:
/// https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/usersettings
/// </summary>
public static class UserSettingRegistry
{
public static readonly IReadOnlyDictionary<string, UserSettingDefinition> Fields =
new Dictionary<string, UserSettingDefinition>(StringComparer.OrdinalIgnoreCase)
{
// ── Language / locale ─────────────────────────────────────────────────────
["uilanguageid"] = new UserSettingDefinition()
{
FieldName = "uilanguageid",
DisplayName = "UI Language",
FieldType = UserSettingFieldType.Language,
HelpText = "LCID of the language shown in the user interface (e.g. 1033=English, 1040=Italian). The language must be provisioned in Dataverse."
},
["helplanguageid"] = new UserSettingDefinition()
{
FieldName = "helplanguageid",
DisplayName = "Help Language",
FieldType = UserSettingFieldType.Language,
HelpText = "LCID of the language used for help content. The language must be provisioned in Dataverse."
},
["localeid"] = new UserSettingDefinition()
{
FieldName = "localeid",
DisplayName = "Locale",
FieldType = UserSettingFieldType.Language,
HelpText = "LCID that controls number, date and currency formatting. The language must be provisioned in Dataverse."
},

// ── Date / time ───────────────────────────────────────────────────────────
["dateformatcode"] = new UserSettingDefinition()
{
FieldName = "dateformatcode",
DisplayName = "Date Format Code",
FieldType = UserSettingFieldType.Integer,
HelpText = "Information about how the date is displayed in Microsoft Dynamics 365."
},
["dateformatstring"] = new UserSettingDefinition()
{
FieldName = "dateformatstring",
DisplayName = "Date Format String",
FieldType = UserSettingFieldType.String,
MaxLength = 255,
HelpText = "String showing how the date is displayed throughout Microsoft 365 (e.g. 'M/d/yyyy', 'dd/MM/yyyy')."
},
["dateseparator"] = new UserSettingDefinition()
{
FieldName = "dateseparator",
DisplayName = "Date Separator",
FieldType = UserSettingFieldType.String,
MaxLength = 5,
HelpText = "Character used to separate the month, the day, and the year in dates (e.g. '/', '-', '.')."
},
["longdateformatcode"] = new UserSettingDefinition()
{
FieldName = "longdateformatcode",
DisplayName = "Long Date Format Code",
FieldType = UserSettingFieldType.Integer,
HelpText = "Information that specifies how Long Date is displayed throughout Microsoft 365."
},
["timeformatcode"] = new UserSettingDefinition()
{
FieldName = "timeformatcode",
DisplayName = "Time Format Code",
FieldType = UserSettingFieldType.Picklist,
AllowedValues = new Dictionary<int, string>
{
{ 0, "12-hour (AM/PM)" },
{ 1, "24-hour" }
},
HelpText = "Time display format: 0=12-hour (AM/PM), 1=24-hour."
},
["timeformatstring"] = new UserSettingDefinition()
{
FieldName = "timeformatstring",
DisplayName = "Time Format String",
FieldType = UserSettingFieldType.String,
MaxLength = 255,
HelpText = "Text for how time is displayed in Microsoft Dynamics 365 (e.g. 'hh:mm tt', 'HH:mm')."
},
["timeseparator"] = new UserSettingDefinition()
{
FieldName = "timeseparator",
DisplayName = "Time Separator",
FieldType = UserSettingFieldType.String,
MaxLength = 5,
HelpText = "Character used as the separator in time values (e.g. ':')."
},
["amdesignator"] = new UserSettingDefinition()
{
FieldName = "amdesignator",
DisplayName = "AM Designator",
FieldType = UserSettingFieldType.String,
MaxLength = 25,
HelpText = "AM designator to use in Microsoft Dynamics 365 (e.g. 'AM')."
},
["pmdesignator"] = new UserSettingDefinition()
{
FieldName = "pmdesignator",
DisplayName = "PM Designator",
FieldType = UserSettingFieldType.String,
MaxLength = 25,
HelpText = "PM designator to use in Microsoft Dynamics 365 (e.g. 'PM')."
},
["timezonecode"] = new UserSettingDefinition()
{
FieldName = "timezonecode",
DisplayName = "Time Zone",
FieldType = UserSettingFieldType.Integer,
MinValue = 0,
HelpText = "Windows time zone code. Common values: 85=UTC, 110=Romance (Paris/Rome/Madrid), 35=Eastern (US). See https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/default-time-zones for codes."
},
["timezonebias"] = new UserSettingDefinition()
{
FieldName = "timezonebias",
DisplayName = "Time Zone Bias",
FieldType = UserSettingFieldType.Integer,
HelpText = "Local time zone adjustment for the user. System calculated based on the time zone selected."
},
["timezonedaylightbias"] = new UserSettingDefinition()
{
FieldName = "timezonedaylightbias",
DisplayName = "Time Zone Daylight Bias",
FieldType = UserSettingFieldType.Integer,
HelpText = "Daylight saving time bias for the time zone."
},
["timezonedaylightday"] = new UserSettingDefinition()
{
FieldName = "timezonedaylightday",
DisplayName = "Time Zone Daylight Day",
FieldType = UserSettingFieldType.Integer,
HelpText = "Day of week when daylight saving time starts."
},
["timezonedaylightdayofweek"] = new UserSettingDefinition()
{
FieldName = "timezonedaylightdayofweek",
DisplayName = "Time Zone Daylight Day of Week",
FieldType = UserSettingFieldType.Integer,
HelpText = "Day of week for daylight saving time transition."
},
["timezonedaylighthour"] = new UserSettingDefinition()
{
FieldName = "timezonedaylighthour",
DisplayName = "Time Zone Daylight Hour",
FieldType = UserSettingFieldType.Integer,
HelpText = "Hour when daylight saving time starts."
},
["timezonedaylightminute"] = new UserSettingDefinition()
{
FieldName = "timezonedaylightminute",
DisplayName = "Time Zone Daylight Minute",
FieldType = UserSettingFieldType.Integer,
HelpText = "Minute when daylight saving time starts."
},
["timezonedaylightmonth"] = new UserSettingDefinition()
{
FieldName = "timezonedaylightmonth",
DisplayName = "Time Zone Daylight Month",
FieldType = UserSettingFieldType.Integer,
HelpText = "Month when daylight saving time starts."
},
["timezonedaylightsecond"] = new UserSettingDefinition()
{
FieldName = "timezonedaylightsecond",
DisplayName = "Time Zone Daylight Second",
FieldType = UserSettingFieldType.Integer,
HelpText = "Second when daylight saving time starts."
},
["timezonedaylightyear"] = new UserSettingDefinition()
{
FieldName = "timezonedaylightyear",
DisplayName = "Time Zone Daylight Year",
FieldType = UserSettingFieldType.Integer,
HelpText = "Year when daylight saving time starts."
},
["timezonestandardbias"] = new UserSettingDefinition()
{
FieldName = "timezonestandardbias",
DisplayName = "Time Zone Standard Bias",
FieldType = UserSettingFieldType.Integer,
HelpText = "Standard time bias for the time zone."
},
["timezonestandardday"] = new UserSettingDefinition()
{
FieldName = "timezonestandardday",
DisplayName = "Time Zone Standard Day",
FieldType = UserSettingFieldType.Integer,
HelpText = "Day of week when standard time starts."
},
["timezonestandarddayofweek"] = new UserSettingDefinition()
{
FieldName = "timezonestandarddayofweek",
DisplayName = "Time Zone Standard Day of Week",
FieldType = UserSettingFieldType.Integer,
HelpText = "Day of week for standard time transition."
},
["timezonestandardhour"] = new UserSettingDefinition()
{
FieldName = "timezonestandardhour",
DisplayName = "Time Zone Standard Hour",
FieldType = UserSettingFieldType.Integer,
HelpText = "Hour when standard time starts."
},
["timezonestandardminute"] = new UserSettingDefinition()
{
FieldName = "timezonestandardminute",
DisplayName = "Time Zone Standard Minute",
FieldType = UserSettingFieldType.Integer,
HelpText = "Minute when standard time starts."
},
["timezonestandardmonth"] = new UserSettingDefinition()
{
FieldName = "timezonestandardmonth",
DisplayName = "Time Zone Standard Month",
FieldType = UserSettingFieldType.Integer,
HelpText = "Month when standard time starts."
},
["timezonestandardsecond"] = new UserSettingDefinition()
{
FieldName = "timezonestandardsecond",
DisplayName = "Time Zone Standard Second",
FieldType = UserSettingFieldType.Integer,
HelpText = "Second when standard time starts."
},
["timezonestandardyear"] = new UserSettingDefinition()
{
FieldName = "timezonestandardyear",
DisplayName = "Time Zone Standard Year",
FieldType = UserSettingFieldType.Integer,
HelpText = "Year when standard time starts."
},

// ── Workday ───────────────────────────────────────────────────────────────
["workdaystarttime"] = new UserSettingDefinition()
{
FieldName = "workdaystarttime",
DisplayName = "Workday Start Time",
FieldType = UserSettingFieldType.Time,
HelpText = "Workday start time for the user in HH:mm format (e.g. 08:00)."
},
["workdaystoptime"] = new UserSettingDefinition()
{
FieldName = "workdaystoptime",
DisplayName = "Workday Stop Time",
FieldType = UserSettingFieldType.Time,
HelpText = "Workday stop time for the user in HH:mm format (e.g. 17:00)."
},

// ── Calendar ──────────────────────────────────────────────────────────────
["calendartype"] = new UserSettingDefinition()
{
FieldName = "calendartype",
DisplayName = "Calendar Type",
FieldType = UserSettingFieldType.Integer,
HelpText = "Calendar type for the system. Set to Gregorian US by default."
},
["defaultcalendarview"] = new UserSettingDefinition()
{
FieldName = "defaultcalendarview",
DisplayName = "Default Calendar View",
FieldType = UserSettingFieldType.Integer,
HelpText = "Default calendar view for the user."
},
["addressbooksyncinterval"] = new UserSettingDefinition()
{
FieldName = "addressbooksyncinterval",
DisplayName = "Address Book Sync Interval",
FieldType = UserSettingFieldType.Integer,
HelpText = "Normal polling frequency used for address book synchronization in Microsoft Office Outlook (in minutes)."
},

// ── Full-name convention ──────────────────────────────────────────────────
["fullnameconventioncode"] = new UserSettingDefinition()
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
["numbergroupformat"] = new UserSettingDefinition()
{
FieldName = "numbergroupformat",
DisplayName = "Number Group Format",
FieldType = UserSettingFieldType.String,
MaxLength = 25,
HelpText = "Information that specifies how numbers are grouped in Microsoft Dynamics 365 (e.g. '3' for thousands grouping)."
},
["negativeformatcode"] = new UserSettingDefinition()
{
FieldName = "negativeformatcode",
DisplayName = "Negative Format",
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
["decimalsymbol"] = new UserSettingDefinition()
{
FieldName = "decimalsymbol",
DisplayName = "Decimal Symbol",
FieldType = UserSettingFieldType.String,
MaxLength = 5,
HelpText = "Character used as the decimal separator (e.g. '.' or ',')."
},
["numberseparator"] = new UserSettingDefinition()
{
FieldName = "numberseparator",
DisplayName = "Number Separator (Thousands)",
FieldType = UserSettingFieldType.String,
MaxLength = 5,
HelpText = "Character used as the thousands separator (e.g. ',' or '.')."
},
["pricingdecimalprecision"] = new UserSettingDefinition()
{
FieldName = "pricingdecimalprecision",
DisplayName = "Pricing Decimal Precision",
FieldType = UserSettingFieldType.Integer,
MinValue = 0,
HelpText = "Number of decimal places used for prices."
},
["nexttrackingnumber"] = new UserSettingDefinition()
{
FieldName = "nexttrackingnumber",
DisplayName = "Next Tracking Number",
FieldType = UserSettingFieldType.Integer,
HelpText = "Next tracking number."
},
["trackingtokenid"] = new UserSettingDefinition()
{
FieldName = "trackingtokenid",
DisplayName = "Tracking Token ID",
FieldType = UserSettingFieldType.Integer,
HelpText = "Token that is used to render the tracking ID in email responses."
},
["autocaptureuserstatus"] = new UserSettingDefinition()
{
FieldName = "autocaptureuserstatus",
DisplayName = "Auto Capture User Status",
FieldType = UserSettingFieldType.Integer,
HelpText = "Set user status for ADC Suggestions."
},

// ── Currency formatting ───────────────────────────────────────────────────
["currencyformatcode"] = new UserSettingDefinition()
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
["negativecurrencyformatcode"] = new UserSettingDefinition()
{
FieldName = "negativecurrencyformatcode",
DisplayName = "Negative Currency Format",
FieldType = UserSettingFieldType.Integer,
MinValue = 0,
MaxValue = 15,
HelpText = "Information that specifies how negative currency numbers are displayed in Microsoft Dynamics 365 (0–15)."
},
["currencysymbol"] = new UserSettingDefinition()
{
FieldName = "currencysymbol",
DisplayName = "Currency Symbol",
FieldType = UserSettingFieldType.String,
MaxLength = 13,
HelpText = "Currency symbol (e.g. '$', '€', '£'). Maximum 13 characters."
},
["currencydecimalprecision"] = new UserSettingDefinition()
{
FieldName = "currencydecimalprecision",
DisplayName = "Currency Decimal Precision",
FieldType = UserSettingFieldType.Integer,
HelpText = "Number of decimal places that can be used for currency."
},

// ── Paging ────────────────────────────────────────────────────────────────
["paginglimit"] = new UserSettingDefinition()
{
FieldName = "paginglimit",
DisplayName = "Paging Limit",
FieldType = UserSettingFieldType.Integer,
MinValue = 0,
HelpText = "Number of records per page (common values: 50, 100, 250, 500)."
},

// ── Email / tracking ─────────────────────────────────────────────────────
["incomingemailfilteringmethod"] = new UserSettingDefinition()
{
FieldName = "incomingemailfilteringmethod",
DisplayName = "Incoming Email Filtering Method",
FieldType = UserSettingFieldType.Picklist,
AllowedValues = new Dictionary<int, string>
{
{ 0, "All email messages" },
{ 1, "Email messages in response to Dynamics 365 email" },
{ 2, "Email messages from Dynamics 365 Leads, Contacts and Accounts" },
{ 3, "Email messages from Dynamics 365 records that are email enabled" },
{ 4, "No email messages" }
},
HelpText = "Which incoming emails to track: 0=All, 1=InResponse, 2=FromLeadsContactsAccounts, 3=FromEmailEnabledRecords, 4=None."
},
["ignoreunsolicitedemail"] = new UserSettingDefinition()
{
FieldName = "ignoreunsolicitedemail",
DisplayName = "Ignore Unsolicited Email",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Whether to ignore unsolicited (non-tracked) incoming email. Accepts true/false, 1/0, yes/no."
},

// ── Script errors ─────────────────────────────────────────────────────────
["reportscripterrors"] = new UserSettingDefinition()
{
FieldName = "reportscripterrors",
DisplayName = "Report Script Errors",
FieldType = UserSettingFieldType.Picklist,
AllowedValues = new Dictionary<int, string>
{
{ 1, "Ask me for permission to send an error report to Microsoft" },
{ 2, "Automatically send an error report to Microsoft without asking me for permission" },
{ 3, "Never send an error report to Microsoft about Microsoft Dynamics 365" }
},
HelpText = "How script errors are handled: 1=AskMe, 2=AutoSend, 3=Never."
},

// ── Advanced Find ─────────────────────────────────────────────────────────
["advancedfindstartupmode"] = new UserSettingDefinition()
{
FieldName = "advancedfindstartupmode",
DisplayName = "Advanced Find Startup Mode",
FieldType = UserSettingFieldType.Integer,
HelpText = "Default mode, such as simple or detailed, for advanced find."
},

// ── Sync intervals ────────────────────────────────────────────────────────
["offlinesyncinterval"] = new UserSettingDefinition()
{
FieldName = "offlinesyncinterval",
DisplayName = "Offline Sync Interval",
FieldType = UserSettingFieldType.Integer,
HelpText = "Normal polling frequency used for background offline synchronization in Microsoft Office Outlook (in minutes)."
},
["outlooksyncinterval"] = new UserSettingDefinition()
{
FieldName = "outlooksyncinterval",
DisplayName = "Outlook Sync Interval",
FieldType = UserSettingFieldType.Integer,
HelpText = "Normal polling frequency used for record synchronization in Microsoft Office Outlook (in minutes)."
},

// ── Homepage / navigation ─────────────────────────────────────────────────
["homepagearea"] = new UserSettingDefinition()
{
FieldName = "homepagearea",
DisplayName = "Homepage Area",
FieldType = UserSettingFieldType.String,
MaxLength = 200,
HelpText = "Web site home page for the user."
},
["homepagelayout"] = new UserSettingDefinition()
{
FieldName = "homepagelayout",
DisplayName = "Homepage Layout",
FieldType = UserSettingFieldType.String,
MaxLength = 2000,
HelpText = "Configuration of the home page layout."
},
["homepagesubarea"] = new UserSettingDefinition()
{
FieldName = "homepagesubarea",
DisplayName = "Homepage Subarea",
FieldType = UserSettingFieldType.String,
MaxLength = 200,
HelpText = "Web site page for the user."
},
["defaultsearchexperience"] = new UserSettingDefinition()
{
FieldName = "defaultsearchexperience",
DisplayName = "Default Search Experience",
FieldType = UserSettingFieldType.Picklist,
AllowedValues = new Dictionary<int, string>
{
{ 0, "Relevance search" },
{ 1, "Categorized search" },
{ 2, "Use last search" },
{ 3, "Custom search" }
},
HelpText = "Default search experience for the user: 0=Relevance, 1=Categorized, 2=Last used, 3=Custom."
},
["defaultcountrycode"] = new UserSettingDefinition()
{
FieldName = "defaultcountrycode",
DisplayName = "Default Country Code",
FieldType = UserSettingFieldType.String,
MaxLength = 30,
HelpText = "Text area to enter default country code."
},

// ── Entity form mode ──────────────────────────────────────────────────────
["entityformmode"] = new UserSettingDefinition()
{
FieldName = "entityformmode",
DisplayName = "Entity Form Mode",
FieldType = UserSettingFieldType.Picklist,
AllowedValues = new Dictionary<int, string>
{
{ 0, "Organization default" },
{ 1, "Read-optimized" },
{ 2, "Edit" }
},
HelpText = "Indicates the form mode to be used: 0=Organization default, 1=Read-optimized, 2=Edit."
},
["visualizationpanelayout"] = new UserSettingDefinition()
{
FieldName = "visualizationpanelayout",
DisplayName = "Visualization Panel Layout",
FieldType = UserSettingFieldType.Picklist,
AllowedValues = new Dictionary<int, string>
{
{ 0, "Top-bottom" },
{ 1, "Side-by-side" }
},
HelpText = "The layout of the visualization panel: 0=Top-bottom, 1=Side-by-side."
},
["userprofile"] = new UserSettingDefinition()
{
FieldName = "userprofile",
DisplayName = "User Profile",
FieldType = UserSettingFieldType.String,
MaxLength = 1024,
HelpText = "Privileges for a user's profile."
},

// ── Release channel ───────────────────────────────────────────────────────
["releasechannel"] = new UserSettingDefinition()
{
FieldName = "releasechannel",
DisplayName = "Release Channel",
FieldType = UserSettingFieldType.Picklist,
AllowedValues = new Dictionary<int, string>
{
{ 0, "None" },
{ 1, "Semi-annual channel override" },
{ 2, "Monthly channel override" },
{ 3, "Inner channel override" }
},
HelpText = "Model app channel override: 0=None, 1=Semi-annual, 2=Monthly, 3=Inner."
},

// ── D365 auto-install ─────────────────────────────────────────────────────
["d365autoinstallattemptstatus"] = new UserSettingDefinition()
{
FieldName = "d365autoinstallattemptstatus",
DisplayName = "D365 Auto-install Attempt Status",
FieldType = UserSettingFieldType.Picklist,
AllowedValues = new Dictionary<int, string>
{
{ 0, "Not attempted" },
{ 1, "Auto installed" },
{ 2, "Already installed" },
{ 3, "Teams admin blocked" },
{ 4, "Unauthorized" },
{ 5, "No Solution" },
{ 6, "No Graph API" },
{ 7, "Resource Disabled" }
},
HelpText = "Status of auto install of Dynamics 365 to Teams attempt: 0=NotAttempted, 1=AutoInstalled, 2=AlreadyInstalled, 3=TeamsAdminBlocked, 4=Unauthorized, 5=NoSolution, 6=NoGraphAPI, 7=ResourceDisabled."
},

// ── Data validation ───────────────────────────────────────────────────────
["datavalidationmodeforexporttoexcel"] = new UserSettingDefinition()
{
FieldName = "datavalidationmodeforexporttoexcel",
DisplayName = "Data Validation Mode for Export to Excel",
FieldType = UserSettingFieldType.Picklist,
AllowedValues = new Dictionary<int, string>
{
{ 0, "Full" },
{ 1, "None" }
},
HelpText = "Level of data validation in Excel worksheets exported for import: 0=Full, 1=None."
},

// ── DataSearch teaching bubbles ───────────────────────────────────────────
["tablescopeddvsearchfeatureteachingbubbleviews"] = new UserSettingDefinition()
{
FieldName = "tablescopeddvsearchfeatureteachingbubbleviews",
DisplayName = "Table-scoped DV Search Feature Teaching Bubble Views",
FieldType = UserSettingFieldType.Integer,
MinValue = 0,
MaxValue = 100,
HelpText = "Number of times a user has interacted with the Table-scoped Dataverse Search feature teaching bubble (0-100)."
},
["tablescopeddvsearchquickfindteachingbubbleviews"] = new UserSettingDefinition()
{
FieldName = "tablescopeddvsearchquickfindteachingbubbleviews",
DisplayName = "Table-scoped DV Search Quick Find Teaching Bubble Views",
FieldType = UserSettingFieldType.Integer,
MinValue = 0,
MaxValue = 100,
HelpText = "Number of times a user has interacted with the Table-scoped Dataverse Search Quick Find teaching bubble (0-100)."
},

// ── Boolean settings ──────────────────────────────────────────────────────
["showweeknumber"] = new UserSettingDefinition()
{
FieldName = "showweeknumber",
DisplayName = "Show Week Number",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Whether to display ISO week numbers in calendars. Accepts true/false, 1/0, yes/no."
},
["synccontactcompany"] = new UserSettingDefinition()
{
FieldName = "synccontactcompany",
DisplayName = "Sync Contact Company",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Whether to sync the parent account name to the Outlook contact Company field. Accepts true/false, 1/0, yes/no."
},
["autocreatecontactonpromote"] = new UserSettingDefinition()
{
FieldName = "autocreatecontactonpromote",
DisplayName = "Auto-create Contact on Promote",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Auto-create contact on client promote. Accepts true/false, 1/0, yes/no."
},
["getstartedpanecontentenabled"] = new UserSettingDefinition()
{
FieldName = "getstartedpanecontentenabled",
DisplayName = "Get Started Pane Content Enabled",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Whether the Get Started pane in lists is enabled. Accepts true/false, 1/0, yes/no."
},
["isappsforcrmalertdismissed"] = new UserSettingDefinition()
{
FieldName = "isappsforcrmalertdismissed",
DisplayName = "Is Apps for CRM Alert Dismissed",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Show or dismiss alert for Apps for 365. Accepts true/false, 1/0, yes/no."
},
["isautodatacaptureenabled"] = new UserSettingDefinition()
{
FieldName = "isautodatacaptureenabled",
DisplayName = "Is Auto Data Capture Enabled",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Whether the Auto Capture feature is enabled. Accepts true/false, 1/0, yes/no."
},
["isdefaultcountrycodecheckenabled"] = new UserSettingDefinition()
{
FieldName = "isdefaultcountrycodecheckenabled",
DisplayName = "Is Default Country Code Check Enabled",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Enable or disable country code selection. Accepts true/false, 1/0, yes/no."
},
["isduplicatedetectionenabledwhengoingonline"] = new UserSettingDefinition()
{
FieldName = "isduplicatedetectionenabledwhengoingonline",
DisplayName = "Is Duplicate Detection Enabled When Going Online",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Whether duplicate detection is enabled when going online. Accepts true/false, 1/0, yes/no."
},
["isemailconversationviewenabled"] = new UserSettingDefinition()
{
FieldName = "isemailconversationviewenabled",
DisplayName = "Is Email Conversation View Enabled",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Enable or disable email conversation view on timeline wall selection. Accepts true/false, 1/0, yes/no."
},
["isguidedhelpenabled"] = new UserSettingDefinition()
{
FieldName = "isguidedhelpenabled",
DisplayName = "Is Guided Help Enabled",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Enable or disable guided help. Accepts true/false, 1/0, yes/no."
},
["isresourcebookingexchangesyncenabled"] = new UserSettingDefinition()
{
FieldName = "isresourcebookingexchangesyncenabled",
DisplayName = "Is Resource Booking Exchange Sync Enabled",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Whether synchronization of user resource booking with Exchange is enabled at user level. Accepts true/false, 1/0, yes/no."
},
["issendasallowed"] = new UserSettingDefinition()
{
FieldName = "issendasallowed",
DisplayName = "Is Send As Allowed",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Whether send as other user privilege is enabled. Accepts true/false, 1/0, yes/no."
},
["splitviewstate"] = new UserSettingDefinition()
{
FieldName = "splitviewstate",
DisplayName = "Split View State",
FieldType = UserSettingFieldType.Boolean,
HelpText = "For internal use only. Accepts true/false, 1/0, yes/no."
},
["trytogglestatus"] = new UserSettingDefinition()
{
FieldName = "trytogglestatus",
DisplayName = "Try Toggle Status",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Try toggle status for the user. Accepts true/false, 1/0, yes/no."
},
["useimagestrips"] = new UserSettingDefinition()
{
FieldName = "useimagestrips",
DisplayName = "Use Image Strips",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Whether image strips are used to render images. Accepts true/false, 1/0, yes/no."
},
["usecrmformforappointment"] = new UserSettingDefinition()
{
FieldName = "usecrmformforappointment",
DisplayName = "Use CRM Form for Appointment",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Open appointments using the Dynamics 365 form instead of the Outlook form. Accepts true/false, 1/0, yes/no."
},
["usecrmformforcontact"] = new UserSettingDefinition()
{
FieldName = "usecrmformforcontact",
DisplayName = "Use CRM Form for Contact",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Open contacts using the Dynamics 365 form instead of the Outlook form. Accepts true/false, 1/0, yes/no."
},
["usecrmformforemail"] = new UserSettingDefinition()
{
FieldName = "usecrmformforemail",
DisplayName = "Use CRM Form for Email",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Open emails using the Dynamics 365 form instead of the Outlook form. Accepts true/false, 1/0, yes/no."
},
["usecrmformfortask"] = new UserSettingDefinition()
{
FieldName = "usecrmformfortask",
DisplayName = "Use CRM Form for Task",
FieldType = UserSettingFieldType.Boolean,
HelpText = "Open tasks using the Dynamics 365 form instead of the Outlook form. Accepts true/false, 1/0, yes/no."
},
};
}

}
