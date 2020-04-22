using Bit.Core.Exceptions.Contracts;
using System;
using System.Net;
using System.Runtime.Serialization;

namespace Bit.Core.Exceptions
{
    [Serializable]
    public class ForbiddenException : AppException, IHttpStatusCodeAwareException
    {
        public ForbiddenException()
            : this(ExceptionMessageKeys.ForbiddenException)
        {

        }

        public ForbiddenException(string message)
            : base(message)
        {

        }

        public ForbiddenException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected ForbiddenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Forbidden;
    }
}
