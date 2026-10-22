using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins that a number the library writes into an attribute reads the same in every culture. Blazor writes an attribute
/// value that is not a string with its ToString in the current culture, so a negative one - a tabindex of -1, a set
/// size of -1 for "not known yet" - reaches the page as "‎−1" under fa-IR and as "؜-1" under ar-SA, which
/// no browser reads as a number: a roving tab stop then falls back to the element's own focusability, and every day of a
/// calendar or every cell of a grid lands in the tab order. Such a value is written as a string ("0" / "-1") or with
/// the invariant culture.
/// </summary>
/// <remarks>
/// The suite itself runs in the culture of the machine (invariant on CI), so these are the tests that run components
/// under the cultures that write a number differently.
/// </remarks>
[TestClass]
public class BitCultureInvariantMarkupContractTests : BunitTestContext
{
    // What a culture writes into a number and no attribute that holds one may carry: the bidi marks before a minus
    // sign, the minus sign itself (U+2212) and the Arabic decimal and group separators.
    private static readonly Regex CultureMarks = new("[‎‏؜−٫٬]");

    // The attributes that hold text for a reader, which is meant to be in the reader's culture.
    private static readonly HashSet<string> TextAttributes = ["aria-label", "aria-description", "aria-roledescription", "aria-valuetext", "title", "alt", "placeholder", "value"];

    [TestMethod]
    public void NoNegativeNumberLiteralIsBoundToAnAttribute()
    {
        // `tabindex="@(focused ? 0 : -1)"` binds an int, which is written in the current culture; `? "0" : "-1"` binds
        // the string the browser reads.
        var pattern = new Regex(@"[\w:-]+=""@\([^""]*\?\s*(-\d+\s*:\s*-?\d+|-?\d+\s*:\s*-\d+)\s*\)""");

        var offenders = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project), "*.razor", SearchOption.AllDirectories))
            .Where(file => file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") is false)
            .SelectMany(file => File.ReadAllLines(file).Select((line, index) => (File: file, Number: index + 1, Line: line)))
            .Where(entry => pattern.IsMatch(entry.Line))
            .Select(entry => $"{Path.GetRelativePath(SourceFiles.Root, entry.File)}:{entry.Number}: {entry.Line.Trim()}")
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.IsEmpty(offenders, string.Join(Environment.NewLine, offenders));
    }

    [TestMethod, DataRow("fa-IR"), DataRow("ar-SA")]
    public void TheRovingTabStopsOfTheCalendarsAreWrittenInvariantly(string culture)
    {
        InCulture(culture, () =>
        {
            AssertNoCultureMarks(RenderComponent<BitCalendar>());
            AssertNoCultureMarks(RenderComponent<BitDatePicker>(parameters => parameters.Add(p => p.IsOpen, true)));
            AssertNoCultureMarks(RenderComponent<BitDateRangePicker>(parameters => parameters.Add(p => p.IsOpen, true)));
        });
    }

    [TestMethod, DataRow("fa-IR"), DataRow("ar-SA")]
    public void TheTabStopsAComponentTakesOutOfTheTabOrderAreWrittenInvariantly(string culture)
    {
        InCulture(culture, () =>
        {
            AssertNoCultureMarks(RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
            {
                parameters.Add(p => p.Items, [new() { Text = "Folder 1" }, new() { Text = "Folder 2", IsSelected = true }]);
                parameters.Add(p => p.Disabled, true);
            }));

            AssertNoCultureMarks(RenderComponent<BitMenuButton<BitMenuButtonItem>>(parameters =>
            {
                parameters.Add(p => p.Items, [new() { Text = "Item 1" }]);
                parameters.Add(p => p.Split, true);
                parameters.Add(p => p.Disabled, true);
            }));

            AssertNoCultureMarks(RenderComponent<BitNumberField<int>>(parameters => parameters.Add(p => p.Mode, BitSpinButtonMode.Compact)));
        });
    }

    [TestMethod, DataRow("fa-IR"), DataRow("ar-SA")]
    public void TheDayIdsOfTheFullCalendarAreGregorianInEveryCulture(string culture)
    {
        // A day's id is what its script, its keyboard and an app's own code find the day by, so it names the same date
        // whatever calendar the culture reads it in - Persian under fa-IR, Um Al Qura under ar-SA (which also has no
        // year for a date outside 1900 to 2077).
        InCulture(culture, () =>
        {
            var component = RenderComponent<BitFullCalendar>(parameters =>
            {
                parameters.Add(p => p.DefaultDate, new DateTime(2026, 3, 18));
                parameters.Add(p => p.DefaultView, BitFullCalendarView.Day);
            });

            var ids = component.FindAll(".bit-bfc-mini-calendar-day").Select(day => day.Id ?? string.Empty).ToList();

            Assert.IsTrue(ids.Any(id => id.EndsWith("-20260318", StringComparison.Ordinal)), string.Join(", ", ids));
        });
    }

    // Both the current and the default thread cultures, since a render can also run on a dispatcher thread of its own.
    private static void InCulture(string name, Action action)
    {
        var (culture, uiCulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        var (defaultCulture, defaultUICulture) = (CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture);

        try
        {
            var target = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = target;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = target;
            action();
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, uiCulture);
            (CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture) = (defaultCulture, defaultUICulture);
        }
    }

    private static void AssertNoCultureMarks<TComponent>(IRenderedComponent<TComponent> component) where TComponent : IComponent
    {
        var offenders = component.FindAll("*")
                                 .SelectMany(element => element.Attributes.Select(attribute => (Element: element, Attribute: attribute)))
                                 .Where(entry => TextAttributes.Contains(entry.Attribute.Name) is false && CultureMarks.IsMatch(entry.Attribute.Value))
                                 .Select(entry => $"<{entry.Element.LocalName} class=\"{entry.Element.ClassName}\"> {entry.Attribute.Name}=\"{entry.Attribute.Value}\"")
                                 .Distinct()
                                 .ToList();

        Assert.IsEmpty(offenders, $"{typeof(TComponent).Name}: {string.Join(Environment.NewLine, offenders)}");
    }
}
