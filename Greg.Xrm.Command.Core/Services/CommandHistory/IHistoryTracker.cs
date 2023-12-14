namespace Greg.Xrm.Command.Services.CommandHistory
{
	public interface IHistoryTracker
	{
		Task SetMaxLengthAsync(int maxLength);


		Task AddAsync(params string[] command);

		Task<IReadOnlyList<string>> GetLastAsync(int? count);

		Task ClearAsync();
	}

}
