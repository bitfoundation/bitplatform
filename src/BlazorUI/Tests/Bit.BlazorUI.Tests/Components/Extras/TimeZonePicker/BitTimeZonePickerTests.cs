using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.TimeZonePicker;

[TestClass]
public class BitTimeZonePickerTests : BunitTestContext
{
    // A moment without daylight saving time in the northern hemisphere, so the offsets below hold whenever the tests run.
    private static readonly DateTimeOffset January = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset July = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    // Given out of order on purpose: the list shows them west to east whatever order they come in.
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



    [TestMethod]
    public void BitTimeZonePickerShouldRenderExpectedElements()
    {
        var component = RenderComponent<BitTimeZonePicker<string>>();

        Assert.IsNotNull(component.Find(".bit-tzp"));
        Assert.IsNotNull(component.Find("button.bit-tzp-fld"));
        Assert.IsNotNull(component.Find(".bit-tzp-cal"));
        Assert.IsNotNull(component.Find(".bit-tzp-ovl"));

        Assert.AreEqual("button", component.Find("button.bit-tzp-fld").GetAttribute("type"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRefuseAValueTypeItCannotHold()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => RenderComponent<BitTimeZonePicker<int>>());
    }

    [TestMethod]
    public void BitTimeZonePickerShouldBeASelectOnlyCombobox()
    {
        var component = RenderPicker<string>();

        var field = component.Find("button.bit-tzp-fld");

        Assert.AreEqual("combobox", field.GetAttribute("role"));
        Assert.AreEqual("listbox", field.GetAttribute("aria-haspopup"));
        Assert.AreEqual("false", field.GetAttribute("aria-expanded"));
        Assert.AreEqual("Time zone", field.GetAttribute("aria-label"));

        field.Click();

        field = component.Find("button.bit-tzp-fld");
        var list = component.Find(".bit-tzp-lst");
        var search = component.Find(".bit-tzp-srch");

        Assert.AreEqual("true", field.GetAttribute("aria-expanded"));
        Assert.AreEqual("listbox", list.GetAttribute("role"));
        Assert.AreEqual(list.Id, field.GetAttribute("aria-controls"));
        Assert.AreEqual(list.Id, search.GetAttribute("aria-controls"));

        var options = component.FindAll("button.bit-tzp-itm");

        Assert.IsTrue(options.All(o => o.GetAttribute("role") == "option" && o.GetAttribute("tabindex") == "-1"));
        Assert.AreEqual(options[0].Id, search.GetAttribute("aria-activedescendant"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldListTheTimeZonesByTheirOffsets()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();

        CollectionAssert.AreEqual(new[] { "Los Angeles", "New York", "London", "Berlin", "Tehran", "Kolkata", "Tokyo" }, ItemNames(component));
        CollectionAssert.AreEqual(new[] { "UTC-08:00", "UTC-05:00", "UTC+00:00", "UTC+01:00", "UTC+03:30", "UTC+05:30", "UTC+09:00" },
                                  component.FindAll(".bit-tzp-itm .bit-tzp-ofs").Select(o => o.TextContent).ToArray());
    }

    [TestMethod]
    public void BitTimeZonePickerShouldComputeTheOffsetsForTheReferenceDate()
    {
        var component = RenderPicker<string>(referenceDate: July);

        component.Find("button.bit-tzp-fld").Click();

        // Daylight saving time moves New York to -04:00 and Berlin to +02:00, while Tehran observes none.
        var offsets = component.FindAll(".bit-tzp-itm").ToDictionary(i => i.QuerySelector(".bit-tzp-inm")!.TextContent, i => i.QuerySelector(".bit-tzp-ofs")!.TextContent);

        Assert.AreEqual("UTC-04:00", offsets["New York"]);
        Assert.AreEqual("UTC+02:00", offsets["Berlin"]);
        Assert.AreEqual("UTC+03:30", offsets["Tehran"]);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldShowTheCountryOfEachTimeZone()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();

        var tehran = component.FindAll(".bit-tzp-itm").Single(i => i.QuerySelector(".bit-tzp-inm")!.TextContent == "Tehran");

        Assert.AreEqual("Iran", tehran.QuerySelector(".bit-tzp-icn")!.TextContent);
        Assert.AreEqual("(UTC+03:30) Tehran, Iran", tehran.GetAttribute("title"));

        // The parts are read apart by a screen reader too.
        Assert.AreEqual("UTC+03:30, Tehran, Iran", Regex.Replace(tehran.TextContent, @"\s+", " ").Trim());
    }

    [TestMethod]
    public void BitTimeZonePickerShouldLeaveOutTheOffsetsAndTheCountriesWhenAsked()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.NoOffset, true);
            parameters.Add(p => p.NoCountry, true);
            parameters.Add(p => p.DefaultValue, "Asia/Tehran");
        });

