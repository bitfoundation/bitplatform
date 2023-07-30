using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Buttons;

[TestClass]
public class BitLoadingButtonTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true),
        DataRow(false)
    ]
    public void BitLoadingButtonTest(bool isEnabled)
    {
        var com = RenderComponent<BitLoadingButton>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
        });

        var bitLoadingButton = com.Find(".bit-ldb");

        if (isEnabled)
        {
            Assert.IsFalse(bitLoadingButton.ClassList.Contains("bit-dis"));
        }
        else
        {
            Assert.IsTrue(bitLoadingButton.ClassList.Contains("bit-dis"));
        }
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

        var styleClass = style is BitButtonStyle.Standard ? "bit-ldb-std" : "bit-ldb-pri";

        var bitLoadingButton = com.Find(".bit-ldb");

        Assert.IsTrue(bitLoadingButton.ClassList.Contains(styleClass));

        Assert.IsTrue(bitLoadingButton.GetAttribute("type").Equals(type.GetValue()));
    }

    [DataTestMethod,
        DataRow(true),
        DataRow(false),
    ]
    public void BitLoadingButtonContentTest(bool isLoading)
    {
        const string textContent = "Hi";

        var com = RenderComponent<BitLoadingButton>(parameters =>
        {
            parameters.Add(p => p.ChildContent, textContent);
            parameters.Add(p => p.IsLoading, isLoading);
        });

        var bitLoadingButton = com.Find(".bit-ldb");

        if (isLoading)
        {
            Assert.IsTrue(bitLoadingButton.FirstElementChild.ClassList.Contains("bit-ldb-ldg"));
        }
        else
        {
            Assert.AreEqual(textContent, bitLoadingButton.TextContent);
        }
    }

    [DataTestMethod,
        DataRow(BitLabelPosition.Top),
        DataRow(BitLabelPosition.Top),
        DataRow(BitLabelPosition.Top),
        DataRow(BitLabelPosition.Top),

        DataRow(BitLabelPosition.Right),
        DataRow(BitLabelPosition.Right),
        DataRow(BitLabelPosition.Right),
        DataRow(BitLabelPosition.Right),

        DataRow(BitLabelPosition.Bottom),
        DataRow(BitLabelPosition.Bottom),
        DataRow(BitLabelPosition.Bottom),
        DataRow(BitLabelPosition.Bottom),

        DataRow(BitLabelPosition.Left),
        DataRow(BitLabelPosition.Left),
        DataRow(BitLabelPosition.Left),
        DataRow(BitLabelPosition.Left),

        DataRow(null),
    ]
    public void BitLoadingButtonLoaderTest(BitLabelPosition? labelPosition)
    {
        const string loadingLabel = "I'm Loading Label";

        var com = RenderComponent<BitLoadingButton>(parameters =>
        {
            parameters.Add(p => p.IsLoading, true);
            parameters.Add(p => p.LoadingLabel, loadingLabel);
            if (labelPosition.HasValue)
            {
                parameters.Add(p => p.LoadingLabelPosition, labelPosition.Value);
            }
        });

        var bitLoadingButton = com.Find(".bit-ldb");

        var labelPositionClass = labelPosition switch
        {
            BitLabelPosition.Top => "bit-ldb-top",
            BitLabelPosition.Right => "bit-ldb-right",
            BitLabelPosition.Bottom => "bit-ldb-bottom",
            BitLabelPosition.Left => "bit-ldb-left",
            _ => "bit-ldb-right"
        };

        Assert.AreEqual(loadingLabel, bitLoadingButton.LastElementChild.TextContent);

        Assert.IsTrue(bitLoadingButton.FirstElementChild.ClassList.Contains("bit-ldb-ldg"));

        Assert.IsTrue(bitLoadingButton.FirstElementChild.ClassList.Contains(labelPositionClass));
    }
}
