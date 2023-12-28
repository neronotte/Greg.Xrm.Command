using Microsoft.Xrm.Sdk.Metadata;
using System.ComponentModel.DataAnnotations;

namespace Greg.Xrm.Command.Commands.Table.Builders
{
    public class CommandArgsConstants
    {
        public const string TABLE = "table";
        public const string SOLUTION = "solution";
        public const string NAME = "name";
        public const string SCHEMA_NAME = "schemaName";
        public const string DESCRIPTION = "description";
        public const string TYPE = "type";
        public const string STRING_FORMAT = "stringFormat";
        public const string MEMO_FORMAT = "memoFormat";
        public const string INT_FORMAT = "intFormat";
        public const string REQUIRED_LEVEL = "requiredLevel";
        public const string MAX_LENGTH = "len";
        public const string AUTONUMBER = "autoNumber";
        public const string AUDIT = "audit";
        public const string OPTIONS = "options";
        public const string GLOBAL_OPTIONSET_NAME = "globalOptionSetName";
        public const string MULTISELECT = "multiselect";
        public const string MIN = "min";
        public const string MAX = "max";
        public const string PRECISION = "precision";
        public const string PRECISION_SOURCE = "precisionSource";
        public const string IME_MODE = "imeMode";
        public const string DATETIME_BEHAVIOR = "dateTimeBehavior";
        public const string DATETIME_FORMAT = "dateTimeFormat";
        public const string TRUE_LABEL = "trueLabel";
        public const string FALSE_LABEL = "falseLabel";

        public const string TABLE_SHORT = "t";
        public const string SOLUTION_SHORT = "s";
        public const string NAME_SHORT = "n";
        public const string SCHEMA_NAME_SHORT = "sn";
        public const string DESCRIPTION_SHORT = "d";
        public const string TYPE_SHORT = "at";
        public const string STRING_FORMAT_SHORT = "sf";
        public const string MEMO_FORMAT_SHORT = "mf";
        public const string INT_FORMAT_SHORT = "if";
        public const string REQUIRED_LEVEL_SHORT = "r";
        public const string MAX_LENGTH_SHORT = "l";
        public const string AUTONUMBER_SHORT = "an";
        public const string AUDIT_SHORT = "a";
        public const string OPTIONS_SHORT = "o";
        public const string GLOBAL_OPTIONSET_NAME_SHORT = "gon";
        public const string MULTISELECT_SHORT = "m";
        public const string MIN_SHORT = "min";
        public const string MAX_SHORT = "max";
        public const string PRECISION_SHORT = "p";
        public const string PRECISION_SOURCE_SHORT = "ps";
        public const string IME_MODE_SHORT = "ime";
        public const string DATETIME_BEHAVIOR_SHORT =  "dtb";
        public const string DATETIME_FORMAT_SHORT =  "dtf";
        public const string TRUE_LABEL_SHORT =  "tl";
        public const string FALSE_LABEL_SHORT =  "fl";

		//TABLE
		public const string PLURAL = "plural";
		public const string OWNERSHIP = "ownership";
		public const string IS_ACTIVITY = "isActivity";
		public const string PRIMARY_ATTRIBUTE_NAME = "primaryAttributeName";
		public const string PRIMARY_ATTRIBUTE_SCHEMA_NAME = "primaryAttributeSchemaName";
		public const string PRIMARY_ATTRIBUTE_DESCRIPTION = "primaryAttributeDescription";
		public const string PRIMARY_ATTRIBUTE_AUTONUMBER_FORMAT = "primaryAttributeAutoNumberFormat";
		public const string PRIMARY_ATTRIBUTE_REQUIRED_LEVEL = "primaryAttributeRequiredLevel";
		public const string PRIMARY_ATTRIBUTE_MAX_LENGTH = "primaryAttributeMaxLength"; 

