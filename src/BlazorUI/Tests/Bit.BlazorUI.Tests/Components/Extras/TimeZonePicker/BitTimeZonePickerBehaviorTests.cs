using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TimeZonePicker;

/// <summary>
/// Covers what <see cref="BitTimeZonePickerTests"/> leaves out: the keys that open, walk, pick from and close the list,
/// the callout round trips (what the toggle is handed, the dismissals the JS side reports, the keys the field prevents
/// while open), the open state bound both ways and held by the page, the focus ring and the focus events, the
/// responsive panel, the parts that Styles and Classes reach, the attributes splatted on the root and on the field, the
/// look parameters, the templates, and what a disabled or a disposed picker takes back.
/// </summary>
[TestClass]
public class BitTimeZonePickerBehaviorTests : BunitTestContext
{
    // A moment without daylight saving time in the northern hemisphere, so the offsets below hold whenever the tests run.
    private static readonly DateTimeOffset January = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    // West to east once the list has sorted them: Los Angeles, New York, London, Berlin, Tehran, Kolkata, Tokyo.
    private static readonly List<BitTimeZone> SevenTimeZones =
    [
        BitTimeZones.Find("Asia/Tokyo")!,
        BitTimeZones.Find("Europe/Berlin")!,
        BitTimeZones.Find("America/New_York")!,
        BitTimeZones.Find("Asia/Tehran")!,
        BitTimeZones.Find("Europe/London")!,
        BitTimeZones.Find("Asia/Kolkata")!,
        BitTimeZones.Find("America/Los_Angeles")!,
    ];



    #region Keyboard

