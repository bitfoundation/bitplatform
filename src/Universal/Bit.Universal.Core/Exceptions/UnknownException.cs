using System;
using System.Runtime.Serialization;

namespace Bit.Core.Exceptions
{
    [Serializable]
    public class UnknownException : ApplicationException
    {
        public UnknownException()
        {
        }

        public UnknownException(string message)
            : base(message)
        {
        }

        public UnknownException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UnknownException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
