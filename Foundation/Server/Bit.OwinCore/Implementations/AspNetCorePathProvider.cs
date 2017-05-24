using Foundation.Api.Implementations;
using Foundation.Core.Contracts;
using Microsoft.AspNetCore.Hosting;

namespace Foundation.AspNetCore.Implementations
{
    public class AspNetCorePathProvider : DefaultPathProvider, IPathProvider
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public AspNetCorePathProvider(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public override string GetCurrentAppPath()
        {
            return _hostingEnvironment.ContentRootPath;
        }
    }
}
