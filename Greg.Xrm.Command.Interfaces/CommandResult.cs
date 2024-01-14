namespace Greg.Xrm.Command
{
    public class CommandResult : Dictionary<string, object>
	{
        protected CommandResult(bool isSuccess, string? errorMessage = null, Exception? exception = null)
		{
			this.IsSuccess = isSuccess;
			this.ErrorMessage = errorMessage ?? string.Empty;
			this.Exception = exception;
		}


		public bool IsSuccess { get; protected set; }
		public string ErrorMessage { get; protected set; }
		public Exception? Exception { get; protected set; }



		public static CommandResult Success()
		{
			return new CommandResult(true);
		}

		public static CommandResult Fail(string message, Exception? exception = null)
		{
			return new CommandResult(false, message, exception);
		}
	}
}
