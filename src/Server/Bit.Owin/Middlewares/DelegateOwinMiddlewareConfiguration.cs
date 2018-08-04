using Bit.Owin.Contracts;
using Owin;
using System;

namespace Bit.Owin.Middlewares
{
    public class DelegateOwinMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        protected DelegateOwinMiddlewareConfiguration()
        {

        }

        private readonly Action<IAppBuilder> _owinAppCustomizer;

        public DelegateOwinMiddlewareConfiguration(Action<IAppBuilder> owinAppCustomizer)
        {
            _owinAppCustomizer = owinAppCustomizer ?? throw new ArgumentNullException(nameof(owinAppCustomizer));
        }

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            _owinAppCustomizer(owinApp);
        }
    }
}
