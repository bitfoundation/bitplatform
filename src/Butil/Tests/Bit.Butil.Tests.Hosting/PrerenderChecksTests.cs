using System.Net;
using ButilTests.Hosting.Infrastructure;
using Modes = ButilTests.Harness.Web.HarnessRenderModes;

namespace ButilTests.Hosting;

/// <summary>
/// What particular members answer while a page renders statically (see PrerenderChecksPage in the harness host).
/// The sweep in <see cref="PrerenderSweepTests"/> checks that every call completes; these check the value of the ones
/// that answer a security question, which has to come out as "no" when there is no runtime to ask.
/// </summary>
[TestClass]
public class PrerenderChecksTests
{
    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    public async Task WebAuthn_verify_fails_closed_during_a_static_render(string mode)
    {
        using var response = await HarnessHostFactory.Shared(mode).CreateClient().GetAsync("/prerender-checks");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        // The safe defaults of a static render add up to a stored credential id of "" and an assertion that
        // never throws - which a check reading only "did anything throw" would take for a verified user.
        Assert.AreEqual("False", RenderedHtml.TextOf(html, "check-webauthn-verify"));
    }
}
