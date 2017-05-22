using System.Net;

namespace Foundation.Api.Exceptions
{
    public interface IHttpStatusCodeAwareException
    {
        HttpStatusCode StatusCode { get; set; }
    }
}
