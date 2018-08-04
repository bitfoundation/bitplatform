using Bit.Owin.Metadata;
using System;
using System.Runtime.Serialization;

namespace Bit.Owin.Exceptions
{
    [Serializable]
    public class DomainLogicException : AppException
    {
        public DomainLogicException()
            : this(BitMetadataBuilder.DomainLogicException)
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
