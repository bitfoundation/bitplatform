using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Inputs.OtpInput;

[TestClass]
public class BitOtpInputTests : BunitTestContext
{
    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
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

    [DataTestMethod,
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
}
