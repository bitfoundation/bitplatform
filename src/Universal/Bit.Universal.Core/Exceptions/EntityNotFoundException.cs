using Bit.Core.Exceptions.Contracts;
using System;
using System.Net;
using System.Runtime.Serialization;

namespace Bit.Core.Exceptions
{
    [Serializable]
    public class EntityNotFoundException : AppException, IHttpStatusCodeAwareException
    {
        public EntityNotFoundException()
            : this(ExceptionMessageKeys.EntityNotFoundException)
        {

        }

        public EntityNotFoundException(string message)
            : base(message)
        {

        }

        public EntityNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected EntityNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.NotFound;
    }
}
