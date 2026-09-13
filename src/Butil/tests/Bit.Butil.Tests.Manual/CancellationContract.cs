using Bit.Butil;

namespace ButilTests.Manual;

/// <summary>
/// Pins the handles the cancellable, browser-mediated APIs put on the wire: <see cref="WebOtp"/> and
/// <see cref="DigitalCredentials"/> each hand JavaScript an instance handle and a per-call one, and a
/// cancellation registration has to abort the call it belongs to.
/// </summary>
/// <remarks>
/// The failure this exists for is invisible to a browser test. A token that is <b>already</b> cancelled
/// runs its registration synchronously, so the abort is dispatched <em>before</em> the call it cancels.
/// With a single per-instance handle that abort finds nothing pending and is dropped, and the browser
/// then opens a wallet chooser or an SMS prompt for a request the caller had already given up on. A
/// handle per call is what lets the JavaScript side hold the abort against a request that has not
/// started yet - and, just as importantly, keeps it from cancelling the <em>next</em> call instead.
/// <br/>
/// Neither API can be driven headlessly (both prompt, and both are Chromium-on-Android in practice), so
/// what is checked here is the argument shape the JavaScript half depends on - the interop contract this
/// harness owns. The JavaScript half itself lives in <c>webOtp.ts</c>, <c>digitalCredentials.ts</c> and
/// the registry they share, <c>abortable.ts</c>.
/// </remarks>
internal static class CancellationContract
{
    public static async Task<(int Passed, int Failed)> Run(List<string> failures)
    {
        var checks = new Checks(failures);

        await AlreadyCancelledReceiveIsAbortedBeforeItIsDispatched(checks);
        await CancellingAnInFlightReceiveAbortsThatSameRequest(checks);
        await EachReceiveGetsItsOwnHandleAndAbortEndsThemAll(checks);
        await AnEmptyCodeIsReportedAsNoCode(checks);
        await AlreadyCancelledExchangeIsAbortedBeforeItIsDispatched(checks);
        await APresentationAndAnIssuanceGetHandlesOfTheirOwn(checks);
        await AnAlreadyCancelledFetchIsNeverSent(checks);
        await AnUncancellableTokenRegistersNothing(checks);

        return (checks.Passed, checks.Failed);
    }

    private static async Task AlreadyCancelledReceiveIsAbortedBeforeItIsDispatched(Checks checks)
    {
        var runtime = new StubJSRuntime();

        await new WebOtp(runtime).Receive(TimeSpan.FromSeconds(1), new CancellationToken(canceled: true));

        var abort = runtime.First("BitButil.webOtp.abort");
        var receive = runtime.First("BitButil.webOtp.receive");

        checks.That(abort is not null, "an already-cancelled token must still abort");
        checks.That(receive is not null, "the receive is dispatched either way - the JS side is what declines to start it");
        if (abort is null || receive is null) return;

        checks.That(abort.Order < receive.Order,
            "the abort of an already-cancelled token is dispatched before the receive, which is why JS has to hold it");
        checks.That(Equals(receive.Args[0], abort.Args[0]), "both calls name the same WebOtp instance");
        checks.That(Equals(receive.Args[1], abort.Args[1]),
            "the abort must name the per-call handle of the receive it cancels, not the instance alone");
    }

    private static async Task CancellingAnInFlightReceiveAbortsThatSameRequest(Checks checks)
    {
        using var source = new CancellationTokenSource();
        var runtime = new StubJSRuntime { OnInvoke = identifier => { if (identifier == "BitButil.webOtp.receive") source.Cancel(); } };

        await new WebOtp(runtime).Receive(cancellationToken: source.Token);

        var receive = runtime.First("BitButil.webOtp.receive");
        var abort = runtime.First("BitButil.webOtp.abort");

        checks.That(receive is not null && abort is not null, "cancelling during interop dispatch must still abort");
        if (receive is null || abort is null) return;

        checks.That(receive.Order < abort.Order, "an in-flight cancellation aborts after the receive was dispatched");
        checks.That(Equals(receive.Args[1], abort.Args[1]), "the abort must name the request that was dispatched");
    }

