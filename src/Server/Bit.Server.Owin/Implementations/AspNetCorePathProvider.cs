using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Bit.Owin.Implementations
{
    public class AspNetCorePathProvider : DefaultPathProvider, IPathProvider
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        protected AspNetCorePathProvider()
        {
        }

        public AspNetCorePathProvider(IWebHostEnvironment webHostEnvironment)
        {
            if (webHostEnvironment == null)
                throw new ArgumentNullException(nameof(webHostEnvironment));

            _webHostEnvironment = webHostEnvironment;
        }

        public override string GetCurrentAppPath()
        {
            return _webHostEnvironment.ContentRootPath;
        }
    }
}
