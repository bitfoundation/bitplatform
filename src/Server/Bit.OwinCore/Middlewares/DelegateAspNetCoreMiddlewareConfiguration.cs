using System;
using Bit.OwinCore.Contracts;
using Microsoft.AspNetCore.Builder;

namespace Bit.OwinCore.Middlewares
{
    public class DelegateAspNetCoreMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        public virtual MiddlewarePosition MiddlewarePosition { get; set; } = MiddlewarePosition.BeforeOwinMiddlewares;

        private readonly Action<IApplicationBuilder> _aspNetCoreAppCustomizer;

        public DelegateAspNetCoreMiddlewareConfiguration(Action<IApplicationBuilder> aspNetCoreAppCustomizer)
        {
            if (aspNetCoreAppCustomizer == null)
                throw new ArgumentNullException(nameof(aspNetCoreAppCustomizer));

            _aspNetCoreAppCustomizer = aspNetCoreAppCustomizer;
        }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            _aspNetCoreAppCustomizer(aspNetCoreApp);
        }
    }
}
