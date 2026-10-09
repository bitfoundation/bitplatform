using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins that a state of a component (under the pointer, pressed, focused) never paints a part over an app's class.
/// </summary>
/// <remarks>
/// A state rule outranks the rule at rest - <c>.bit-acb:hover</c> is a class and a pseudo-class against the single
/// class of an app's <c>Class</c> or <c>Classes</c> - so one that set the colors itself would hand the part back to
/// the library exactly while the user is looking at it. The states move a private variable instead, which the rule at
/// rest paints from, so a part the app paints keeps that paint in every state it keeps it at rest (BitSplitter's
/// gutter, #13502). Two kinds of state rule still paint, by design: a disabled one, since a state the component is in
/// is read before anything else, and one inside <c>@media (forced-colors: active)</c>, where the browser forces the
/// app's colors anyway and the system colors are what the state draws with. The browser's scrollbar parts and the
/// controls BitMap's providers draw are left out as well, since no <c>Class</c> or <c>Classes</c> of a component
/// names them.
/// </remarks>
[TestClass]
public sealed class BitComponentStatePaintTests
{
    // The interaction states. A :not() of one is the opposite of the state, and is cut out before this is matched.
    private static readonly Regex StatePseudoClass = new(
        @":(?:hover|active|focus|focus-visible|focus-within)(?![a-z-])",
        RegexOptions.Compiled);

    private static readonly Regex DisabledSelector = new(
        @"\.bit-dis(?![a-z0-9-])|:disabled|\[disabled|\[aria-disabled",
        RegexOptions.Compiled);

    // BitMap's Leaflet and OpenLayers controls, whose own stylesheets paint their hover and focus.
    private static readonly Regex ThirdPartyControl = new(
        @"\.leaflet-bar a|\.ol-control button",
        RegexOptions.Compiled);

    // The properties an app's class paints a part with.
    private static readonly Regex PaintDeclaration = new(
        @"^\s*(?<property>color|background|background-color|border(?:-(?:top|right|bottom|left|inline|block)(?:-(?:start|end))?)?(?:-color)?|fill|stroke)\s*:",
        RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void NoInteractionStatePaintsOverAnAppsClass()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateComponentStylesheets())
        {
            var stylesheet = SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Replace("\r\n", "\n");

            foreach (var rule in SourceFiles.GetScssRules(stylesheet))
            {
                var headers = rule.Ancestors.Append(rule.Header).ToArray();

                if (headers.Any(header => header.Contains("forced-colors", StringComparison.Ordinal))) continue;

                // The selector the declarations apply to: every rule header around them, less the at-rules (@media,
                // @include, @if, ...), which apply to the selector they are written in.
                var selector = string.Join(" ", headers.Where(header => header.StartsWith('@') is false));

                if (selector.Length == 0) continue;

                var withoutNegations = RemoveNegations(selector);

                if (StatePseudoClass.IsMatch(withoutNegations) is false) continue;
                if (DisabledSelector.IsMatch(withoutNegations)) continue;

                // The browser's own scrollbar is no part an app's Class or Classes names.
                if (selector.Contains("::-webkit-scrollbar", StringComparison.Ordinal)) continue;

                // Nor are the controls a map provider draws, and its stylesheet - loaded after ours - paints their
                // states at the same weight, so a state of ours that only moved a variable would lose to it.
                if (ThirdPartyControl.IsMatch(selector)) continue;

                var declarations = SourceFiles.GetScssDeclarations(stylesheet[rule.Index..], rule.Header);

                foreach (Match paint in PaintDeclaration.Matches(declarations))
                {
                    var line = stylesheet.AsSpan(0, rule.Index).Count('\n') + 1;

                    offenders.Add($"{Path.GetFileName(file)}:{line} {paint.Groups["property"].Value} in {Squash(selector)}");
                }
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"A state rule paints a part directly, which outranks an app's class on it; move a private variable the rule at rest paints from instead:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }

    // The selector less every :not(...) in it, nested parentheses and all.
    private static string RemoveNegations(string selector)
    {
        var result = new StringBuilder(selector.Length);

        for (var i = 0; i < selector.Length; i++)
        {
            if (string.CompareOrdinal(selector, i, ":not(", 0, 5) != 0)
            {
                result.Append(selector[i]);
                continue;
            }

            var depth = 0;
            for (i += 4; i < selector.Length; i++)
            {
                if (selector[i] == '(') depth++;
                else if (selector[i] == ')' && --depth == 0) break;
            }
        }

        return result.ToString();
    }

    private static string Squash(string selector) => Regex.Replace(selector, @"\s+", " ");

    private static IEnumerable<string> EnumerateComponentStylesheets()
        => new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories));
}
