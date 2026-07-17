namespace Greg.Xrm.Command.Commands.Data.RecordPayload.ValueConverters
{
	/// <summary>
	/// Sentinel value returned by converters for field types that cannot be set via the API
	/// (e.g. File and Image fields). The RecordPayloadProcessor recognises this type and
	/// emits a warning instead of adding the attribute to the entity.
	/// </summary>
	public sealed class SkippedFieldValue
	{
		private SkippedFieldValue() { }

		public static SkippedFieldValue Instance { get; } = new SkippedFieldValue();
	}
}
