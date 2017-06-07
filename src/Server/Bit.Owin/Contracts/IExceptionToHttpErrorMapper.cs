using System;
using System.Net;

namespace Bit.Owin.Contracts
{
    public interface IExceptionToHttpErrorMapper
    {
        bool IsKnownError(Exception exp);

        string GetMessage(Exception exp);

        HttpStatusCode GetStatusCode(Exception exp);

        string GetReasonPhrase(Exception exp);
    }
}
