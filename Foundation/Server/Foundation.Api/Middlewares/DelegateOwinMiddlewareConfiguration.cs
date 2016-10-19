using Foundation.Api.Contracts;
using System;
using Owin;

namespace Foundation.Api.Middlewares
{
    public class DelegateOwinMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        private readonly Action<IAppBuilder> _owinAppCustomizer;

        public DelegateOwinMiddlewareConfiguration(Action<IAppBuilder> owinAppCustomizer)
        {
            if (owinAppCustomizer == null)
                throw new ArgumentNullException(nameof(owinAppCustomizer));

            _owinAppCustomizer = owinAppCustomizer;
        }

        protected DelegateOwinMiddlewareConfiguration()
        {

        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            _owinAppCustomizer(owinApp);
        }
    }
}
