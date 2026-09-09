using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The abort race in the fetch module, driven from JS rather than from a harness button: the point
/// is the ordering between <c>abort</c> and the request it names, which .NET cannot schedule
/// deterministically - it registers the cancellation callback before it posts the call, so a token
/// that fires immediately reaches JS first.
/// </summary>
[TestClass]
public class FetchTests : ButilPageTest
{
    [TestMethod]
    public async Task Abort_Arriving_Before_The_Request_Still_Aborts_It()
    {
        var aborted = await Page.EvaluateAsync<bool>(
            """
            async () => {
                const id = 'e2e-abort-race';
                BitButil.fetch.abort(id);
                const resp = await BitButil.fetch.send(id, { url: location.href, method: 'GET' }, null, false);
                return resp.aborted === true;
            }
            """);

        Assert.IsTrue(aborted, "an abort that arrived before the request must still abort it");
    }

    [TestMethod]
    public async Task Abort_Arriving_Before_A_Streamed_Request_Prevents_The_Upload()
    {
        // The stream reference stands in for the .NET one; whether stream() was ever called is what
        // says the body stayed where it was rather than being pulled across for a doomed request.
        var result = await Page.EvaluateAsync<bool[]>(
            """
            async () => {
                const id = 'e2e-abort-race-stream';
                let pulled = false;
                const streamRef = {
                    stream: async () => { pulled = true; return new ReadableStream({ start(c) { c.close(); } }); }
                };

                BitButil.fetch.abort(id);
                const resp = await BitButil.fetch.sendStream(id, { url: location.href, method: 'POST' }, streamRef, null, false, null);
                return [resp.aborted === true, pulled];
            }
            """);

        Assert.IsTrue(result[0], "a streamed request aborted before it began must report as aborted");
        Assert.IsFalse(result[1], "nothing of the body may be pulled once the request is already aborted");
    }

    [TestMethod]
    public async Task Abort_After_The_Request_Finished_Does_Not_Abort_The_Next_One()
    {
        // A pending abort is a note about one single-use id. An abort that missed its request - the
        // token firing just after it completed - must not follow the id into anything else.
        var ok = await Page.EvaluateAsync<bool>(
            """
            async () => {
                const first = await BitButil.fetch.send('e2e-abort-late-a', { url: location.href, method: 'GET' }, null, false);
                BitButil.fetch.abort('e2e-abort-late-a');
                const second = await BitButil.fetch.send('e2e-abort-late-b', { url: location.href, method: 'GET' }, null, false);
                return first.aborted === false && second.aborted === false && second.ok === true;
            }
            """);

        Assert.IsTrue(ok, "an abort that arrived after its request finished must not affect another request");
    }
}
