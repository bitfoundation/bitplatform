using System;
using System.Runtime.Serialization;

namespace Bit.Owin.Exceptions
{
    [Serializable]
    public class LoginFailureException : AppException
    {
        public LoginFailureException()
        {
        }

        public LoginFailureException(string message)
            : base(message)
        {
        }

        public LoginFailureException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected LoginFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
