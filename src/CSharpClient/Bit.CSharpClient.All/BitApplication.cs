using Autofac;
using Bit.Model.Events;
using Bit.ViewModel;
using Plugin.Connectivity.Abstractions;
using Prism;
using Prism.Autofac;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit
{
    public abstract class BitApplication : PrismApplication
    {
        protected BitApplication(IPlatformInitializer platformInitializer = null)
            : base(platformInitializer)
        {
            if (MainPage == null)
                MainPage = new ContentPage { Title = "DefaultPage" };
        }

        protected async sealed override void OnInitialized()
        {
            try
            {
                await OnInitializedAsync().ConfigureAwait(false);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        protected virtual Task OnInitializedAsync()
        {
            IConnectivity connectivity = Container.Resolve<IConnectivity>();

            IEventAggregator eventAggregator = Container.Resolve<IEventAggregator>();

            connectivity.ConnectivityChanged += (sender, e) =>
            {
                eventAggregator.GetEvent<ConnectivityChangedEvent>()
                    .Publish(new ConnectivityChangedEvent { IsConnected = e.IsConnected });
            };

            return Task.CompletedTask;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.GetBuilder().Register<IContainerProvider>(c => Container).SingleInstance().PreserveExistingDefaults();
        }
    }
}
