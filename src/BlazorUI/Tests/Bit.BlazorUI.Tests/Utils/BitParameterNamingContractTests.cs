using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the naming rule for a component's state: a parameter of a component is spelled without an Is (Disabled,
/// Vertical, Loading, Open), and only a model of one item of a parent - a *Option or *Item component - spells its flags
/// with one (IsDisabled, IsSelected, IsExpanded), which keeps the per-item setting apart from the component's own.
/// </summary>
/// <remarks>
/// The parameters that predate the rule are listed in <see cref="_outliers"/>. They are renamed one concept at a time,
/// each break recorded in BreakingChanges.md, and a rename takes its entries off the list; nothing new joins it.
/// </remarks>
[TestClass]
public partial class BitParameterNamingContractTests
{
    // The parameters spelled with an Is when the rule was set, by component. A bindable value is listed with its
    // callback and its uncontrolled default (IsOpen, IsOpenChanged, DefaultIsOpen): they are renamed as one.
    private static readonly Dictionary<string, string[]> _outliersByComponent = new()
    {
        ["BitMenuButton"] = ["IsOpen", "IsOpenChanged"],
        ["BitCircularTimePicker"] = ["IsOpen", "IsOpenChanged"],
        ["BitDatePicker"] = ["IsMonthPickerVisible", "IsOpen", "IsOpenChanged"],
        ["BitDateRangePicker"] = ["IsMonthPickerVisible", "IsOpen", "IsOpenChanged"],
        ["BitDropdown"] = ["IsOpen", "IsOpenChanged"],
        ["BitNumberField"] = ["IsInputReadOnly"],
        ["BitTimePicker"] = ["IsOpen", "IsOpenChanged"],
        ["BitDropMenu"] = ["IsOpen", "IsOpenChanged", "DefaultIsOpen"],
        ["_BitNavBarChild"] = ["IsLink"],
        ["BitCallout"] = ["IsOpen", "IsOpenChanged", "DefaultIsOpen"],
        ["BitDialog"] = ["IsAlert", "IsCancelButtonEnabled", "IsDraggable", "IsOkButtonEnabled", "IsOpen", "IsOpenChanged", "DefaultIsOpen"],
        ["BitModal"] = ["IsAlert", "IsOpen", "IsOpenChanged", "DefaultIsOpen"],
        ["BitPanel"] = ["IsAlert", "IsOpen", "IsOpenChanged"],
        ["BitOverlay"] = ["IsOpen", "IsOpenChanged", "DefaultIsOpen"],
        ["BitContextMenu"] = ["IsOpen", "IsOpenChanged"],
        ["BitCountryPicker"] = ["IsOpen", "IsOpenChanged"],
        ["BitFcDayCell"] = ["IsRovingCell", "IsLastColumn", "IsRowStart"],
        ["BitFcMonthEventBadge"] = ["IsLead"],
        ["BitNavPanel"] = ["IsOpen", "IsOpenChanged"],
        ["BitNotificationCenter"] = ["IsOpen", "IsOpenChanged"],
        ["BitPhoneInput"] = ["IsOpen", "IsOpenChanged"],
        ["BitPopConfirm"] = ["IsOpen", "IsOpenChanged"],
        ["BitTimeZonePicker"] = ["IsOpen", "IsOpenChanged"],
        ["BitChartLegacy"] = ["IsDateAdapterRequired"],
        ["BitDataGridLegacyColumnBase"] = ["IsDefaultSort"],
        ["BitAiChatMessage"] = ["IsLatest"],
        ["BitAiCodeBlockView"] = ["IsStreaming"],
        ["BitAiStreamingMarkdown"] = ["IsStreaming"],
    };

    private static IEnumerable<(string Component, string Parameter)> Outliers =>
        _outliersByComponent.SelectMany(e => e.Value.Select(p => (e.Key, p)));

