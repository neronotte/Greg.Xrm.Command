namespace Greg.Xrm.Command.Commands.Script.Helpers
{
    public static class FieldMetadataHelper
    {
        public static string NormalizeFieldType(string type, string? typeName)
        {
            var normalized = type.Replace("Type", "");
            switch (normalized.ToLower())
            {
                case "string": return "String";
                case "memo": return "Memo";
                case "integer": return "Integer";
                case "decimal": return "Decimal";
                case "double": return "Decimal";
                case "money": return "Money";
                case "boolean": return "Boolean";
                case "datetime": return "DateTime";
                case "lookup": return "Lookup";
                case "picklist": return "Picklist";
                case "multiselectpicklist": return "Picklist";
                default: return normalized;
            }
        }
    }
}
