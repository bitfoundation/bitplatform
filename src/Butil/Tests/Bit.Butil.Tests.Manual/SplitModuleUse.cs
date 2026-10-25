using Bit.Butil;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ButilTests.Manual;

/// <summary>
/// A consumer that uses <em>one narrow member</em> of each API whose JavaScript is split across a family
/// of modules - so the trimmed run can check the thing the split exists for: calling one member keeps
/// that member's module and leaves the family's other modules behind.
/// </summary>
/// <remarks>
/// <see cref="ConsumerComponent"/> asks whether an unused <em>service</em> is trimmed away. This asks the
/// finer question a split module family raises, and the one a consumer actually feels: an app that reads a
/// computed style must not download the Typed OM, the stylesheet registry, the highlight API and the
/// Houdini worklets along with it. The trimmer works per method body, and an interop identifier is a
/// literal in one - so the modules that survive are decided by which methods do.
/// <br/>
/// That is only true while nothing roots the whole class. <c>DotNetObjectReference.Create(x)</c> preserves
/// every public method of <c>x</c>'s type, which would keep every identifier in it and quietly undo the
/// split - which is why the services here that take JavaScript callbacks hand JavaScript a small internal
/// relay object instead of themselves, and why this file is the check that they still do.
/// </remarks>
internal sealed class SplitModuleUse
{
    [Inject] public Crypto Crypto { get; set; } = default!;

    [Inject] public Css Css { get; set; } = default!;

    [Inject] public WebAudio WebAudio { get; set; } = default!;

    [Inject] public IndexedDb IndexedDb { get; set; } = default!;

    [Inject] public Performance Performance { get; set; } = default!;

    [Inject] public UserAgent UserAgent { get; set; } = default!;

    /// <summary>The services injected here, for the registration half of the report.</summary>
    public static Type[] InjectedTypes =>
    [
        typeof(Crypto), typeof(Css), typeof(WebAudio), typeof(IndexedDb), typeof(Performance), typeof(UserAgent)
    ];

    /// <summary>
    /// One family per row: the modules <see cref="Use"/>'s calls need, and the modules of the same family
    /// that those calls do not reach and that must therefore not survive trimming.
    /// </summary>
    /// <remarks>
    /// The <c>Used</c> names also go into <see cref="ScriptTrimming.MustSurviveModules"/>, so the exact
    /// comparison there would already fail on a surviving sibling. This list is kept beside it anyway
    /// because the two failures read very differently: "an unexpected module survived" says something
    /// changed, while "the crypto family did not narrow: cryptoKeys survived, though only Crypto.RandomUuid
    /// is called" says which claim about the library stopped being true.
    /// </remarks>
    public static (string Family, string[] Used, string[] Unused)[] Families =>
    [
        ("crypto", ["crypto"], ["cryptoSign", "cryptoKeys", "cryptoDerive", "cryptoCipher", "cryptoKeyMaterial"]),
        ("css", ["css"], ["cssStyleSheet", "cssTypedOm", "cssHighlight", "cssWorklet"]),
        ("element", ["element"], ["elementAria", "elementDom", "elementState", "elementEvents"]),
        ("webAudio", ["webAudio"], ["webAudioNodes", "webAudioParams", "webAudioAnalyser", "webAudioWorklet", "webAudioMedia"]),
        ("indexedDb", ["indexedDb", "indexedDbStore"], ["indexedDbIndex", "indexedDbCursor", "indexedDbInfo", "indexedDbTransaction"]),
        ("performance", ["performance"], ["performanceVitals"]),
        ("userAgent", ["userAgent"], ["userAgentParser"])
    ];

    /// <summary>Every module name this file expects to be reachable after trimming.</summary>
    public static string[] UsedModules => [.. Families.SelectMany(family => family.Used)];

