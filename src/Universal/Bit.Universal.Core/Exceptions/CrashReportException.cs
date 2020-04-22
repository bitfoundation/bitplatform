using Bit.Core.Contracts;
using System;
using System.Runtime.Serialization;

namespace Bit.Owin.Exceptions
{
    [Serializable]
    public class CrashReportException : AppException
    {
        public CrashReportException()
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
