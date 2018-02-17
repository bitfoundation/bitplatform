using System;
using System.Runtime.Serialization;

namespace Bit.Owin.Exceptions
{
    [Serializable]
    public class AppException : Exception, IKnownException
    {
        public AppException()
        {
        }

        public AppException(string message) : base(message)
        {
        }

        public AppException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AppException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
