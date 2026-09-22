using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Guards the "a <see cref="Microsoft.JSInterop.DotNetObjectReference{T}"/> belongs to the component that
/// created it" rule: the JavaScript side is handed one, holds it for as long as it has listeners to call it
/// from, and lets go of it - but never releases it.
///
/// <para>
/// Releasing it from JavaScript looks harmless while the only thing that ever tears a feature down is the
/// component's own disposal, and it is not. Several components hand the same reference to more than one
/// feature (a picker gives it to its gesture, its callout and its keyboard handling), and some tear one
/// feature down while the component stays on the page - a breadcrumb stops observing its own width when
/// auto-collapsing is turned off, a panel drops its swipe gestures every time it closes. A release from
/// JavaScript there takes the rest of that component's interop with it, silently: the callbacks simply stop
/// arriving. It also cuts the other way, since the JS teardown bails out early on an element that is already
/// gone, leaving the reference registered for the life of the page.
/// </para>
///
/// <para>
/// So the rule is one-sided and easy to check: no TypeScript in the library calls <c>dispose()</c> on
/// anything typed <c>DotNetObject</c>.
/// </para>
/// </summary>
[TestClass]
public class DotNetReferenceOwnershipContractTests
{
    // Anything declared to be a DotNetObject: a parameter ("obj: DotNetObject"), a field or an interface
    // member, optional or not ("dotnetObj?: DotNetObject", "dotnetObj: DotNetObject | undefined").
    private static readonly Regex DotNetObjectDeclarationRegex =
        new(@"(?<name>\w+)\s*\??\s*:\s*DotNetObject\b", RegexOptions.Compiled);

    [TestMethod]
    public void TypeScript_ShouldNeverDisposeADotNetReference()
    {
        var blazorUiRoot = JsInteropSources.TryFindBlazorUiRoot();
        if (blazorUiRoot is null)
        {
            Assert.Inconclusive(
                "Skipped: could not locate the BlazorUI source root (the folder containing 'Bit.BlazorUI' " +
                "and 'Bit.BlazorUI.Extras'). The source tree is required to scan the TypeScript sources.");
            return;
        }

        var scannedNames = 0;
        var violations = new List<string>();

        foreach (var directory in JsInteropSources.ProjectDirectories(blazorUiRoot))
        foreach (var file in JsInteropSources.EnumerateSourceFiles(directory, "*.ts"))
        {
            if (file.EndsWith(".d.ts", StringComparison.OrdinalIgnoreCase)) continue;

            var text = File.ReadAllText(file);
            var masked = TsPromiseMethodScanner.MaskNonCode(text);

            // Names are collected per file: a DotNetObject declared in one of these namespaced classes is
            // only ever used in that same file, and scoping it keeps an everyday name like "obj" from
            // answering for an unrelated object somewhere else.
            var names = DotNetObjectDeclarationRegex.Matches(masked)
                .Select(m => m.Groups["name"].Value)
                .ToHashSet(StringComparer.Ordinal);

            scannedNames += names.Count;

            foreach (var name in names)
            {
                foreach (Match call in Regex.Matches(masked, $@"\b{Regex.Escape(name)}\s*\??\s*\.\s*dispose\s*\("))
                {
                    var line = masked.AsSpan(0, call.Index).Count('\n') + 1;
                    violations.Add($"  - {Path.GetFileName(file)}({line}): {call.Value.Trim()}");
                }
            }
        }

        Assert.IsTrue(scannedNames > 0,
            "Expected to find TypeScript declarations of DotNetObject, but found none. " +
            "The scanning in this test is likely broken or the source layout changed.");

        Assert.AreEqual(0, violations.Count,
            "The JavaScript side must never release a .NET reference - the component that created it owns it " +
            "and disposes it after the JS cleanup, so the cleanup's own callbacks still have a live target. " +
            "Let go of the reference here (set it to undefined, drop the entry) and leave the disposing to " +
            "the component:" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }
}
