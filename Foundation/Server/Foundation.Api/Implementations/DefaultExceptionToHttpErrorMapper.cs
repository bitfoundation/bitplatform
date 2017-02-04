using Foundation.Api.Contracts;
using Foundation.Api.Exceptions;
using Foundation.Api.Metadata;
using Foundation.Core.Contracts;
using Foundation.Core.Models;
using System;
using System.Net;
using System.Reflection;

namespace Foundation.Api.Implementations
{
    public class DefaultExceptionToHttpErrorMapper : IExceptionToHttpErrorMapper
    {
        private readonly AppEnvironment _activeAppEnvironment;

        public DefaultExceptionToHttpErrorMapper(IAppEnvironmentProvider appEnvironmentProvider)
        {
            if (appEnvironmentProvider == null)
                throw new ArgumentNullException(nameof(appEnvironmentProvider));

            _activeAppEnvironment = appEnvironmentProvider.GetActiveAppEnvironment();
        }

        protected DefaultExceptionToHttpErrorMapper()
        {

        }

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

            string message = FoundationMetadataBuilder.UnKnownError;

            if (IsKnownError(exp))
                message = exp.Message;

            if (_activeAppEnvironment.DebugMode == true)
                message = exp.ToString();

            return message;
        }

        public virtual string GetReasonPhrase(Exception exp)
        {
            exp = UnWrapException(exp);

            string reasonPhrase = FoundationMetadataBuilder.UnKnownError;

            if (IsKnownError(exp))
                reasonPhrase = FoundationMetadataBuilder.KnownError;

            return reasonPhrase;
        }

        public virtual HttpStatusCode GetStatusCode(Exception exp)
        {
            exp = UnWrapException(exp);

            if (exp is IHttpStatusCodeAwareException)
                return ((IHttpStatusCodeAwareException)exp).StatusCode;
            if (exp is BadRequestException)
                return HttpStatusCode.BadRequest;
            else if (exp is ResourceNotFoundException)
                return HttpStatusCode.NotFound;

            return HttpStatusCode.InternalServerError;
        }

        public virtual bool IsKnownError(Exception exp)
        {
            exp = UnWrapException(exp);

            if (exp is IKnwoException)
                return true;

            return false;
        }
    }
}
