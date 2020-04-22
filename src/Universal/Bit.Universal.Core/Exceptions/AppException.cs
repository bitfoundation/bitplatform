using Bit.Core.Contracts;
using Bit.ViewModel.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Bit.Owin.Exceptions
{
    [Serializable]
    public class AppException : ApplicationException, IKnownException, IExceptionData
    {
        public AppException()
        {
        }

        public AppException(string message)
            : base(message)
        {
        }

        public AppException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AppException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public virtual IDictionary<string, string?> Items { get; set; } = new Dictionary<string, string?> { };
    }
}
