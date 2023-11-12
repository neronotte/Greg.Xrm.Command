using System.Diagnostics;
using System.Runtime.Serialization;

namespace Greg.Xrm.Command
{
    [DebuggerDisplay("{Message}")]
    [Serializable]
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

        protected CommandException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ErrorCode = info.GetInt32(nameof(ErrorCode));
        }

        public int ErrorCode { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ErrorCode), ErrorCode);
        }




        public const int ConnectionNotSet = 100001;
        public const int ConnectionInvalid = 100002;
        
        public const int CommandRequiredArgumentNotProvided = 200001;
		public const int CommandInvalidArgumentType = 200002;
		public const int CommandInvalidArgumentValue = 200003;
        
        public const int DuplicateOption = 300001;

        public const int XrmError = 900001;

	}
}
