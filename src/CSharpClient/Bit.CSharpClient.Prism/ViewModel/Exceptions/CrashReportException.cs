using System;
using System.Runtime.Serialization;

namespace Bit.ViewModel.Exceptions
{
    public class CrashReportException : AppException
    {
        public CrashReportException()
            : this(nameof(CrashReportException))
        {
        }

        public CrashReportException(string message)
            : base(message)
        {
        }

        public CrashReportException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected CrashReportException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
