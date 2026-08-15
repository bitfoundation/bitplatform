using System.Runtime.CompilerServices;

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

    /// <summary>
    /// The handlers of one message live in a plain <c>List&lt;WeakHandler&gt;</c> inside a ConcurrentDictionary - the
    /// dictionary is concurrent, the list is not. Publishers genuinely run off the renderer's thread (SignalR
    /// callbacks and the faulted-task continuation), while unsubscribes run on it during component disposal, so
    /// <c>ToArray</c> could observe the torn state <c>RemoveAll</c> leaves behind between its <c>Array.Clear</c> and
    /// its <c>_size</c> assignment - a null entry, dereferenced one line later.
    /// </summary>
    [TestMethod]
    public async Task Subscribe_Should_NotLoseHandlers_WhenItRunsConcurrentlyWithUnsubscribe()
    {
        const int handlerCount = 2_000;

        var pubSub = CreatePubSubService();

        // Subscribed up front and unsubscribed on the racing thread. RemoveAll compacts the list in place, so it is
        // the mutation that can overwrite what Add is writing at the same moment.
        var doomedTargets = Enumerable.Range(0, handlerCount).Select(_ => new SubscriberTarget(new StrongBox<int>(0))).ToArray();
        var unsubscribes = doomedTargets.Select(target => pubSub.Subscribe(Message, target.HandleAsync)).ToArray();

        var survivingTargets = Enumerable.Range(0, handlerCount).Select(_ => new SubscriberTarget(new StrongBox<int>(0))).ToArray();

        using var start = new Barrier(2);

        var subscriber = Task.Run(() =>
        {
            start.SignalAndWait();
            foreach (var target in survivingTargets)
            {
                pubSub.Subscribe(Message, target.HandleAsync);
            }
        });

        var unsubscriber = Task.Run(() =>
        {
            start.SignalAndWait();
            foreach (var unsubscribe in unsubscribes)
            {
                unsubscribe();
            }
        });

        await Task.WhenAll(subscriber, unsubscriber);

        pubSub.Publish(Message);

        // Every surviving subscription must have been invoked exactly once. Fewer means the handler list lost an
        // entry: Add and RemoveAll were interleaving on a plain List<T> that only Add was holding a lock for.
        Assert.AreEqual(handlerCount, survivingTargets.Count(target => target.Counter.Value == 1),
            "Subscriptions were lost to a concurrent unsubscribe, so a component that subscribed successfully never receives its messages.");
        Assert.AreEqual(0, doomedTargets.Count(target => target.Counter.Value != 0),
            "An unsubscribed handler was still invoked.");
    }

    /// <summary>
    /// A faulted handler task is routed to <c>ClientExceptionHandlerBase</c> through a <c>ContinueWith</c> that nobody
    /// awaits, so the continuation is the terminal observer of that exception. If resolving the handler throws - the
    /// service provider here is a bare fake, which is exactly what a torn-down Blazor Server circuit scope behaves
    /// like - the continuation must not fault in turn and take the original exception down with it.
    /// </summary>
    [TestMethod]
    public async Task AFaultedHandler_Should_NotProduceAnUnobservedTaskException_WhenTheExceptionHandlerCannotBeResolved()
    {
        var pubSub = CreatePubSubService();
        var unobserved = new List<Exception>();

        void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs e) => unobserved.Add(e.Exception);

        TaskScheduler.UnobservedTaskException += OnUnobserved;
        try
        {
            RunFaultingPublish(pubSub);

            // The continuation runs on the thread pool; give it a turn, then force the finalizers that raise
            // UnobservedTaskException for any task that faulted without being observed.
            await Task.Delay(200);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await Task.Delay(200);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }

        Assert.IsEmpty(unobserved,
            "The faulted-handler continuation threw while resolving the exception handler, which destroys the exception it exists to report.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunFaultingPublish(PubSubService pubSub)
    {
        var target = new FaultingSubscriberTarget();
        _ = pubSub.Subscribe(Message, target.HandleAsync);
        pubSub.Publish(Message);
        GC.KeepAlive(target);
    }

    private sealed class FaultingSubscriberTarget
    {
        public async Task HandleAsync(object? payload)
        {
            await Task.Yield();
            throw new InvalidOperationException("handler blew up");
        }
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
        public StrongBox<int> Counter { get; } = counter;

        public Task HandleAsync(object? payload)
        {
            counter.Value++;
            return Task.CompletedTask;
        }
    }
}
