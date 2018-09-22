using Autofac;
using Bit.Model.Events;
using Bit.View;
using Bit.ViewModel;
using Prism;
using Prism.Autofac;
using Prism.Events;
using Prism.Ioc;
using Prism.Plugin.Popups;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Bit
{
    public abstract class BitApplication : PrismApplication
    {
        protected BitApplication(IPlatformInitializer platformInitializer = null)
            : base(platformInitializer)
        {
            _eventAggregator = new Lazy<IEventAggregator>(() =>
            {
                return Container.Resolve<IEventAggregator>();
            }, isThreadSafe: true);

            if (MainPage == null)
                MainPage = new ContentPage { Title = "DefaultPage" };
        }

        protected override void OnPropertyChanging([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanging(propertyName);

            if (propertyName == nameof(MainPage) && MainPage != null)
            {
                MainPage.SizeChanged -= MainPage_SizeChanged;
            }
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(MainPage) && MainPage != null)
            {
                MainPage.SizeChanged += MainPage_SizeChanged;
            }
        }

        private readonly Lazy<IEventAggregator> _eventAggregator;

        private void MainPage_SizeChanged(object sender, EventArgs e)
        {
            PublishOrientationAndOrSizeChangedEvent();
        }

        protected virtual void PublishOrientationAndOrSizeChangedEvent()
        {
            double width = MainPage.Width;
            double height = MainPage.Height;

            if (width == 0 || height == 0)
                return;

            DeviceOrientation newOrientation = (width < height) ? DeviceOrientation.Portrait : DeviceOrientation.Landscape;

            _eventAggregator?.Value.GetEvent<OrientationAndOrSizeChanged>().Publish(new OrientationAndOrSizeChanged
            {
                NewOrientation = newOrientation,
                NewWidth = width,
                NewHeight = height
            });
        }

        protected sealed override async void OnInitialized()
        {
            try
            {
                await OnInitializedAsync();
                await Task.Yield();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        protected virtual Task OnInitializedAsync()
        {
            Connectivity.ConnectivityChanged += (sender, e) =>
            {
                _eventAggregator.Value.GetEvent<ConnectivityChangedEvent>()
                    .Publish(new ConnectivityChangedEvent { IsConnected = e.NetworkAccess != NetworkAccess.None });
            };

            return Task.CompletedTask;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ContainerBuilder containerBuilder = containerRegistry.GetBuilder();

            containerBuilder.RegisterCallback(componentRegistry =>
            {
                componentRegistry.Registered += (_, registeredArgs) =>
                {
                    registeredArgs.ComponentRegistration.Activated += (__, activatedArgs) =>
                    {
                        if (activatedArgs.Instance is BitViewModelBase)
                            activatedArgs.Context.InjectUnsetProperties(activatedArgs.Instance, activatedArgs.Parameters);
                    };
                };
            });

            containerBuilder.Register(c => Container).SingleInstance().PreserveExistingDefaults();
            containerBuilder.Register(c => Container.GetContainer()).PreserveExistingDefaults();
            containerRegistry.RegisterPopupNavigationService();
        }
    }
}
