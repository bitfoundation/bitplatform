using Prism.Events;
using System;
using System.Reactive.Subjects;

namespace SubscribeAsyncTestProj
{
    public class Tests
    {
        public void Test()
        {
            new Subject<string>().Subscribe((arg) => { });

            new EventAggregator().GetEvent<TestEvent>().Subscribe(arg => { });
        }
    }

    public class TestEvent : PubSubEvent<TestEvent> { }
}
