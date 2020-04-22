using System;
using System.Net;
using System.Runtime.Serialization;

namespace Bit.Owin.Exceptions
{
    [Serializable]
    public class UnauthorizedException : AppException, IHttpStatusCodeAwareException
    {
        public UnauthorizedException()
            : this(ExceptionMessageKeys.UnauthorizedException)
        {

        }

        public UnauthorizedException(string message)
            : base(message)
        {

        }

        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected UnauthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Unauthorized;
    }
}
