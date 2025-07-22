using Microsoft.Xrm.Sdk.Metadata;
using Models = Greg.Xrm.Command.Commands.Script.Models;
using System.Linq;
using System.Collections.Generic;

namespace Greg.Xrm.Command.Commands.Script.MetadataExtractor
{
    public class EntityMetadataExtractor
    {
        private Models.EntityMetadata BuildEntityMetadata(EntityMetadata e, List<AttributeMetadata> fields, List<string> prefixes)
        {
            var entity = new Models.EntityMetadata
            {
                SchemaName = e.LogicalName,
                DisplayName = e.DisplayName?.UserLocalizedLabel?.Label ?? e.LogicalName,
                PluralName = e.DisplayCollectionName?.UserLocalizedLabel?.Label ?? e.LogicalName,
                IsCustomEntity = prefixes.Any(pre => e.LogicalName.StartsWith(pre)),
                Fields = new List<Models.FieldMetadata>()
            };
            var primaryField = e.Attributes?.FirstOrDefault(a => a.LogicalName == e.PrimaryNameAttribute);
            if (primaryField is StringAttributeMetadata strAttr)
            {
                entity.PrimaryFieldName = strAttr.DisplayName?.UserLocalizedLabel?.Label ?? strAttr.LogicalName;
                entity.PrimaryFieldSchemaName = strAttr.LogicalName;
                entity.PrimaryFieldDescription = strAttr.Description?.UserLocalizedLabel?.Label;
                entity.PrimaryFieldMaxLength = strAttr.MaxLength;
                entity.PrimaryFieldAutoNumberFormat = strAttr.AutoNumberFormat;
                entity.PrimaryFieldRequiredLevel = strAttr.RequiredLevel?.Value.ToString();
            }
            foreach (var a in fields)
            {
                var field = new Models.FieldMetadata
                {
                    LogicalName = a.LogicalName,
                    DisplayName = a.DisplayName?.UserLocalizedLabel?.Label ?? a.LogicalName,
                    FieldType = new FieldMetadataHelper().NormalizeFieldType(a.AttributeType?.ToString() ?? "String", a.AttributeTypeName?.Value),
                    RequiredLevel = a.RequiredLevel?.Value.ToString() ?? "None",
                    IsCustomField = prefixes.Any(pre => a.LogicalName.StartsWith(pre)),
                    IsLookup = a.AttributeType == AttributeTypeCode.Lookup || a.AttributeType == AttributeTypeCode.Owner
                };
                switch (a)
                {
                    case StringAttributeMetadata str:
                        field.MaxLength = str.MaxLength;
                        field.Format = str.Format?.ToString() ?? string.Empty;
                        field.AutoNumberFormat = str.AutoNumberFormat;
                        break;
                    case MemoAttributeMetadata memo:
                        field.MaxLength = memo.MaxLength;
                        field.Format = memo.Format?.ToString() ?? string.Empty;
                        break;
                    case IntegerAttributeMetadata integer:
                        field.MinValue = integer.MinValue;
                        field.MaxValue = integer.MaxValue;
                        field.IntegerFormat = integer.Format?.ToString();
                        break;
                    case DecimalAttributeMetadata dec:
                        field.MinValue = (double?)dec.MinValue;
                        field.MaxValue = (double?)dec.MaxValue;
                        field.Precision = dec.Precision;
                        break;
                    case MoneyAttributeMetadata money:
                        field.MinValue = money.MinValue;
                        field.MaxValue = money.MaxValue;
                        field.Precision = money.Precision;
                        field.PrecisionSource = money.PrecisionSource;
                        break;
                    case BooleanAttributeMetadata boolean:
                        field.TrueLabel = boolean.OptionSet?.TrueOption?.Label?.UserLocalizedLabel?.Label;
                        field.FalseLabel = boolean.OptionSet?.FalseOption?.Label?.UserLocalizedLabel?.Label;
                        break;
                    case DateTimeAttributeMetadata dt:
                        field.DateTimeBehavior = dt.DateTimeBehavior?.Value.ToString();
                        field.DateTimeFormat = dt.Format?.ToString();
                        break;
                    case PicklistAttributeMetadata picklist:
                        field.GlobalOptionSetName = picklist.OptionSet?.IsGlobal == true ? picklist.OptionSet.Name : null;
                        if (picklist.OptionSet?.Options != null)
                        {
                            field.Options = picklist.OptionSet.Options.Select(o => new Models.OptionSetOption
                            {
                                Value = o.Value ?? 0,
                                Label = o.Label?.UserLocalizedLabel?.Label ?? string.Empty
                            }).ToList();
                        }
                        break;
                    case MultiSelectPicklistAttributeMetadata multi:
                        field.GlobalOptionSetName = multi.OptionSet?.IsGlobal == true ? multi.OptionSet.Name : null;
                        if (multi.OptionSet?.Options != null)
                        {
                            field.Options = multi.OptionSet.Options.Select(o => new Models.OptionSetOption
                            {
                                Value = o.Value ?? 0,
                                Label = o.Label?.UserLocalizedLabel?.Label ?? string.Empty
                            }).ToList();
                        }
                        break;
                }
                entity.Fields.Add(field);
            }
            return entity;
        }

        public List<Models.EntityMetadata> ExtractEntitiesByPrefix(IEnumerable<EntityMetadata> entityMetadataList, List<string> prefixes)
        {
            var entities = new List<Models.EntityMetadata>();
            foreach (var e in entityMetadataList)
            {
                var fields = (e.Attributes ?? Enumerable.Empty<AttributeMetadata>())
                    .Where(a => 
                        (a.AttributeType != AttributeTypeCode.Virtual || (a.AttributeType == AttributeTypeCode.Virtual && a.AttributeTypeName?.Value == "MultiSelectPicklistType"))
                ).ToList();
                entities.Add(BuildEntityMetadata(e, fields, prefixes));
            }
            return entities;
        }

        public List<Models.EntityMetadata> ExtractEntitiesBySolution(IEnumerable<EntityMetadata> entityMetadataList, List<Guid> entityIds, List<string> prefixes)
        {
            var entities = new List<Models.EntityMetadata>();
            foreach (var e in entityMetadataList.Where(e => entityIds.Contains(e.MetadataId.GetValueOrDefault())))
            {
                var fields = (e.Attributes ?? Enumerable.Empty<AttributeMetadata>())
                    .Where(a => 
                        (a.AttributeType != AttributeTypeCode.Virtual || (a.AttributeType == AttributeTypeCode.Virtual && a.AttributeTypeName?.Value == "MultiSelectPicklistType"))
                ).ToList();
                entities.Add(BuildEntityMetadata(e, fields, prefixes));
            }
            return entities;
        }

        public Models.EntityMetadata? ExtractEntityByName(IEnumerable<EntityMetadata> entityMetadataList, string tableName, List<string> prefixes)
        {
            var e = entityMetadataList.FirstOrDefault(e => e.LogicalName == tableName);
            if (e == null) return null;
            var fields = (e.Attributes ?? Enumerable.Empty<AttributeMetadata>())
                .Where(a => 
                    (a.AttributeType != AttributeTypeCode.Virtual || (a.AttributeType == AttributeTypeCode.Virtual && a.AttributeTypeName?.Value == "MultiSelectPicklistType"))
            ).ToList();
            return BuildEntityMetadata(e, fields, prefixes);
        }
    }
}
