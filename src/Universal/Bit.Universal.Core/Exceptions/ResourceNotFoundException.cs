using Bit.Core.Exceptions.Contracts;
using System;
using System.Net;
using System.Runtime.Serialization;

namespace Bit.Core.Exceptions
{
    [Serializable]
    public class ResourceNotFoundException : AppException, IHttpStatusCodeAwareException
    {
        public ResourceNotFoundException()
            : this(ExceptionMessageKeys.ResourceNotFoundException)
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

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.NotFound;
    }
}
