using Prism.Events;
using Prism.Ioc;

namespace BitPrismTestsProj
{
    public class Tests
    {
        public void Test()
        {
            new EventAggregator().GetEvent<TestEvent>().Subscribe(arg => { });

            IContainerRegistry containerRegistry = null;

            containerRegistry.RegisterForNavigation(typeof(object), string.Empty);
        }
    }

    public class TestEvent : PubSubEvent<TestEvent> { }
}
