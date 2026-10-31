using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The JSON cap of the encoder every structured-messaging channel shares - ports, workers, windows, data
/// channels, broadcast channels and service workers - driven from JS: the shapes that matter are ones a
/// sender builds in the browser, and a few of them are too large to be worth sending across .NET interop
/// just to be measured.
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

    [TestMethod]
    public async Task The_Cap_Counts_Every_Character_The_Json_Will_Have()
    {
        // Each of these is small as a structured clone and large as JSON, in a way a count of values misses: one
        // String object shared a hundred times (JSON writes the string each time), an array that is nothing but
        // holes (each one written as null), and control characters (each one written as a six-character escape).
        // Then the edge itself: a string whose quoted JSON is exactly the cap fits, and one character more does not.
        var result = await Page.EvaluateAsync<string[]>(
            """
            () => {
                const limit = 32 * 1024 * 1024;
                const warn = console.warn;
                console.warn = () => {};
                try {
                    const json = data => BitButil.utils.encodeMessage(structuredClone(data))[1];
                    const holes = [];
                    holes.length = 30_000_000;
                    return [
                        json(Array(100).fill(new String('A'.repeat(1024 * 1024)))),
                        json(holes),
                        json({ text: '\u0001'.repeat(6 * 1024 * 1024) }),
                        String(json('x'.repeat(limit - 2)).length),
                        json('x'.repeat(limit - 1))
                    ];
                } finally {
                    console.warn = warn;
                }
            }
            """);

        Assert.AreEqual("\"[object Array]\"", result[0], "a shared String object is written once per reference");
        Assert.AreEqual("\"[object Array]\"", result[1], "a hole is written as null");
        Assert.AreEqual("\"[object Object]\"", result[2], "a control character is written as an escape");
        Assert.AreEqual((32 * 1024 * 1024).ToString(), result[3], "JSON of exactly the cap must arrive whole");
        Assert.AreEqual("\"[object String]\"", result[4], "JSON one character past the cap must not");
    }

    [TestMethod]
    public async Task Refusing_Shared_References_Takes_No_Longer_Than_Cloning_Them()
    {
        // Forty levels of x = [x, x] over a single 0: a few hundred bytes to clone, and 2^40 leaves to write. A cap
        // spent as the JSON is written refuses it only after writing 32 MB of it - most of a second, per message,
        // for a sender who can post as fast as it likes. Counted once per shared object, it is refused at once.
        var milliseconds = await Page.EvaluateAsync<double>(
            """
            () => {
                let shared = 0;
                for (let i = 0; i < 40; i++) shared = [shared, shared];
                const message = structuredClone(shared);
                const warn = console.warn;
                console.warn = () => {};
                try {
                    const start = performance.now();
                    for (let i = 0; i < 10; i++) BitButil.utils.encodeMessage(message);
                    return (performance.now() - start) / 10;
                } finally {
                    console.warn = warn;
                }
            }
            """);

        Assert.IsLessThan(50.0, milliseconds, "refusing the message must not take the work of writing what it spells out");
    }

    [TestMethod]
    public async Task A_Message_Whose_Own_ToString_Is_Not_A_Function_Still_Arrives()
    {
        // JSON cannot write either of these, so each arrives as its type name - which is not found by calling the
        // message's own toString, since a cloned object can carry one that is not a function.
        var result = await Page.EvaluateAsync<string[]>(
            """
            () => {
                const cyclic = { toString: 0 };
                cyclic.self = cyclic;
                return [
                    BitButil.utils.encodeMessage(structuredClone({ toString: 0, big: 1n }))[1],
                    BitButil.utils.encodeMessage(structuredClone(cyclic))[1]
                ];
            }
            """);

        CollectionAssert.AreEqual(new[] { "\"[object Object]\"", "\"[object Object]\"" }, result);
    }

    [TestMethod]
    public async Task A_Channel_Whose_Json_Blazor_Writes_Is_Handed_The_Message_Or_Its_Type_Name()
    {
        // BroadcastChannel and the service worker hand .NET a JsonElement, so Blazor's interop writes their JSON
        // rather than the encoder. What they pass on is the message itself when it fits - the very object, not a
        // copy - and its type name when it does not.
        var result = await Page.EvaluateAsync<string[]>(
            """
            () => {
                const small = { a: [1, 2, 3] };
                let shared = 'A'.repeat(1000);
                for (let i = 0; i < 40; i++) shared = [shared, shared];
                const warn = console.warn;
                console.warn = () => {};
                try {
                    return [String(BitButil.utils.boundMessage(small) === small), BitButil.utils.boundMessage(structuredClone(shared))];
                } finally {
                    console.warn = warn;
                }
            }
            """);

        CollectionAssert.AreEqual(new[] { "true", "[object Array]" }, result);
    }
}
