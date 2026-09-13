//+:cnd:noEmit
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging.Abstractions;
using Boilerplate.Client.Core.Infrastructure.Services;
using Microsoft.Extensions.Time.Testing;

namespace Boilerplate.Tests.Features.Ads;

/// <summary>
/// <see cref="AdsService"/> is a state machine driven entirely by Google Publisher Tag callbacks, and every way it
/// can go wrong is silent: the section that consumes it renders a loading indicator until <c>Init</c> returns, and a
/// cancellation raised out of it is dropped by <c>ClientExceptionHandlerBase.IgnoreException</c>. So a regression
/// here shows up as "the Upgrade button never appears" or "clicking Upgrade does nothing", with no log and no error.
/// <para>
/// These are plain unit tests on purpose - the service only needs an <see cref="IJSRuntime"/> and a clock, so they
/// need no server, no browser and no ad network.
/// </para>
/// </summary>
[TestClass]
public class AdsServiceTests
{
    /// <summary>
    /// The no-fill case: Google raises <c>slotRenderEnded</c> with <c>isEmpty</c>, which reaches
    /// <c>AdNotAvailable</c>. Before, that callback was empty, so <c>Init</c> awaited a completion that never came.
    /// </summary>
    [TestMethod]
    public async Task Init_Should_ReturnNotAvailable_WhenTheSlotIsNotFilled()
    {
        var (sut, jsRuntime, _) = CreateSut();
        jsRuntime.OnInit = service => service.AdNotAvailable();

        var result = await sut.Init("/ad-unit-path");

        Assert.AreEqual(AdInitResult.NotAvailable, result);
    }

    /// <summary>
    /// A slot that never renders at all raises nothing, so the wait needs its own deadline.
    /// </summary>
    [TestMethod]
    public async Task Init_Should_ReturnNotAvailable_WhenNoCallbackArrivesBeforeTheTimeout()
    {
        var (sut, jsRuntime, timeProvider) = CreateSut();
        jsRuntime.OnInit = _ => { }; // Google never answers.

        var initTask = sut.Init("/ad-unit-path");

        Assert.IsFalse(initTask.IsCompleted, "Init must not give up before its deadline.");

        timeProvider.Advance(TimeSpan.FromSeconds(15));

        Assert.AreEqual(AdInitResult.NotAvailable, await initTask);
    }

    /// <summary>
    /// <c>Ads.init</c> awaits the ad script's own load before it returns, so a CDN that never answers either way
    /// leaves the interop call itself pending. The deadline therefore has to be armed before the call, not after it -
    /// otherwise the timeout never starts and the section renders its loading indicator forever, which is the exact
    /// symptom the deadline exists to prevent.
    /// </summary>
    [TestMethod]
    public async Task Init_Should_ReturnNotAvailable_WhenTheAdScriptNeverSettles()
    {
        var (sut, jsRuntime, timeProvider) = CreateSut();
        jsRuntime.InitNeverCompletes = true;

        var initTask = sut.Init("/ad-unit-path");

        Assert.IsFalse(initTask.IsCompleted);

        timeProvider.Advance(TimeSpan.FromSeconds(15));

        Assert.AreEqual(AdInitResult.NotAvailable, await initTask);
    }

    /// <summary>
    /// The BP-378 regression, and the reason a failed init must not be remembered: an ad blocker fails the first
    /// init, and the user then navigates away and back. If the failure were latched, the second Init would return
    /// instantly and successfully, the Upgrade button would render enabled, and clicking it would await a reward
    /// that no slot can grant.
    /// </summary>
    [TestMethod]
    public async Task Init_Should_RetryAfterAFailure_AndNotReportTheFailedInitAsReady()
    {
        var (sut, jsRuntime, _) = CreateSut();
        jsRuntime.OnInit = service => service.ScriptFailed();

        Assert.AreEqual(AdInitResult.ScriptFailed, await sut.Init("/ad-unit-path"));

        jsRuntime.OnInit = service => service.AdReady();

        Assert.AreEqual(AdInitResult.Ready, await sut.Init("/ad-unit-path"), "A failed init must be retried, not replayed.");
        Assert.AreEqual(2, jsRuntime.InitCalls);
    }

    /// <summary>
    /// A successful init is a latch, so a component that re-renders does not re-define the slot.
    /// </summary>
    [TestMethod]
    public async Task Init_Should_NotReinitialize_WhenTheAdIsAlreadyReady()
    {
        var (sut, jsRuntime, _) = CreateSut();
        jsRuntime.OnInit = service => service.AdReady();

        await sut.Init("/ad-unit-path");
        await sut.Init("/ad-unit-path");

        Assert.AreEqual(1, jsRuntime.InitCalls);
    }

    /// <summary>
    /// Watching an ad destroys its slot, so the next visit has to build a new one. Without this the second visit
    /// would show an enabled Upgrade button backed by a destroyed slot.
    /// </summary>
    [TestMethod]
    public async Task Init_Should_RunAgainAfterAWatch_BecauseWatchingDestroysTheSlot()
    {
        var (sut, jsRuntime, _) = CreateSut();
        jsRuntime.OnInit = service => service.AdReady();
        jsRuntime.OnWatch = service => service.AdRewardGranted(1, "coin");

        await sut.Init("/ad-unit-path");
        Assert.AreEqual(AdWatchResult.Rewarded, await sut.Watch());

        await sut.Init("/ad-unit-path");

        Assert.AreEqual(2, jsRuntime.InitCalls);
    }

