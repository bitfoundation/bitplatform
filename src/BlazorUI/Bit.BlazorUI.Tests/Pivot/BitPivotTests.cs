using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Pivot;

[TestClass]
public class BitPivotTests : BunitTestContext
{
    [DataTestMethod,
     DataRow(Visual.Fluent, BitPivotLinkFormat.Links, BitPivotLinkSize.Large, BitPivotOverflowBehavior.None),
     DataRow(Visual.Fluent, BitPivotLinkFormat.Tabs, BitPivotLinkSize.Normal, BitPivotOverflowBehavior.Scroll),
     DataRow(Visual.Fluent, BitPivotLinkFormat.Tabs, BitPivotLinkSize.Normal, BitPivotOverflowBehavior.Menu),

     DataRow(Visual.Cupertino, BitPivotLinkFormat.Links, BitPivotLinkSize.Large, BitPivotOverflowBehavior.None),
     DataRow(Visual.Cupertino, BitPivotLinkFormat.Tabs, BitPivotLinkSize.Normal, BitPivotOverflowBehavior.Scroll),
     DataRow(Visual.Cupertino, BitPivotLinkFormat.Tabs, BitPivotLinkSize.Normal, BitPivotOverflowBehavior.Menu),

     DataRow(Visual.Material, BitPivotLinkFormat.Links, BitPivotLinkSize.Large, BitPivotOverflowBehavior.None),
     DataRow(Visual.Material, BitPivotLinkFormat.Tabs, BitPivotLinkSize.Normal, BitPivotOverflowBehavior.Scroll),
     DataRow(Visual.Material, BitPivotLinkFormat.Tabs, BitPivotLinkSize.Normal, BitPivotOverflowBehavior.Menu)]
    public void BitPivotShouldRepectLinkFormatClasses(Visual visual, BitPivotLinkFormat linkFormat, BitPivotLinkSize linkSize, BitPivotOverflowBehavior overflowBehavior)
    {
        var component = RenderComponent<BitPivotTest>(parameters =>
        {
            parameters.Add(p => p.Visual, visual);
            parameters.Add(p => p.LinkFormat, linkFormat);
            parameters.Add(p => p.LinkSize, linkSize);
            parameters.Add(p => p.OverflowBehavior, overflowBehavior);
        });

        var visualClass = visual.ToString().ToLower();

        var linkFormatClass = $"bit-pvt-{linkFormat.ToString().ToLower()}-{visualClass}";
        var linkSizeClass = $"bit-pvt-{linkSize.ToString().ToLower()}-{visualClass}";
        var overflowBehaviorClass = $"bit-pvt-{overflowBehavior.ToString().ToLower()}-{visualClass}";

        var bitPivot = component.Find($".bit-pvt");

        Assert.IsTrue(bitPivot.ClassList.Contains(linkFormatClass));
        Assert.IsTrue(bitPivot.ClassList.Contains(linkSizeClass));
        Assert.IsTrue(bitPivot.ClassList.Contains(overflowBehaviorClass));
    }


    [DataTestMethod,
     DataRow(false, false),
     DataRow(true, true)]
    public void BitPivotShouldRespectSelectKey(bool isEnabled, bool expectedResult)
    {
        var component = RenderComponent<BitPivotTest>(parameters =>
        {
            parameters.AddCascadingValue(this);
            parameters.AddChildContent<BitPivotItem>();
            parameters.AddChildContent<BitPivotItem>(parameters => parameters.Add(p => p.IsEnabled, isEnabled));
        });

        //component.FindAll(".bit-pvt > div:first-child > div")[1].Click();

        //TODO: bypassed - BUnit 2-way bound parameters issue
        //Assert.AreEqual(component.FindAll(".bit-pvt > div:first-child > div")[1].ClassList.Contains("selected-item"), expectedResult);
    }

    [DataTestMethod, DataRow("Detailed label")]
    public void BitPivotAriaLabelTest(string ariaLabel)
    {
        var com = RenderComponent<BitPivotTest>(parameters =>
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
     DataRow(BitPivotPosition.Right)]
    public void BitPivotShouldRepectPosition(BitPivotPosition position)
    {
        var component = RenderComponent<BitPivotTest>(parameters =>
        {
            parameters.Add(p => p.Position, position);
        });

        var positionClass = position == BitPivotPosition.Top ? $"position-top"
                            : position == BitPivotPosition.Bottom ? $"position-bottom"
                            : position == BitPivotPosition.Left ? $"position-left"
                            : position == BitPivotPosition.Right ? $"position-right"
                            : string.Empty;

        var bitPivot = component.Find(".bit-pvt");

        Assert.IsTrue(bitPivot.ClassList.Contains(positionClass));
    }
}
