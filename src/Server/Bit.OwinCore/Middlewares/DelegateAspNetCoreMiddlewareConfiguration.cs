using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;
using System;

namespace Bit.OwinCore.Middlewares
{
    public class DelegateAspNetCoreMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        private readonly Action<IApplicationBuilder> _aspNetCoreAppCustomizer;

        public DelegateAspNetCoreMiddlewareConfiguration(Action<IApplicationBuilder> aspNetCoreAppCustomizer)
        {
            _aspNetCoreAppCustomizer = aspNetCoreAppCustomizer ?? throw new ArgumentNullException(nameof(aspNetCoreAppCustomizer));
        }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            _aspNetCoreAppCustomizer(aspNetCoreApp);
        }
    }
}
