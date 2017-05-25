using System;
using System.Runtime.Serialization;
using Bit.Owin.Metadata;

namespace Bit.Owin.Exceptions
{
    [Serializable]
    public class BadRequestException : AppException
    {
        public BadRequestException()
            : this(BitMetadataBuilder.BadRequestException)
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
