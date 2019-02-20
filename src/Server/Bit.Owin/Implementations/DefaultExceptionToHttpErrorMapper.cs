using System;
using System.Net;
using System.Reflection;
using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.Owin.Exceptions;
using Bit.Owin.Metadata;

namespace Bit.Owin.Implementations
{
    public class DefaultExceptionToHttpErrorMapper : IExceptionToHttpErrorMapper
    {
        public virtual AppEnvironment AppEnvironment { get; set; }

        protected virtual Exception UnWrapException(Exception exp)
        {
            if (exp == null)
                throw new ArgumentNullException(nameof(exp));

            if (exp is TargetInvocationException)
                return exp.InnerException;

            return exp;
        }

        public virtual string GetMessage(Exception exp)
        {
            exp = UnWrapException(exp);

            string message = BitMetadataBuilder.UnknownError;

            if (IsKnownError(exp))
                message = exp.Message;
            else if (AppEnvironment.DebugMode == true)
                message = exp.ToString();

            return message;
        }

        public virtual string GetReasonPhrase(Exception exp)
        {
            exp = UnWrapException(exp);

            string reasonPhrase = BitMetadataBuilder.UnknownError;

            if (IsKnownError(exp))
                reasonPhrase = BitMetadataBuilder.KnownError;

            return reasonPhrase;
        }

        public virtual HttpStatusCode GetStatusCode(Exception exp)
        {
            exp = UnWrapException(exp);

            if (exp is IHttpStatusCodeAwareException httpStatusCodeAwareException)
                return httpStatusCodeAwareException.StatusCode;

            return HttpStatusCode.InternalServerError;
        }

        public virtual bool IsKnownError(Exception exp)
        {
            exp = UnWrapException(exp);

            if (exp is IKnownException)
                return true;

            return false;
        }
    }
}