    /// <summary>
    /// <c>Ads.watch</c> answers false when no live slot exists. Without that answer, <c>Watch</c> awaited a
    /// completion source that nothing would ever complete - the button spun forever.
    /// </summary>
    [TestMethod]
    public async Task Watch_Should_Fail_WhenThereIsNoLiveSlot()
    {
        var (sut, jsRuntime, _) = CreateSut();
        jsRuntime.OnInit = service => service.AdReady();
        jsRuntime.WatchReturnsTrue = false;

        await sut.Init("/ad-unit-path");

        Assert.AreEqual(AdWatchResult.Failed, await sut.Watch());
    }

    /// <summary>
    /// Google raises both <c>rewardedSlotGranted</c> and <c>rewardedSlotClosed</c> for a completed reward, so the
    /// completion sources are written twice. With the non-Try members that threw inside a [JSInvokable] callback,
    /// where nothing catches it.
    /// </summary>
    [TestMethod]
    public async Task Watch_Should_NotThrow_WhenTheAdIsBothGrantedAndClosed()
    {
        var (sut, jsRuntime, _) = CreateSut();
        jsRuntime.OnInit = service => service.AdReady();
        jsRuntime.OnWatch = service =>
        {
            service.AdRewardGranted(1, "coin");
            service.AdClosed(1, "coin");
        };

        await sut.Init("/ad-unit-path");

        Assert.AreEqual(AdWatchResult.Rewarded, await sut.Watch());
    }

    /// <summary>
    /// Asserted on the JS runtime rather than the return value: <c>gpt.js</c> sets identifiers the moment it loads, so
    /// "the script was never asked for" is the property that matters. Checking only the result would still pass if the
    /// script had loaded and the answer been discarded afterwards.
    /// </summary>
    [TestMethod]
    public async Task Init_Should_NotLoadTheAdScript_WithoutAdvertisingConsent()
    {
        var (sut, jsRuntime, _) = CreateSut(advertisingConsent: false);
        jsRuntime.OnInit = service => service.AdReady();

        Assert.AreEqual(AdInitResult.ConsentRequired, await sut.Init("/ad-unit-path"));

        Assert.AreEqual(0, jsRuntime.InitCalls, "gpt.js must not be requested before the user has agreed to it.");
    }

    /// <summary>And the gate opens: agreeing has to let the very next Init through, with no restart.</summary>
    [TestMethod]
    public async Task Init_Should_LoadTheAdScript_OnceAdvertisingConsentIsGranted()
    {
        var (sut, jsRuntime, _) = CreateSut(advertisingConsent: false);
        jsRuntime.OnInit = service => service.AdReady();

        Assert.AreEqual(AdInitResult.ConsentRequired, await sut.Init("/ad-unit-path"));

        await jsRuntime.ConsentService.Set(ConsentCategory.Advertising, granted: true);

        Assert.AreEqual(AdInitResult.Ready, await sut.Init("/ad-unit-path"));
        Assert.AreEqual(1, jsRuntime.InitCalls);
    }

    private static (AdsService sut, FakeAdsJsRuntime jsRuntime, FakeTimeProvider timeProvider) CreateSut(bool advertisingConsent = true)
    {
        var jsRuntime = new FakeAdsJsRuntime();
        var timeProvider = new FakeTimeProvider();

        // The rest of these tests are about the Google Publisher Tag state machine, so consent is already given.
        var consentService = new ConsentService(new PubSubService(new ServiceCollection().BuildServiceProvider()), new FakeStorageService());
        if (advertisingConsent)
        {
            consentService.Set(ConsentCategory.Advertising, granted: true).GetAwaiter().GetResult();
        }

        var sut = new AdsService(consentService, jsRuntime, NullLogger<AdsService>.Instance, timeProvider);
        jsRuntime.Service = sut;
        jsRuntime.ConsentService = consentService;
        return (sut, jsRuntime, timeProvider);
    }

    /// <summary>An <see cref="IStorageService"/> that keeps the values in a dictionary, which is all a consent decision needs.</summary>
    private sealed class FakeStorageService : IStorageService
    {
        private readonly Dictionary<string, string?> items = [];

        public ValueTask SetItem(string key, string? value, bool persistent = true) { items[key] = value; return ValueTask.CompletedTask; }
        public ValueTask<string?> GetItem(string key) => ValueTask.FromResult(items.GetValueOrDefault(key));
        public ValueTask<bool> IsPersistent(string key) => ValueTask.FromResult(true);
        public ValueTask RemoveItem(string key) { items.Remove(key); return ValueTask.CompletedTask; }
        public ValueTask Clear() { items.Clear(); return ValueTask.CompletedTask; }
    }

    /// <summary>
    /// Stands in for Ads.ts: it records the calls and raises the [JSInvokable] callbacks the real Google Publisher
    /// Tag listeners would raise.
    /// </summary>
    private sealed class FakeAdsJsRuntime : IJSRuntime
    {
        public AdsService Service { get; set; } = default!;
        /// <summary>Handed back to the tests that need to change the answer mid-test.</summary>
        public ConsentService ConsentService { get; set; } = default!;
        public int InitCalls { get; private set; }
        public bool WatchReturnsTrue { get; set; } = true;
        /// <summary>Stands in for an ad script request that neither loads nor errors.</summary>
        public bool InitNeverCompletes { get; set; }
        public Action<AdsService>? OnInit { get; set; }
        public Action<AdsService>? OnWatch { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier is "Ads.init")
            {
                InitCalls++;

                if (InitNeverCompletes) return new ValueTask<TValue>(new TaskCompletionSource<TValue>().Task);

                OnInit?.Invoke(Service);
                return ValueTask.FromResult(default(TValue)!);
            }

            if (identifier is "Ads.watch")
            {
                if (WatchReturnsTrue)
                {
                    OnWatch?.Invoke(Service);
                }
                return ValueTask.FromResult((TValue)(object)WatchReturnsTrue);
            }

            throw new NotSupportedException(identifier);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }
}
