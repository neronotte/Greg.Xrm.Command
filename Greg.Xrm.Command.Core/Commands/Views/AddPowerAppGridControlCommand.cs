using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Views
{
	[Command("view", "addPowerappGridControl", HelpText = "(Preview) Adds a Power Apps grid control to a given view.")]
	[Alias("view", "pappgrid")]
	public class AddPowerAppGridControlCommand
	{
		[Option("viewName", "n", Order = 1, HelpText = "The display name of the view to update.")]
		[Required]
		public string ViewName { get; set; } = string.Empty;

		[Option("table", "t", Order = 2, HelpText = "The name of the table that contains the view. Required only if the view name is not unique in the system.")]
		public string? TableName { get; set; }


		[Option("type", "q", Order = 3, HelpText = "The type of query.", DefaultValue = QueryType1.SavedQuery)]
		public QueryType1 QueryType { get; set; } = QueryType1.SavedQuery;


		[Option("force", "f", Order = 4, HelpText = "Force the update of the control if a custom control is already set on the view.")]
		public bool Force { get; set; } = false;


		[Option("accessibleLabel", "al", Order = 5, HelpText = "The accessible label for the grid control.")]
		public string? AccessibleLabel { get; set; }

		[Option("enableEditing", "ee", Order = 6, HelpText = "Enable editing functionality in the grid control.", DefaultValue = false)]
		public bool EnableEditing { get; set; } = false;

		[Option("disableChildItemsEditing", "dcie", Order = 7, HelpText = "Disable editing of child items in the grid control.", DefaultValue = false)]
		public bool DisableChildItemsEditing { get; set; } = false;

		[Option("enableFiltering", "ef", Order = 8, HelpText = "Enable filtering functionality in the grid control.", DefaultValue = true)]
		public bool EnableFiltering { get; set; } = true;

		[Option("enableSorting", "es", Order = 9, HelpText = "Enable sorting functionality in the grid control.", DefaultValue = true)]
		public bool EnableSorting { get; set; } = true;

		[Option("enableGrouping", "eg", Order = 10, HelpText = "Enable grouping functionality in the grid control.", DefaultValue = false)]
		public bool EnableGrouping { get; set; } = false;

		[Option("enableAggregation", "ea", Order = 11, HelpText = "Enable aggregation functionality in the grid control.", DefaultValue = false)]
		public bool EnableAggregation { get; set; } = false;

		[Option("enableColumnMoving", "ecm", Order = 12, HelpText = "Enable column moving functionality in the grid control.", DefaultValue = false)]
		public bool EnableColumnMoving { get; set; } = false;

		[Option("enableMultipleSelection", "ems", Order = 13, HelpText = "Enable multiple selection functionality in the grid control.", DefaultValue = true)]
		public bool EnableMultipleSelection { get; set; } = true;

		[Option("enableRangeSelection", "ers", Order = 14, HelpText = "Enable range selection functionality in the grid control.", DefaultValue = true)]
		public bool EnableRangeSelection { get; set; } = true;

		[Option("enableJumpBar", "ejb", Order = 15, HelpText = "Enable jump bar functionality in the grid control.", DefaultValue = false)]
		public bool EnableJumpBar { get; set; } = false;

		[Option("enablePagination", "ep", Order = 16, HelpText = "Enable pagination functionality in the grid control.", DefaultValue = false)]
		public bool EnablePagination { get; set; } = false;

		[Option("enableDropdownColor", "edc", Order = 17, HelpText = "Enable dropdown color functionality in the grid control.", DefaultValue = true)]
		public bool EnableDropdownColor { get; set; } = true;

		[Option("enableStatusIcons", "esi", Order = 18, HelpText = "Enable status icons functionality in the grid control.", DefaultValue = false)]
		public bool EnableStatusIcons { get; set; } = false;

		[Option("enableTypeIcons", "eti", Order = 19, HelpText = "Enable type icons functionality in the grid control.", DefaultValue = false)]
		public bool EnableTypeIcons { get; set; } = false;

		[Option("navigationTypesAllowed", "nav", HelpText = "The types of navigation allowed in the grid control.", DefaultValue = NavigationTypes.All)]
		public NavigationTypes NavigationTypesAllowed { get; set; } = NavigationTypes.All;

		[Option("reflowBehavior", "ref", Order = 20, HelpText = "The reflow behavior of the grid control.", DefaultValue = Reflow.Reflow)]
		public Reflow ReflowBehavior { get; set; } = Reflow.Reflow;

		[Option("showAvatar", "avatar", Order = 21, HelpText = "Show avatar in the grid control.", DefaultValue = true)]
		public bool ShowAvatar { get; set; } = true;

		[Option("numberOfListColumns", "cols", Order = 22, HelpText = "The number of list columns in the grid control.", DefaultValue = 3)]
		public int NumberOfListColumns { get; set; } = 3;

		[Option("contextualFilters", "cf", Order = 23, HelpText = "Enable contextual lookup column filters in the grid control.", DefaultValue = true)]
		public bool ContextualLookupColumnFilters { get; set; } = true;

		[Option("lookupFilterBeginsWith", "lfbw", Order = 24, HelpText = "Filter lookup suggestions from their starting letter.", DefaultValue = false)]
		public bool LookupFilterBeginsWith { get; set; } = false;

		[Option("useFirstColumnForLookupEdits", "ufcfle", Order = 25, HelpText = "Use first column for lookup edits in the grid control.", DefaultValue = false)]
		public bool UseFirstColumnForLookupEdits { get; set; } = false;

		[Option("customizerControl", "cc", Order = 26, HelpText = "The full name of the grid customizer control.")]
		public string? GridCustomizerControlFullName { get; set; }


		public enum NavigationTypes
		{
			All,
			PrimaryOnly,
			None
		}
		public enum Reflow
		{
			Reflow,
			GridOnly,
			ListOnly
		}
	}
}
