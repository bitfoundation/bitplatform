using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The JSON cap of the encoder every structured-messaging channel shares - ports, workers, windows and
/// data channels - driven from JS: the shapes that matter are ones a sender builds in the browser, and
/// a few of them are too large to be worth sending across .NET interop just to be measured.
/// </summary>
[TestClass]
public class MessageEncodingTests : ButilPageTest
{
    [TestMethod]
    public async Task Shared_References_Cannot_Spell_Out_A_Message_Without_End()
    {
        // Forty levels of x = [x, x]: a few kilobytes as a structured clone, 2^40 leaves as JSON. Without
        // the cap this does not come back at all.
        var result = await Page.EvaluateAsync<string>(
            """
            () => {
                let shared = 'A'.repeat(1000);
                for (let i = 0; i < 40; i++) shared = [shared, shared];
                const [isBinary, json] = BitButil.utils.encodeMessage(structuredClone(shared));
                return `${isBinary}|${json}`;
            }
            """);

        Assert.AreEqual("false|\"[object Array]\"", result);
    }

    [TestMethod]
    public async Task Many_Small_Values_Under_The_Cap_Arrive_Whole()
    {
        // 2.5 million small numbers are under 5 MB of JSON. A cap that counted values rather than the
        // output they make refused this, though it is a seventh of the size the cap allows.
        var whole = await Page.EvaluateAsync<bool>(
            """
            () => {
                const values = Array.from({ length: 2_500_000 }, (_, i) => i % 10);
                const [, json] = BitButil.utils.encodeMessage(values);
                return json === JSON.stringify(values);
            }
            """);

        Assert.IsTrue(whole, "a message well under the cap must arrive as its full JSON");
    }

    [TestMethod]
    public async Task A_Message_Over_The_Cap_Arrives_As_Its_Type_Name_With_A_Warning()
    {
        // A cyclic graph also loses its shape, but that is not the cap - so it must not warn about it.
        var result = await Page.EvaluateAsync<string[]>(
            """
            () => {
                const warnings = [];
                const warn = console.warn;
                console.warn = (...args) => warnings.push(args.join(' '));
                try {
                    const [, large] = BitButil.utils.encodeMessage({ text: 'x'.repeat(33 * 1024 * 1024) });
                    const afterLarge = warnings.length;
                    const cyclic = {};
                    cyclic.self = cyclic;
                    const [, cycle] = BitButil.utils.encodeMessage(cyclic);
                    return [large, String(afterLarge), cycle, String(warnings.length)];
                } finally {
                    console.warn = warn;
                }
            }
            """);

        Assert.AreEqual("\"[object Object]\"", result[0]);
        Assert.AreEqual("1", result[1], "a message over the cap must say so in the console");
        Assert.AreEqual("\"[object Object]\"", result[2]);
        Assert.AreEqual("1", result[3], "a cyclic message is not over the cap and must not warn that it is");
    }
}
