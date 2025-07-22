using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Models
{
    public class Extractor_OptionSetMetadata
    {
        public string EntityName { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string OptionSetName { get; set; } = string.Empty;
        public int OptionValue { get; set; }
        public string OptionLabel { get; set; } = string.Empty;
        public Extractor_OptionSetSourceType SourceType { get; set; }

        public static List<Extractor_OptionSetMetadata> ExtractOptionSets(IEnumerable<EntityMetadata> entityMetadataList, IEnumerable<OptionSetMetadata> globalOptionSets)
        {
            var optionSets = new List<Extractor_OptionSetMetadata>();
            foreach (var entityMetadata in entityMetadataList)
            {
                foreach (var attribute in entityMetadata.Attributes)
                {
                    if (attribute is PicklistAttributeMetadata picklistAttr)
                    {
                        HandlePicklist(globalOptionSets, optionSets, entityMetadata, picklistAttr);
                    }
                    else if (attribute is StateAttributeMetadata stateAttr)
                    {
                        HandleState(optionSets, entityMetadata, stateAttr);
                    }
                    else if (attribute is StatusAttributeMetadata statusAttr)
                    {
                        HandleStatus(optionSets, entityMetadata, statusAttr);
                    }
                }
            }
            return optionSets;
        }

        private static void HandleStatus(List<Extractor_OptionSetMetadata> optionSets, EntityMetadata entityMetadata, StatusAttributeMetadata statusAttr)
        {
            if ((statusAttr.OptionSet?.Options) == null)
            {
                return;
            }
            foreach (var option in statusAttr.OptionSet.Options)
            {
                optionSets.Add(new Extractor_OptionSetMetadata
                {
                    EntityName = entityMetadata.LogicalName,
                    FieldName = statusAttr.LogicalName,
                    OptionSetName = "",
                    OptionValue = option.Value ?? 0,
                    OptionLabel = option.Label?.UserLocalizedLabel?.Label ?? "",
                    SourceType = Extractor_OptionSetSourceType.State
                });
            }
        }

        private static void HandleState(List<Extractor_OptionSetMetadata> optionSets, EntityMetadata entityMetadata, StateAttributeMetadata stateAttr)
        {
            if ((stateAttr.OptionSet?.Options) == null)
            {
                return;
            }
            foreach (var option in stateAttr.OptionSet.Options)
            {
                optionSets.Add(new Extractor_OptionSetMetadata
                {
                    EntityName = entityMetadata.LogicalName,
                    FieldName = stateAttr.LogicalName,
                    OptionSetName = "",
                    OptionValue = option.Value ?? 0,
                    OptionLabel = option.Label?.UserLocalizedLabel?.Label ?? "",
                    SourceType = Extractor_OptionSetSourceType.State
                });
            }
        }

        private static void HandlePicklist(IEnumerable<OptionSetMetadata> globalOptionSets, List<Extractor_OptionSetMetadata> optionSets, EntityMetadata entityMetadata, PicklistAttributeMetadata picklistAttr)
        {
            if (picklistAttr.OptionSet?.IsGlobal == true)
            {
                var globalOptionSet = globalOptionSets
                    .FirstOrDefault(os => os.Name == picklistAttr.OptionSet.Name);
                if ((globalOptionSet?.Options) == null)
                {
                    return;
                }
                foreach (var option in globalOptionSet.Options)
                {
                    optionSets.Add(new Extractor_OptionSetMetadata
                    {
                        EntityName = entityMetadata.LogicalName,
                        FieldName = picklistAttr.LogicalName,
                        OptionSetName = globalOptionSet.Name,
                        OptionValue = option.Value ?? 0,
                        OptionLabel = option.Label?.UserLocalizedLabel?.Label ?? "",
                        SourceType = Extractor_OptionSetSourceType.Global
                    });
                }
            }
            else if (picklistAttr.OptionSet?.Options != null)
            {
                foreach (var option in picklistAttr.OptionSet.Options)
                {
                    optionSets.Add(new Extractor_OptionSetMetadata
                    {
                        EntityName = entityMetadata.LogicalName,
                        FieldName = picklistAttr.LogicalName,
                        OptionSetName = "",
                        OptionValue = option.Value ?? 0,
                        OptionLabel = option.Label?.UserLocalizedLabel?.Label ?? "",
                        SourceType = Extractor_OptionSetSourceType.Local
                    });
                }
            }
        }
    }
}
