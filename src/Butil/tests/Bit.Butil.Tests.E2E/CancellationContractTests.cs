using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

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
/// what is checked here is the argument shape the JavaScript half depends on. The JavaScript half itself
/// lives in <c>webOtp.ts</c> and <c>digitalCredentials.ts</c>.
/// </remarks>
[TestClass]
public class CancellationContractTests
{
    [TestMethod]
    public async Task An_already_cancelled_token_aborts_the_receive_it_belongs_to_before_it_is_dispatched()
    {
        var runtime = new RecordingJSRuntime();

        await new WebOtp(runtime).Receive(TimeSpan.FromSeconds(1), new CancellationToken(canceled: true));

        var abort = runtime.First("BitButil.webOtp.abort");
        var receive = runtime.First("BitButil.webOtp.receive");

        Assert.IsNotNull(abort, "an already-cancelled token must still abort.");
        Assert.IsNotNull(receive, "the receive is dispatched either way - the JS side is what declines to start it.");
        Assert.IsLessThan(receive.Order, abort.Order,
            "the abort of an already-cancelled token is dispatched before the receive, which is why JS has to hold it.");
        Assert.AreEqual(receive.Args[0], abort.Args[0], "both name the same WebOtp instance.");
        Assert.AreEqual(receive.Args[1], abort.Args[1],
            "the abort must name the per-call handle of the receive it cancels, not the instance alone.");
    }

    [TestMethod]
    public async Task Cancelling_while_the_receive_is_in_flight_aborts_that_same_request()
    {
        using var source = new CancellationTokenSource();
        var runtime = new RecordingJSRuntime { OnInvoke = identifier => { if (identifier == "BitButil.webOtp.receive") source.Cancel(); } };

        await new WebOtp(runtime).Receive(cancellationToken: source.Token);

        var receive = runtime.First("BitButil.webOtp.receive");
        var abort = runtime.First("BitButil.webOtp.abort");

        Assert.IsNotNull(receive);
        Assert.IsNotNull(abort, "cancelling during interop dispatch must still abort.");
        Assert.IsGreaterThan(receive.Order, abort.Order);
        Assert.AreEqual(receive.Args[1], abort.Args[1], "the abort must name the request that was dispatched.");
    }

    [TestMethod]
    public async Task Each_receive_gets_its_own_handle_and_Abort_ends_them_all()
    {
        var runtime = new RecordingJSRuntime();
        var webOtp = new WebOtp(runtime);

        await webOtp.Receive();
        await webOtp.Receive();
        await webOtp.Abort();

        var receives = runtime.All("BitButil.webOtp.receive");
        var abort = runtime.First("BitButil.webOtp.abort");

        Assert.HasCount(2, receives);
        Assert.AreEqual(receives[0].Args[0], receives[1].Args[0], "both waits belong to the same instance.");
        Assert.AreNotEqual(receives[0].Args[1], receives[1].Args[1],
            "a reused handle would let a stale abort cancel the next wait.");

        Assert.IsNotNull(abort);
        Assert.HasCount(2, abort.Args);
        Assert.IsNull(abort.Args[1], "Abort() passes no per-call handle, so it ends every wait the instance has in flight.");
    }

    [TestMethod]
    public async Task An_already_cancelled_token_aborts_the_exchange_it_belongs_to_before_it_is_dispatched()
    {
        var runtime = new RecordingJSRuntime();

        await new DigitalCredentials(runtime).Get(Requests(), cancellationToken: new CancellationToken(canceled: true));

        var abort = runtime.First("BitButil.digitalCredentials.abort");
        var get = runtime.First("BitButil.digitalCredentials.get");

        Assert.IsNotNull(abort);
        Assert.IsNotNull(get);
        Assert.IsLessThan(get.Order, abort.Order);
        Assert.AreEqual(get.Args[1], abort.Args[1],
            "the abort must name the per-call handle of the exchange it cancels, or the wallet chooser opens anyway.");
    }

    [TestMethod]
    public async Task A_presentation_and_an_issuance_get_handles_of_their_own()
    {
        var runtime = new RecordingJSRuntime();
        var digitalCredentials = new DigitalCredentials(runtime);

        await digitalCredentials.Get(Requests());
        await digitalCredentials.Create(Requests());
        await digitalCredentials.Abort();

        var get = runtime.First("BitButil.digitalCredentials.get");
        var create = runtime.First("BitButil.digitalCredentials.create");
        var abort = runtime.First("BitButil.digitalCredentials.abort");

        Assert.IsNotNull(get);
        Assert.IsNotNull(create);
        Assert.AreEqual(get.Args[0], create.Args[0], "both belong to the same service instance.");
        Assert.AreNotEqual(get.Args[1], create.Args[1], "a shared handle would make either one cancel the other.");

        Assert.IsNotNull(abort);
        Assert.HasCount(2, abort.Args);
        Assert.IsNull(abort.Args[1], "Abort() ends every exchange the instance started.");
    }

    private static DigitalCredentialRequest[] Requests()
        => [new DigitalCredentialRequest { Protocol = "openid4vp", Data = new { nonce = "server-generated" } }];

    private sealed record Call(int Order, string Identifier, object?[] Args);

    /// <summary>
    /// Answers every call with <c>default</c> - there is no JavaScript here - while recording what was
    /// invoked and with which arguments, and can run a hook as a call is dispatched so a token can be
    /// cancelled mid-flight.
    /// </summary>
    private sealed class RecordingJSRuntime : IJSRuntime
    {
        private readonly List<Call> _calls = [];

        public Action<string>? OnInvoke { get; set; }

        public Call? First(string identifier) => All(identifier).FirstOrDefault();

        public Call[] All(string identifier)
        {
            lock (_calls) return [.. _calls.Where(call => call.Identifier == identifier)];
        }

        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            lock (_calls) _calls.Add(new Call(_calls.Count, identifier, args ?? []));

            OnInvoke?.Invoke(identifier);

            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
