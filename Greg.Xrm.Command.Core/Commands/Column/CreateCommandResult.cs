namespace Greg.Xrm.Command.Commands.Column
{
	public class CreateCommandResult : CommandResult
	{
		public CreateCommandResult(Guid attributeId) : base(true)
		{
			this["Attribute ID"] = attributeId;
		}
	}
}
