using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the right-to-left rules written with <c>:dir()</c> (#13566): every part such a rule
/// turns around is read off the computed styles, for the component with no direction, with <c>Dir="BitDir.Rtl"</c>,
/// inside a box that reads right to left, and with <c>Dir="BitDir.Ltr"</c> inside a right-to-left component.
///
/// The same page is then read the way an engine without <c>:dir()</c> (Chromium before 120, Safari before 16.4, the
/// WebViews a Blazor Hybrid app may run in) reads it: the stylesheets are rewritten in place the way such an engine
/// parses them - a rule naming <c>:dir()</c> outside a forgiving list dropped whole, a <c>:dir()</c> alternative of an
/// <c>:is()</c> dropped on its own, <c>@supports not selector(:dir(rtl))</c> applied and <c>@supports selector(:dir(...))</c>
/// left out. There an explicit <c>Dir</c> still has to turn every part around, and the class it renders must not reach
/// the parts of a component nested in it that reads the other way. Run them with BROWSER=firefox and BROWSER=webkit too.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitDirFallbackBrowserTests
public class BitDirFallbackBrowserTests : PerformanceTestBase
{
    private const string PageUrl = "/regression/dir-fallback";

    private static readonly string[] Probes =
    [
        "modal", "overlay", "collapse", "timeline", "tooltip", "panel", "snackbar-bar", "snackbar-entry", "message",
        "ellipsis", "rolling-square", "slider", "chart", "card", "datagrid", "rte"
    ];

    // The probes that can be nested in a component of their own kind, which #nested-same does.
    private static readonly string[] NestableProbes = ["collapse", "tooltip", "message", "card"];

    // Each probe's part, read the way its right-to-left rule changes it.
    private const string ReadSet = @"(set) => {
        const root = document.querySelector(set);
        const part = (probe, selector) => root.querySelector(`[data-probe=""${probe}""] ${selector}`);
        const style = (probe, selector, pseudo) => {
            const el = part(probe, selector);
            return el ? getComputedStyle(el, pseudo) : null;
        };
        const variable = (probe, selector, name) => style(probe, selector)?.getPropertyValue(name).trim();
        const origin = (probe, selector) => {
            const s = style(probe, selector);
            return s ? (parseFloat(s.transformOrigin) < 1 ? 'left' : 'right') : null;
        };
        const mask = (probe, selector) => {
            const s = style(probe, selector);
            if (!s) return null;
            const image = s.maskImage && s.maskImage !== 'none' ? s.maskImage : s.webkitMaskImage;
            return /to left/.test(image) ? 'to left' : /to right/.test(image) ? 'to right' : image;
        };
        const read = {
            'modal': () => style('modal', '.bit-mdl-tlf')?.justifyContent,
            'overlay': () => style('overlay', '.bit-ovl-tlf')?.justifyContent,
            'collapse': () => mask('collapse', '.bit-col-con'),
            'timeline': () => variable('timeline', '.bit-tln-hrz', '--bit-tln-dvd-dir-s'),
            'tooltip': () => variable('tooltip', '.bit-ttp-wrp', '--bit-ttp-dir'),
            'panel': () => variable('panel', '.bit-pnl', '--bit-pnl-transform-factor'),
            'snackbar-bar': () => origin('snackbar-bar', '.bit-snb-prb'),
            'snackbar-entry': () => variable('snackbar-entry', '.bit-snb-cst', '--bit-snb-ent'),
            'message': () => origin('message', '.bit-msg-prb'),
            'ellipsis': () => style('ellipsis', '.bit-ldn-elp-ccn')?.transform,
            'rolling-square': () => style('rolling-square', '.bit-ldn-rsq-ccn')?.transform,
            'slider': () => style('slider', '.bit-sld-inp-near')?.clipPath,
            'chart': () => style('chart', '.bit-cht-mid')?.flexDirection,
            'card': () => style('card', '.bit-crd-skb', '::after')?.animationDirection,
            'datagrid': () => style('datagrid', '.bit-dtg-dir-glyph')?.transform,
            'rte': () => style('rte', 'svg.bit-rte-flip')?.transform
        };
        const result = {};
        for (const [probe, reader] of Object.entries(read)) {
            const value = reader();
            result[probe] = value === undefined || value === null ? null : String(value);
        }
        return JSON.stringify(result);
    }";

    // The stylesheets as an engine without :dir() parses them.
    private const string DropDir = @"() => {
        // The selector less every :is(...) / :where(...) group, nested parentheses and all.
        const outsideForgiving = selector => {
            let out = '', depth = 0, inGroup = 0;
            for (let i = 0; i < selector.length; i++) {
                if (inGroup === 0 && /^:(is|where)\(/.test(selector.slice(i))) {
                    inGroup = depth + 1;
                    i = selector.indexOf('(', i);
                    depth++;
                    continue;
                }
                const c = selector[i];
                if (c === '(') depth++;
                else if (c === ')') { depth--; if (inGroup && depth < inGroup) { inGroup = 0; continue; } }
                if (inGroup === 0) out += c;
            }
            return out;
        };
        // Splits on the commas outside parentheses.
        const split = list => {
            const parts = [];
            let depth = 0, start = 0;
            for (let i = 0; i < list.length; i++) {
                if (list[i] === '(') depth++;
                else if (list[i] === ')') depth--;
                else if (list[i] === ',' && depth === 0) { parts.push(list.slice(start, i).trim()); start = i + 1; }
            }
            parts.push(list.slice(start).trim());
            return parts;
        };
        // A forgiving list keeps the alternatives an engine knows; null when one is left with none.
        const forgive = selector => {
            const match = /:(is|where)\(/.exec(selector);
            if (!match) return selector;
            const open = match.index + match[0].length - 1;
            let depth = 0, close = open;
            for (; close < selector.length; close++) {
                if (selector[close] === '(') depth++;
                else if (selector[close] === ')' && --depth === 0) break;
            }
            const kept = split(selector.slice(open + 1, close)).filter(alternative => !alternative.includes(':dir('));
            if (kept.length === 0) return null;
            const rest = forgive(selector.slice(close + 1));
            return rest === null ? null : selector.slice(0, open + 1) + kept.map(k => forgive(k)).join(', ') + ')' + rest;
        };
        let dropped = 0;
        const walk = list => {
            for (let i = list.cssRules.length - 1; i >= 0; i--) {
                const rule = list.cssRules[i];
                if (rule instanceof CSSSupportsRule && /selector\(\s*:dir\(/.test(rule.conditionText)) {
                    const applies = /^\s*not\b/.test(rule.conditionText);
                    const inner = applies ? Array.from(rule.cssRules, r => r.cssText) : [];
                    list.deleteRule(i);
                    for (const text of inner.reverse()) list.insertRule(text, i);
                    dropped++;
                    continue;
                }
                if (rule instanceof CSSStyleRule) {
                    if (!rule.selectorText.includes(':dir(')) continue;
                    const kept = split(rule.selectorText).map(forgive);
                    if (kept.some(alternative => alternative === null || outsideForgiving(alternative).includes(':dir('))) {
                        list.deleteRule(i);
                    } else {
                        rule.selectorText = kept.join(', ');
                    }
                    dropped++;
                    continue;
                }
                if (rule.cssRules) walk(rule);
            }
        };
        for (const sheet of document.styleSheets) {
            try { walk(sheet); } catch { /* a cross-origin sheet */ }
        }
        return dropped;
    }";

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    [TestMethod]
    public async Task WithDir_EveryPartFollowsTheDirectionItIsLaidOutIn()
    {
        await Open();

        var (ltr, rtl) = await References();

        AssertSet(await Read("#inherited"), ltr, rtl, "rtl", "a direction inherited from a box");
        AssertSet(await Read("#nested"), ltr, rtl, "ltr", "Dir=Ltr inside a right-to-left component");
        AssertSet(await Read("#nested-same"), ltr, rtl, NestableProbes.ToDictionary(probe => probe, _ => "ltr"), "Dir=Ltr inside a right-to-left component of its own kind");
    }

    [TestMethod]
    public async Task WithoutDir_AnExplicitDirStillTurnsEveryPartAround()
    {
        await Open();

        var (ltr, rtl) = await References();

        Assert.IsTrue(await Page.EvaluateAsync<int>(DropDir) > 0, "No :dir() rule was found to drop.");

        AssertSet(await Read("#ltr"), ltr, rtl, "ltr", "no direction");
        AssertSet(await Read("#own"), ltr, rtl, "rtl", "Dir=Rtl");

        // The class reaches the parts through child combinators only, so a component nested in one that reads the other
        // way keeps its own direction, whatever kind of component it is nested in.
        AssertSet(await Read("#nested"), ltr, rtl, "ltr", "Dir=Ltr inside a right-to-left component");

        // What :dir() alone can tell: a direction inherited from a box is lost, which is what proves the rules were dropped.
        AssertSet(await Read("#inherited"), ltr, rtl, "ltr", "a direction inherited from a box, without :dir()");
    }

    [TestMethod]
    public async Task WithoutDir_OnlyTheSupportsFallbacksReachANestedInstanceOfTheirOwnKind()
    {
        // The class an explicit Dir renders is read behind a descendant combinator only inside
        // @supports not selector(:dir(rtl)), where nothing better is known (the card's skeleton, the grid's glyphs, the
        // editor's icons); there, and only for those parts, a nested instance of the same kind that reads the other way
        // is turned around with its outer one. The parts reached through child combinators never are.
        await Open();

        var (ltr, rtl) = await References();
        await Page.EvaluateAsync<int>(DropDir);

        var nested = await Read("#nested-same");
        var expected = NestableProbes.ToDictionary(probe => probe, probe => probe is "card" ? "rtl" : "ltr");

        AssertSet(nested, ltr, rtl, expected, "Dir=Ltr inside a right-to-left component of its own kind, without :dir()");
    }

    private async Task Open()
    {
        await Page.GotoAsync($"{BaseUrl}{PageUrl}");
        await WaitForStatus("Ready"); // wait for Blazor SignalR circuit to be interactive

        // The snack bar's items arrive with a render of their own after the first one.
        await Page.WaitForFunctionAsync(@"() => document.querySelectorAll('[data-probe=snackbar-bar] .bit-snb-prb').length === 4",
            null, new() { Timeout = DefaultTimeout });
    }

    // What each part looks like reading left to right, and reading right to left, which must differ.
    private async Task<(Dictionary<string, string?> Ltr, Dictionary<string, string?> Rtl)> References()
    {
        var ltr = await Read("#ltr");
        var rtl = await Read("#own");

        foreach (var probe in Probes)
        {
            Assert.IsNotNull(ltr[probe], $"{probe}: the part is not on the page.");
            Assert.IsNotNull(rtl[probe], $"{probe}: the part is not on the page with Dir=Rtl.");
            Assert.AreNotEqual(ltr[probe], rtl[probe], $"{probe}: Dir=Rtl does not turn the part around ({rtl[probe]}).");
        }

        return (ltr, rtl);
    }

    private async Task<Dictionary<string, string?>> Read(string set)
        => JsonSerializer.Deserialize<Dictionary<string, string?>>(await Page.EvaluateAsync<string>(ReadSet, set), _json)!;

    private static void AssertSet(Dictionary<string, string?> actual, Dictionary<string, string?> ltr, Dictionary<string, string?> rtl, string expected, string where)
        => AssertSet(actual, ltr, rtl, Probes.ToDictionary(probe => probe, _ => expected), where);

    private static void AssertSet(Dictionary<string, string?> actual, Dictionary<string, string?> ltr, Dictionary<string, string?> rtl, Dictionary<string, string> expected, string where)
    {
        var wrong = expected.Keys.Select(probe =>
        {
            var value = actual[probe];
            var reads = value == ltr[probe] ? "ltr" : value == rtl[probe] ? "rtl" : $"neither ({value ?? "missing"})";

            return reads == expected[probe] ? null : $"{probe} reads {reads}, expected {expected[probe]}";
        }).Where(message => message is not null).ToArray();

        Assert.AreEqual(0, wrong.Length, $"{where}:\n{string.Join("\n", wrong)}");
    }
}
