using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.OtpInput;

[TestClass]
public class BitOtpInputTests : BunitTestContext
{
    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOtpInputTest(bool isEnabled)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitOtpInput = com.Find(".bit-otp");

        if (isEnabled)
        {
            Assert.IsFalse(bitOtpInput.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitOtpInput.ClassList.Contains("bit-dis"));
        }
    }

    [TestMethod,
        DataRow(4),
        DataRow(6)
    ]
    public void BitOtpInputLengthTest(int length)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, length);
        });

        var bitOtpInput = com.Find(".bit-otp-iwr");

        Assert.AreEqual(length, bitOtpInput.Children.Length);
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOtpInputReversedTest(bool reversed)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Reversed, reversed);
        });

        var bitOtpInput = com.Find(".bit-otp");

        if (reversed)
        {
            Assert.IsTrue(bitOtpInput.ClassList.Contains("bit-otp-rvs"));
        }
        else
        {
            Assert.IsFalse(bitOtpInput.ClassList.Contains("bit-otp-rvs"));
        }
    }

    [TestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitOtpInputVerticalTest(bool vertical)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Vertical, vertical);
        });

        var bitOtpInput = com.Find(".bit-otp");

        if (vertical)
        {
            Assert.IsTrue(bitOtpInput.ClassList.Contains("bit-otp-vrt"));
        }
        else
        {
            Assert.IsFalse(bitOtpInput.ClassList.Contains("bit-otp-vrt"));
        }
    }

    [TestMethod,
        DataRow(BitInputType.Text),
        DataRow(BitInputType.Number),
        DataRow(BitInputType.Password)
    ]
    public void BitInputTypeTest(BitInputType inputType)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 1);
            parameters.Add(p => p.Type, inputType);
        });

        string inputTypeAttribute = inputType switch
        {
            BitInputType.Text => "text",
            BitInputType.Number => "number",
            BitInputType.Password => "password",
            _ => string.Empty
        };

        string inputModeAttribute = inputType switch
        {
            BitInputType.Text => "text",
            BitInputType.Number => "numeric",
            BitInputType.Password => "text",
            _ => string.Empty
        };

        var bitOtpInput = com.Find(".bit-otp-inp");

        Assert.AreEqual(inputTypeAttribute, bitOtpInput.GetAttribute("type"));
        Assert.AreEqual(inputModeAttribute, bitOtpInput.GetAttribute("inputmode"));
    }

    [TestMethod]
    public void BitOtpInputShouldRespondToFocusEventsWithIndex()
    {
        int focusInIndex = -1;
        int focusOutIndex = -1;

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.OnFocusIn, args => focusInIndex = args.Index);
            parameters.Add(p => p.OnFocusOut, args => focusOutIndex = args.Index);
        });

        var input = com.FindAll(".bit-otp-inp")[1];

        input.FocusIn();
        Assert.AreEqual(1, focusInIndex);

        com.FindAll(".bit-otp-inp")[1].FocusOut();
        Assert.AreEqual(1, focusOutIndex);
    }

    [TestMethod]
    public void BitOtpInputShouldToggleFocusedClassWithoutFocusCallbacks()
    {
        // The Focused class/style must be applied based purely on the input focus state,
        // independent of whether OnFocusIn/OnFocusOut delegates are attached.
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 3);
            parameters.Add(p => p.Classes, new BitOtpInputClassStyles { Focused = "custom-focused" });
        });

        com.FindAll(".bit-otp-inp")[1].FocusIn();
        Assert.IsTrue(com.FindAll(".bit-otp-inp")[1].ClassList.Contains("custom-focused"));

        com.FindAll(".bit-otp-inp")[1].FocusOut();
        Assert.IsFalse(com.FindAll(".bit-otp-inp")[1].ClassList.Contains("custom-focused"));
    }

    [TestMethod]
    public void BitOtpInputShouldRespectDefaultValue()
    {
        var defaultValue = "1234";

        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 4);
            parameters.Add(p => p.DefaultValue, defaultValue);
        });

        Assert.AreEqual(defaultValue, com.Instance.Value);

        var inputs = com.FindAll(".bit-otp-inp");
        Assert.AreEqual("1", inputs[0].GetAttribute("value"));
        Assert.AreEqual("2", inputs[1].GetAttribute("value"));
        Assert.AreEqual("3", inputs[2].GetAttribute("value"));
        Assert.AreEqual("4", inputs[3].GetAttribute("value"));
    }
}
