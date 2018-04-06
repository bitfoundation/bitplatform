using Autofac;
using Bit.Model.Events;
using Plugin.Connectivity.Abstractions;
using Prism;
using Prism.Autofac;
using Prism.Events;
using Prism.Ioc;

namespace Bit
{
    public abstract class BitApplication : PrismApplication
    {
        protected BitApplication(IPlatformInitializer platformInitializer = null)
            : base(platformInitializer)
        {

        }

        protected override void OnInitialized()
        {
            IConnectivity connectivity = Container.Resolve<IConnectivity>();

            IEventAggregator eventAggregator = Container.Resolve<IEventAggregator>();

            connectivity.ConnectivityChanged += (sender, e) =>
            {
                eventAggregator.GetEvent<ConnectivityChangedEvent>()
                    .Publish(new ConnectivityChangedEvent { IsConnected = e.IsConnected });
            };
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.GetBuilder().Register<IContainerProvider>(c => Container).SingleInstance().PreserveExistingDefaults();
        }
    }
}
