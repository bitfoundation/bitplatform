using System;
using System.Runtime.Serialization;
using Bit.Owin.Metadata;

namespace Bit.Owin.Exceptions
{
    [Serializable]
    public class ResourceNotFoundException : AppException
    {
        public ResourceNotFoundException()
            : this(BitMetadataBuilder.ResourceNotFoundaException)
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
