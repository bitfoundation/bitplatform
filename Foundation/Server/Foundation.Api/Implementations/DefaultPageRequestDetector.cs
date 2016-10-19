using Foundation.Api.Contracts;
using Microsoft.Owin;
using System;

namespace Foundation.Api.Implementations
{
    public class DefaultPageRequestDetector : IPageRequestDetector
    {
        public virtual bool IsAuthorizeRequiredPageRequest(IOwinContext owinContext)
        {
            return IsPageRequest(owinContext);
        }

        public virtual bool IsPageRequest(IOwinContext owinContext)
        {
            if (owinContext == null)
                throw new ArgumentNullException(nameof(owinContext));

            string requestPath = owinContext.Request?.Path.Value;
            return requestPath != null && requestPath == "/" || requestPath.ToLowerInvariant().Contains("-page");
        }
    }
}
