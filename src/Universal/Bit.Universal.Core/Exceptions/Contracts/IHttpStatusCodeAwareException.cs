using System.Net;

namespace Bit.Core.Exceptions.Contracts
{
    public interface IHttpStatusCodeAwareException
    {
        HttpStatusCode StatusCode { get; set; }
    }
}
