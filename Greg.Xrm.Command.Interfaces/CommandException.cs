using System.Diagnostics;

namespace Greg.Xrm.Command
{
	[DebuggerDisplay("{Message}")]
    public class CommandException : Exception
    {
        public CommandException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public CommandException(int errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }


        public int ErrorCode { get; }





        public const int ConnectionNotSet = 100001;
        public const int ConnectionInvalid = 100002;
        
        public const int CommandRequiredArgumentNotProvided = 200001;
		public const int CommandInvalidArgumentType = 200002;
		public const int CommandInvalidArgumentValue = 200003;
        public const int CommandCannotBeCreated = 200004;

		public const int DuplicateCommand = 300001;
		public const int DuplicateOption = 300002;

		public const int XrmError = 900001;

	}
}
