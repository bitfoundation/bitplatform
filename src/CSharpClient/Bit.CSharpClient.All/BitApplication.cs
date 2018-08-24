using Autofac;
using Bit.Model.Events;
using Bit.View;
using Bit.ViewModel;
using Prism;
using Prism.Autofac;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
#if !WPF
using Xamarin.Essentials;
#endif
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

        protected async sealed override void OnInitialized()
        {
            try
            {
                await OnInitializedAsync().ConfigureAwait(false);
                await Task.Yield();
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
            }
        }

        protected virtual Task OnInitializedAsync()
        {
#if !WPF
            Connectivity.ConnectivityChanged += (sender, e) =>
            {
                _eventAggregator.Value.GetEvent<ConnectivityChangedEvent>()
                    .Publish(new ConnectivityChangedEvent { IsConnected = e.NetworkAccess != NetworkAccess.None });
            };
#else
            System.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += (sender, e) =>
            {
                _eventAggregator.Value.GetEvent<ConnectivityChangedEvent>()
                   .Publish(new ConnectivityChangedEvent { IsConnected = e.IsAvailable });
            };
#endif

            return Task.CompletedTask;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.GetBuilder().Register<IContainerProvider>(c => Container).SingleInstance().PreserveExistingDefaults();
        }
    }
}
