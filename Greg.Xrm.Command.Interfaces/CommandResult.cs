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
		public int ExitCode { get; private init; } = -1;



		public static CommandResult Success()
		{
			return new CommandResult(true) { ExitCode = 0 };
		}

		public static CommandResult Fail(string message, Exception? exception = null)
		{
			return new CommandResult(false, message, exception);
		}

		public static CommandResult Fail(string message, int exitCode, Exception? exception = null)
		{
			return new CommandResult(false, message, exception) { ExitCode = exitCode };
		}
	}
}
