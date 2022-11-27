﻿using System.Drawing;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitLoadingButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(Visual.Fluent, true),
        DataRow(Visual.Fluent, false),

        DataRow(Visual.Cupertino, true),
        DataRow(Visual.Cupertino, false),

        DataRow(Visual.Material, true),
        DataRow(Visual.Material, false),
    ]
    public void BitLoadingButtonTest(Visual visual, bool isEnabled)
    {
        var com = RenderComponent<BitLoadingButton>(parameters =>
        {
            parameters.AddCascadingValue(visual);
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var isEnabledClass = isEnabled ? "enabled" : "disabled";
        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        var bitLoadingButton = com.Find(".bit-lbtn");

        Assert.IsTrue(bitLoadingButton.ClassList.Contains($"bit-lbtn-{isEnabledClass}-{visualClass}"));
    }

    [DataTestMethod,
        DataRow(Visual.Fluent, BitButtonSize.Small),
        DataRow(Visual.Fluent, BitButtonSize.Medium),
        DataRow(Visual.Fluent, BitButtonSize.Large),

        DataRow(Visual.Cupertino, BitButtonSize.Small),
        DataRow(Visual.Cupertino, BitButtonSize.Medium),
        DataRow(Visual.Cupertino, BitButtonSize.Large),

        DataRow(Visual.Material, BitButtonSize.Small),
        DataRow(Visual.Material, BitButtonSize.Medium),
        DataRow(Visual.Material, BitButtonSize.Large)
    ]
    public void BitLoadingButtonSizeTest(Visual visual, BitButtonSize size)
    {
        var com = RenderComponent<BitLoadingButton>(parameters =>
        {
            parameters.AddCascadingValue(visual);
            parameters.Add(p => p.ButtonSize, size);
        });

        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
        var sizeClass = size == BitButtonSize.Small ? "sm" : size == BitButtonSize.Medium ? "md" : "lg";

        var bitLoadingButton = com.Find(".bit-lbtn");

        Assert.IsTrue(bitLoadingButton.ClassList.Contains($"bit-lbtn-{sizeClass}-{visualClass}"));
    }

    [DataTestMethod,
        DataRow(BitButtonStyle.Primary, BitButtonType.Button),
        DataRow(BitButtonStyle.Primary, BitButtonType.Submit),
        DataRow(BitButtonStyle.Primary, BitButtonType.Reset),

        DataRow(BitButtonStyle.Standard, BitButtonType.Button),
        DataRow(BitButtonStyle.Standard, BitButtonType.Submit),
        DataRow(BitButtonStyle.Standard, BitButtonType.Reset)
    ]
    public void BitLoadingButtonStyleAndTypeTest(BitButtonStyle style, BitButtonType type)
    {
        var com = RenderComponent<BitLoadingButton>(parameters =>
        {
            parameters.Add(p => p.ButtonStyle, style);
            parameters.Add(p => p.ButtonType, type);
        });

        var styleClass = style is BitButtonStyle.Standard ? "standard" : "primary";

        var bitLoadingButton = com.Find(".bit-lbtn");

        Assert.IsTrue(bitLoadingButton.ClassList.Contains(styleClass));

        Assert.IsTrue(bitLoadingButton.GetAttribute("type").Equals(type.GetValue()));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitLoadingButtonContentTest(bool isLoading)
    {
        string TextContent = "Hi";

        var com = RenderComponent<BitLoadingButton>(parameters =>
        {
            parameters.Add(p => p.ChildContent, TextContent);
            parameters.Add(p => p.IsLoading, isLoading);
        });

        var bitLoadingButton = com.Find(".bit-lbtn");

        if (isLoading)
        {
            Assert.IsTrue(bitLoadingButton.FirstElementChild.ClassList.Contains("loading"));
        }
        else
        {
            Assert.AreEqual(bitLoadingButton.TextContent, TextContent);
        }
    }

    [DataTestMethod,
        DataRow(BitLabelPosition.Top, BitSpinnerSize.XSmall),
        DataRow(BitLabelPosition.Top, BitSpinnerSize.Small),
        DataRow(BitLabelPosition.Top, BitSpinnerSize.Medium),
        DataRow(BitLabelPosition.Top, BitSpinnerSize.Large),

        DataRow(BitLabelPosition.Right, BitSpinnerSize.XSmall),
        DataRow(BitLabelPosition.Right, BitSpinnerSize.Small),
        DataRow(BitLabelPosition.Right, BitSpinnerSize.Medium),
        DataRow(BitLabelPosition.Right, BitSpinnerSize.Large),

        DataRow(BitLabelPosition.Bottom, BitSpinnerSize.XSmall),
        DataRow(BitLabelPosition.Bottom, BitSpinnerSize.Small),
        DataRow(BitLabelPosition.Bottom, BitSpinnerSize.Medium),
        DataRow(BitLabelPosition.Bottom, BitSpinnerSize.Large),

        DataRow(BitLabelPosition.Left, BitSpinnerSize.XSmall),
        DataRow(BitLabelPosition.Left, BitSpinnerSize.Small),
        DataRow(BitLabelPosition.Left, BitSpinnerSize.Medium),
        DataRow(BitLabelPosition.Left, BitSpinnerSize.Large),
    ]
    public void BitLoadingButtonLoaderTest(BitLabelPosition labelPosition, BitSpinnerSize spinnerSize)
    {
        string loadingLabel = "I'm Loading Label";

        var com = RenderComponent<BitLoadingButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingLabel, loadingLabel);
            parameters.Add(p => p.LoadingLabelPosition, labelPosition);
            parameters.Add(p => p.LoadingSpinnerSize, spinnerSize);
        });

        var bitLoadingButton = com.Find(".bit-lbtn");

        var labelPositionClass = labelPosition switch
        {
            BitLabelPosition.Top => "top",
            BitLabelPosition.Right => "right",
            BitLabelPosition.Bottom => "bottom",
            BitLabelPosition.Left => "left",
            _ => string.Empty
        };

        var spinnerSizeClass = spinnerSize switch
        {
            BitSpinnerSize.XSmall => "xSmall",
            BitSpinnerSize.Small => "small",
            BitSpinnerSize.Medium => "medium",
            BitSpinnerSize.Large => "large",
            _ => string.Empty
        };

        Assert.AreEqual(bitLoadingButton.LastElementChild.TextContent, loadingLabel);

        Assert.IsTrue(bitLoadingButton.FirstElementChild.ClassList.Contains("loading"));

        Assert.IsTrue(bitLoadingButton.FirstElementChild.ClassList.Contains(labelPositionClass));

        Assert.IsTrue(bitLoadingButton.FirstElementChild.ClassList.Contains(spinnerSizeClass));
    }
}
