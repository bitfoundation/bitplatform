using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using Bit.Owin.Exceptions;
using Bit.Owin.Metadata;
using System;
using System.Net;
using System.Reflection;

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

            if (IsKnownError(exp))
                return exp.Message;
            if (AppEnvironment.DebugMode)
                return exp.ToString();
            return BitMetadataBuilder.UnKnownError;
        }

        public virtual string GetReasonPhrase(Exception exp)
        {
            exp = UnWrapException(exp);

            string reasonPhrase = BitMetadataBuilder.UnKnownError;

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
            return exp is IKnownException;
        }
    }
}
