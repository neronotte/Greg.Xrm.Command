namespace Greg.Xrm.Command.Commands.Relationship
{
	public interface ICreateNNStrategy
	{
		Task CreateAsync(CreateNNCommand command, string currentSolutionName, int defaultLanguageCode, string publisherPrefix);
	}
}