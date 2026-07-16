using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// Unit tests for <see cref="BrouterRouteContext"/>'s activation/deactivation bookkeeping,
/// focused on the invariant that a deactivation is never delivered to content that never received
/// its activation callback (superseded commit before the arrival flush, or static prerendering).
/// </summary>
[TestClass]
public class RouteContextActivationTests
{
    private sealed class RecordingRoute : IBrouterRoute
    {
        public int Activations;
        public int Deactivations;
        public int Renavigations;
        public bool? LastActivationWasFirst;

        public ValueTask OnActivatedAsync(BrouterRouteActivation activation)
        {
            Activations++;
            LastActivationWasFirst = activation.IsFirstActivation;
            return ValueTask.CompletedTask;
        }

        public ValueTask OnDeactivatedAsync(BrouterRouteDeactivation deactivation)
        {
            Deactivations++;
            return ValueTask.CompletedTask;
        }

        public ValueTask OnRenavigatedAsync(BrouterRouteRenavigation renavigation)
        {
            Renavigations++;
            return ValueTask.CompletedTask;
        }
    }

    private static void FailOnError(Exception ex) => Assert.Fail($"unexpected lifecycle error: {ex}");

    [TestMethod]
    public void Deactivation_is_suppressed_when_content_was_never_activated()
    {
        // Born active (as it is when its render mounts the content) but the activation callback
        // never ran - mirrors a commit superseded before the OnAfterRenderAsync flush, or prerender.
        var context = new BrouterRouteContext(initiallyActive: true);
        var handler = new RecordingRoute();
        context.Register(handler);

        context.FireDeactivated(BrouterRouteDeactivationReason.Disposing, BrouterLocation.Empty, FailOnError);

        Assert.AreEqual(0, handler.Deactivations, "OnDeactivated must not fire before OnActivated ever did.");
        Assert.IsFalse(context.IsActive, "IsActive is still cleared so a later reveal activates for real.");
        Assert.IsFalse(context.HasEverActivated);
    }

    [TestMethod]
    public void Reveal_after_a_suppressed_deactivation_activates_as_the_first_activation()
    {
        var context = new BrouterRouteContext(initiallyActive: true);
        var handler = new RecordingRoute();
        context.Register(handler);

        // Suppressed (never activated), then the instance is revealed and genuinely arrives.
        context.FireDeactivated(BrouterRouteDeactivationReason.Hidden, BrouterLocation.Empty, FailOnError);
        context.FireArrival(BrouterLocation.Empty, BrouterLocation.Empty, FailOnError);

        Assert.AreEqual(1, handler.Activations);
        Assert.AreEqual(true, handler.LastActivationWasFirst, "The first real activation must report IsFirstActivation.");
        Assert.AreEqual(0, handler.Renavigations);
    }

    [TestMethod]
    public void Deactivation_fires_normally_after_a_real_activation()
    {
        var context = new BrouterRouteContext(initiallyActive: true);
        var handler = new RecordingRoute();
        context.Register(handler);

        context.FireArrival(BrouterLocation.Empty, BrouterLocation.Empty, FailOnError);   // activates (first)
        context.FireDeactivated(BrouterRouteDeactivationReason.Hidden, BrouterLocation.Empty, FailOnError);

        Assert.AreEqual(1, handler.Activations);
        Assert.AreEqual(1, handler.Deactivations, "A deactivation after a real activation still fires.");
        Assert.IsFalse(context.IsActive);
    }

    [TestMethod]
    public void Second_arrival_on_a_still_active_instance_is_a_renavigation()
    {
        var context = new BrouterRouteContext(initiallyActive: true);
        var handler = new RecordingRoute();
        context.Register(handler);

        context.FireArrival(BrouterLocation.Empty, BrouterLocation.Empty, FailOnError);   // activation
        context.FireArrival(BrouterLocation.Empty, BrouterLocation.Empty, FailOnError);   // stays active -> renavigation

        Assert.AreEqual(1, handler.Activations);
        Assert.AreEqual(1, handler.Renavigations);
    }
}
