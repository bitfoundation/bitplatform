using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Navs.Pivot;

[TestClass]
public class BitPivotTests : BunitTestContext
{
    [DataTestMethod,
         DataRow(BitPivotHeaderType.Link, BitSize.Large, BitPivotOverflowBehavior.None),
         DataRow(BitPivotHeaderType.Tab, BitSize.Medium, BitPivotOverflowBehavior.Scroll),
         DataRow(BitPivotHeaderType.Tab, BitSize.Small, BitPivotOverflowBehavior.Menu)
     ]
    public void BitPivotShouldRespectLinkFormatClasses(BitPivotHeaderType linkFormat, BitSize linkSize, BitPivotOverflowBehavior overflowBehavior)
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.HeaderType, linkFormat);
            parameters.Add(p => p.Size, linkSize);
            parameters.Add(p => p.OverflowBehavior, overflowBehavior);
        });

        var linkSizeClass = $"bit-pvt-{linkSize switch { BitSize.Small => "sm", BitSize.Medium => "md", BitSize.Large => "lg", _ => "md" }}";
        var linkFormatClass = $"bit-pvt-{linkFormat.ToString().ToLower()}";
        var overflowBehaviorClass = $"bit-pvt-{overflowBehavior.ToString().ToLower()}";

        var bitPivot = component.Find(".bit-pvt");

        Assert.IsTrue(bitPivot.ClassList.Contains(linkFormatClass));
        Assert.IsTrue(bitPivot.ClassList.Contains(linkSizeClass));
        Assert.IsTrue(bitPivot.ClassList.Contains(overflowBehaviorClass));
    }

    [Ignore("bypassed - BUnit 2-way bound parameters issue")]
    [DataTestMethod,
         DataRow(false, false),
         DataRow(true, true)
     ]
    public void BitPivotShouldRespectSelectKey(bool isEnabled, bool expectedResult)
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddCascadingValue(this);
            parameters.AddChildContent<BitPivotItem>();
            parameters.AddChildContent<BitPivotItem>(parameters => parameters.Add(p => p.IsEnabled, isEnabled));
        });

        component.FindAll(".bit-pvt > div:first-child > div")[1].Click();

        //TODO: bypassed - BUnit 2-way bound parameters issue
        Assert.AreEqual(component.FindAll(".bit-pvt > div:first-child > div")[1].ClassList.Contains("bit-pvti-sel"), expectedResult);
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitPivotAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitPivot>(parameters =>
        {
            parameters.AddChildContent<BitPivotItem>();
            parameters.AddChildContent<BitPivotItem>(parameters => parameters.Add(p => p.AriaLabel, ariaLabel));
        });

        var bitPivots = com.FindAll(".bit-pvt > div:first-child > div");

        foreach (var bitPivot in bitPivots)
        {
            Assert.IsTrue(bitPivot.GetAttribute("aria-label").Equals(ariaLabel));
        }
    }

    [DataTestMethod,
         DataRow(BitPivotPosition.Top),
         DataRow(BitPivotPosition.Bottom),
         DataRow(BitPivotPosition.Left),
         DataRow(BitPivotPosition.Right)
    ]
    public void BitPivotShouldRespectPosition(BitPivotPosition position)
    {
        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Position, position);
        });

        var positionClass = position switch
        {
            BitPivotPosition.Top => "bit-pvt-top",
            BitPivotPosition.Bottom => "bit-pvt-bottom",
            BitPivotPosition.Left => "bit-pvt-left",
            BitPivotPosition.Right => "bit-pvt-right",
            _ => string.Empty
        };

        var bitPivot = component.Find(".bit-pvt");

        Assert.IsTrue(bitPivot.ClassList.Contains(positionClass));
    }
}
