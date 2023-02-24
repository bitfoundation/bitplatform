﻿using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.OtpInput;

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
            Assert.IsFalse(bitOtpInput.ClassList.Contains("disabled"));
        }
        else
        {
            Assert.IsTrue(bitOtpInput.ClassList.Contains("disabled"));
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

        var bitOtpInput = com.Find(".bit-otp");

        Assert.AreEqual(length, bitOtpInput.Children.Length);
    }

    [DataTestMethod,
    DataRow(BitOtpInputDirection.LeftToRight),
    DataRow(BitOtpInputDirection.RightToLeft),
    DataRow(BitOtpInputDirection.TopToBottom),
    DataRow(BitOtpInputDirection.BottomToTop)
    ]
    public void BitOtpInputDirectionTest(BitOtpInputDirection direction)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Direction, direction);
        });

        string directionClass = direction switch
        {
            BitOtpInputDirection.LeftToRight => "left-to-right",
            BitOtpInputDirection.RightToLeft => "right-to-left",
            BitOtpInputDirection.TopToBottom => "top-to-bottom",
            BitOtpInputDirection.BottomToTop => "bottom-to-top",
            _ => null
        };

        var bitOtpInput = com.Find(".bit-otp");

        Assert.IsTrue(bitOtpInput.ClassList.Contains(directionClass));
    }

    [DataTestMethod,
    DataRow(BitOtpInputType.Text),
    DataRow(BitOtpInputType.Number),
    DataRow(BitOtpInputType.Password)
    ]
    public void BitOtpInputTypeTest(BitOtpInputType inputType)
    {
        var com = RenderComponent<BitOtpInput>(parameters =>
        {
            parameters.Add(p => p.Length, 1);
            parameters.Add(p => p.InputType, inputType);
        });

        string inputTypeAttribute = inputType switch
        {
            BitOtpInputType.Text => "text",
            BitOtpInputType.Number => "number",
            BitOtpInputType.Password => "password",
            _ => string.Empty
        };

        string inputModeAttribute = inputType switch
        {
            BitOtpInputType.Text => "text",
            BitOtpInputType.Number => "numeric",
            BitOtpInputType.Password => "text",
            _ => string.Empty
        };

        var bitOtpInput = com.Find(".input");

        Assert.IsTrue(bitOtpInput.GetAttribute("type").Equals(inputTypeAttribute));
        Assert.IsTrue(bitOtpInput.GetAttribute("inputmode").Equals(inputModeAttribute));
    }
}
