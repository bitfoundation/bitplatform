using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;

namespace Bit.Owin.Implementations
{
    public class AspNetCorePathProvider : DefaultPathProvider, IPathProvider
    {
        private readonly IHostEnvironment _HostEnvironment;

        protected AspNetCorePathProvider()
        {
        }

        public AspNetCorePathProvider(IHostEnvironment hostEnvironment)
        {
            if (hostEnvironment == null)
                throw new ArgumentNullException(nameof(hostEnvironment));

            _HostEnvironment = hostEnvironment;
        }

        public override string GetCurrentAppPath()
        {
            return _HostEnvironment.ContentRootPath;
        }
    }
}
