using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using System.Linq.Expressions;

namespace Greg.Xrm.Command.Commands.Table.ExportMetadata
{
	public class ExcelMetadataSheetWriterTable : IExcelMetadataSheetWriter
	{
		public void Write(ExcelPackage package, EntityMetadata entityMetadata)
		{
			var ws = package.Workbook.Worksheets.Add("Summary");
			ws.View.ShowGridLines = false;

			ws.Cells[1, 1, 1, 10]
				.MergeCells()
				.SetValue("Table summary")
				.Title();

			ws.Cells[2, 1]
				.SetValue(entityMetadata.SchemaName)
				.Explanatory();

			var row = 3;

			ws.Cells[++row, 1].SetValue("Property");
			ws.Cells[row, 2].SetValue("Value");

			Write(ws, ++row, entityMetadata, e => e.SchemaName);
			Write(ws, ++row, entityMetadata, e => e.LogicalName);
			Write(ws, ++row, entityMetadata, e => e.LogicalCollectionName);
			Write(ws, ++row, entityMetadata, e => e.DisplayName);
			Write(ws, ++row, entityMetadata, e => e.DisplayCollectionName);
			Write(ws, ++row, entityMetadata, e => e.Description);
			Write(ws, ++row, entityMetadata, e => e.MetadataId);
			Write(ws, ++row, entityMetadata, e => e.TableType);
			Write(ws, ++row, entityMetadata, e => e.IsCustomEntity);
			Write(ws, ++row, entityMetadata, e => e.PrimaryIdAttribute);
			Write(ws, ++row, entityMetadata, e => e.PrimaryImageAttribute);
			Write(ws, ++row, entityMetadata, e => e.PrimaryNameAttribute);

			Write(ws, ++row, entityMetadata, e => e.ActivityTypeMask);
			Write(ws, ++row, entityMetadata, e => e.AutoCreateAccessTeams);
			Write(ws, ++row, entityMetadata, e => e.AutoRouteToOwnerQueue);
			Write(ws, ++row, entityMetadata, e => e.CanBeInManyToMany);
			Write(ws, ++row, entityMetadata, e => e.CanBePrimaryEntityInRelationship);
			Write(ws, ++row, entityMetadata, e => e.CanBeRelatedEntityInRelationship);
			Write(ws, ++row, entityMetadata, e => e.CanCreateAttributes);
			Write(ws, ++row, entityMetadata, e => e.CanCreateCharts);
			Write(ws, ++row, entityMetadata, e => e.CanCreateForms);
			Write(ws, ++row, entityMetadata, e => e.CanCreateViews);
			Write(ws, ++row, entityMetadata, e => e.CanModifyAdditionalSettings);
			Write(ws, ++row, entityMetadata, e => e.CanTriggerWorkflow);
			Write(ws, ++row, entityMetadata, e => e.EntityColor);
			Write(ws, ++row, entityMetadata, e => e.EntitySetName);
			Write(ws, ++row, entityMetadata, e => e.ExternalCollectionName);
			Write(ws, ++row, entityMetadata, e => e.ExternalName);
			Write(ws, ++row, entityMetadata, e => e.HasActivities);
			Write(ws, ++row, entityMetadata, e => e.HasChanged);
			Write(ws, ++row, entityMetadata, e => e.HasFeedback);
			Write(ws, ++row, entityMetadata, e => e.HasNotes);
			Write(ws, ++row, entityMetadata, e => e.IconLargeName);
			Write(ws, ++row, entityMetadata, e => e.IconMediumName);
			Write(ws, ++row, entityMetadata, e => e.IconSmallName);
			Write(ws, ++row, entityMetadata, e => e.IsActivity);
			Write(ws, ++row, entityMetadata, e => e.IsActivityParty);
			Write(ws, ++row, entityMetadata, e => e.IsAuditEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsAvailableOffline);
			Write(ws, ++row, entityMetadata, e => e.IsBPFEntity);
			Write(ws, ++row, entityMetadata, e => e.IsBusinessProcessEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsChildEntity);
			Write(ws, ++row, entityMetadata, e => e.IsConnectionsEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsCustomizable);
			Write(ws, ++row, entityMetadata, e => e.IsDocumentManagementEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsDocumentRecommendationsEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsDuplicateDetectionEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsEnabledForCharts);
			Write(ws, ++row, entityMetadata, e => e.IsEnabledForExternalChannels);
			Write(ws, ++row, entityMetadata, e => e.IsImportable);
			Write(ws, ++row, entityMetadata, e => e.IsIntersect);
			Write(ws, ++row, entityMetadata, e => e.IsKnowledgeManagementEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsLogicalEntity);
			Write(ws, ++row, entityMetadata, e => e.IsMailMergeEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsManaged);
			Write(ws, ++row, entityMetadata, e => e.IsMappable);
			Write(ws, ++row, entityMetadata, e => e.IsMSTeamsIntegrationEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsOfflineInMobileClient);
			Write(ws, ++row, entityMetadata, e => e.IsOneNoteIntegrationEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsOptimisticConcurrencyEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsPrivate);
			Write(ws, ++row, entityMetadata, e => e.IsQuickCreateEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsReadingPaneEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsReadOnlyInMobileClient);
			Write(ws, ++row, entityMetadata, e => e.IsRenameable);
			Write(ws, ++row, entityMetadata, e => e.IsRetentionEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsRetrieveAuditEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsRetrieveMultipleAuditEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsSLAEnabled);
			Write(ws, ++row, entityMetadata, e => e.IsSolutionAware);
			Write(ws, ++row, entityMetadata, e => e.IsStateModelAware);
			Write(ws, ++row, entityMetadata, e => e.IsValidForAdvancedFind);
			Write(ws, ++row, entityMetadata, e => e.IsValidForQueue);
			Write(ws, ++row, entityMetadata, e => e.IsVisibleInMobile);
			Write(ws, ++row, entityMetadata, e => e.IsVisibleInMobileClient);
			Write(ws, ++row, entityMetadata, e => e.MobileOfflineFilters);
			Write(ws, ++row, entityMetadata, e => e.ObjectTypeCode);
			Write(ws, ++row, entityMetadata, e => e.OwnershipType);
			Write(ws, ++row, entityMetadata, e => e.RecurrenceBaseEntityLogicalName);
			Write(ws, ++row, entityMetadata, e => e.ReportViewName);
			Write(ws, ++row, entityMetadata, e => e.SettingOf);
			Write(ws, ++row, entityMetadata, e => e.SyncToExternalSearchIndex);
			Write(ws, ++row, entityMetadata, e => e.UsesBusinessDataLabelTable);


			ws.CreateTable("Summary", 4, 1, row, 2).ShowFirstColumn = true;
		}

		private static void Write<T>(ExcelWorksheet ws, int row, EntityMetadata e, Expression<Func<EntityMetadata, T>> propertyAccessor)
		{
			var propertyName = e.GetMemberName(propertyAccessor);

			propertyName = propertyName.SplitNameInPartsByCapitalLetters();

			ws.Cells[row, 1].SetValue(propertyName);

			var value = propertyAccessor.Compile()(e);
			ws.Cells[row, 2]
				.SetValue(Format(value))
				.TextAlign(OfficeOpenXml.Style.ExcelHorizontalAlignment.Left);
		}

		private static object? Format<T>(T? value)
		{
			if (value is null) return null;


			if (value is BooleanManagedProperty p1) return p1.Value;
			if (value is Label p2) return p2.UserLocalizedLabel?.Label ?? p2.LocalizedLabels?.FirstOrDefault()?.Label;
			if (value.GetType().IsValueType) return value;


			return value.ToString();
		}
	}
}
