using System;
using System.Runtime.Serialization;

namespace Bit.Core.Exceptions
{
    [Serializable]
    public class DomainLogicException : AppException
    {
        public DomainLogicException()
            : this(ExceptionMessageKeys.DomainLogicException)
        {
        }

        public DomainLogicException(string message)
            : base(message)
        {
        }

        public DomainLogicException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected DomainLogicException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