        Assert.AreEqual(0, component.FindAll(".bit-tzp-fld .bit-tzp-ofs").Count);

        component.Find("button.bit-tzp-fld").Click();

        Assert.AreEqual(0, component.FindAll(".bit-tzp-itm .bit-tzp-ofs").Count);
        Assert.AreEqual(0, component.FindAll(".bit-tzp-icn").Count);
        Assert.AreEqual("Tehran", component.FindAll(".bit-tzp-itm").Single(i => i.ClassList.Contains("bit-tzp-sel")).GetAttribute("title"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldBindTheIanaIdOfTheTimeZone()
    {
        string? value = null;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("button.bit-tzp-fld").Click();
        PickItem(component, "Tehran");

        Assert.AreEqual("Asia/Tehran", value);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldBindTheTimeZoneInfoOfTheTimeZone()
    {
        TimeZoneInfo? value = null;

        var component = RenderPicker<TimeZoneInfo>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("button.bit-tzp-fld").Click();
        PickItem(component, "Tokyo");

        Assert.IsNotNull(value);
        Assert.AreEqual(TimeSpan.FromHours(9), value.GetUtcOffset(January));
        Assert.AreEqual("Asia/Tokyo", BitTimeZones.Find(value)?.Id);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldBindTheBitTimeZoneOfTheList()
    {
        BitTimeZone? value = null;

        var component = RenderPicker<BitTimeZone>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("button.bit-tzp-fld").Click();
        PickItem(component, "Berlin");

        Assert.AreSame(BitTimeZones.Find("Europe/Berlin"), value);
    }

    [TestMethod,
        DataRow("Asia/Calcutta"),
        DataRow("India Standard Time"),
        DataRow("asia/kolkata")]
    public void BitTimeZonePickerShouldRecognizeAValueWrittenAnyWayItCanBe(string value)
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Value, value));

        Assert.AreEqual("Kolkata", component.Find(".bit-tzp-txt").TextContent);
        Assert.AreEqual("UTC+05:30", component.Find(".bit-tzp-fld .bit-tzp-ofs").TextContent);
        Assert.AreEqual("Asia/Kolkata", component.Instance.SelectedTimeZone?.Id);

        component.Find("button.bit-tzp-fld").Click();

        Assert.AreEqual("Kolkata", component.Find(".bit-tzp-sel .bit-tzp-inm").TextContent);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRecognizeATimeZoneInfoValue()
    {
        var component = RenderPicker<TimeZoneInfo>(parameters => parameters.Add(p => p.Value, TimeZoneInfo.FindSystemTimeZoneById("Europe/London")));

        Assert.AreEqual("London", component.Find(".bit-tzp-txt").TextContent);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldShowAValueTheListDoesNotOffer()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Value, "Africa/Cairo");
            parameters.Add(p => p.Placeholder, "Pick one");
        });