    /// <summary>
    /// The same families' unused modules, which the <em>reference</em> closure reaches even though no call
    /// does: the scan follows the types a class mentions, and a class carries every interop identifier its
    /// methods hold. Over-including is the safe direction for an untrimmed publish - bytes rather than a
    /// broken app - and it is what separates <see cref="ScriptTrimming.ScanReachableModules"/> from what
    /// ILLink concludes.
    /// <br/>
    /// Two of the families are absent from it, and both absences are worth stating. The <c>element</c> family
    /// narrows even here, because its siblings are separate static extension classes that this project never
    /// names - so an untrimmed publish of an app using only the core extensions ships none of them either.
    /// And <c>cryptoKeyMaterial</c> is reached by no C# call site at all: it is pulled in as a JavaScript
    /// dependency of cryptoKeys and cryptoDerive.
    /// </summary>
    public static string[] SiblingModulesReachedByReference =>
    [
        "cryptoSign", "cryptoKeys", "cryptoDerive", "cryptoCipher",
        "cssStyleSheet", "cssTypedOm", "cssHighlight", "cssWorklet",
        "webAudioNodes", "webAudioParams", "webAudioAnalyser", "webAudioWorklet", "webAudioMedia",
        "indexedDbIndex", "indexedDbCursor", "indexedDbInfo", "indexedDbTransaction",
        "performanceVitals", "userAgentParser"
    ];

    public void Inject(IServiceProvider serviceProvider)
    {
        Crypto = (Crypto)serviceProvider.GetRequiredService(typeof(Crypto));
        Css = (Css)serviceProvider.GetRequiredService(typeof(Css));
        WebAudio = (WebAudio)serviceProvider.GetRequiredService(typeof(WebAudio));
        IndexedDb = (IndexedDb)serviceProvider.GetRequiredService(typeof(IndexedDb));
        Performance = (Performance)serviceProvider.GetRequiredService(typeof(Performance));
        UserAgent = (UserAgent)serviceProvider.GetRequiredService(typeof(UserAgent));
    }

    /// <summary>
    /// One call per family, chosen to be the smallest thing a real app would do with that API.
    /// </summary>
    /// <remarks>
    /// The runtime is a stub, so these throw as often as not - which is irrelevant here, since the trimmer
    /// works off the call being present in IL rather than off it running.
    /// </remarks>
    public async Task<(int Succeeded, int Threw)> Use()
    {
        (string Name, Func<Task> Call)[] steps =
        [
            // Randomness only: none of signing, key material, derivation or the ciphers.
            ("Crypto.RandomUuid", () => Crypto.RandomUuid().AsTask()),

            // A computed style: not the Typed OM, the stylesheet registry, highlights or the worklets.
            ("Css.GetComputedStyle", () => Css.GetComputedStyle(default, ["color"]).AsTask()),

            // The core ElementReference surface, which is a static extension class rather than a service -
            // so this is also the check that calling one extension does not drag its four siblings.
            ("ElementReference.Focus", () => Bit.Butil.ElementReferenceExtensions.Focus(default).AsTask()),

            // Fire-and-forget playback: not the node graph, the params, the analyser, the worklet or the
            // media-stream nodes.
            ("WebAudio.PlayTone", () => WebAudio.PlayTone(440, 10).AsTask()),

            // A database and one store write. Two modules on purpose: the handle's own members are split
            // the same way its JavaScript is, so this is what proves the split reaches past the service
            // class into the handle it hands out.
            ("IndexedDb.Open + Put", async () =>
            {
                var handle = await IndexedDb.Open("butil-manual-test", 1, []);
                if (handle is not null) await handle.Put("store", "value");
            }),

            // A timestamp: not the Web Vitals accumulator, which is the expensive half of this API.
            ("Performance.Now", () => Performance.Now().AsTask()),

            // Client Hints: not the user-agent string parser, which is the biggest module Butil ships.
            ("UserAgent.IsMobile", () => UserAgent.IsMobile().AsTask())
        ];

        var succeeded = 0;
        var threw = 0;
        foreach (var (_, call) in steps)
        {
            try
            {
                await call();
                succeeded++;
            }
            catch
            {
                threw++;
            }
        }

        return (succeeded, threw);
    }
}
