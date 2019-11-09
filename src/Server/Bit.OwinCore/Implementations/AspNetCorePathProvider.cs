using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Microsoft.AspNetCore.Hosting;

namespace Bit.OwinCore.Implementations
{
#if DotNet
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
#else
    public class AspNetCorePathProvider : DefaultPathProvider, IPathProvider
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AspNetCorePathProvider(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public override string GetCurrentAppPath()
        {
            return _webHostEnvironment.ContentRootPath;
        }
    }
#endif
}