        // The value is still the value of the field, rather than an empty field.
        Assert.AreEqual("Africa/Cairo", component.Find(".bit-tzp-txt").TextContent);
        Assert.AreEqual(0, component.FindAll(".bit-tzp-pls").Count);
        Assert.IsNull(component.Instance.SelectedTimeZone);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldShowThePlaceholderWithoutAValue()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Placeholder, "Pick one"));

        Assert.AreEqual("Pick one", component.Find(".bit-tzp-pls").TextContent);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldStartOnTheDefaultValue()
    {
        string? changed = null;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "Europe/London");
            parameters.Add(p => p.OnChange, v => changed = v);
        });

        Assert.AreEqual("London", component.Find(".bit-tzp-txt").TextContent);

        component.Find("button.bit-tzp-fld").Click();
        PickItem(component, "Tokyo");

        Assert.AreEqual("Asia/Tokyo", changed);
        Assert.AreEqual("Tokyo", component.Find(".bit-tzp-txt").TextContent);
    }

    [TestMethod,
        DataRow("tok", "Tokyo"),
        DataRow("germ", "Berlin"),
        DataRow("new y", "New York"),
        DataRow("+3:30", "Tehran"),
        DataRow("utc+0530", "Kolkata"),
        DataRow("GMT-8", "Los Angeles"),
        DataRow("pacific", "Los Angeles"),
        DataRow("Asia/Calcutta", "Kolkata"),
        DataRow("india standard", "Kolkata")]
    public void BitTimeZonePickerShouldSearchTheTimeZones(string term, string expectedFirst)
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").Input(term);

        Assert.AreEqual(expectedFirst, ItemNames(component).First());
    }

    [TestMethod]
    public void BitTimeZonePickerShouldFindEveryTimeZoneOfAnHourWithoutMinutes()
    {
        var component = RenderPicker<string>(timeZones: [BitTimeZones.Find("Asia/Karachi")!, BitTimeZones.Find("Asia/Kolkata")!, BitTimeZones.Find("Asia/Kathmandu")!, BitTimeZones.Find("Asia/Dhaka")!]);

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").Input("+5");

        CollectionAssert.AreEqual(new[] { "Karachi", "Kolkata", "Kathmandu" }, ItemNames(component));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldHandTheSearchEveryCharacterTypedOnTheField()
    {
        var component = RenderPicker<string>();

        // The keys typed in the moment the list takes to open land on the field, and a search is typed with punctuation:
        // none of it may be lost on its way to the search box.
        foreach (var key in new[] { "+", "3", ":", "3", "0" })
        {
            component.Find("button.bit-tzp-fld").KeyDown(key);
        }

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual("+3:30", component.Find(".bit-tzp-srch").GetAttribute("value"));
        CollectionAssert.AreEqual(new[] { "Tehran" }, ItemNames(component));

        // A space on the field stays its native click, and a modified key a shortcut.
        component.Find("button.bit-tzp-fld").KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });

        Assert.AreEqual("+3:30", component.Find(".bit-tzp-srch").GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldKeepQuickFindOffItsField()
    {
        RenderPicker<string>();

        var keys = (string[])Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.Extras.setPreventKeys" && ((string[])i.Arguments[1]!).Contains("Home")).Arguments[1]!;

        CollectionAssert.IsSubsetOf(new[] { "/", "'" }, keys);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldAnnounceTheResultCountOfASearch()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();

        component.Find(".bit-tzp-srch").Input("tokyo");
        StringAssert.StartsWith(component.Find(".bit-tzp-lvr").TextContent, "1 time zone found");

        component.Find(".bit-tzp-srch").Input("asia");
        StringAssert.Contains(component.Find(".bit-tzp-lvr").TextContent, "3 time zones found");

        component.Find(".bit-tzp-srch").Input("zzzz");
        StringAssert.StartsWith(component.Find(".bit-tzp-lvr").TextContent, "No results found");
        Assert.AreEqual("No results found", component.Find(".bit-tzp-nor").TextContent.Trim());
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNotFilterForATermOfPunctuationAlone()
    {
        var component = RenderPicker<string>();

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").Input("+");

        Assert.AreEqual(SevenTimeZones.Count, component.FindAll("button.bit-tzp-itm").Count);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldSelectTheActiveTimeZoneWithTheKeyboard()
    {
        string? value = null;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.Find("button.bit-tzp-fld").KeyDown("ArrowDown");

        Assert.IsTrue(component.Instance.IsOpen);

        var search = component.Find(".bit-tzp-srch");
        search.KeyDown("ArrowDown");
        search.KeyDown("ArrowDown");
        component.Find(".bit-tzp-srch").KeyDown("Enter");

        Assert.AreEqual("Europe/London", value);
        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldTypeAheadWithoutASearchBox()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.NoSearchBox, true));

        var field = component.Find("button.bit-tzp-fld");
        field.Click();
        component.Find("button.bit-tzp-fld").KeyDown("t");

        var active = component.Find(".bit-tzp-act");

        Assert.AreEqual("Tehran", active.QuerySelector(".bit-tzp-inm")!.TextContent);
        Assert.AreEqual(active.Id, component.Find("button.bit-tzp-fld").GetAttribute("aria-activedescendant"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldPinThePreferredTimeZones()
    {
        var component = RenderPicker<string>(parameters =>
        {
            // Written as any id the table resolves; one the list does not offer is skipped.
            parameters.Add(p => p.PreferredTimeZones, ["Tokyo Standard Time", "Asia/Calcutta", "Africa/Cairo"]);
        });

        component.Find("button.bit-tzp-fld").Click();

        var names = ItemNames(component);

        CollectionAssert.AreEqual(new[] { "Tokyo", "Kolkata", "Los Angeles" }, names.Take(3).ToArray());
        Assert.AreEqual(SevenTimeZones.Count, names.Length);

        var items = component.FindAll("button.bit-tzp-itm");

        Assert.IsTrue(items[1].ClassList.Contains("bit-tzp-ilp"));
        Assert.AreEqual(1, component.FindAll(".bit-tzp-ilp").Count);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldOfferEachTimeZoneOnce()
    {
        var component = RenderPicker<string>(timeZones: [BitTimeZones.Find("Asia/Kolkata")!, new BitTimeZone("Asia/Calcutta", "Calcutta", "IN"), BitTimeZones.Find("Asia/Tokyo")!]);

        component.Find("button.bit-tzp-fld").Click();

        CollectionAssert.AreEqual(new[] { "Kolkata", "Tokyo" }, ItemNames(component));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldDetectTheTimeZoneOfTheBrowser()
    {
        Context.JSInterop.Setup<string?>("BitBlazorUI.Extras.getTimeZone").SetResult("Asia/Calcutta");

        string? value = null;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.DetectTimeZone, true);
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        // A renamed id the browser reports is written down under its canonical one.
        component.WaitForAssertion(() => Assert.AreEqual("Asia/Kolkata", value));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldLeaveAValueAloneWhenDetecting()
    {
        Context.JSInterop.Setup<string?>("BitBlazorUI.Extras.getTimeZone").SetResult("Asia/Tokyo");

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.DetectTimeZone, true);
            parameters.Add(p => p.DefaultValue, "Europe/London");
        });

        Assert.AreEqual("London", component.Find(".bit-tzp-txt").TextContent);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Extras.getTimeZone"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNotDetectWithoutBeingAsked()
    {
        var component = RenderPicker<string>();

        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.Extras.getTimeZone"));
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)]
    public void BitTimeZonePickerShouldRespectDisabled(bool isEnabled)
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Disabled, isEnabled is false));

        var root = component.Find(".bit-tzp");
        var field = component.Find("button.bit-tzp-fld");

        Assert.AreEqual(isEnabled is false, root.ClassList.Contains("bit-dis"));
        Assert.AreEqual(isEnabled is false, field.HasAttribute("disabled"));
        Assert.AreEqual(isEnabled ? null : "true", field.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRespectReadOnly()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.ReadOnly, true));

        var field = component.Find("button.bit-tzp-fld");

        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-tzp-rdo"));
        Assert.AreEqual("true", field.GetAttribute("aria-readonly"));
        Assert.IsFalse(field.HasAttribute("disabled"));

        field.Click();
        field.KeyDown("ArrowDown");

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldNameTheFieldAndTheListByTheLabel()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Label, "Display time zone");
            parameters.Add(p => p.Required, true);
            parameters.Add(p => p.Description, "Dates are shown in it.");
        });

        var label = component.Find(".bit-tzp-lbl");
        var field = component.Find("button.bit-tzp-fld");

        Assert.AreEqual(label.Id, field.GetAttribute("aria-labelledby"));
        Assert.IsNull(field.GetAttribute("aria-label"));
        Assert.AreEqual(label.Id, component.Find(".bit-tzp-lst").GetAttribute("aria-labelledby"));
        Assert.AreEqual("true", field.GetAttribute("aria-required"));
        Assert.AreEqual(component.Find(".bit-tzp-des").Id, field.GetAttribute("aria-describedby"));
        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-tzp-req"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldShowAndAnnounceAnErrorMessage()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.ErrorMessage, "No office there."));

        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-inv"));
        Assert.AreEqual("true", component.Find("button.bit-tzp-fld").GetAttribute("aria-invalid"));
        Assert.AreEqual(component.Find(".bit-tzp-erm").Id, component.Find("button.bit-tzp-fld").GetAttribute("aria-describedby"));
        StringAssert.StartsWith(component.Find(".bit-tzp-lvr").TextContent, "No office there.");
    }

    [TestMethod]
    public void BitTimeZonePickerShouldValidateInsideAnEditForm()
    {
        var model = new ValidationModel();
        var editContext = new EditContext(model);
        editContext.EnableDataAnnotationsValidation(Context.Services);
        Expression<Func<string?>> expression = () => model.TimeZoneId;

        var component = RenderComponent<CascadingValue<EditContext>>(parameters =>
        {
            parameters.Add(p => p.Value, editContext);
            parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<BitTimeZonePicker<string>>(0);
                builder.AddAttribute(1, nameof(BitTimeZonePicker<string>.TimeZones), SevenTimeZones);
                builder.AddAttribute(2, nameof(BitTimeZonePicker<string>.Value), model.TimeZoneId);
                builder.AddAttribute(3, nameof(BitTimeZonePicker<string>.ValueChanged), EventCallback.Factory.Create<string?>(this, v => model.TimeZoneId = v));
                builder.AddAttribute(4, nameof(BitTimeZonePicker<string>.ValueExpression), expression);
                builder.AddAttribute(5, nameof(BitTimeZonePicker<string>.ReferenceDate), (DateTimeOffset?)January);
                builder.CloseComponent();
            }));
        });

        component.InvokeAsync(() => editContext.Validate());

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual("true", component.Find("button.bit-tzp-fld").GetAttribute("aria-invalid"));
            Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-inv"));
        });

        component.Find("button.bit-tzp-fld").Click();
        component.FindAll("button.bit-tzp-itm").Single(i => i.QuerySelector(".bit-tzp-inm")!.TextContent == "Berlin").Click();

        Assert.AreEqual("Europe/Berlin", model.TimeZoneId);

        component.WaitForAssertion(() =>
        {
            Assert.IsNull(component.Find("button.bit-tzp-fld").GetAttribute("aria-invalid"));
            Assert.IsFalse(component.Find(".bit-tzp").ClassList.Contains("bit-inv"));
        });
    }

    [TestMethod]
    public void BitTimeZonePickerShouldPostTheIanaIdOfTheSelectionWhenNamed()
    {
        var component = RenderPicker<TimeZoneInfo>(parameters =>
        {
            parameters.Add(p => p.Name, "timeZone");
            parameters.Add(p => p.DefaultValue, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"));
        });

        var hidden = component.Find("input[type=hidden]");

        Assert.AreEqual("timeZone", hidden.GetAttribute("name"));
        Assert.AreEqual("Asia/Tokyo", hidden.GetAttribute("value"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldSelectATimeZoneProgrammatically()
    {
        string? value = null;

        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.Value, value);
            parameters.Add(p => p.ValueChanged, v => value = v);
        });

        component.InvokeAsync(() => component.Instance.SelectTimeZoneAsync("Tokyo Standard Time"));

        Assert.AreEqual("Asia/Tokyo", value);

        // One the list does not offer is not selected.
        component.InvokeAsync(() => component.Instance.SelectTimeZoneAsync("Africa/Cairo"));

        Assert.AreEqual("Asia/Tokyo", value);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldOpenAndCloseProgrammatically()
    {
        var component = RenderPicker<string>();

        component.InvokeAsync(() => component.Instance.OpenAsync());

        Assert.IsTrue(component.Instance.IsOpen);
        Assert.AreEqual("true", component.Find("button.bit-tzp-fld").GetAttribute("aria-expanded"));

        component.InvokeAsync(() => component.Instance.CloseAsync());

        Assert.IsFalse(component.Instance.IsOpen);
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRenderTheTemplates()
    {
        var component = RenderPicker<string>(parameters =>
        {
            parameters.Add(p => p.DefaultValue, "Asia/Tokyo");
            parameters.Add(p => p.ValueTemplate, zone => $"<i class=\"vt\">{zone?.Id}</i>");
            parameters.Add(p => p.ItemTemplate, zone => $"<i class=\"it\">{zone.Id}</i>");
        });

        Assert.AreEqual("Asia/Tokyo", component.Find(".vt").TextContent);

        component.Find("button.bit-tzp-fld").Click();

        Assert.AreEqual(SevenTimeZones.Count, component.FindAll(".it").Count);
    }

    [TestMethod,
        DataRow(BitSize.Small, "bit-tzp-sm"),
        DataRow(BitSize.Medium, "bit-tzp-md"),
        DataRow(BitSize.Large, "bit-tzp-lg"),
        DataRow(null, null)]
    public void BitTimeZonePickerShouldRespectSize(BitSize? size, string? expectedClass)
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Size, size));

        var root = component.Find(".bit-tzp");
        var callout = component.Find(".bit-tzp-cal");

        foreach (var sizeClass in new[] { "bit-tzp-sm", "bit-tzp-md", "bit-tzp-lg" })
        {
            Assert.AreEqual(sizeClass == expectedClass, root.ClassList.Contains(sizeClass));
            Assert.AreEqual(sizeClass == expectedClass, callout.ClassList.Contains(sizeClass));
        }
    }

    [TestMethod]
    public void BitTimeZonePickerShouldRepeatTheColorOnTheCallout()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Color, BitColor.Success));

        Assert.IsTrue(component.Find(".bit-tzp").ClassList.Contains("bit-tzp-suc"));
        Assert.IsTrue(component.Find(".bit-tzp-cal").ClassList.Contains("bit-tzp-suc"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldMarkAnExplicitRtlCallout()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.Dir, BitDir.Rtl));

        Assert.AreEqual("rtl", component.Find(".bit-tzp").GetAttribute("dir"));
        Assert.IsTrue(component.Find(".bit-tzp-cal").ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitTimeZonePickerShouldShowTranslatedNames()
    {
        var component = RenderPicker<string>(parameters => parameters.Add(p => p.DefaultValue, "Europe/Vienna"),
                                             timeZones: [new BitTimeZone("Europe/Vienna", "Wien", "AT", "W. Europe Standard Time", "Österreich")]);

        Assert.AreEqual("Wien", component.Find(".bit-tzp-txt").TextContent);

        component.Find("button.bit-tzp-fld").Click();
        component.Find(".bit-tzp-srch").Input("osterr");

        Assert.AreEqual("Österreich", component.Find(".bit-tzp-icn").TextContent);
    }



    private IRenderedComponent<BitTimeZonePicker<TValue>> RenderPicker<TValue>(Action<ComponentParameterCollectionBuilder<BitTimeZonePicker<TValue>>>? parameterBuilder = null,
                                                                               List<BitTimeZone>? timeZones = null,
                                                                               DateTimeOffset? referenceDate = null)
    {
        return RenderComponent<BitTimeZonePicker<TValue>>(parameters =>
        {
            parameters.Add(p => p.TimeZones, timeZones ?? SevenTimeZones);
            parameters.Add(p => p.ReferenceDate, referenceDate ?? January);
            parameterBuilder?.Invoke(parameters);
        });
    }

    private static string[] ItemNames<TValue>(IRenderedComponent<BitTimeZonePicker<TValue>> component)
    {
        return [.. component.FindAll("button.bit-tzp-itm .bit-tzp-inm").Select(n => n.TextContent)];
    }

    private static void PickItem<TValue>(IRenderedComponent<BitTimeZonePicker<TValue>> component, string name)
    {
        component.FindAll("button.bit-tzp-itm").Single(i => i.QuerySelector(".bit-tzp-inm")!.TextContent == name).Click();
    }

    private class ValidationModel
    {
        [Required]
        public string? TimeZoneId { get; set; }
    }
}