        public const string PLURAL_SHORT = "p";
		public const string OWNERSHIP_SHORT = "o";
		public const string IS_ACTIVITY_SHORT = "act";
		public const string PRIMARY_ATTRIBUTE_NAME_SHORT = "pan";
		public const string PRIMARY_ATTRIBUTE_SCHEMA_NAME_SHORT = "pas";
		public const string PRIMARY_ATTRIBUTE_DESCRIPTION_SHORT = "pad";
		public const string PRIMARY_ATTRIBUTE_AUTONUMBER_FORMAT_SHORT = "paan";
		public const string PRIMARY_ATTRIBUTE_REQUIRED_LEVEL_SHORT = "par";
		public const string PRIMARY_ATTRIBUTE_MAX_LENGTH_SHORT = "palen";

		public const string TABLE_COMMAND = "pacx table create ";
		public const string COLUMN_COMMAND = "pacx column create ";

        public static Dictionary<string, string> GetArgumentDictionary()
        {
			var argumentsDictionary = new Dictionary<string, string>
			{
				{ TABLE, TABLE_SHORT },
				{ SOLUTION, SOLUTION_SHORT },
				{ NAME, NAME_SHORT },
				{ SCHEMA_NAME, SCHEMA_NAME_SHORT },
				{ DESCRIPTION, DESCRIPTION_SHORT },
				{ TYPE, TYPE_SHORT },
				{ STRING_FORMAT, STRING_FORMAT_SHORT },
				{ MEMO_FORMAT, MEMO_FORMAT_SHORT },
				{ INT_FORMAT, INT_FORMAT_SHORT },
				{ REQUIRED_LEVEL, REQUIRED_LEVEL_SHORT },
				{ OPTIONS, OPTIONS_SHORT },
				{ AUTONUMBER, AUTONUMBER_SHORT },
				{ AUDIT, AUDIT_SHORT },
				{ MAX_LENGTH, MAX_LENGTH_SHORT },
				{ GLOBAL_OPTIONSET_NAME, GLOBAL_OPTIONSET_NAME_SHORT },
				{ MULTISELECT, MULTISELECT_SHORT },
				{ MIN, MIN_SHORT },
				{ MAX, MAX_SHORT },
				{ PRECISION, PRECISION_SHORT },
				{ PRECISION_SOURCE, PRECISION_SOURCE_SHORT },
				{ IME_MODE, IME_MODE_SHORT },
				{ DATETIME_BEHAVIOR, DATETIME_BEHAVIOR_SHORT },
				{ DATETIME_FORMAT, DATETIME_FORMAT_SHORT },
				{ TRUE_LABEL, TRUE_LABEL_SHORT },
				{ FALSE_LABEL, FALSE_LABEL_SHORT },
				{ PLURAL, PLURAL_SHORT },
				{ OWNERSHIP, OWNERSHIP_SHORT },
				{ IS_ACTIVITY, IS_ACTIVITY_SHORT },
				{ PRIMARY_ATTRIBUTE_NAME, PRIMARY_ATTRIBUTE_NAME_SHORT },
				{ PRIMARY_ATTRIBUTE_SCHEMA_NAME, PRIMARY_ATTRIBUTE_SCHEMA_NAME_SHORT },
				{ PRIMARY_ATTRIBUTE_DESCRIPTION, PRIMARY_ATTRIBUTE_DESCRIPTION_SHORT },
				{ PRIMARY_ATTRIBUTE_AUTONUMBER_FORMAT, PRIMARY_ATTRIBUTE_AUTONUMBER_FORMAT_SHORT },
				{ PRIMARY_ATTRIBUTE_REQUIRED_LEVEL, PRIMARY_ATTRIBUTE_REQUIRED_LEVEL_SHORT },
				{ PRIMARY_ATTRIBUTE_MAX_LENGTH, PRIMARY_ATTRIBUTE_MAX_LENGTH_SHORT }
			};

			return argumentsDictionary;
        }

	}
}
