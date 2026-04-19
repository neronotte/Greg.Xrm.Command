using Microsoft.Xrm.Sdk.Metadata;

namespace Greg.Xrm.Command.Commands.Script.Service.ColumnScriptGenerators
{
	internal class ScriptGeneratorFactory
	{
		public IColumnScriptGenerator CreateFor(AttributeMetadata field)
		{
			if (field is StringAttributeMetadata stringField)
			{
				return new ColumnScriptGeneratorForString(stringField);
			}
			if (field is PicklistAttributeMetadata picklistField)
			{
				return new ColumnScriptGeneratorForPicklist(picklistField);
			}
			if (field is MultiSelectPicklistAttributeMetadata multiSelectPicklistField)
			{
				return new ColumnScriptGeneratorForPicklist(multiSelectPicklistField);
			}
			if (field is BooleanAttributeMetadata booleanField)
			{
				return new ColumnScriptGeneratorForBoolean(booleanField);
			}
			if (field is DateTimeAttributeMetadata dateTimeField)
			{
				return new ColumnScriptGeneratorForDateTime(dateTimeField);
			}
			if (field is DecimalAttributeMetadata decimalField)
			{
				return new ColumnScriptGeneratorForDecimal(decimalField);
			}
			if (field is DoubleAttributeMetadata doubleField)
			{
				return new ColumnScriptGeneratorForDouble(doubleField);
			}
			if (field is FileAttributeMetadata fileField)
			{
				return new ColumnScriptGeneratorForFile(fileField);
			}
			if (field is ImageAttributeMetadata imageField)
			{
				return new ColumnScriptGeneratorForImage(imageField);
			}
			if (field is IntegerAttributeMetadata integerField)
			{
				return new ColumnScriptGeneratorForInteger(integerField);
			}
			if (field is MemoAttributeMetadata memoField)
			{
				return new ColumnScriptGeneratorForMemo(memoField);
			}
			if (field is MoneyAttributeMetadata moneyField)
			{
				return new ColumnScriptGeneratorForMoney(moneyField);
			}
			if (field is UniqueIdentifierAttributeMetadata pk)
			{
				return new ColumnScriptGeneratorForUniqueIdentifier(pk);
			}
			if (field is LookupAttributeMetadata)
			{
				return new ColumnScriptGeneratorForLookup();
			}

			return new ColumnScriptGeneratorForUnsupportedType(field);

		}
	}
}
