using System;
using System.Net;

namespace Foundation.Api.Contracts
{
    public interface IExceptionToHttpErrorMapper
    {
        bool IsKnownError(Exception exp);

        string GetMessage(Exception exp);

        HttpStatusCode GetStatusCode(Exception exp);

        string GetReasonPhrase(Exception exp);
    }
}
