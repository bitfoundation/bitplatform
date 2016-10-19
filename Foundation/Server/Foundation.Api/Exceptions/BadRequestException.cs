using System;
using System.Net;
using System.Runtime.Serialization;
using Foundation.Api.Metadata;

namespace Foundation.Api.Exceptions
{
    [Serializable]
    public class BadRequestException : AppException
    {
        public BadRequestException()
            : this(FoundationMetadataBuilder.BadRequestException)
        {

        }

        public BadRequestException(string message)
            : base(message)
        {

        }

        public BadRequestException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected BadRequestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
