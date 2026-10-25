using System;
using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TimePicker;

// A 12-hour picker writes, shows, reads and takes from the keyboard the AM/PM designators of its culture - which for
// fa-IR and ar-SA are words of their own script rather than "AM" and "PM".
[TestClass]
public class BitTimePickerCultureTests : BunitTestContext
{
    [TestMethod,
        DataRow("fa-IR"),
        DataRow("ar-SA")
    ]
    public void BitTimePickerShouldShowTheDesignatorsOfTheCulture(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(17, 30, 0));
        });

        var am = component.Find(".bit-tpc-bam");
        var pm = component.Find(".bit-tpc-bpm");

        Assert.AreNotEqual("AM", culture.DateTimeFormat.AMDesignator);
        Assert.AreEqual(culture.DateTimeFormat.AMDesignator, am.TextContent.Trim());
        Assert.AreEqual(culture.DateTimeFormat.PMDesignator, pm.TextContent.Trim());
        Assert.AreEqual("true", pm.GetAttribute("aria-pressed"));
        Assert.AreEqual("false", am.GetAttribute("aria-pressed"));
    }

    [TestMethod,
        DataRow("fa-IR", 13, 45, true),
        DataRow("fa-IR", 9, 5, false),
        DataRow("ar-SA", 13, 45, true),
        DataRow("ar-SA", 9, 5, false)
    ]
    public void BitTimePickerShouldWriteTheValueWithTheDesignatorOfTheCulture(string name, int hour, int minute, bool pm)
    {
        var culture = CultureInfo.GetCultureInfo(name);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Add(p => p.Value, new TimeSpan(hour, minute, 0));
        });

        var designator = pm ? culture.DateTimeFormat.PMDesignator : culture.DateTimeFormat.AMDesignator;
        var expected = $"{(hour % 12 == 0 ? 12 : hour % 12):D2}:{minute:D2} {designator}";

        Assert.AreEqual(expected, component.Find(".bit-tpc-inp").GetAttribute("value").NormalizeSpaces());
    }

    [TestMethod]
    public void BitTimePickerShouldDropTheDesignatorOfATwelveHourCultureInTwentyFourHours()
    {
        // ar-SA writes its times on a 12-hour clock ("h:mm tt"), which a 24-hour picker rewrites without the designator.
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("ar-SA"));
            parameters.Add(p => p.Value, new TimeSpan(13, 45, 0));
        });

        Assert.AreEqual("13:45", component.Find(".bit-tpc-inp").GetAttribute("value"));
        Assert.IsEmpty(component.FindAll(".bit-tpc-bam"));
        Assert.IsEmpty(component.FindAll(".bit-tpc-bpm"));
    }

    [TestMethod,
        DataRow("fa-IR"),
        DataRow("ar-SA")
    ]
    public void BitTimePickerShouldSetTheHalfOfTheDayWithTheFirstLetterOfTheDesignatorsOfTheCulture(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);
        TimeSpan? value = new TimeSpan(2, 15, 0);

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-tin").KeyDown(new KeyboardEventArgs { Key = culture.DateTimeFormat.PMDesignator[..1] });

        Assert.AreEqual(new TimeSpan(14, 15, 0), value);

        component.Find(".bit-tpc-tin").KeyDown(new KeyboardEventArgs { Key = culture.DateTimeFormat.AMDesignator[..1] });

        Assert.AreEqual(new TimeSpan(2, 15, 0), value);
    }

    [TestMethod,
        DataRow("fa-IR"),
        DataRow("ar-SA")
    ]
    public void BitTimePickerShouldReadATypedTimeWithTheDesignatorOfTheCulture(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);
        TimeSpan? value = null;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.AllowTextInput, true);
            parameters.Add(p => p.Culture, culture);
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
            parameters.Bind(p => p.Value, value, v => value = v);
        });

        component.Find(".bit-tpc-inp").Input($"02:30 {culture.DateTimeFormat.PMDesignator}");

        Assert.AreEqual(new TimeSpan(14, 30, 0), value);
    }

    [TestMethod]
    public void BitTimePickerShouldFollowTheDirectionOfAnArSaCulture()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Culture, CultureInfo.GetCultureInfo("ar-SA"));
        });

        Assert.IsTrue(component.Find(".bit-tpc").ClassList.Contains("bit-rtl"));
        Assert.IsTrue(component.Find(".bit-tpc-cal").ClassList.Contains("bit-tpc-rtl"));
    }
}