    [TestMethod]
    public void BitTimeZonePickerShouldCloseTheListWithEscapeAndAltArrowUp()
    {
        var closed = 0;

        var component = RenderPicker<string>(parameters => parameters.Add(p => p.OnClose, () => closed++));

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").KeyDown("Escape");

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, closed);

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });

        // From the search box Alt+Up only closes the list.
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(2, closed);
        Assert.IsNull(component.Instance.Value);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldSelectWithAltArrowUpWithoutASearchBox()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.NoSearchBox, true));

        component.Find("button.bit-tzp-fld").Click();
        component.Find("button.bit-tzp-fld").KeyDown("ArrowDown");
        component.Find("button.bit-tzp-fld").KeyDown(new KeyboardEventArgs { Key = "ArrowUp", AltKey = true });

        // A select-only list takes the option the arrows are on, the way the combobox pattern has it.
        Assert.AreEqual("America/New_York", component.Instance.Value);
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod,
        DataRow("Home", 0),
        DataRow("End", 6)]
    public void BitTimeZonePickerShouldOpenOnTheFirstOrLastTimeZoneWithHomeOrEnd(string key, int expected)
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").KeyDown(key);

        Assert.IsTrue(component.Instance.IsOpen);

        var options = component.FindAll("button.bit-tzp-itm");

        Assert.AreEqual(options[expected].Id, component.Find(".bit-tzp-srch").GetAttribute("aria-activedescendant"));
        Assert.IsTrue(options[expected].ClassList.Contains("bit-tzp-act"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNotOpenOnALetterTypedWithAModifier()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").KeyDown(new KeyboardEventArgs { Key = "t", CtrlKey = true });
        component.Find("button.bit-tzp-fld").KeyDown(new KeyboardEventArgs { Key = "t", AltKey = true });
        component.Find("button.bit-tzp-fld").KeyDown(new KeyboardEventArgs { Key = "t", MetaKey = true });

        // A modified letter is a shortcut of the page, and a space is the native click of the field.
        component.Find("button.bit-tzp-fld").KeyDown(" ");

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldWrapTheKeyboardNavigationOfTheList()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").KeyDown("ArrowUp");

        // Up from the first time zone lands on the last one.
        Assert.IsTrue(component.FindAll("button.bit-tzp-itm")[6].ClassList.Contains("bit-tzp-act"));

        component.Find(".bit-tzp-srch").KeyDown("ArrowDown");

        Assert.IsTrue(component.FindAll("button.bit-tzp-itm")[0].ClassList.Contains("bit-tzp-act"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRespectNoWrapNavigation()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.NoWrapNavigation, true));

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").KeyDown("ArrowUp");

        Assert.IsTrue(component.FindAll("button.bit-tzp-itm")[0].ClassList.Contains("bit-tzp-act"));

        component.Find(".bit-tzp-srch").KeyDown("End");

        // Home and End belong to the caret of the search box, so the active time zone stays where it is.
        Assert.IsTrue(component.FindAll("button.bit-tzp-itm")[0].ClassList.Contains("bit-tzp-act"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNavigateWithThePageKeys()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").KeyDown("PageDown");

        // A page is ten rows, clamped to the end of a shorter list.
        Assert.IsTrue(component.FindAll("button.bit-tzp-itm")[6].ClassList.Contains("bit-tzp-act"));

        component.Find(".bit-tzp-srch").KeyDown("PageUp");

        Assert.IsTrue(component.FindAll("button.bit-tzp-itm")[0].ClassList.Contains("bit-tzp-act"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldSelectWithSpaceOnlyWithoutASearchBox()
    {
        var withSearchBox = RenderPicker<string>();

        withSearchBox.Find("button.bit-tzp-fld").Click();
        withSearchBox.Find(".bit-tzp-srch").KeyDown(" ");

        // The space stays available for typing a term.
        Assert.IsNull(withSearchBox.Instance.Value);
        Assert.IsTrue(withSearchBox.Instance.IsOpen);

        var withoutSearchBox = RenderPicker<string>(parameters => parameters.Add(p => p.NoSearchBox, true));

        withoutSearchBox.Find("button.bit-tzp-fld").Click();
        withoutSearchBox.Find("button.bit-tzp-fld").KeyDown(" ");

        Assert.AreEqual("America/Los_Angeles", withoutSearchBox.Instance.Value);
        Assert.IsFalse(withoutSearchBox.Instance.IsOpen);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldHandTheListKeysOfTheFieldToTheSearchBox()
    {
        string? searched = null;

        var component = RenderPicker<string>(parameters => parameters.Add(p => p.OnSearch, s => searched = s));

        component.Find("button.bit-tzp-fld").Click();

        // A Shift+Tab took the focus back to the field with the list still open.
        component.Find("button.bit-tzp-fld").KeyDown("ArrowDown");

        Assert.IsTrue(component.FindAll("button.bit-tzp-itm")[1].ClassList.Contains("bit-tzp-act"));

        component.Find("button.bit-tzp-fld").KeyDown("t");

        Assert.AreEqual("t", searched);

        // Enter presses the field, which closes the list as a click on it does.
        component.Find("button.bit-tzp-fld").KeyDown("Enter");

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldReportTheSearchTerm()
    {
        var searched = new List<string?>();

        var component = RenderPicker<string>(parameters => parameters.Add(p => p.OnSearch, s => searched.Add(s)));

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").Input("tok");
        component.Find(".bit-tzp-srch").Input("");

        CollectionAssert.AreEqual(new[] { "tok", "" }, searched.Select(s => s ?? "").ToArray());
    }

    #endregion

    #region The callout

    [TestMethod]
    public void BitTimeZonePickerShouldPutTheOpenSearchBoxIntoTheTabOrderAfterTheField()
    {
        var closed = 0;

        var component = RenderPicker<string>(parameters => parameters.Add(p => p.OnClose, () => closed++));

        component.Find("button.bit-tzp-fld").Click();

        var open = LastToggle();
        Assert.AreEqual(true, open.Arguments[6]);
        Assert.AreEqual(component.Find("button.bit-tzp-fld").Id, open.Arguments[^3]);

        // The Tab of the search box is the JS side's to answer, which reports it through OnTabOut.
        component.Find(".bit-tzp-srch").KeyDown("Tab");

        Assert.IsTrue(component.Instance.IsOpen);

        component.InvokeAsync(() => component.Instance._OnTabOut()).GetAwaiter().GetResult();

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, closed);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldCloseAListWithoutASearchBoxOnTab()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.NoSearchBox, true));

        component.Find("button.bit-tzp-fld").Click();

        // Nothing to tab into: the focus stays on the field.
        Assert.AreEqual("", LastToggle().Arguments[^3]);

        component.Find("button.bit-tzp-fld").KeyDown("Tab");

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRelocateItsCalloutWithItsRootAsTheChain()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();

        var toggle = LastToggle();

        // Positioned against the field, inheriting from the root.
        Assert.AreEqual(component.Find("button.bit-tzp-fld").Id, toggle.Arguments[1]);
        Assert.AreEqual(component.Find(".bit-tzp-cal").Id, toggle.Arguments[3]);
        Assert.AreEqual(component.Find(".bit-tzp-ovl").Id, toggle.Arguments[5]);
        Assert.AreEqual(component.Find(".bit-tzp").Id, toggle.Arguments[^2]);
    }

    [TestMethod,
        DataRow(null, null),
        DataRow(BitDir.Rtl, true),
        DataRow(BitDir.Ltr, false),
        DataRow(BitDir.Auto, null)]
    public void BitTimeZonePickerShouldHandTheCalloutOnlyADirectionItsDirNames(BitDir? dir, bool? expected)
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Dir, dir));

        component.Find("button.bit-tzp-fld").Click();

        // A Dir left unset inherits the page's direction, and Auto takes the content's: the script reads it off the
        // element itself then, rather than being told the page is left to right.
        Assert.AreEqual(expected, LastToggle().Arguments[9]);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldHandTheCalloutItsPlacementParameters()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.DropDirection, BitDropDirection.All);
            parameters.Add(p => p.MaxHeight, 240);
            parameters.Add(p => p.Responsive, true);
        });

        component.Find("button.bit-tzp-fld").Click();

        var toggle = LastToggle();

        Assert.AreEqual(BitResponsiveMode.Panel, toggle.Arguments[7]);
        Assert.AreEqual(BitDropDirection.All, toggle.Arguments[8]);
        // The scroll container starts under the search box.
        Assert.AreEqual(32, toggle.Arguments[11]);
        Assert.AreEqual(240, toggle.Arguments[17]);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldHandTheCalloutItsDefaultsWithoutASearchBox()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.NoSearchBox, true));

        component.Find("button.bit-tzp-fld").Click();

        var toggle = LastToggle();

        Assert.AreEqual(BitResponsiveMode.None, toggle.Arguments[7]);
        Assert.AreEqual(BitDropDirection.TopAndBottom, toggle.Arguments[8]);
        Assert.AreEqual(0, toggle.Arguments[11]);
        // Zero leaves the height to the viewport.
        Assert.AreEqual(0, toggle.Arguments[17]);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldCloseWhenAnotherCalloutOpens()
    {
        var closed = 0;

        var component = RenderPicker<string>(parameters => parameters.Add(p => p.OnClose, () => closed++));

        component.Find("button.bit-tzp-fld").Click();

        var toggles = CountInvocations("BitBlazorUI.Callouts.toggle");

        component.InvokeAsync(() => component.Instance._CloseCalloutBeforeAnotherCalloutIsOpened()).GetAwaiter().GetResult();

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, closed);

        // The JS side has already hidden it.
        Assert.AreEqual(toggles, CountInvocations("BitBlazorUI.Callouts.toggle"));
        Assert.AreEqual("false", component.Find("button.bit-tzp-fld").GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldShowTheOverlayOnlyWhileTheCalloutIsOpen()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Styles, new BitTimeZonePickerClassStyles { Overlay = "opacity:0.5" }));

        StringAssert.Contains(component.Find(".bit-tzp-ovl").GetAttribute("style"), "display:none");

        component.Find("button.bit-tzp-fld").Click();

        var style = component.Find(".bit-tzp-ovl").GetAttribute("style")!;
        StringAssert.Contains(style, "display:block");
        // The overlay style is written after the display, so a display of its own would still win.
        StringAssert.Contains(style, "opacity:0.5");

        component.Find(".bit-tzp-ovl").Click();

        Assert.IsFalse(component.Instance.IsOpen);
        StringAssert.Contains(component.Find(".bit-tzp-ovl").GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRenderTheListOnlyAfterItIsOpened()
    {
        var component = RenderPicker<string>();

        Assert.AreEqual(0, component.FindAll("button.bit-tzp-itm").Count);

        component.Find("button.bit-tzp-fld").Click();

        Assert.AreEqual(SevenTimeZones.Count, component.FindAll("button.bit-tzp-itm").Count);

        component.Find("button.bit-tzp-fld").Click();

        // Once rendered the list stays in the document, so the next opening is measured against its real content.
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(SevenTimeZones.Count, component.FindAll("button.bit-tzp-itm").Count);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldOwnItsOpenCalloutBesideASplattedAriaOwns()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitTimeZonePicker<string>>(0);
            builder.AddAttribute(1, nameof(BitTimeZonePicker<string>.TimeZones), SevenTimeZones);
            builder.AddAttribute(2, "aria-owns", "own-popup");
            builder.CloseComponent();
        });

        Assert.AreEqual("own-popup", component.Find(".bit-tzp").GetAttribute("aria-owns"));

        component.Find("button.bit-tzp-fld").Click();

        Assert.AreEqual($"own-popup {component.Find(".bit-tzp-cal").Id}", component.Find(".bit-tzp").GetAttribute("aria-owns"));

        component.Find("button.bit-tzp-fld").Click();

        Assert.AreEqual("own-popup", component.Find(".bit-tzp").GetAttribute("aria-owns"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldScrollTheActiveOptionIntoView()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();

        // The first opening lands on the first time zone, which is scrolled to as well.
        Assert.AreEqual(component.FindAll("button.bit-tzp-itm")[0].Id, Context.JSInterop.Invocations["BitBlazorUI.Extras.scrollOptionIntoView"].Last().Arguments[0]);

        // Up from the first wraps to the last one (End belongs to the caret of the search box).
        component.Find(".bit-tzp-srch").KeyDown("ArrowUp");

        Assert.AreEqual(component.FindAll("button.bit-tzp-itm")[6].Id, Context.JSInterop.Invocations["BitBlazorUI.Extras.scrollOptionIntoView"].Last().Arguments[0]);
    }

    #endregion

    #region IsOpen and the public API

    [TestMethod]
    public void BitTimeZonePickerShouldRespectTwoWayBoundIsOpen()
    {
        var isOpen = false;
        var opened = 0;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.OnOpen, () => opened++);
        });

        component.Find("button.bit-tzp-fld").Click();

        Assert.IsTrue(isOpen);
        Assert.AreEqual(1, opened);

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual("false", component.Find("button.bit-tzp-fld").GetAttribute("aria-expanded"));
        Assert.AreEqual(false, LastToggle().Arguments[6]);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldOpenThroughIsOpenOnTheSelectedTimeZone()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.DefaultValue, "Asia/Tehran"));

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual("true", component.Find("button.bit-tzp-fld").GetAttribute("aria-expanded"));
        Assert.AreEqual(true, LastToggle().Arguments[6]);

        // The list opened from the outside still starts on the selection, so the first Enter picks it rather than nothing.
        var active = component.Find(".bit-tzp-act");
        Assert.AreEqual("Tehran", active.QuerySelector(".bit-tzp-inm")!.TextContent);
        Assert.AreEqual(active.Id, component.Find(".bit-tzp-srch").GetAttribute("aria-activedescendant"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNotOpenAnOpenListOrCloseAClosedOneTwice()
    {
        var opened = 0;
        var closed = 0;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.OnOpen, () => opened++);
            parameters.Add(p => p.OnClose, () => closed++);
        });

        component.InvokeAsync(() => component.Instance.CloseAsync()).GetAwaiter().GetResult();
        component.InvokeAsync(() => component.Instance.OpenAsync()).GetAwaiter().GetResult();
        component.InvokeAsync(() => component.Instance.OpenAsync()).GetAwaiter().GetResult();
        component.InvokeAsync(() => component.Instance.CloseAsync()).GetAwaiter().GetResult();

        Assert.AreEqual(1, opened);
        Assert.AreEqual(1, closed);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNotOpenProgrammaticallyWhenItCannotBeUsed()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.ReadOnly, true));

        component.InvokeAsync(() => component.Instance.OpenAsync()).GetAwaiter().GetResult();

        Assert.IsFalse(component.Instance.IsOpen);
        Context.JSInterop.VerifyNotInvoke("BitBlazorUI.Callouts.toggle");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldSelectATimeZoneInfoProgrammatically()
    {
        string? changed = null;

        var component = RenderPicker<string>(parameters => parameters.Add(p => p.OnChange, v => changed = v));

        component.InvokeAsync(() => component.Instance.SelectTimeZoneAsync(TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"))).GetAwaiter().GetResult();

        Assert.AreEqual("Asia/Tokyo", changed);
        Assert.AreEqual("Tokyo", component.Find(".bit-tzp-txt").TextContent);
        Assert.AreEqual("Asia/Tokyo", component.Instance.SelectedTimeZone?.Id);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNotReportTheSelectionOfTheSelectedTimeZone()
    {
        var changes = 0;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "Asia/Tehran");
            parameters.Add(p => p.OnChange, _ => changes++);
        });

        component.Find("button.bit-tzp-fld").Click();
        PickItem(component, "Tehran");

        Assert.AreEqual(0, changes);
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual("Tehran", component.Find(".bit-tzp-txt").TextContent);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNotChangeAOneWayBoundValue()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Value, "Asia/Tokyo"));

        component.Find("button.bit-tzp-fld").Click();
        PickItem(component, "Tehran");

        // Without a ValueChanged or an OnChange the page keeps the value, so the field keeps showing it.
        Assert.AreEqual("Asia/Tokyo", component.Instance.Value);
        Assert.AreEqual("Tokyo", component.Find(".bit-tzp-txt").TextContent);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldShowTheDisplayNameOfATimeZoneInfoTheListDoesNotOffer()
    {
        var custom = TimeZoneInfo.CreateCustomTimeZone("Custom/Zone", TimeSpan.FromHours(1), "Custom display name", "Custom standard name");

        var component = RenderPicker<TimeZoneInfo>(parameters => parameters.Add(p => p.Value, custom));

        Assert.AreEqual("Custom display name", component.Find(".bit-tzp-txt").TextContent);
        Assert.IsNull(component.Instance.SelectedTimeZone);
        Assert.AreEqual(0, component.FindAll(".bit-tzp-fld .bit-tzp-ofs").Count);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRenderNoHiddenFieldWithoutAName()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.DefaultValue, "Asia/Tokyo"));

        Assert.AreEqual(0, component.FindAll("input[type=hidden]").Count);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldPostTheCanonicalIdOfAValueWrittenAnotherWay()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Name, "timeZone");
            parameters.Add(p => p.Value, "India Standard Time");
        });

        // A Windows id is still the value, but what a form posts is the one id every runtime reads back.
        Assert.AreEqual("Asia/Kolkata", component.Find("input[type=hidden]").GetAttribute("value"));
    }

    #endregion

    #region Focus

    [TestMethod]
    public void BitTimeZonePickerShouldKeepTheFocusRingWhileTheCalloutIsOpen()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Focus();

        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-tzp-fcs"));

        component.Find("button.bit-tzp-fld").Click();
        component.Find("button.bit-tzp-fld").Blur();

        // The focus went into the search box of the list, which is part of the field as far as the user is concerned.
        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-tzp-fcs"));

        component.Find(".bit-tzp-srch").KeyDown("Escape");

        // Escape gives the focus back to the field.
        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-tzp-fcs"));

        component.Find("button.bit-tzp-fld").Blur();

        Assert.IsFalse(component.Find(".bit-tzp").ClassList.Contains("bit-tzp-fcs"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRaiseTheFocusEvents()
    {
        var focused = 0;
        var blurred = 0;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.OnFocus, () => focused++);
            parameters.Add(p => p.OnBlur, () => blurred++);
        });

        var field = component.Find("button.bit-tzp-fld");

        // One listener per direction: focusin and focusout would also fire for the search box of the list.
        Assert.IsTrue(field.HasAttribute("blazor:onfocus"));
        Assert.IsTrue(field.HasAttribute("blazor:onblur"));
        Assert.IsFalse(field.HasAttribute("blazor:onfocusin"));
        Assert.IsFalse(field.HasAttribute("blazor:onfocusout"));

        field.Focus();
        component.Find("button.bit-tzp-fld").Blur();

        Assert.AreEqual(1, focused);
        Assert.AreEqual(1, blurred);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNotRaiseTheFocusEventsWhileDisabled()
    {
        var focused = 0;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.OnFocus, () => focused++);
        });

        component.Find("button.bit-tzp-fld").Focus();

        Assert.AreEqual(0, focused);
        Assert.IsFalse(component.Find(".bit-tzp").ClassList.Contains("bit-tzp-fcs"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldFocusTheFieldFromItsLabelWithoutOpeningTheList()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Label, "Time zone"));

        component.Find(".bit-tzp-lbl").Click();

        // A label tied to the button through a for attribute would forward its click and open the list.
        Assert.IsFalse(component.Find(".bit-tzp-lbl").HasAttribute("for"));
        Assert.IsFalse(component.Instance.IsOpen);
        Context.JSInterop.VerifyInvoke("Blazor._internal.domWrapper.focus");
    }

    #endregion

    #region Responsive

    [TestMethod]
    public void BitTimeZonePickerShouldRespectResponsive()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.Label, "Time zone");
            parameters.Add(p => p.ResponsiveCloseButtonAriaLabel, "Schließen");
            parameters.Add(p => p.ResponsiveCloseIcon, BitIconInfo.Css("fa-solid fa-xmark"));
        });

        var callout = component.Find(".bit-tzp-cal");

        Assert.IsTrue(callout.ClassList.Contains("bit-tzp-res"));
        // A closed panel is only slid out of view, so it is made inert or the Tab key would still walk into it.
        Assert.IsTrue(callout.HasAttribute("inert"));
        Assert.AreEqual("Time zone", component.Find(".bit-tzp-rlb").TextContent.Trim());

        var close = component.Find("button.bit-tzp-cls");

        Assert.AreEqual("Schließen", close.GetAttribute("aria-label"));
        Assert.AreEqual("Schließen", close.GetAttribute("title"));
        StringAssert.Contains(close.QuerySelector("i")!.ClassName, "fa-xmark");

        component.Find("button.bit-tzp-fld").Click();

        Assert.IsFalse(component.Find(".bit-tzp-cal").HasAttribute("inert"));

        component.Find("button.bit-tzp-cls").Click();

        Assert.IsFalse(component.Instance.IsOpen);
        Assert.IsTrue(component.Find(".bit-tzp-cal").HasAttribute("inert"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNameTheResponsiveCloseButtonByDefault()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.ResponsiveCloseIconName, "ChromeClose");
        });

        var close = component.Find("button.bit-tzp-cls");

        Assert.AreEqual("Close", close.GetAttribute("aria-label"));
        Assert.AreEqual("button", close.GetAttribute("type"));
        Assert.AreEqual("true", close.QuerySelector("i")!.GetAttribute("aria-hidden"));
        StringAssert.Contains(close.QuerySelector("i")!.ClassName, "ChromeClose");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRenderNoResponsivePartsByDefault()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Label, "Time zone"));

        Assert.AreEqual(0, component.FindAll(".bit-tzp-cls").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tzp-rlc").Count);
        Assert.IsFalse(component.Find(".bit-tzp-cal").ClassList.Contains("bit-tzp-res"));
        Assert.IsFalse(component.Find(".bit-tzp-cal").HasAttribute("inert"));
    }

    #endregion

    #region Look

    [TestMethod,
        DataRow(BitColor.Primary, "bit-tzp-pri"),
        DataRow(BitColor.Secondary, "bit-tzp-sec"),
        DataRow(BitColor.Tertiary, "bit-tzp-ter"),
        DataRow(BitColor.Info, "bit-tzp-inf"),
        DataRow(BitColor.Success, "bit-tzp-suc"),
        DataRow(BitColor.Warning, "bit-tzp-wrn"),
        DataRow(BitColor.SevereWarning, "bit-tzp-swr"),
        DataRow(BitColor.Error, "bit-tzp-err"),
        DataRow(BitColor.PrimaryBackground, "bit-tzp-pbg"),
        DataRow(BitColor.SecondaryBackground, "bit-tzp-sbg"),
        DataRow(BitColor.TertiaryBackground, "bit-tzp-tbg"),
        DataRow(BitColor.PrimaryForeground, "bit-tzp-pfg"),
        DataRow(BitColor.SecondaryForeground, "bit-tzp-sfg"),
        DataRow(BitColor.TertiaryForeground, "bit-tzp-tfg"),
        DataRow(BitColor.PrimaryBorder, "bit-tzp-pbr"),
        DataRow(BitColor.SecondaryBorder, "bit-tzp-sbr"),
        DataRow(BitColor.TertiaryBorder, "bit-tzp-tbr")]
    public void BitTimeZonePickerShouldRespectColor(BitColor color, string cssClass)
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Color, color));

        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains(cssClass));
        Assert.IsTrue(component.Find(".bit-tzp-cal").ClassList.Contains(cssClass));
    }

    [TestMethod,
        DataRow(BitColorKind.Primary, "bit-tzp-bpr", "bit-tzp-brp"),
        DataRow(BitColorKind.Secondary, "bit-tzp-bse", "bit-tzp-brs"),
        DataRow(BitColorKind.Tertiary, "bit-tzp-btr", "bit-tzp-brt"),
        DataRow(BitColorKind.Transparent, "bit-tzp-btn", "bit-tzp-brn")]
    public void BitTimeZonePickerShouldRespectBackgroundAndBorder(BitColorKind kind, string background, string border)
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Background, kind);
            parameters.Add(p => p.Border, kind);
        });

        var root = component.Find(".bit-tzp");

        Assert.IsTrue(root.ClassList.Contains(background));
        Assert.IsTrue(root.ClassList.Contains(border));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldPublishNoColorBackgroundOrBorderWhileTheyAreUnset()
    {
        var component = RenderPicker<string>();

        // The public --bit-TimeZonePicker-* variables restyle the default look, so an unset parameter publishes no class
        // that would read as a choice - on the root and on the callout that repeats its size and color.
        string[] looks = ["bit-tzp-pri", "bit-tzp-md", "bit-tzp-bpr", "bit-tzp-brp", "bit-tzp-nbd", "bit-tzp-und"];

        Assert.IsFalse(component.Find(".bit-tzp").ClassList.Any(looks.Contains));
        Assert.IsFalse(component.Find(".bit-tzp-cal").ClassList.Any(looks.Contains));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRespectNoBorderAndUnderlined()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.NoBorder, true);
            parameters.Add(p => p.Underlined, true);
        });

        var root = component.Find(".bit-tzp");

        Assert.IsTrue(root.ClassList.Contains("bit-tzp-nbd"));
        Assert.IsTrue(root.ClassList.Contains("bit-tzp-und"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRespectStylesAndClasses()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "Asia/Tehran");
            parameters.Add(p => p.Label, "Time zone");
            parameters.Add(p => p.Description, "Shown in it.");
            parameters.Add(p => p.ErrorMessage, "Wrong.");
            parameters.Add(p => p.Responsive, true);
            parameters.Add(p => p.Styles, new BitTimeZonePickerClassStyles
            {
                Root = "margin:1px",
                Label = "color:red",
                Field = "color:blue",
                Text = "color:green",
                Offset = "color:gray",
                Caret = "color:black",
                ErrorMessageContainer = "color:crimson",
                ErrorMessage = "font-weight:bold",
                DescriptionContainer = "color:navy",
                Description = "font-style:italic",
                Callout = "width:200px",
                SearchBox = "padding:1px",
                ScrollContainer = "padding:2px",
                List = "padding:3px",
                Item = "padding:4px",
                ItemName = "padding:5px",
                ItemCountry = "padding:6px",
                NoResults = "padding:7px",
                ResponsiveLabelContainer = "padding:8px",
                ResponsiveLabel = "padding:9px",
                ResponsiveCloseButton = "padding:10px",
                ResponsiveCloseIcon = "padding:11px",
            });
            parameters.Add(p => p.Classes, new BitTimeZonePickerClassStyles
            {
                Root = "root-class",
                Label = "label-class",
                Field = "field-class",
                Text = "text-class",
                Offset = "offset-class",
                Caret = "caret-class",
                ErrorMessageContainer = "erm-class",
                ErrorMessage = "err-class",
                DescriptionContainer = "des-class",
                Description = "desc-class",
                Overlay = "overlay-class",
                Callout = "callout-class",
                SearchBox = "search-class",
                ScrollContainer = "scroll-class",
                List = "list-class",
                Item = "item-class",
                ItemName = "name-class",
                ItemCountry = "country-class",
                NoResults = "nor-class",
                ResponsiveLabelContainer = "rlc-class",
                ResponsiveLabel = "rlb-class",
                ResponsiveCloseButton = "cls-class",
                ResponsiveCloseIcon = "cli-class",
            });
        });

        var root = component.Find(".bit-tzp");
        Assert.IsTrue(root.ClassList.Contains("root-class"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");

        AssertPart(component, ".bit-tzp-lbl", "label-class", "color:red");
        AssertPart(component, ".bit-tzp-fld", "field-class", "color:blue");
        AssertPart(component, ".bit-tzp-tcn", "text-class", "color:green");
        AssertPart(component, ".bit-tzp-fld .bit-tzp-ofs", "offset-class", "color:gray");
        AssertPart(component, ".bit-tzp-car", "caret-class", "color:black");
        AssertPart(component, ".bit-tzp-erm", "erm-class", "color:crimson");
        AssertPart(component, ".bit-tzp-erm span", "err-class", "font-weight:bold");
        AssertPart(component, ".bit-tzp-des", "des-class", "color:navy");
        AssertPart(component, ".bit-tzp-des span", "desc-class", "font-style:italic");
        AssertPart(component, ".bit-tzp-cal", "callout-class", "width:200px");
        AssertPart(component, ".bit-tzp-sbx", "search-class", "padding:1px");
        AssertPart(component, ".bit-tzp-scn", "scroll-class", "padding:2px");
        AssertPart(component, ".bit-tzp-lst", "list-class", "padding:3px");
        AssertPart(component, ".bit-tzp-rlc", "rlc-class", "padding:8px");
        AssertPart(component, ".bit-tzp-rlb", "rlb-class", "padding:9px");
        AssertPart(component, ".bit-tzp-cls", "cls-class", "padding:10px");
        AssertPart(component, ".bit-tzp-cls i", "cli-class", "padding:11px");
        Assert.IsTrue(component.Find(".bit-tzp-ovl").ClassList.Contains("overlay-class"));

        component.Find("button.bit-tzp-fld").Click();

        AssertPart(component, ".bit-tzp-itm", "item-class", "padding:4px");
        AssertPart(component, ".bit-tzp-inm", "name-class", "padding:5px");
        AssertPart(component, ".bit-tzp-icn", "country-class", "padding:6px");

        component.Find(".bit-tzp-srch").Input("zzzz");

        AssertPart(component, ".bit-tzp-nor", "nor-class", "padding:7px");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRespectTheTemplates()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.LabelTemplate, b => b.AddMarkupContent(0, "<b class=\"lt\">Zone</b>"));
            parameters.Add(p => p.DescriptionTemplate, b => b.AddMarkupContent(0, "<i class=\"dt\">About</i>"));
            parameters.Add(p => p.ErrorMessageTemplate, b => b.AddMarkupContent(0, "<u class=\"et\">Wrong</u>"));
            parameters.Add(p => p.NoResultsTemplate, b => b.AddMarkupContent(0, "<s class=\"nt\">Nothing</s>"));
        });

        Assert.AreEqual("Zone", component.Find(".bit-tzp-lbl .lt").TextContent);
        Assert.AreEqual("About", component.Find(".bit-tzp-des .dt").TextContent);
        Assert.AreEqual("Wrong", component.Find(".bit-tzp-erm .et").TextContent);

        // A template is a message too: the field is described by both and is invalid.
        var field = component.Find("button.bit-tzp-fld");
        Assert.AreEqual("true", field.GetAttribute("aria-invalid"));
        Assert.AreEqual($"{component.Find(".bit-tzp-erm").Id} {component.Find(".bit-tzp-des").Id}", field.GetAttribute("aria-describedby"));
        Assert.AreEqual(component.Find(".bit-tzp-lbl").Id, field.GetAttribute("aria-labelledby"));

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").Input("zzzz");

        Assert.AreEqual("Nothing", component.Find(".bit-tzp-nor .nt").TextContent);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldAnnounceTheSearchThroughTheSuppliedWording()
    {
        var counts = new List<int>();

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.SearchResultsAnnouncement, count => { counts.Add(count); return $"{count} zonas"; });
            parameters.Add(p => p.NoResultsMessage, "Nada");
        });

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").Input("asia");

        StringAssert.StartsWith(component.Find(".bit-tzp-lvr").TextContent, "3 zonas");

        component.Find(".bit-tzp-srch").Input("zzzz");

        // An empty result is worded by NoResultsMessage, so the wording is never asked for zero.
        StringAssert.StartsWith(component.Find(".bit-tzp-lvr").TextContent, "Nada");
        CollectionAssert.AreEqual(new[] { 3 }, counts);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRespectSearchBoxTexts()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.SearchBoxPlaceholder, "Buscar");
            parameters.Add(p => p.SearchBoxAriaLabel, "Buscar zonas");
        });

        var search = component.Find(".bit-tzp-srch");

        Assert.AreEqual("Buscar", search.GetAttribute("placeholder"));
        Assert.AreEqual("Buscar zonas", search.GetAttribute("aria-label"));
        Assert.AreEqual("combobox", search.GetAttribute("role"));
        Assert.AreEqual("list", search.GetAttribute("aria-autocomplete"));
        Assert.AreEqual("off", search.GetAttribute("autocomplete"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNameTheSearchBoxByItsPlaceholderWhenItHasNoLabel()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.SearchBoxPlaceholder, "Buscar"));

        Assert.AreEqual("Buscar", component.Find(".bit-tzp-srch").GetAttribute("aria-label"));

        component.Render(parameters => parameters.Add(p => p.SearchBoxPlaceholder, (string?)null));

        Assert.AreEqual("Search time zones", component.Find(".bit-tzp-srch").GetAttribute("aria-label"));
        Assert.AreEqual("Search", component.Find(".bit-tzp-srch").GetAttribute("placeholder"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldPreferItsAriaLabelOverItsLabel()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Label, "Time zone");
            parameters.Add(p => p.AriaLabel, "Zone of the meeting");
        });

        var field = component.Find("button.bit-tzp-fld");

        Assert.AreEqual("Zone of the meeting", field.GetAttribute("aria-label"));
        Assert.IsNull(field.GetAttribute("aria-labelledby"));
        Assert.AreEqual("Zone of the meeting", component.Find(".bit-tzp-lst").GetAttribute("aria-label"));
        Assert.IsNull(component.Find(".bit-tzp-lst").GetAttribute("aria-labelledby"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNameTheListTimeZonesByDefault()
    {
        var component = RenderPicker<string>();

        Assert.AreEqual("Time zones", component.Find(".bit-tzp-lst").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldTitleTheFieldWithWhatItShows()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.DefaultValue, "Asia/Tehran"));

        // A narrow field cuts the text short, so the whole of it is the tooltip.
        Assert.AreEqual("(UTC+03:30) Tehran, Iran", component.Find("button.bit-tzp-fld").GetAttribute("title"));
        Assert.IsFalse(component.Find(".bit-tzp").HasAttribute("title"));

        component.Render(parameters => parameters.Add(p => p.Title, "Own title"));

        Assert.AreEqual("Own title", component.Find("button.bit-tzp-fld").GetAttribute("title"));
        Assert.AreEqual("Own title", component.Find(".bit-tzp").GetAttribute("title"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldMarkTheFieldInvalidWhenForcedTo()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Invalid, true));

        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find("button.bit-tzp-fld").GetAttribute("aria-invalid"));
        Assert.AreEqual(0, component.FindAll(".bit-tzp-erm").Count);
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitTimeZonePickerShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Visibility, visibility);
            parameters.Add(p => p.Style, "margin:1px");
        });

        var style = component.Find(".bit-tzp").GetAttribute("style")!;

        StringAssert.Contains(style, "margin:1px");

        if (expected is null)
        {
            Assert.IsFalse(style.Contains("visibility") || style.Contains("display"));
        }
        else
        {
            StringAssert.Contains(style, expected);
        }
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRespectIdClassAndTabIndex()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Id, "zone-picker");
            parameters.Add(p => p.Class, "custom-class");
            parameters.Add(p => p.TabIndex, "3");
        });

        Assert.AreEqual("zone-picker", component.Find(".bit-tzp").Id);
        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("custom-class"));
        Assert.AreEqual("3", component.Find("button.bit-tzp-fld").GetAttribute("tabindex"));
    }

    #endregion

    #region Splatted attributes

    [TestMethod]
    public void BitTimeZonePickerShouldSplatTheInputAttributesOnTheField()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Description, "Shown in it.");
            parameters.Add(p => p.InputHtmlAttributes, new Dictionary<string, object>
            {
                ["data-test"] = "field",
                ["title"] = "own title",
                ["aria-describedby"] = "own-hint",
            });
        });

        var field = component.Find("button.bit-tzp-fld");

        Assert.AreEqual("field", field.GetAttribute("data-test"));
        Assert.AreEqual("own title", field.GetAttribute("title"));
        // The page's description is kept beside the one the field writes.
        Assert.AreEqual($"own-hint {component.Find(".bit-tzp-des").Id}", field.GetAttribute("aria-describedby"));
        Assert.IsNull(component.Find(".bit-tzp").GetAttribute("data-test"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldSplatTheHtmlAttributesOnTheRoot()
    {
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitTimeZonePicker<string>>(0);
            builder.AddAttribute(1, nameof(BitTimeZonePicker<string>.TimeZones), SevenTimeZones);
            builder.AddAttribute(2, "data-test", "root");
            builder.AddAttribute(3, "lang", "fa");
            builder.CloseComponent();
        });

        var root = component.Find(".bit-tzp");

        Assert.AreEqual("root", root.GetAttribute("data-test"));
        Assert.AreEqual("fa", root.GetAttribute("lang"));
        Assert.IsNull(component.Find("button.bit-tzp-fld").GetAttribute("data-test"));
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)]
    public void BitTimeZonePickerShouldDropTheSplattedActivationHandlersWhileDisabled(bool disabled)
    {
        var clicks = 0;
        var hovers = 0;

        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitTimeZonePicker<string>>(0);
            builder.AddAttribute(1, nameof(BitTimeZonePicker<string>.TimeZones), SevenTimeZones);
            builder.AddAttribute(2, nameof(BitTimeZonePicker<string>.Disabled), disabled);
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++));
            builder.AddAttribute(4, "onmouseover", EventCallback.Factory.Create<MouseEventArgs>(this, () => hovers++));
            builder.CloseComponent();
        });

        var root = component.Find(".bit-tzp");

        // A click of the field bubbles up to the root, and a screen reader dispatches one whatever the pointer-events say.
        Assert.AreEqual(disabled is false, root.HasAttribute("blazor:onclick"));
        Assert.IsTrue(root.HasAttribute("blazor:onmouseover"));

        root.MouseOver();

        Assert.AreEqual(1, hovers);
        Assert.AreEqual(0, clicks);
    }

    #endregion

    #region Disabled while open, and disposal

    [TestMethod]
    public void BitTimeZonePickerShouldCloseItsListWhenDisabledWhileOpen()
    {
        var isOpen = false;
        var closed = 0;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
            parameters.Add(p => p.OnClose, () => closed++);
        });

        component.Find("button.bit-tzp-fld").Click();

        Assert.IsTrue(isOpen);

        // A form disabled for its submit while the list is open.
        component.Render(parameters => parameters.Add(p => p.Disabled, true));

        Assert.IsFalse(isOpen);
        Assert.IsFalse(component.Instance.IsOpen);
        Assert.AreEqual(1, closed);

        // The JS side is told to take the relocated list off the screen, not left holding it.
        Assert.AreEqual(false, LastToggle().Arguments[6]);
        StringAssert.Contains(component.Find(".bit-tzp-ovl").GetAttribute("style"), "display:none");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldHideAListAOneWayIsOpenKeepsOpenWhileDisabled()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual(true, LastToggle().Arguments[6]);

        // Every render of the page hands the one-way IsOpen down again, which is what keeps it from being set back.
        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Disabled, true);
        });

        // The picker cannot set a one-way IsOpen back to false, so the list is hidden instead.
        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual(false, LastToggle().Arguments[6]);
        StringAssert.Contains(component.Find(".bit-tzp-ovl").GetAttribute("style"), "display:none");
        Assert.AreEqual("false", component.Find("button.bit-tzp-fld").GetAttribute("aria-expanded"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Disabled, false);
        });

        Assert.AreEqual(true, LastToggle().Arguments[6]);
        StringAssert.Contains(component.Find(".bit-tzp-ovl").GetAttribute("style"), "display:block");
        Assert.AreEqual("true", component.Find("button.bit-tzp-fld").GetAttribute("aria-expanded"));
    }

    [TestMethod,
        DataRow(true, false),
        DataRow(false, true)]
    public void BitTimeZonePickerShouldNotOpenThroughIsOpenWhenItCannotBeUsed(bool disabled, bool readOnly)
    {
        var isOpen = false;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Disabled, disabled);
            parameters.Add(p => p.ReadOnly, readOnly);
            parameters.Add(p => p.IsOpen, isOpen);
            parameters.Add(p => p.IsOpenChanged, v => isOpen = v);
        });

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.IsFalse(isOpen);
        Assert.IsFalse(component.Instance.IsOpen);
        StringAssert.Contains(component.Find(".bit-tzp-ovl").GetAttribute("style"), "display:none");
        Assert.IsFalse(Context.JSInterop.Invocations.Any(i => i.Identifier == "BitBlazorUI.Callouts.toggle" && Equals(i.Arguments[6], true)));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldSwapTheKeysOfTheFieldWhenOpenedAndClosedThroughIsOpen()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.NoSearchBox, true));

        CollectionAssert.DoesNotContain(LastFieldKeys(), "Enter");

        component.Render(parameters => parameters.Add(p => p.IsOpen, true));

        // Enter picks the active time zone of the open list, and the click it would also fire must not reopen it.
        CollectionAssert.Contains(LastFieldKeys(), "Enter");
        CollectionAssert.Contains(LastFieldKeys(), "PageDown");

        component.Render(parameters => parameters.Add(p => p.IsOpen, false));

        // Enter is the native click that opens the closed field again.
        CollectionAssert.DoesNotContain(LastFieldKeys(), "Enter");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldPutTheFieldKeysBackWhenAnotherCalloutOpens()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();

        CollectionAssert.Contains(LastFieldKeys(), "Enter");

        component.InvokeAsync(() => component.Instance._CloseCalloutBeforeAnotherCalloutIsOpened()).GetAwaiter().GetResult();

        CollectionAssert.DoesNotContain(LastFieldKeys(), "Enter");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldWireTheSearchBoxKeysOnlyWhileItHasOne()
    {
        var component = RenderPicker<string>();

        // The field's listener and the search box's.
        Assert.AreEqual(2, CountInvocations("BitBlazorUI.Extras.setPreventKeys"));

        component.Render(parameters => parameters.Add(p => p.NoSearchBox, true));

        // A search box that went away takes its listener with it.
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Extras.disposePreventKeys"));

        component.Render(parameters => parameters.Add(p => p.NoSearchBox, false));

        Assert.AreEqual(3, CountInvocations("BitBlazorUI.Extras.setPreventKeys"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldTakeBackItsCalloutAndListenersWhenDisposed()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();

        component.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Callouts.clear"));
        // The field's listener and the search box's.
        Assert.AreEqual(2, CountInvocations("BitBlazorUI.Extras.disposePreventKeys"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldDisposeWithoutASearchBox()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.NoSearchBox, true));

        component.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();

        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Callouts.clear"));
        Assert.AreEqual(1, CountInvocations("BitBlazorUI.Extras.disposePreventKeys"));
    }

    #endregion



    private IRenderedComponent<BitTimeZonePicker<TValue>> RenderPicker<TValue>(Action<ComponentParameterCollectionBuilder<BitTimeZonePicker<TValue>>>? parameterBuilder = null)
    {
        return RenderComponent<BitTimeZonePicker<TValue>>(parameters =>
        {
            parameters.Add(p => p.TimeZones, SevenTimeZones);
            parameters.Add(p => p.ReferenceDate, January);
            parameterBuilder?.Invoke(parameters);
        });
    }

    private static void PickItem<TValue>(IRenderedComponent<BitTimeZonePicker<TValue>> component, string name)
    {
        component.FindAll("button.bit-tzp-itm").Single(i => i.QuerySelector(".bit-tzp-inm")!.TextContent == name).Click();
    }

    private static void AssertPart<TValue>(IRenderedComponent<BitTimeZonePicker<TValue>> component, string selector, string cssClass, string style)
    {
        var part = component.Find(selector);

        Assert.IsTrue(part.ClassList.Contains(cssClass), $"{selector} lacks {cssClass}");
        StringAssert.Contains(part.GetAttribute("style"), style, $"{selector} lacks {style}");
    }

    // The keys the field's own listener was last handed: the field's sets are the ones that carry Home, which the
    // search box leaves to its caret.
    private string[] LastFieldKeys()
    {
        return (string[])Context.JSInterop.Invocations
                                .Last(i => i.Identifier == "BitBlazorUI.Extras.setPreventKeys" && ((string[])i.Arguments[1]!).Contains("Home"))
                                .Arguments[1]!;
    }

    private int CountInvocations(string identifier)
    {
        return Context.JSInterop.Invocations.Count(i => i.Identifier == identifier);
    }

    private JSRuntimeInvocation LastToggle()
    {
        return Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Callouts.toggle");
    }
}
