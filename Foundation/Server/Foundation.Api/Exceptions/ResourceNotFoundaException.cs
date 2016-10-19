using System;
using System.Net;
using System.Runtime.Serialization;
using Foundation.Api.Metadata;

namespace Foundation.Api.Exceptions
{
    [Serializable]
    public class ResourceNotFoundaException : AppException
    {
        public ResourceNotFoundaException()
            : this(FoundationMetadataBuilder.ResourceNotFoundaException)
        {

        }

        public ResourceNotFoundaException(string message)
            : base(message)
        {

        }

        public ResourceNotFoundaException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected ResourceNotFoundaException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
