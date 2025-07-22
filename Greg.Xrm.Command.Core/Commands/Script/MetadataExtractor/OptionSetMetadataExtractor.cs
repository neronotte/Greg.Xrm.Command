using Microsoft.Xrm.Sdk.Metadata;
using System.Linq;
using System.Collections.Generic;
using Greg.Xrm.Command.Commands.Script.Models;

namespace Greg.Xrm.Command.Commands.Script.MetadataExtractor
{
    public class OptionSetMetadataExtractor
    {
        public List<Extractor_OptionSetMetadata> ExtractOptionSets(IEnumerable<EntityMetadata> entityMetadataList, IEnumerable<OptionSetMetadata> globalOptionSets)
        {
            var optionSets = new List<Extractor_OptionSetMetadata>();
            foreach (var entityMetadata in entityMetadataList)
            {
                foreach (var attribute in entityMetadata.Attributes ?? new AttributeMetadata[0])
                {
                    if (attribute is PicklistAttributeMetadata picklistAttr)
                    {
                        if (picklistAttr.OptionSet?.IsGlobal == true)
                        {
                            var globalOptionSet = globalOptionSets
                                .FirstOrDefault(os => os.Name == picklistAttr.OptionSet.Name);
                            if (globalOptionSet?.Options != null)
                            {
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
                    else if (attribute is StateAttributeMetadata stateAttr)
                    {
                        if (stateAttr.OptionSet?.Options != null)
                        {
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
                    }
                    else if (attribute is StatusAttributeMetadata statusAttr)
                    {
                        if (statusAttr.OptionSet?.Options != null)
                        {
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
                    }
                }
            }
            return optionSets;
        }
    }
}
