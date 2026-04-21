using Greg.Xrm.Command.Parsing;
using Greg.Xrm.Command.Services;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	/// <summary>
	/// Sets one or more properties on the <c>usersettings</c> record of the specified (or
	/// currently logged-in) user. Each field exposed by <see cref="UserSettingRegistry"/>
	/// is mapped to a dedicated strongly-typed CLI option — enum for picklists, <c>int?</c>
	/// for numeric / LCID fields, <c>bool?</c> for flags, <c>string?</c> for free-text and
	/// HH:mm time strings — so the parser rejects malformed input up-front and validation
	/// beyond type-checking is expressed via DataAnnotations on this class.
	/// Example: <c>pacx usersettings set --uilanguageid 1033 --helplanguageid 1033 --paginglimit 250</c>.
	/// </summary>
	[Command("usersettings", "set", HelpText = "Sets one or more user setting properties for the specified or currently logged-in user.")]
	public class SetCommand : ICanProvideUsageExample, IValidatableObject
	{
		// ?? Nested enums ?????????????????????????????????????????????????????????????
		// Same pattern as Solution.ListCommand. Underlying numeric values match the
		// Dataverse option codes, so users can pass the name or the number
		// (e.g. --timeformatcode TwentyFourHour  or  --timeformatcode 1).

		public enum TimeFormat { TwelveHour = 0, TwentyFourHour = 1 }

		public enum FullNameConvention
		{
			LastFirst = 0,
			FirstLast = 1,
			LastFirstMI = 2,
			FirstMILast = 3,
			LastFirstMIAlt = 4,
			FILast = 5,
			LastFI = 6
		}

		public enum NegativeNumberFormat
		{
			Parentheses = 0,
			MinusPrefix = 1,
			MinusPrefixWithSpace = 2,
			MinusSuffix = 3,
			MinusSuffixWithSpace = 4
		}

		public enum CurrencySymbolPosition
		{
			SymbolBeforeAmount = 0,
			SymbolAfterAmount = 1,
			SymbolBeforeAmountWithSpace = 2,
			SymbolAfterAmountWithSpace = 3
		}

		public enum IncomingEmailFilter
		{
			All = 0,
			InResponseToDynamicsEmail = 1,
			FromLeadsContactsAccounts = 2,
			FromEmailEnabledRecords = 3,
			None = 4
		}

		public enum ScriptErrorReportingMode { AskMe = 1, AutoSend = 2, Never = 3 }

		public enum SearchExperience { Relevance = 0, Categorized = 1, UseLastSearch = 2, Custom = 3 }

		public enum FormMode { OrganizationDefault = 0, ReadOptimized = 1, Edit = 2 }

		public enum PaneLayout { TopBottom = 0, SideBySide = 1 }

		public enum ReleaseChannelOverride { None = 0, SemiAnnual = 1, Monthly = 2, Inner = 3 }

		public enum D365AutoInstallStatus
		{
			NotAttempted = 0,
			AutoInstalled = 1,
			AlreadyInstalled = 2,
			TeamsAdminBlocked = 3,
			Unauthorized = 4,
			NoSolution = 5,
			NoGraphAPI = 6,
			ResourceDisabled = 7
		}

		public enum ExcelExportValidationMode { Full = 0, None = 1 }


		// ?? Options ??????????????????????????????????????????????????????????????????

		[Option("user", "u", Order = 1, HelpText = "Domain name of the user whose settings to update (e.g. DOMAIN\\john.doe). If omitted, the current user's settings are updated.")]
		public string? UserDomainName { get; set; }

		// Language / locale — CultureInfo-level validity is checked in IValidatableObject.Validate;
		// Dataverse provisioning check runs in the executor.
		[Option("uilanguageid", Order = 10, HelpText = "LCID of the UI language (e.g. 1033=English, 1040=Italian). The language must be provisioned in Dataverse.")]
		public int? UILanguageId { get; set; }

		[Option("helplanguageid", Order = 11, HelpText = "LCID of the Help language. The language must be provisioned in Dataverse.")]
		public int? HelpLanguageId { get; set; }

		[Option("localeid", Order = 12, HelpText = "LCID that controls number, date and currency formatting. The language must be provisioned in Dataverse.")]
		public int? LocaleId { get; set; }

		// Date / time
		[Option("dateformatcode", Order = 20, HelpText = "Information about how the date is displayed in Microsoft Dynamics 365.")]
		public int? DateFormatCode { get; set; }

		[StringLength(255)]
		[Option("dateformatstring", Order = 21, HelpText = "String showing how the date is displayed (e.g. 'M/d/yyyy', 'dd/MM/yyyy').")]
		public string? DateFormatString { get; set; }

		[StringLength(5)]
		[Option("dateseparator", Order = 22, HelpText = "Character used to separate the month, day and year in dates (e.g. '/', '-', '.').")]
		public string? DateSeparator { get; set; }

		[Option("longdateformatcode", Order = 23, HelpText = "Information that specifies how Long Date is displayed.")]
		public int? LongDateFormatCode { get; set; }

		[Option("timeformatcode", Order = 24, HelpText = "Time display format.")]
		public TimeFormat? TimeFormatCodeValue { get; set; }

		[StringLength(255)]
		[Option("timeformatstring", Order = 25, HelpText = "Text for how time is displayed (e.g. 'hh:mm tt', 'HH:mm').")]
		public string? TimeFormatString { get; set; }

		[StringLength(5)]
		[Option("timeseparator", Order = 26, HelpText = "Character used as the separator in time values (e.g. ':').")]
		public string? TimeSeparator { get; set; }

		[StringLength(25)]
		[Option("amdesignator", Order = 27, HelpText = "AM designator (e.g. 'AM').")]
		public string? AmDesignator { get; set; }

		[StringLength(25)]
		[Option("pmdesignator", Order = 28, HelpText = "PM designator (e.g. 'PM').")]
		public string? PmDesignator { get; set; }

		[Range(0, int.MaxValue)]
		[Option("timezonecode", Order = 29, HelpText = "Windows time zone code (e.g. 85=UTC, 110=Romance, 35=Eastern US).")]
		public int? TimeZoneCode { get; set; }

		[Option("timezonebias", Order = 30, HelpText = "Local time zone adjustment for the user.")]
		public int? TimeZoneBias { get; set; }

		[Option("timezonedaylightbias", Order = 31, HelpText = "Daylight saving time bias for the time zone.")]
		public int? TimeZoneDaylightBias { get; set; }

		[Option("timezonedaylightday", Order = 32, HelpText = "Day of week when daylight saving time starts.")]
		public int? TimeZoneDaylightDay { get; set; }

		[Option("timezonedaylightdayofweek", Order = 33, HelpText = "Day of week for daylight saving time transition.")]
		public int? TimeZoneDaylightDayOfWeek { get; set; }

		[Option("timezonedaylighthour", Order = 34, HelpText = "Hour when daylight saving time starts.")]
		public int? TimeZoneDaylightHour { get; set; }

		[Option("timezonedaylightminute", Order = 35, HelpText = "Minute when daylight saving time starts.")]
		public int? TimeZoneDaylightMinute { get; set; }

		[Option("timezonedaylightmonth", Order = 36, HelpText = "Month when daylight saving time starts.")]
		public int? TimeZoneDaylightMonth { get; set; }

		[Option("timezonedaylightsecond", Order = 37, HelpText = "Second when daylight saving time starts.")]
		public int? TimeZoneDaylightSecond { get; set; }

		[Option("timezonedaylightyear", Order = 38, HelpText = "Year when daylight saving time starts.")]
		public int? TimeZoneDaylightYear { get; set; }

		[Option("timezonestandardbias", Order = 39, HelpText = "Standard time bias for the time zone.")]
		public int? TimeZoneStandardBias { get; set; }

		[Option("timezonestandardday", Order = 40, HelpText = "Day of week when standard time starts.")]
		public int? TimeZoneStandardDay { get; set; }

		[Option("timezonestandarddayofweek", Order = 41, HelpText = "Day of week for standard time transition.")]
		public int? TimeZoneStandardDayOfWeek { get; set; }

		[Option("timezonestandardhour", Order = 42, HelpText = "Hour when standard time starts.")]
		public int? TimeZoneStandardHour { get; set; }

		[Option("timezonestandardminute", Order = 43, HelpText = "Minute when standard time starts.")]
		public int? TimeZoneStandardMinute { get; set; }

		[Option("timezonestandardmonth", Order = 44, HelpText = "Month when standard time starts.")]
		public int? TimeZoneStandardMonth { get; set; }

		[Option("timezonestandardsecond", Order = 45, HelpText = "Second when standard time starts.")]
		public int? TimeZoneStandardSecond { get; set; }

		[Option("timezonestandardyear", Order = 46, HelpText = "Year when standard time starts.")]
		public int? TimeZoneStandardYear { get; set; }

		// Workday — HH:mm format validated in IValidatableObject.Validate.
		[Option("workdaystarttime", Order = 50, HelpText = "Workday start time in HH:mm format (e.g. 08:00).")]
		public string? WorkdayStartTime { get; set; }

		[Option("workdaystoptime", Order = 51, HelpText = "Workday stop time in HH:mm format (e.g. 17:00).")]
		public string? WorkdayStopTime { get; set; }

		// Calendar
		[Option("calendartype", Order = 60, HelpText = "Calendar type for the system (default: Gregorian US).")]
		public int? CalendarType { get; set; }

		[Option("defaultcalendarview", Order = 61, HelpText = "Default calendar view for the user.")]
		public int? DefaultCalendarView { get; set; }

		[Option("addressbooksyncinterval", Order = 62, HelpText = "Address book synchronization frequency in Outlook (minutes).")]
		public int? AddressBookSyncInterval { get; set; }

		// Full-name convention
		[Option("fullnameconventioncode", Order = 70, HelpText = "How full names are displayed.")]
		public FullNameConvention? FullNameConventionCodeValue { get; set; }

		// Number formatting
		[StringLength(25)]
		[Option("numbergroupformat", Order = 80, HelpText = "How numbers are grouped (e.g. '3' for thousands grouping).")]
		public string? NumberGroupFormat { get; set; }

		[Option("negativeformatcode", Order = 81, HelpText = "Negative number display.")]
		public NegativeNumberFormat? NegativeFormatCodeValue { get; set; }

		[StringLength(5)]
		[Option("decimalsymbol", Order = 82, HelpText = "Character used as the decimal separator (e.g. '.' or ',').")]
		public string? DecimalSymbol { get; set; }

		[StringLength(5)]
		[Option("numberseparator", Order = 83, HelpText = "Character used as the thousands separator (e.g. ',' or '.').")]
		public string? NumberSeparator { get; set; }

		[Range(0, int.MaxValue)]
		[Option("pricingdecimalprecision", Order = 84, HelpText = "Number of decimal places used for prices.")]
		public int? PricingDecimalPrecision { get; set; }

		[Option("nexttrackingnumber", Order = 85, HelpText = "Next tracking number.")]
		public int? NextTrackingNumber { get; set; }

		[Option("trackingtokenid", Order = 86, HelpText = "Token used to render the tracking ID in email responses.")]
		public int? TrackingTokenId { get; set; }

		[Option("autocaptureuserstatus", Order = 87, HelpText = "User status for ADC Suggestions.")]
		public int? AutoCaptureUserStatus { get; set; }

		// Currency formatting
		[Option("currencyformatcode", Order = 90, HelpText = "Currency symbol position.")]
		public CurrencySymbolPosition? CurrencyFormatCodeValue { get; set; }

		[Range(0, 15)]
		[Option("negativecurrencyformatcode", Order = 91, HelpText = "How negative currency values are displayed (0–15).")]
		public int? NegativeCurrencyFormatCode { get; set; }

		[StringLength(13)]
		[Option("currencysymbol", Order = 92, HelpText = "Currency symbol (e.g. '$', '€', '£'). Max 13 characters.")]
		public string? CurrencySymbol { get; set; }

		[Option("currencydecimalprecision", Order = 93, HelpText = "Number of decimal places used for currency.")]
		public int? CurrencyDecimalPrecision { get; set; }

		// Paging
		[Range(0, int.MaxValue)]
		[Option("paginglimit", Order = 100, HelpText = "Number of records per page (common values: 50, 100, 250, 500).")]
		public int? PagingLimit { get; set; }

		// Email / tracking
		[Option("incomingemailfilteringmethod", Order = 110, HelpText = "Which incoming emails to track.")]
		public IncomingEmailFilter? IncomingEmailFilteringMethodValue { get; set; }

		[Option("ignoreunsolicitedemail", Order = 111, HelpText = "Ignore unsolicited (non-tracked) incoming email.")]
		public bool? IgnoreUnsolicitedEmail { get; set; }

		// Script errors
		[Option("reportscripterrors", Order = 120, HelpText = "How script errors are handled.")]
		public ScriptErrorReportingMode? ReportScriptErrorsValue { get; set; }

		// Advanced Find
		[Option("advancedfindstartupmode", Order = 130, HelpText = "Default mode (simple or detailed) for advanced find.")]
		public int? AdvancedFindStartupMode { get; set; }

		// Sync intervals
		[Option("offlinesyncinterval", Order = 140, HelpText = "Background offline synchronization frequency in Outlook (minutes).")]
		public int? OfflineSyncInterval { get; set; }

		[Option("outlooksyncinterval", Order = 141, HelpText = "Record synchronization frequency in Outlook (minutes).")]
		public int? OutlookSyncInterval { get; set; }

		// Homepage / navigation
		[StringLength(200)]
		[Option("homepagearea", Order = 150, HelpText = "Web site home page for the user.")]
		public string? HomepageArea { get; set; }

		[StringLength(2000)]
		[Option("homepagelayout", Order = 151, HelpText = "Configuration of the home page layout.")]
		public string? HomepageLayout { get; set; }

		[StringLength(200)]
		[Option("homepagesubarea", Order = 152, HelpText = "Web site page for the user.")]
		public string? HomepageSubarea { get; set; }

		[Option("defaultsearchexperience", Order = 153, HelpText = "Default search experience.")]
		public SearchExperience? DefaultSearchExperienceValue { get; set; }

		[StringLength(30)]
		[Option("defaultcountrycode", Order = 154, HelpText = "Default country code.")]
		public string? DefaultCountryCode { get; set; }

		// Entity form mode
		[Option("entityformmode", Order = 160, HelpText = "Form mode.")]
		public FormMode? EntityFormModeValue { get; set; }

		[Option("visualizationpanelayout", Order = 161, HelpText = "Visualization panel layout.")]
		public PaneLayout? VisualizationPaneLayoutValue { get; set; }

		[StringLength(1024)]
		[Option("userprofile", Order = 162, HelpText = "Privileges for a user's profile.")]
		public string? UserProfile { get; set; }

		// Release channel
		[Option("releasechannel", Order = 170, HelpText = "Model app channel override.")]
		public ReleaseChannelOverride? ReleaseChannelValue { get; set; }

		// D365 auto-install
		[Option("d365autoinstallattemptstatus", Order = 180, HelpText = "Status of auto install of Dynamics 365 to Teams attempt.")]
		public D365AutoInstallStatus? D365AutoInstallAttemptStatusValue { get; set; }

		// Data validation
		[Option("datavalidationmodeforexporttoexcel", Order = 190, HelpText = "Data validation in exported Excel worksheets.")]
		public ExcelExportValidationMode? DataValidationModeForExportToExcelValue { get; set; }

		// DataSearch teaching bubbles
		[Range(0, 100)]
		[Option("tablescopeddvsearchfeatureteachingbubbleviews", Order = 200, HelpText = "Interactions with the Table-scoped Dataverse Search teaching bubble (0-100).")]
		public int? TableScopedDvSearchFeatureTeachingBubbleViews { get; set; }

		[Range(0, 100)]
		[Option("tablescopeddvsearchquickfindteachingbubbleviews", Order = 201, HelpText = "Interactions with the Table-scoped Dataverse Search Quick Find teaching bubble (0-100).")]
		public int? TableScopedDvSearchQuickFindTeachingBubbleViews { get; set; }

		// Boolean settings
		[Option("showweeknumber", Order = 210, HelpText = "Display ISO week numbers in calendars.")]
		public bool? ShowWeekNumber { get; set; }

		[Option("synccontactcompany", Order = 211, HelpText = "Sync the parent account name to the Outlook contact Company field.")]
		public bool? SyncContactCompany { get; set; }

		[Option("autocreatecontactonpromote", Order = 212, HelpText = "Auto-create contact on client promote.")]
		public bool? AutoCreateContactOnPromote { get; set; }

		[Option("getstartedpanecontentenabled", Order = 213, HelpText = "Enable the Get Started pane in lists.")]
		public bool? GetStartedPaneContentEnabled { get; set; }

		[Option("isappsforcrmalertdismissed", Order = 214, HelpText = "Show or dismiss alert for Apps for 365.")]
		public bool? IsAppsForCrmAlertDismissed { get; set; }

		[Option("isautodatacaptureenabled", Order = 215, HelpText = "Enable the Auto Capture feature.")]
		public bool? IsAutoDataCaptureEnabled { get; set; }

		[Option("isdefaultcountrycodecheckenabled", Order = 216, HelpText = "Enable country code selection.")]
		public bool? IsDefaultCountryCodeCheckEnabled { get; set; }

		[Option("isduplicatedetectionenabledwhengoingonline", Order = 217, HelpText = "Enable duplicate detection when going online.")]
		public bool? IsDuplicateDetectionEnabledWhenGoingOnline { get; set; }

		[Option("isemailconversationviewenabled", Order = 218, HelpText = "Enable email conversation view on timeline wall selection.")]
		public bool? IsEmailConversationViewEnabled { get; set; }

		[Option("isguidedhelpenabled", Order = 219, HelpText = "Enable guided help.")]
		public bool? IsGuidedHelpEnabled { get; set; }

		[Option("isresourcebookingexchangesyncenabled", Order = 220, HelpText = "Enable sync of user resource booking with Exchange.")]
		public bool? IsResourceBookingExchangeSyncEnabled { get; set; }

		[Option("issendasallowed", Order = 221, HelpText = "Enable send-as-other-user privilege.")]
		public bool? IsSendAsAllowed { get; set; }

		[Option("splitviewstate", Order = 222, HelpText = "For internal use only.")]
		public bool? SplitViewState { get; set; }

		[Option("trytogglestatus", Order = 223, HelpText = "Try toggle status for the user.")]
		public bool? TryToggleStatus { get; set; }

		[Option("useimagestrips", Order = 224, HelpText = "Whether image strips are used to render images.")]
		public bool? UseImageStrips { get; set; }

		[Option("usecrmformforappointment", Order = 225, HelpText = "Open appointments using the Dynamics 365 form instead of Outlook.")]
		public bool? UseCrmFormForAppointment { get; set; }

		[Option("usecrmformforcontact", Order = 226, HelpText = "Open contacts using the Dynamics 365 form instead of Outlook.")]
		public bool? UseCrmFormForContact { get; set; }

		[Option("usecrmformforemail", Order = 227, HelpText = "Open emails using the Dynamics 365 form instead of Outlook.")]
		public bool? UseCrmFormForEmail { get; set; }

		[Option("usecrmformfortask", Order = 228, HelpText = "Open tasks using the Dynamics 365 form instead of Outlook.")]
		public bool? UseCrmFormForTask { get; set; }


		// ?? Helpers ??????????????????????????????????????????????????????????????????

		private static readonly string[] TimeFormats = ["hh\\:mm", "h\\:mm", "HH\\:mm", "H\\:mm"];

		/// <summary>
		/// Returns every provided user-setting keyed by the Dataverse field name
		/// (i.e. the <see cref="OptionAttribute.LongName"/>). Enum values are normalised to
		/// their underlying <c>int</c> so the caller can write the payload straight on the
		/// Dataverse entity.
		/// </summary>
		public IReadOnlyDictionary<string, object> GetProvidedSettings()
		{
			var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			foreach (var property in typeof(SetCommand).GetProperties())
			{
				var optionAttr = property.GetCustomAttribute<OptionAttribute>();
				if (optionAttr is null) continue;
				if (!UserSettingRegistry.Contains(optionAttr.LongName)) continue;

				var value = property.GetValue(this);
				if (value is null) continue;

				// Enum ? underlying int (Dataverse picklist value).
				if (value is Enum e)
					value = ((IConvertible)e).ToInt32(CultureInfo.InvariantCulture);

				// Trim whitespace-only strings and skip empty ones.
				if (value is string s)
				{
					if (string.IsNullOrWhiteSpace(s)) continue;
					value = s;
				}

				result[optionAttr.LongName] = value;
			}
			return result;
		}

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			// At least one setting must be supplied.
			if (GetProvidedSettings().Count == 0)
			{
				yield return new ValidationResult(
					"At least one user setting option must be provided (e.g. --uilanguageid 1033). " +
					"Run 'pacx help usersettings set' to see the full list of supported options.");
			}

			// Language fields — must be a recognised Windows LCID. Dataverse provisioning
			// is checked later by the executor.
			foreach (var (member, lcid) in EnumerateLanguageFields())
			{
				if (!IsKnownWindowsLcid(lcid))
					yield return new ValidationResult(
						$"LCID {lcid} is not a recognised Windows culture identifier. " +
						"See https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid for the full list.",
						[member]);
			}

			// HH:mm time fields.
			foreach (var (member, raw) in EnumerateTimeFields())
			{
				if (!TimeSpan.TryParseExact(raw, TimeFormats, CultureInfo.InvariantCulture, out _))
					yield return new ValidationResult(
						$"'{raw}' is not a valid time. Use HH:mm format (e.g. 09:00, 17:30).",
						[member]);
			}
		}

		private IEnumerable<(string Member, int Lcid)> EnumerateLanguageFields()
		{
			if (UILanguageId.HasValue) yield return (nameof(UILanguageId), UILanguageId.Value);
			if (HelpLanguageId.HasValue) yield return (nameof(HelpLanguageId), HelpLanguageId.Value);
			if (LocaleId.HasValue) yield return (nameof(LocaleId), LocaleId.Value);
		}

		private IEnumerable<(string Member, string Raw)> EnumerateTimeFields()
		{
			if (!string.IsNullOrWhiteSpace(WorkdayStartTime)) yield return (nameof(WorkdayStartTime), WorkdayStartTime);
			if (!string.IsNullOrWhiteSpace(WorkdayStopTime)) yield return (nameof(WorkdayStopTime), WorkdayStopTime);
		}

		private static bool IsKnownWindowsLcid(int lcid)
		{
			try
			{
				_ = CultureInfo.GetCultureInfo(lcid);
				return true;
			}
			catch (CultureNotFoundException) { return false; }
			catch (ArgumentOutOfRangeException) { return false; }
		}

		public void WriteUsageExamples(MarkdownWriter writer)
		{
			writer.WriteParagraph("Set the UI language for the current user to Italian (LCID 1040):");
			writer.WriteCodeBlock("pacx usersettings set --uilanguageid 1040", "Powershell");

			writer.WriteParagraph("Align UI, Help and Locale to English (LCID 1033) in a single call:");
			writer.WriteCodeBlock("pacx usersettings set --uilanguageid 1033 --helplanguageid 1033 --localeid 1033", "Powershell");

			writer.WriteParagraph("Set the time zone for another user (by domain name):");
			writer.WriteCodeBlock("pacx usersettings set --user DOMAIN\\\\john.doe --timezonecode 85", "Powershell");

			writer.WriteParagraph("Switch to 24-hour time format (by enum name) and raise the paging limit:");
			writer.WriteCodeBlock("pacx usersettings set --timeformatcode TwentyFourHour --paginglimit 250", "Powershell");

			writer.WriteParagraph("Show week numbers in the calendar:");
			writer.WriteCodeBlock("pacx usersettings set --showweeknumber true", "Powershell");
		}
	}
}