    [TestMethod]
    public void NoComponentParameterShouldBeSpelledWithAnIsBesidesTheKnownOutliers()
    {
        var known = Outliers.ToHashSet();

        var offenders = ReadIsParameters().Where(p => known.Contains(p) is false)
                                          .Select(p => $"{p.Component}.{p.Parameter}")
                                          .ToList();

        Assert.IsEmpty(offenders, "A component parameter is spelled with an Is; spell it without (Disabled, Vertical, Loading, Open). " +
                                  "Only a *Option or *Item component spells its flags with one: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void TheKnownOutliersShouldStillExist()
    {
        var found = ReadIsParameters().ToHashSet();

        var stale = Outliers.Where(p => found.Contains(p) is false)
                             .Select(p => $"{p.Component}.{p.Parameter}")
                             .ToList();

        Assert.IsEmpty(stale, "A listed outlier is no longer a parameter spelled with an Is (renamed or removed); take it off the list: " + string.Join(", ", stale));
    }

    [TestMethod]
    public void TheKnownOutliersShouldBeListedOnce()
    {
        var repeated = Outliers.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => $"{g.Key.Component}.{g.Key.Parameter}").ToList();

        Assert.IsEmpty(repeated, string.Join(", ", repeated));
        Assert.IsTrue(_outliersByComponent.Values.All(v => v.Length > 0), "A component with no outlier left is taken off the list.");
    }

    [TestMethod]
    public void TheRuleShouldReadAnIsWhereverAWordOfTheNameStartsWithIt()
    {
        Assert.IsTrue(IsWord().IsMatch("IsOpen"));
        Assert.IsTrue(IsWord().IsMatch("IsOpenChanged"));
        Assert.IsTrue(IsWord().IsMatch("DefaultIsExpanded"));
        Assert.IsFalse(IsWord().IsMatch("Isolated"));
        Assert.IsFalse(IsWord().IsMatch("VisibleItems"));
        Assert.IsFalse(IsWord().IsMatch("AxisInverted"));
        Assert.IsFalse(IsWord().IsMatch("Disabled"));

        Assert.IsTrue(IsItemModel(typeof(BitDropdownOption<>)));
        Assert.IsTrue(IsItemModel(typeof(BitPivotItem)));
        Assert.IsFalse(IsItemModel(typeof(BitDropdown<,>)));
    }

    // Every parameter of a component of the four packages, declared on the type that declares it (a base's parameters
    // once, on the base), whose name has an Is word, except on a *Option or *Item component.
    private static IEnumerable<(string Component, string Parameter)> ReadIsParameters()
    {
        var assemblies = new[] { typeof(BitButton).Assembly, typeof(BitAppShell).Assembly, typeof(Bit.BlazorUI.Legacy.BitRichTextEditorLegacy).Assembly, typeof(BitAiChat).Assembly };

        foreach (var type in assemblies.SelectMany(LoadableTypes.Of))
        {
            if (type.IsPublic is false || typeof(IComponent).IsAssignableFrom(type) is false || IsItemModel(type)) continue;

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (property.GetCustomAttribute<ParameterAttribute>() is null || IsWord().IsMatch(property.Name) is false) continue;

                // A predicate (IsDateDisabled, IsStickyItem) asks its question of one item at a time, which is what
                // the Is of an item model is for.
                if (typeof(Delegate).IsAssignableFrom(property.PropertyType)) continue;

                yield return (NameOf(type), property.Name);
            }
        }
    }

    private static bool IsItemModel(Type type)
    {
        var name = NameOf(type);

        return name.EndsWith("Option", StringComparison.Ordinal) || name.EndsWith("Item", StringComparison.Ordinal);
    }

    private static string NameOf(Type type) => type.Name.Split('`')[0];

    // An Is that starts a word of a PascalCase name: at its start or after a lowercase letter or a digit, and followed by
    // the capital of the next word.
    [GeneratedRegex(@"(?:^|(?<=[a-z0-9]))Is(?=[A-Z])")]
    private static partial Regex IsWord();
}
