using System;
using System.Runtime.Serialization;
using System.Net;

namespace Bit.Owin.Exceptions
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
