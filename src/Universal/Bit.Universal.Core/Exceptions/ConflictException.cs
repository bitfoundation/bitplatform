using System;
using System.Net;
using System.Runtime.Serialization;
using Bit.Core.Exceptions.Contracts;

namespace Bit.Core.Exceptions
{
    [Serializable]
    public class ConflictException : AppException, IHttpStatusCodeAwareException
    {
        public ConflictException()
            : this(ExceptionMessageKeys.ConflictException)
        {

        }

        public ConflictException(string message)
            : base(message)
        {

        }

        public ConflictException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected ConflictException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Conflict;
    }
}
