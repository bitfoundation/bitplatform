using System;
using System.Runtime.Serialization;
using Foundation.Api.Metadata;

namespace Foundation.Api.Exceptions
{
    [Serializable]
    public class ResourceNotFoundException : AppException
    {
        public ResourceNotFoundException()
            : this(FoundationMetadataBuilder.ResourceNotFoundaException)
        {

        }

        public ResourceNotFoundException(string message)
            : base(message)
        {

        }

        public ResourceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected ResourceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
