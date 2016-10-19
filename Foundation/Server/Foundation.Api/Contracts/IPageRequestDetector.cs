using Microsoft.Owin;

namespace Foundation.Api.Contracts
{
    public interface IPageRequestDetector
    {
        bool IsPageRequest(IOwinContext owinContext);

        bool IsAuthorizeRequiredPageRequest(IOwinContext owinContext);
    }
}