    private static async Task EachReceiveGetsItsOwnHandleAndAbortEndsThemAll(Checks checks)
    {
        var runtime = new StubJSRuntime();
        var webOtp = new WebOtp(runtime);

        await webOtp.Receive();
        await webOtp.Receive();
        await webOtp.Abort();

        var receives = runtime.All("BitButil.webOtp.receive");
        var abort = runtime.First("BitButil.webOtp.abort");

        checks.That(receives.Length == 2, "both receives were dispatched");
        checks.That(abort is not null, "Abort() invokes the JS abort");
        if (receives.Length != 2 || abort is null) return;

        checks.That(Equals(receives[0].Args[0], receives[1].Args[0]), "both waits belong to the same instance");
        checks.That(Equals(receives[0].Args[1], receives[1].Args[1]) is false,
            "a reused per-call handle would let a stale abort cancel the next wait");
        checks.That(abort.Args.Length == 2 && abort.Args[1] is null,
            "Abort() passes no per-call handle, so it ends every wait the instance has in flight");
    }

    private static async Task AnEmptyCodeIsReportedAsNoCode(Checks checks)
    {
        // What prerender hands back for a string, and what a JS side that answered with one would: a code
        // is never empty, so "no code" has to have the single spelling Receive documents.
        var runtime = new StubJSRuntime { Answer = identifier => identifier == "BitButil.webOtp.receive" ? "" : null };

        var code = await new WebOtp(runtime).Receive();

        checks.That(code is null, "an empty code is null, not \"\" - a caller's `is not null` must not submit an empty OTP");
    }

    private static async Task AlreadyCancelledExchangeIsAbortedBeforeItIsDispatched(Checks checks)
    {
        var runtime = new StubJSRuntime();

        await new DigitalCredentials(runtime).Get(Requests(), cancellationToken: new CancellationToken(canceled: true));

        var abort = runtime.First("BitButil.digitalCredentials.abort");
        var get = runtime.First("BitButil.digitalCredentials.get");

        checks.That(abort is not null && get is not null, "an already-cancelled token must still abort the exchange");
        if (abort is null || get is null) return;

        checks.That(abort.Order < get.Order, "the abort is dispatched before the exchange it cancels");
        checks.That(Equals(get.Args[1], abort.Args[1]),
            "the abort must name the per-call handle of the exchange it cancels, or the wallet chooser opens anyway");
    }

    private static async Task APresentationAndAnIssuanceGetHandlesOfTheirOwn(Checks checks)
    {
        var runtime = new StubJSRuntime();
        var digitalCredentials = new DigitalCredentials(runtime);

        await digitalCredentials.Get(Requests());
        await digitalCredentials.Create(Requests());
        await digitalCredentials.Abort();

        var get = runtime.First("BitButil.digitalCredentials.get");
        var create = runtime.First("BitButil.digitalCredentials.create");
        var abort = runtime.First("BitButil.digitalCredentials.abort");

        checks.That(get is not null && create is not null && abort is not null, "get, create and abort were all dispatched");
        if (get is null || create is null || abort is null) return;

        checks.That(Equals(get.Args[0], create.Args[0]), "both belong to the same service instance");
        checks.That(Equals(get.Args[1], create.Args[1]) is false, "a shared handle would make either one cancel the other");
        checks.That(abort.Args.Length == 2 && abort.Args[1] is null, "Abort() ends every exchange the instance started");
    }

    private static async Task AnAlreadyCancelledFetchIsNeverSent(Checks checks)
    {
        var runtime = new StubJSRuntime();

        var response = await new Fetch(runtime).Send(new FetchRequest { Url = "https://example.com" },
            cancellationToken: new CancellationToken(canceled: true));

        checks.That(runtime.First("BitButil.fetch.send") is null,
            "a request whose token is already cancelled is never dispatched - JS has no controller yet, so an abort for it would be dropped and the request would run");
        checks.That(response.Aborted, "the documented answer to a cancelled Send is an aborted response, not an exception");
    }

    private static async Task AnUncancellableTokenRegistersNothing(Checks checks)
    {
        var runtime = new StubJSRuntime();

        await new WebOtp(runtime).Receive();
        await new DigitalCredentials(runtime).Get(Requests());

        checks.That(runtime.All("BitButil.webOtp.abort").Length == 0 && runtime.All("BitButil.digitalCredentials.abort").Length == 0,
            "CancellationToken.None can never fire, so nothing is registered and no abort is ever sent");
    }

    private static DigitalCredentialRequest[] Requests()
        => [new DigitalCredentialRequest { Protocol = "openid4vp", Data = new { nonce = "server-generated" } }];

    /// <summary>
    /// Tallies the checks. A class rather than a <c>ref int</c> because every check here is async, and an
    /// async method cannot take one.
    /// </summary>
    private sealed class Checks(List<string> failures)
    {
        public int Passed { get; private set; }

        public int Failed { get; private set; }

        public void That(bool condition, string what)
        {
            if (condition)
            {
                Passed++;
                return;
            }

            Failed++;
            failures.Add($"cancellation contract: {what}.");
        }
    }
}
