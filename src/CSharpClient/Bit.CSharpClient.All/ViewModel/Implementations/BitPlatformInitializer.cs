#if iOS

using Autofac;
using Bit.ViewModel.Contracts;
using Prism;
using Prism.Autofac;
using Prism.Ioc;

namespace Bit.ViewModel.Implementations
{
    public class BitPlatformInitializer : IPlatformInitializer
    {
        public virtual void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.GetBuilder().RegisterType<DefaultBrowserService>().As<IBrowserService>().PreserveExistingDefaults();
        }
    }
}

#elif Android

using Android.App;
using Android.Content;
using Autofac;
using Bit.ViewModel.Contracts;
using Prism;
using Prism.Autofac;
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

        public virtual void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            containerBuilder.Register(c => (Activity)_activity).SingleInstance().PreserveExistingDefaults();
            containerBuilder.Register(c => (Context)_activity).SingleInstance().PreserveExistingDefaults();

            containerBuilder.RegisterType<DefaultBrowserService>().As<IBrowserService>().PreserveExistingDefaults();
        }
    }
}

#else

using Autofac;
using Bit.ViewModel.Contracts;
using Prism;
using Prism.Autofac;
using Prism.Ioc;

namespace Bit.ViewModel.Implementations
{
    public class BitPlatformInitializer : IPlatformInitializer
    {
        public virtual void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.GetBuilder().RegisterType<DefaultBrowserService>().As<IBrowserService>().PreserveExistingDefaults();
        }
    }
}

#endif