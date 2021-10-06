using Bit.Core.Implementations;
using System;
using System.Threading.Tasks;

namespace Prism.Events
{
    public static class PubSubEventExtensions
    {
        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action)
        {
            if (pubSubEvent == null)
                throw new ArgumentNullException(nameof(pubSubEvent));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload);
                }
                catch (Exception exp)
                {
                    BitExceptionHandlerBase.Current?.OnExceptionReceived(exp);
                }
            });
        }

        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action, ThreadOption threadOption)
        {
            if (pubSubEvent == null)
                throw new ArgumentNullException(nameof(pubSubEvent));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

#if !Android && !iOS && !UWP && !Mac
            if (pubSubEvent.SynchronizationContext == null && threadOption == ThreadOption.UIThread)
                threadOption = ThreadOption.PublisherThread;
#endif

            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload);
                }
                catch (Exception exp)
                {
                    BitExceptionHandlerBase.Current?.OnExceptionReceived(exp);
                }
            }, threadOption: threadOption);
        }

        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action, bool keepSubscriberReferenceAlive)
        {
            if (pubSubEvent == null)
                throw new ArgumentNullException(nameof(pubSubEvent));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload);
                }
                catch (Exception exp)
                {
                    BitExceptionHandlerBase.Current?.OnExceptionReceived(exp);
                }
            }, keepSubscriberReferenceAlive: keepSubscriberReferenceAlive);
        }

        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
        {
            if (pubSubEvent == null)
                throw new ArgumentNullException(nameof(pubSubEvent));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

#if !Android && !iOS && !UWP && !Mac
            if (pubSubEvent.SynchronizationContext == null && threadOption == ThreadOption.UIThread)
                threadOption = ThreadOption.PublisherThread;
#endif

            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload);
                }
                catch (Exception exp)
                {
                    BitExceptionHandlerBase.Current?.OnExceptionReceived(exp);
                }
            }, threadOption: threadOption, keepSubscriberReferenceAlive: keepSubscriberReferenceAlive);
        }

        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive, Predicate<TPayload> filter)
        {
            if (pubSubEvent == null)
                throw new ArgumentNullException(nameof(pubSubEvent));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

#if !Android && !iOS && !UWP && !Mac
            if (pubSubEvent.SynchronizationContext == null && threadOption == ThreadOption.UIThread)
                threadOption = ThreadOption.PublisherThread;
#endif

            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload);
                }
                catch (Exception exp)
                {
                    BitExceptionHandlerBase.Current?.OnExceptionReceived(exp);
                }
            }, threadOption: threadOption, keepSubscriberReferenceAlive: keepSubscriberReferenceAlive, filter: filter);
        }
    }
}
