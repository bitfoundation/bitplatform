using System.Runtime.CompilerServices;
using Boilerplate.Client.Core.Infrastructure.Services;

namespace Boilerplate.Tests.Features.PubSub;

/// <summary>
/// Unit tests for <see cref="PubSubService"/> - the weak-reference publish/subscribe hub. They cover message delivery,
/// payload passing, unsubscribe, persistent (published-before-subscribed) messages, static handlers, and the
/// weak-reference contract that lets a collected subscriber's subscription silently drop out.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class PubSubServiceTests
{
    private const string Message = "test-message";
    private const string OtherMessage = "other-message";

    // The service only touches its IServiceProvider inside the faulted-task path (to resolve the exception handler),
    // so a bare fake is enough for every deterministic, non-faulting scenario exercised here.
    private static PubSubService CreatePubSubService() => new(A.Fake<IServiceProvider>());

    [TestMethod]
    public void Publish_Should_InvokeSubscribedHandlerWithPayload()
    {
        var pubSub = CreatePubSubService();
        object? received = null;
        var invoked = false;

        pubSub.Subscribe(Message, payload =>
        {
            invoked = true;
            received = payload;
            return Task.CompletedTask;
        });

        pubSub.Publish(Message, "hello");

        Assert.IsTrue(invoked);
        Assert.AreEqual("hello", received);
    }

    [TestMethod]
    public void Publish_Should_InvokeAllHandlersOfTheSameMessage()
    {
        var pubSub = CreatePubSubService();
        var firstCount = 0;
        var secondCount = 0;

        pubSub.Subscribe(Message, _ => { firstCount++; return Task.CompletedTask; });
        pubSub.Subscribe(Message, _ => { secondCount++; return Task.CompletedTask; });

        pubSub.Publish(Message);

        Assert.AreEqual(1, firstCount);
        Assert.AreEqual(1, secondCount);
    }

    [TestMethod]
    public void Publish_Should_NotInvokeHandlersOfOtherMessages()
    {
        var pubSub = CreatePubSubService();
        var invoked = false;

        pubSub.Subscribe(OtherMessage, _ => { invoked = true; return Task.CompletedTask; });

        pubSub.Publish(Message);

        Assert.IsFalse(invoked);
    }

    [TestMethod]
    public void Unsubscribe_Should_StopFurtherInvocations()
    {
        var pubSub = CreatePubSubService();
        var count = 0;

        var unsubscribe = pubSub.Subscribe(Message, _ => { count++; return Task.CompletedTask; });

        pubSub.Publish(Message);
        unsubscribe();
        pubSub.Publish(Message);

        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void Unsubscribe_Should_KeepOtherHandlersOfTheSameMessage()
    {
        var pubSub = CreatePubSubService();
        var removedCount = 0;
        var keptCount = 0;

        var unsubscribeRemoved = pubSub.Subscribe(Message, _ => { removedCount++; return Task.CompletedTask; });
        pubSub.Subscribe(Message, _ => { keptCount++; return Task.CompletedTask; });

        unsubscribeRemoved();
        pubSub.Publish(Message);

        Assert.AreEqual(0, removedCount);
        Assert.AreEqual(1, keptCount);
    }

    [TestMethod]
    public void Publish_Persistent_Should_DeliverToLaterSubscriber()
    {
        var pubSub = CreatePubSubService();
        object? received = null;

        // No subscriber yet: the persistent message must be retained and replayed on the next matching subscribe.
        pubSub.Publish(Message, "queued", persistent: true);

        pubSub.Subscribe(Message, payload => { received = payload; return Task.CompletedTask; });

        Assert.AreEqual("queued", received);
    }

    [TestMethod]
    public void Publish_NonPersistent_Should_NotDeliverToLaterSubscriber()
    {
        var pubSub = CreatePubSubService();
        var invoked = false;

        pubSub.Publish(Message, "dropped");

        pubSub.Subscribe(Message, _ => { invoked = true; return Task.CompletedTask; });

        Assert.IsFalse(invoked);
    }

    [TestMethod]
    public void Publish_Persistent_Should_DeliverToFirstSubscriberOnly()
    {
        var pubSub = CreatePubSubService();
        var firstCount = 0;
        var secondCount = 0;

        pubSub.Publish(Message, persistent: true);

        pubSub.Subscribe(Message, _ => { firstCount++; return Task.CompletedTask; });
        pubSub.Subscribe(Message, _ => { secondCount++; return Task.CompletedTask; });

        // The persistent message is consumed by the first subscriber and must not linger for the second one.
        Assert.AreEqual(1, firstCount);
        Assert.AreEqual(0, secondCount);
    }

    [TestMethod]
    public void Publish_Persistent_Should_NotDropUnrelatedMessagesWhenAnotherIsConsumed()
    {
        // Guards the persistent-message draining logic: consuming the queued message for one key must neither lose
        // nor duplicate the queued message of a different key.
        var pubSub = CreatePubSubService();
        object? receivedOther = null;
        var otherDeliveryCount = 0;

        pubSub.Publish(Message, "for-message", persistent: true);
        pubSub.Publish(OtherMessage, "for-other", persistent: true);

        // Consume only the first key's message.
        pubSub.Subscribe(Message, _ => Task.CompletedTask);

        // The unrelated key's message must still be intact and delivered exactly once.
        pubSub.Subscribe(OtherMessage, payload =>
        {
            otherDeliveryCount++;
            receivedOther = payload;
            return Task.CompletedTask;
        });

        Assert.AreEqual(1, otherDeliveryCount);
        Assert.AreEqual("for-other", receivedOther);
    }

    [TestMethod]
    public void Publish_Should_SupportStaticHandlers()
    {
        var pubSub = CreatePubSubService();
        staticReceived = null;

        pubSub.Subscribe(Message, StaticHandler);

        pubSub.Publish(Message, 42);

        Assert.AreEqual(42, staticReceived);
    }

    [TestMethod]
    public void Publish_Should_SkipHandlersWhoseTargetHasBeenCollected()
    {
        var pubSub = CreatePubSubService();
        var counter = new StrongBox<int>(0);

        // The subscriber's target is created and dropped inside the helper, leaving only a weak reference behind.
        var weakTarget = SubscribeAndForget(pubSub, counter);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(weakTarget.IsAlive, "The subscriber target should have been garbage collected once no strong reference remains.");

        // Publishing to a collected subscriber must be a no-op and must not throw.
        pubSub.Publish(Message);

        Assert.AreEqual(0, counter.Value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference SubscribeAndForget(PubSubService pubSub, StrongBox<int> counter)
    {
        var target = new SubscriberTarget(counter);
        // Discard the unsubscribe delegate on purpose: it captures the handler (and thus the target) strongly, so
        // keeping it would pin the target and defeat the weak-reference behavior under test.
        _ = pubSub.Subscribe(Message, target.HandleAsync);
        return new WeakReference(target);
    }

    private static object? staticReceived;

    private static Task StaticHandler(object? payload)
    {
        staticReceived = payload;
        return Task.CompletedTask;
    }

    private sealed class SubscriberTarget(StrongBox<int> counter)
    {
        public Task HandleAsync(object? payload)
        {
            counter.Value++;
            return Task.CompletedTask;
        }
    }
}
