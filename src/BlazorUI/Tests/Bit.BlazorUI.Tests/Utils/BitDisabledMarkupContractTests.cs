using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins how a disabled component says so in its markup, which an app's stylesheets, scripts and tests are written
/// against: the bit-dis class on the root of every component that is Disabled, and on every native control it renders
/// disabled the disabled attribute together with aria-disabled="true" - so one [aria-disabled="true"] selector or
/// locator finds a disabled control whether it is a native button, an input or a part conveyed through aria-disabled
/// alone (a loading button, AllowDisabledFocus, a link), and :disabled still finds the native ones.
/// </summary>
/// <remarks>
/// The source of every component is read as well as a render of each, since most of the controls a component renders
/// disabled - the buttons of a calendar, of a toolbar, of an open popup - are drawn only in states a plain render of it
/// never reaches.
/// </remarks>
[TestClass]
public class BitDisabledMarkupContractTests : BunitTestContext
{
    private const string ROOT = "data-splat-root";

    // The elements HTML defines the disabled attribute on.
    private static readonly HashSet<string> NativeControls = new(StringComparer.OrdinalIgnoreCase)
    {
        "button", "input", "select", "textarea", "fieldset", "option", "optgroup",
    };

    private static readonly Regex TagStart = new(@"<(?<name>[a-zA-Z][\w.:-]*)", RegexOptions.Compiled);

    private static readonly Regex DisabledAttribute = new(@"(?<![\w-])disabled\s*=", RegexOptions.Compiled);

    public static IEnumerable<object[]> Components =>
        BitDisabledActivationContractTests.Components.Concat(BitDisabledActivationContractTests.ContentHosts);

    // The carousels, the theme and the accent color switchers read services of the library's own.
    [TestInitialize]
    public void AddServices() => Context.Services.AddBitBlazorUIExtrasServices();

    [TestMethod]
    public void EveryNativeControlWrittenDisabledShouldAlsoBeAriaDisabled()
    {
        var controls = ReadComponentTags()
            .Where(t => NativeControls.Contains(t.Name) && DisabledAttribute.IsMatch(t.Tag))
            .ToArray();

        Assert.IsTrue(controls.Length > 300, $"Only {controls.Length} native controls writing disabled were found in the components' markup.");

        // A hidden input only carries a value to the form; nothing reaches it, so it has no state to report.
        var offenders = controls
            .Where(t => t.Tag.Contains("aria-disabled", StringComparison.Ordinal) is false)
            .Where(t => t.Tag.Contains("type=\"hidden\"", StringComparison.Ordinal) is false)
            .Select(t => $"{t.File}:{t.Line} <{t.Name}>")
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        "These native controls write disabled without aria-disabled, which every disabled native control of the " +
                        $"library writes beside it (see src/BlazorUI/CLAUDE.md):\n{string.Join("\n", offenders)}");
    }

    [TestMethod]
    public void NoElementOtherThanANativeControlShouldWriteTheDisabledAttribute()
    {
        // HTML defines disabled on the form controls alone: on any other element it reports nothing to assistive
        // technologies, takes nothing out of the tab order and matches no :disabled - aria-disabled is what says it.
        var offenders = ReadComponentTags()
            .Where(t => char.IsLower(t.Name[0]) && NativeControls.Contains(t.Name) is false && DisabledAttribute.IsMatch(t.Tag))
            .Select(t => $"{t.File}:{t.Line} <{t.Name}>")
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        $"These elements write the disabled attribute, which HTML defines on the form controls only:\n{string.Join("\n", offenders)}");
    }

    [TestMethod]
    [DynamicData(nameof(Components))]
    public void DisabledComponentShouldCarryTheDisabledClassOnItsRoot(Type type)
    {
        var root = Render(type, disabled: true).Find($"[{ROOT}]");

        Assert.IsTrue(root.ClassList.Contains("bit-dis"), $"The root of a disabled {type.Name} has no bit-dis class.");
    }

    [TestMethod]
    [DynamicData(nameof(Components))]
    public void EnabledComponentShouldNotCarryTheDisabledClassOnItsRoot(Type type)
    {
        var root = Render(type, disabled: false).Find($"[{ROOT}]");

        Assert.IsFalse(root.ClassList.Contains("bit-dis"), $"The root of an enabled {type.Name} has the bit-dis class.");
    }

    [TestMethod]
    [DynamicData(nameof(Components))]
    public void DisabledComponentShouldPairEveryDisabledControlWithAriaDisabled(Type type)
    {
        var component = Render(type, disabled: true);

        var offenders = component.FindAll("[disabled]:not(input[type=hidden])")
                                 .Where(element => element.GetAttribute("aria-disabled") != "true")
                                 .Select(element => element.TagName.ToLowerInvariant() + (element.ClassName is { Length: > 0 } c ? "." + c.Replace(' ', '.') : ""))
                                 .ToArray();

        Assert.AreEqual(0, offenders.Length,
                        $"A disabled {type.Name} renders these elements disabled without aria-disabled=\"true\":\n{string.Join("\n", offenders)}");
    }

    private IRenderedComponent<BitParams> Render(Type type, bool disabled)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent(0, type);
                builder.AddAttribute(1, nameof(BitComponentBase.Disabled), disabled);
                builder.AddAttribute(2, ROOT, "");
                builder.CloseComponent();
            });
        });
    }

    // Every opening tag of the components' markup, with the file and line it starts on. A tag ends at the first '>'
    // outside its attribute values and the parentheses of a Razor expression - never at the one of a '=>'.
    private static IEnumerable<(string File, int Line, string Name, string Tag)> ReadComponentTags()
    {
        var folders = new[]
        {
            SourceFiles.GetDirectory("Bit.BlazorUI", "Components"),
            SourceFiles.GetDirectory("Bit.BlazorUI.Extras", "Components"),
            SourceFiles.GetDirectory("Bit.BlazorUI.Legacy", "Components"),
        };

        var files = folders.SelectMany(folder => Directory.EnumerateFiles(folder, "*.razor", SearchOption.AllDirectories)).ToArray();

        Assert.IsTrue(files.Length > 100, $"Only {files.Length} component markup files were found.");

        foreach (var file in files)
        {
            var markup = SourceFiles.ReadFullPath(file);
            var name = Path.GetRelativePath(SourceFiles.Root, file).Replace('\\', '/');

            foreach (Match match in TagStart.Matches(markup))
            {
                var end = match.Index + match.Length;
                var depth = 0;
                char? quote = null;

                for (; end < markup.Length; end++)
                {
                    var c = markup[end];

                    if (c == '(') depth++;
                    else if (c == ')') depth = Math.Max(0, depth - 1);
                    else if (quote is not null) { if (c == quote) quote = null; }
                    else if (c is '"' or '\'') quote = c;
                    else if (c == '>' && depth == 0 && markup[end - 1] != '=') break;
                }

                var tag = markup[match.Index..Math.Min(end + 1, markup.Length)];
                var line = markup.AsSpan(0, match.Index).Count('\n') + 1;

                yield return (name, line, match.Groups["name"].Value, tag);
            }
        }
    }
}
