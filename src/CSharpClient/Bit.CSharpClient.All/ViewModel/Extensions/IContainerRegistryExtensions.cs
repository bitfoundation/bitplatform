using Bit.ViewModel;
using System;
using Xamarin.Forms;

namespace Prism.Ioc
{
    public static class IContainerRegistryExtensions
    {
        public static void RegisterNavigation<TView, TViewModel>(this IContainerRegistry containerRegistry, string name)
            where TView : Page
            where TViewModel : BitViewModelBase
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            containerRegistry.RegisterForNavigation<TView, TViewModel>(name: name);
            containerRegistry.Register<TViewModel>();
            containerRegistry.Register<TView>();
        }

        public static void RegisterNavigation<TView>(this IContainerRegistry containerRegistry, string name)
            where TView : Page
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            containerRegistry.RegisterForNavigation<TView>(name: name);
            containerRegistry.Register<TView>();
        }

        public static void RegisterNavigation(this IContainerRegistry containerRegistry, Type viewType, string name)
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            containerRegistry.RegisterForNavigation(viewType: viewType, name: name);
            containerRegistry.Register(viewType);
        }

        public static void RegisterNavigationOnIdiom<TView, TViewModel>(this IContainerRegistry containerRegistry, string name, Type desktopView = null, Type tabletView = null, Type phoneView = null)
            where TView : Page
            where TViewModel : BitViewModelBase
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            containerRegistry.RegisterForNavigationOnIdiom<TView, TViewModel>(name: name, desktopView: desktopView, tabletView: tabletView, phoneView: phoneView);
            containerRegistry.Register<TViewModel>();
            containerRegistry.Register<TView>();
            if (desktopView != null)
                containerRegistry.Register(desktopView);
            if (tabletView != null)
                containerRegistry.Register(tabletView);
            if (phoneView != null)
                containerRegistry.Register(phoneView);
        }

        public static void RegisterNavigationOnPlatform<TView, TViewModel>(this IContainerRegistry containerRegistry, params IPlatform[] platforms)
            where TView : Page
            where TViewModel : BitViewModelBase
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            containerRegistry.RegisterForNavigationOnPlatform<TView, TViewModel>(platforms: platforms);
            containerRegistry.Register<TViewModel>();
            containerRegistry.Register<TView>();
        }

        public static void RegisterNavigationOnPlatform<TView, TViewModel>(this IContainerRegistry containerRegistry, string name, params IPlatform[] platforms)
            where TView : Page
            where TViewModel : BitViewModelBase
        {
            if (containerRegistry == null)
                throw new ArgumentNullException(nameof(containerRegistry));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            containerRegistry.RegisterForNavigationOnPlatform<TView, TViewModel>(name: name, platforms: platforms);
            containerRegistry.Register<TViewModel>();
            containerRegistry.Register<TView>();
        }
    }
}
