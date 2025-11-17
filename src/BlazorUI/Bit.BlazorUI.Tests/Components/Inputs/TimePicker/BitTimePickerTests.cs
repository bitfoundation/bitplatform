using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs.TimePicker;

[TestClass]
public class BitTimePickerTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitTimePickerTest(bool isEnabled)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitTimePicker = component.Find(".bit-tpc");

        if (isEnabled)
        {
            Assert.IsFalse(bitTimePicker.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitTimePicker.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod,
        DataRow("<div>This is labelTemplate</div>")
    ]
    public void BitTimePickerShouldRenderLabelTemplate(string labelTemplate)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            //parameters.Add(p => p.LabelTemplate, labelTemplate);
            parameters.Add(p => p.LabelTemplate, builder =>
            {
                builder.AddMarkupContent(0, labelTemplate);
            });
        });

        var labelChild = component.Find(".bit-tpc > label").ChildNodes;
        labelChild.MarkupMatches(labelTemplate);
    }

    [TestMethod,
        DataRow("ClockAria")
    ]
    public void BitTimePickerShouldRespectCalloutAriaLabel(string calloutAriaLabel)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutAriaLabel, calloutAriaLabel);
        });

        var callout = component.Find(".bit-tpc-cac");
        var ariaLabel = callout.GetAttribute("aria-label");

        Assert.AreEqual(calloutAriaLabel, ariaLabel);
    }

    [TestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitTimePickerShouldHandleOnClickEvent(bool isEnabled, int count)
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var clickedValue = 0;

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.OnClick, () => clickedValue++);
        });

        var bitTimePickerInput = component.Find(".bit-tpc-wrp");
        bitTimePickerInput.Click();

        Assert.AreEqual(count, clickedValue);
    }

    [TestMethod,
        DataRow("HourGlass")
    ]
    public void BitTimePickerShouldAcceptIcon(string iconName)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.IconName, iconName);
        });

        var icon = component.Find(".bit-tpc-ico");

        Assert.IsTrue(icon.ClassList.Contains($"bit-icon--{iconName}"));
    }

    [TestMethod,
        DataRow("Select a time..."),
        DataRow(null)
    ]
    public void BitTimePickerShouldRespectPlaceholder(string placeholder)
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Placeholder, placeholder);
        });

        var input = component.Find(".bit-tpc-inp");

        if (placeholder is not null)
        {
            Assert.IsTrue(input.HasAttribute("placeholder"));
            Assert.AreEqual(placeholder, input.GetAttribute("placeholder"));
        }
        else
        {
            Assert.IsFalse(input.HasAttribute("placeholder"));
        }
    }

    [TestMethod]
    public void BitTimePickerShouldRenderStandaloneInput()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.Standalone, true);
        });

        var input = component.Find("input.bit-input-hidden");
        Assert.IsNotNull(input);
    }

    [TestMethod]
    public void BitTimePickerShouldShowAmPmWhenTwelveHours()
    {
        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.TimeFormat, BitTimeFormat.TwelveHours);
        });

        var ampm = component.FindAll(".bit-tpc-am-pm");
        Assert.AreEqual(1, ampm.Count);
    }

    [TestMethod]
    public void BitTimePickerCalloutHtmlAttributesTest()
    {
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var calloutHtmlAttributes = new Dictionary<string, object>
        {
            { "style", "color: blue" }
        };

        var component = RenderComponent<BitTimePicker>(parameters =>
        {
            parameters.Add(p => p.CalloutHtmlAttributes, calloutHtmlAttributes);
        });

        var callout = component.Find(".bit-tpc-cac");
        var calloutStyle = callout.GetAttribute("style");

        Assert.AreEqual("color: blue", calloutStyle);
    }
}
