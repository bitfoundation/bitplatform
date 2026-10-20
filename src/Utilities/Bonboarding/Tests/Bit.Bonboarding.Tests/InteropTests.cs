using Bunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bit.Bonboarding.Tests;

/// <summary>
/// The default browser layer, against bUnit's JavaScript interop. Covers what only that layer
/// decides: which browser callbacks still belong to the step on screen.
/// </summary>
[TestClass]
public class InteropTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Bonboarding/bit-bonboarding.js";

    [TestMethod]
    public async Task Callbacks_Raised_For_A_Replaced_Step_Are_Dropped()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;

        var interop = new BonboardingInterop(
            JSInterop.JSRuntime,
            Options.Create(new BonboardingOptions()),
            NullLogger<BonboardingInterop>.Instance);

        var callbacks = new CountingCallbacks();
        await interop.AttachAsync("s", callbacks);

        await interop.ActivateStepAsync(Activation("s"));
        var first = LastActivationId(module);

        await interop.ActivateStepAsync(Activation("s"));
        var second = LastActivationId(module);

        Assert.AreNotEqual(first, second);

        // Still in flight from the first step when the second replaced it: a late advance would
        // skip a step the user never saw, a late frame would place the card against the old target.
        await interop.OnAdvanceTriggered("s", first);
        await interop.OnTargetLost("s", first);
        await interop.OnWaitTimedOut("s", first);
        await interop.OnGeometryChanged("s", new BonboardingStepGeometry { Activation = first, Sequence = 99 });

        Assert.AreEqual(0, callbacks.Count);

        await interop.OnGeometryChanged("s", new BonboardingStepGeometry { Activation = second, Sequence = 100 });
        await interop.OnAdvanceTriggered("s", second);

        Assert.AreEqual(2, callbacks.Count);

        // Once the step is deactivated, nothing it raised applies any more.
        await interop.DeactivateStepAsync("s");
        await interop.OnAdvanceTriggered("s", second);

        Assert.AreEqual(2, callbacks.Count);

        // Keys are not tied to a step: a key press always means the step on screen.
        await interop.OnKey("s", new BonboardingInterop.KeyPayload("Escape", false));

        Assert.AreEqual(3, callbacks.Count);
    }

    private static BonboardingStepActivation Activation(string sessionId) => new()
    {
        SessionId = sessionId,
        Target = BonboardingTargetDescriptor.None,
        Scroll = "none",
        Interaction = "blocked",
    };

    private static long LastActivationId(BunitJSModuleInterop module)
        => ((BonboardingStepActivation)module.Invocations["activateStep"][^1].Arguments[0]!).Activation;

    private sealed class CountingCallbacks : IBonboardingInteropCallbacks
    {
        public int Count { get; private set; }

        public ValueTask OnGeometryChangedAsync(BonboardingStepGeometry geometry) => Hit();

        public ValueTask OnTargetLostAsync() => Hit();

        public ValueTask OnKeyAsync(string key, bool shiftKey) => Hit();

        public ValueTask OnOverlayClickAsync() => Hit();

        public ValueTask OnAdvanceTriggeredAsync() => Hit();

        public ValueTask OnWaitTimedOutAsync() => Hit();

        private ValueTask Hit()
        {
            Count++;
            return ValueTask.CompletedTask;
        }
    }
}
