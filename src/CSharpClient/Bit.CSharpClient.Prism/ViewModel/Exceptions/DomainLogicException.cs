using System;
using System.Runtime.Serialization;

namespace Bit.ViewModel.Exceptions
{
    [Serializable]
    public class DomainLogicException : AppException
    {
        public DomainLogicException()
            : this(nameof(DomainLogicException))
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
