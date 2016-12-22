using Foundation.AspNetCore.Contracts;
using Microsoft.AspNetCore.Builder;
using System;

namespace Foundation.AspNetCore.Middlewares
{
    public class DelegateAspNetCoreMiddlewareConfiguration : IAspNetCoreMiddlewareConfiguration
    {
        private readonly Action<IApplicationBuilder> _aspNetCoreAppCustomizer;
        private readonly RegisterKind _registerKind;

        public DelegateAspNetCoreMiddlewareConfiguration(Action<IApplicationBuilder> aspNetCoreAppCustomizer, RegisterKind registerKind)
        {
            if (aspNetCoreAppCustomizer == null)
                throw new ArgumentNullException(nameof(aspNetCoreAppCustomizer));

            _aspNetCoreAppCustomizer = aspNetCoreAppCustomizer;
            _registerKind = registerKind;
        }

        public virtual void Configure(IApplicationBuilder aspNetCoreApp)
        {
            if (aspNetCoreApp == null)
                throw new ArgumentNullException(nameof(aspNetCoreApp));

            _aspNetCoreAppCustomizer(aspNetCoreApp);
        }

        public virtual RegisterKind GetRegisterKind()
        {
            return _registerKind;
        }
    }
}
