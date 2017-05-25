using System.Net;

namespace Bit.Owin.Exceptions
{
    public interface IHttpStatusCodeAwareException
    {
        HttpStatusCode StatusCode { get; set; }
    }
}
