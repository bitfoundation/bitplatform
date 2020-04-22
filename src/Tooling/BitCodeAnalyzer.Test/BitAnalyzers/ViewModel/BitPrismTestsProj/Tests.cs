using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Prism.Events;
using Prism.Ioc;
using Prism.Navigation;
using Prism.Services;

namespace BitPrismTestsProj
{
    public class Tests
    {
        public void Test()
        {
            new EventAggregator().GetEvent<TestEvent>().Subscribe(arg => { });

            IContainerRegistry containerRegistry = null;

            INavigationService navgationService = null;

            containerRegistry.RegisterForNavigation(typeof(object), string.Empty);

            IDeviceService devService = null;

            devService.BeginInvokeOnMainThread(() => { });

            AppCenter.Configure(appSecret: "");
            Crashes.TrackError(exception: null);
            Analytics.TrackEvent(name: "");
        }
    }

    public class TestEvent : PubSubEvent<TestEvent> { }
}
