using System;
using System.Runtime.Serialization;
using Foundation.Api.Metadata;

namespace Foundation.Api.Exceptions
{
    [Serializable]
    public class DomainLogicException : AppException
    {
        public DomainLogicException()
            : this(FoundationMetadataBuilder.DomainLogicException)
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
