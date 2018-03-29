#if iOS

using Bit.ViewModel.Contracts;
using Prism;
using Prism.Ioc;

namespace Bit.ViewModel.Implementations
{
    public class BitPlatformInitializer : IPlatformInitializer
    {
        public virtual void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IBrowserService, DefaultBrowserService>();
        }
    }
}

#elif Android

using Android.App;
using Android.Content;
using Bit.ViewModel.Contracts;
using Prism;
using Prism.Ioc;
using System;

namespace Bit.ViewModel.Implementations
{
    public class BitPlatformInitializer : IPlatformInitializer
    {
        private readonly Activity _activity;

        public BitPlatformInitializer(Activity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            _activity = activity;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance<Activity>(_activity);
            containerRegistry.RegisterInstance<Context>(_activity);

            containerRegistry.Register<IBrowserService, DefaultBrowserService>();
        }
    }
}

#else

using Bit.ViewModel.Contracts;
using Prism;
using Prism.Ioc;

namespace Bit.ViewModel.Implementations
{
    public class BitPlatformInitializer : IPlatformInitializer
    {
        public virtual void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IBrowserService, DefaultBrowserService>();
        }
    }
}

#endif