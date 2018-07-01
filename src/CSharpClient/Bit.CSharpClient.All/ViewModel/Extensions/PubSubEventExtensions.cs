using Bit.ViewModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Prism.Events
{
    public static class PubSubEventExtensions
    {
        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action)
        {
            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload).ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            });
        }

        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action, ThreadOption threadOption)
        {
            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload).ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, threadOption: threadOption);
        }

        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action, bool keepSubscriberReferenceAlive)
        {
            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload).ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, keepSubscriberReferenceAlive: keepSubscriberReferenceAlive);
        }

        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
        {
            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload).ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, threadOption: threadOption, keepSubscriberReferenceAlive: keepSubscriberReferenceAlive);
        }

        public static SubscriptionToken SubscribeAsync<TPayload>(this PubSubEvent<TPayload> pubSubEvent, Func<TPayload, Task> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive, Predicate<TPayload> filter)
        {
            return pubSubEvent.Subscribe(async (payload) =>
            {
                try
                {
                    await action(payload).ConfigureAwait(false);
                }
                catch (Exception exp)
                {
                    BitExceptionHandler.Current.OnExceptionReceived(exp);
                }
            }, threadOption: threadOption, keepSubscriberReferenceAlive: keepSubscriberReferenceAlive, filter: filter);
        }
    }
}
