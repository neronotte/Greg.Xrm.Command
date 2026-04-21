using System.Diagnostics.CodeAnalysis;

namespace Greg.Xrm.Command.Commands.UserSettings
{
	/// <summary>
	/// Metadata describing a single Dataverse usersettings attribute exposed by the
	/// <c>pacx usersettings list</c> and <c>pacx usersettings set</c> commands.
	/// </summary>
	/// <param name="FieldName">Logical name of the attribute on the <c>usersettings</c> entity.</param>
	/// <param name="DisplayName">Human-readable name rendered by the <c>list</c> command.</param>
	/// <param name="EnumType">
	/// Optional enum mirroring the picklist codes of the field. When provided, <c>list</c>
	/// renders each value as <c>&lt;code&gt; (&lt;EnumName&gt;)</c>. All validation of picklist
	/// values is already enforced at CLI-parse time by the corresponding strongly-typed
	/// property on <see cref="SetCommand"/>, so no <c>AllowedValues</c> dictionary is needed here.
	/// </param>
	public sealed record UserSettingField(string FieldName, string DisplayName, Type? EnumType = null);

	/// <summary>
	/// Registry of all <c>usersettings</c> fields handled by the commands.
	/// Field names match the official Dataverse schema:
	/// https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/usersettings.
	/// The registry is intentionally minimal: per-field validation rules now live on
	/// <see cref="SetCommand"/> via DataAnnotations / <c>IValidatableObject</c>.
	/// </summary>
	public static class UserSettingRegistry
	{
		public static readonly IReadOnlyList<UserSettingField> Fields =
		[
			// Language / locale
			new("uilanguageid", "UI Language"),
			new("helplanguageid", "Help Language"),
			new("localeid", "Locale"),

			// Date / time
			new("dateformatcode", "Date Format Code"),
			new("dateformatstring", "Date Format String"),
			new("dateseparator", "Date Separator"),
			new("longdateformatcode", "Long Date Format Code"),
			new("timeformatcode", "Time Format Code", typeof(SetCommand.TimeFormat)),
			new("timeformatstring", "Time Format String"),
			new("timeseparator", "Time Separator"),
			new("amdesignator", "AM Designator"),
			new("pmdesignator", "PM Designator"),
			new("timezonecode", "Time Zone"),
			new("timezonebias", "Time Zone Bias"),
			new("timezonedaylightbias", "Time Zone Daylight Bias"),
			new("timezonedaylightday", "Time Zone Daylight Day"),
			new("timezonedaylightdayofweek", "Time Zone Daylight Day of Week"),
			new("timezonedaylighthour", "Time Zone Daylight Hour"),
			new("timezonedaylightminute", "Time Zone Daylight Minute"),
			new("timezonedaylightmonth", "Time Zone Daylight Month"),
			new("timezonedaylightsecond", "Time Zone Daylight Second"),
			new("timezonedaylightyear", "Time Zone Daylight Year"),
			new("timezonestandardbias", "Time Zone Standard Bias"),
			new("timezonestandardday", "Time Zone Standard Day"),
			new("timezonestandarddayofweek", "Time Zone Standard Day of Week"),
			new("timezonestandardhour", "Time Zone Standard Hour"),
			new("timezonestandardminute", "Time Zone Standard Minute"),
			new("timezonestandardmonth", "Time Zone Standard Month"),
			new("timezonestandardsecond", "Time Zone Standard Second"),
			new("timezonestandardyear", "Time Zone Standard Year"),

			// Workday
			new("workdaystarttime", "Workday Start Time"),
			new("workdaystoptime", "Workday Stop Time"),

			// Calendar
			new("calendartype", "Calendar Type"),
			new("defaultcalendarview", "Default Calendar View"),
			new("addressbooksyncinterval", "Address Book Sync Interval"),

			// Full-name convention
			new("fullnameconventioncode", "Full Name Convention", typeof(SetCommand.FullNameConvention)),

			// Number formatting
			new("numbergroupformat", "Number Group Format"),
			new("negativeformatcode", "Negative Format", typeof(SetCommand.NegativeNumberFormat)),
			new("decimalsymbol", "Decimal Symbol"),
			new("numberseparator", "Number Separator (Thousands)"),
			new("pricingdecimalprecision", "Pricing Decimal Precision"),
			new("nexttrackingnumber", "Next Tracking Number"),
			new("trackingtokenid", "Tracking Token ID"),
			new("autocaptureuserstatus", "Auto Capture User Status"),

			// Currency formatting
			new("currencyformatcode", "Currency Format", typeof(SetCommand.CurrencySymbolPosition)),
			new("negativecurrencyformatcode", "Negative Currency Format"),
			new("currencysymbol", "Currency Symbol"),
			new("currencydecimalprecision", "Currency Decimal Precision"),

			// Paging
			new("paginglimit", "Paging Limit"),

			// Email / tracking
			new("incomingemailfilteringmethod", "Incoming Email Filtering Method", typeof(SetCommand.IncomingEmailFilter)),
			new("ignoreunsolicitedemail", "Ignore Unsolicited Email"),

			// Script errors
			new("reportscripterrors", "Report Script Errors", typeof(SetCommand.ScriptErrorReportingMode)),

			// Advanced Find
			new("advancedfindstartupmode", "Advanced Find Startup Mode"),

			// Sync intervals
			new("offlinesyncinterval", "Offline Sync Interval"),
			new("outlooksyncinterval", "Outlook Sync Interval"),

			// Homepage / navigation
			new("homepagearea", "Homepage Area"),
			new("homepagelayout", "Homepage Layout"),
			new("homepagesubarea", "Homepage Subarea"),
			new("defaultsearchexperience", "Default Search Experience", typeof(SetCommand.SearchExperience)),
			new("defaultcountrycode", "Default Country Code"),

			// Entity form mode
			new("entityformmode", "Entity Form Mode", typeof(SetCommand.FormMode)),
			new("visualizationpanelayout", "Visualization Panel Layout", typeof(SetCommand.PaneLayout)),
			new("userprofile", "User Profile"),

			// Release channel
			new("releasechannel", "Release Channel", typeof(SetCommand.ReleaseChannelOverride)),

			// D365 auto-install
			new("d365autoinstallattemptstatus", "D365 Auto-install Attempt Status", typeof(SetCommand.D365AutoInstallStatus)),

			// Data validation
			new("datavalidationmodeforexporttoexcel", "Data Validation Mode for Export to Excel", typeof(SetCommand.ExcelExportValidationMode)),

			// DataSearch teaching bubbles
			new("tablescopeddvsearchfeatureteachingbubbleviews", "Table-scoped DV Search Feature Teaching Bubble Views"),
			new("tablescopeddvsearchquickfindteachingbubbleviews", "Table-scoped DV Search Quick Find Teaching Bubble Views"),

			// Boolean settings
			new("showweeknumber", "Show Week Number"),
			new("synccontactcompany", "Sync Contact Company"),
			new("autocreatecontactonpromote", "Auto-create Contact on Promote"),
			new("getstartedpanecontentenabled", "Get Started Pane Content Enabled"),
			new("isappsforcrmalertdismissed", "Is Apps for CRM Alert Dismissed"),
			new("isautodatacaptureenabled", "Is Auto Data Capture Enabled"),
			new("isdefaultcountrycodecheckenabled", "Is Default Country Code Check Enabled"),
			new("isduplicatedetectionenabledwhengoingonline", "Is Duplicate Detection Enabled When Going Online"),
			new("isemailconversationviewenabled", "Is Email Conversation View Enabled"),
			new("isguidedhelpenabled", "Is Guided Help Enabled"),
			new("isresourcebookingexchangesyncenabled", "Is Resource Booking Exchange Sync Enabled"),
			new("issendasallowed", "Is Send As Allowed"),
			new("splitviewstate", "Split View State"),
			new("trytogglestatus", "Try Toggle Status"),
			new("useimagestrips", "Use Image Strips"),
			new("usecrmformforappointment", "Use CRM Form for Appointment"),
			new("usecrmformforcontact", "Use CRM Form for Contact"),
			new("usecrmformforemail", "Use CRM Form for Email"),
			new("usecrmformfortask", "Use CRM Form for Task"),
		];

		private static readonly IReadOnlyDictionary<string, UserSettingField> _byName =
			Fields.ToDictionary(f => f.FieldName, StringComparer.OrdinalIgnoreCase);

		public static bool Contains(string fieldName) => _byName.ContainsKey(fieldName);

		public static bool TryGet(string fieldName, [MaybeNullWhen(false)] out UserSettingField field)
			=> _byName.TryGetValue(fieldName, out field);

		/// <summary>
		/// Logical names of the LCID-typed fields. Used by the <c>set</c> executor to trigger
		/// the Dataverse "language available?" check.
		/// </summary>
		public static readonly IReadOnlySet<string> LanguageFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"uilanguageid",
			"helplanguageid",
			"localeid"
		};
	}
}
