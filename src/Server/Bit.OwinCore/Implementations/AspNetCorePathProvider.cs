using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Microsoft.AspNetCore.Hosting;

namespace Bit.OwinCore.Implementations
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
