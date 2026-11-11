using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Layouts.Grid;

[TestClass]
public class BitGridTests : BunitTestContext
{
    [TestMethod]
    public void BitGridShouldRenderExpectedElement()
    {
        var component = RenderComponent<BitGrid>();

        var root = component.Find(".bit-grd");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual("bit-grd", root.ClassName);
    }

    [TestMethod]
    public void BitGridShouldRenderChildContent()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.AddChildContent("<div class='child'>Hello</div>");
        });

        Assert.IsNotNull(component.Find(".bit-grd"));

        var child = component.Find(".child");

        Assert.IsNotNull(child);
        Assert.AreEqual("Hello", child.TextContent);
    }

    [TestMethod]
    [DataRow("section")]
    [DataRow("ul")]
    [DataRow("main")]
    public void BitGridShouldRespectElement(string element)
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        Assert.AreEqual(element.ToUpperInvariant(), component.Find(".bit-grd").TagName);
    }

    [TestMethod]
    public void BitGridShouldSetTheColumnsAndSpanCssVariables()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Span, 2);
            parameters.Add(p => p.Columns, 8);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-span:2", style!);
        Assert.Contains("--bit-grd-cols:8", style);
    }

    [TestMethod]
    public void BitGridShouldLeaveAnUnsetColumnCountToTheStylesheet()
    {
        // An unset column count declares nothing, so the stylesheet's fallback to the public
        // --bit-Grid-columns variable (12 when that is unset too) decides it.
        var component = RenderComponent<BitGrid>();

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-span:1", style!);
        Assert.DoesNotContain("--bit-grd-cols:", style, style);
    }

    [TestMethod]
    public void BitGridShouldDeclareAnExplicitDefaultColumnCount()
    {
        // Columns="12" written out is a decision of the grid itself and has to beat an inherited --bit-Grid-columns.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Columns, 12);
        });

        Assert.Contains("--bit-grd-cols:12", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldReportAnUnsetColumnCountAsNull()
    {
        // The count of an unset grid is resolved by the stylesheet from --bit-Grid-columns, so any number reported
        // here could disagree with the columns actually rendered.
        var component = RenderComponent<BitGrid>();

        Assert.IsNull(component.Instance.Columns);
    }

    [TestMethod]
    public void BitGridShouldDeclareTheDefaultColumnCountOnceItIsSetOnARerender()
    {
        // An unset grid and Columns="12" lay out the same until --bit-Grid-columns is declared above them, so
        // going from one to the other has to be seen by the style even though the count it works out to is equal.
        var component = RenderComponent<BitGrid>();

        Assert.DoesNotContain("--bit-grd-cols:", component.Find(".bit-grd").GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.Columns, 12));

        Assert.Contains("--bit-grd-cols:12", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldStopDeclaringTheColumnCountOnceItIsClearedOnARerender()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Columns, 12);
        });

        Assert.Contains("--bit-grd-cols:12", component.Find(".bit-grd").GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.Columns, null));

        Assert.DoesNotContain("--bit-grd-cols:", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldDeclareAColumnCountItWasGivenThroughTheCascadingParameters()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.AddCascadingValue(BitGridParams.ParamName, new BitGridParams { Columns = 12 });
        });

        Assert.Contains("--bit-grd-cols:12", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-12)]
    public void BitGridShouldGuardTheColumnCountAgainstValuesAWidthCannotBeDividedBy(int columns)
    {
        // A column count of 0 would divide the width of every item by zero, which throws the whole
        // flex-basis away and collapses the layout.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Columns, columns);
        });

        Assert.Contains("--bit-grd-cols:1", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-3)]
    public void BitGridShouldGuardTheDefaultSpanAgainstValuesThatAreNotAWidth(int span)
    {
        // A span of no columns is an item asking not to be shown, which only an item can mean. As the default of
        // a grid it would leave every item that named no span of its own with no width and nothing said about it.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Span, span);
        });

        Assert.Contains("--bit-grd-span:1", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldOnlyRenderTheResponsiveColumnsThatWereAskedFor()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Columns, 6);
            parameters.Add(p => p.ColumnsXs, 2);
            parameters.Add(p => p.ColumnsMd, 4);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cols:6", style!);
        Assert.Contains("--bit-grd-cols-xs:2", style);
        Assert.Contains("--bit-grd-cols-md:4", style);

        // The breakpoints that were left alone are filled in by the stylesheet from the breakpoint below
        // them, which is what makes the values mobile first, so they must not be rendered here.
        Assert.DoesNotContain("--bit-grd-cols-sm", style!);
        Assert.DoesNotContain("--bit-grd-cols-lg", style);
        Assert.DoesNotContain("--bit-grd-cols-xl", style);
        Assert.DoesNotContain("--bit-grd-cols-xxl", style);
    }

    [TestMethod]
    public void BitGridShouldRenderEveryResponsiveColumnCount()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.ColumnsXs, 1);
            parameters.Add(p => p.ColumnsSm, 2);
            parameters.Add(p => p.ColumnsMd, 3);
            parameters.Add(p => p.ColumnsLg, 4);
            parameters.Add(p => p.ColumnsXl, 5);
            parameters.Add(p => p.ColumnsXxl, 6);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cols-xs:1", style!);
        Assert.Contains("--bit-grd-cols-sm:2", style);
        Assert.Contains("--bit-grd-cols-md:3", style);
        Assert.Contains("--bit-grd-cols-lg:4", style);
        Assert.Contains("--bit-grd-cols-xl:5", style);
        Assert.Contains("--bit-grd-cols-xxl:6", style);
    }

    [TestMethod]
    public void BitGridShouldGuardTheResponsiveColumnCounts()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.ColumnsSm, 0);
        });

        Assert.Contains("--bit-grd-cols-sm:1", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldUseTheSpacingOnBothAxes()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, "1rem");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap:1rem", style!);
        Assert.Contains("--bit-grd-rgap:1rem", style);
    }

    [TestMethod]
    public void BitGridShouldLeaveTheDefaultSpacingToTheStylesheet()
    {
        // An unset spacing declares no gap of its own, which is what lets the stylesheet resolve it from the
        // --bit-Grid-* variables and the spacing unit (and density) of the theme.
        var component = RenderComponent<BitGrid>();

        var style = component.Find(".bit-grd").GetAttribute("style") ?? string.Empty;

        Assert.DoesNotContain("--bit-grd-cgap:", style);
        Assert.DoesNotContain("--bit-grd-rgap:", style);
    }

    [TestMethod]
    public void BitGridShouldDeclareOnlyTheAxisThatWasGivenASpacing()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.HorizontalSpacing, "1rem");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap:1rem", style!);
        Assert.DoesNotContain("--bit-grd-rgap:", style);
    }

    [TestMethod]
    public void BitGridShouldKeepTheAxesOfTheSpacingApart()
    {
        // The horizontal spacing used to be the gap of both axes, so a grid that only widened the room
        // between its columns silently widened the room between its rows as well.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, "4px");
            parameters.Add(p => p.HorizontalSpacing, "10px");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap:10px", style!);
        Assert.Contains("--bit-grd-rgap:4px", style);
    }

    [TestMethod]
    public void BitGridShouldRespectTheSpacingOfEachAxis()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.HorizontalSpacing, "10px");
            parameters.Add(p => p.VerticalSpacing, "20px");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap:10px", style!);
        Assert.Contains("--bit-grd-rgap:20px", style);
    }

    [TestMethod]
    public void BitGridShouldRenderEveryResponsiveSpacing()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.SpacingXs, "1px");
            parameters.Add(p => p.SpacingSm, "2px");
            parameters.Add(p => p.SpacingMd, "3px");
            parameters.Add(p => p.SpacingLg, "4px");
            parameters.Add(p => p.SpacingXl, "5px");
            parameters.Add(p => p.SpacingXxl, "6px");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        // The spacing of both axes is the base of the chain of each of them.
        Assert.Contains("--bit-grd-cgap-xs:1px", style!);
        Assert.Contains("--bit-grd-rgap-xs:1px", style);
        Assert.Contains("--bit-grd-cgap-sm:2px", style);
        Assert.Contains("--bit-grd-rgap-sm:2px", style);
        Assert.Contains("--bit-grd-cgap-md:3px", style);
        Assert.Contains("--bit-grd-rgap-md:3px", style);
        Assert.Contains("--bit-grd-cgap-lg:4px", style);
        Assert.Contains("--bit-grd-rgap-lg:4px", style);
        Assert.Contains("--bit-grd-cgap-xl:5px", style);
        Assert.Contains("--bit-grd-rgap-xl:5px", style);
        Assert.Contains("--bit-grd-cgap-xxl:6px", style);
        Assert.Contains("--bit-grd-rgap-xxl:6px", style);
    }

    [TestMethod]
    public void BitGridShouldOnlyRenderTheResponsiveSpacingsThatWereAskedFor()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, "1rem");
            parameters.Add(p => p.SpacingMd, "2rem");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap:1rem", style!);
        Assert.Contains("--bit-grd-cgap-md:2rem", style);
        Assert.Contains("--bit-grd-rgap-md:2rem", style);

        // The breakpoints that were left alone are chained to the one below them by the stylesheet, which is
        // what carries a spacing upwards and makes these mobile first.
        Assert.DoesNotContain("--bit-grd-cgap-xs", style!);
        Assert.DoesNotContain("--bit-grd-cgap-sm", style);
        Assert.DoesNotContain("--bit-grd-cgap-lg", style);
        Assert.DoesNotContain("--bit-grd-rgap-xxl", style);
    }

    [TestMethod]
    public void BitGridShouldRenderEveryResponsiveSpacingOfEachAxis()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.HorizontalSpacingXs, "1px");
            parameters.Add(p => p.HorizontalSpacingSm, "2px");
            parameters.Add(p => p.HorizontalSpacingMd, "3px");
            parameters.Add(p => p.HorizontalSpacingLg, "4px");
            parameters.Add(p => p.HorizontalSpacingXl, "5px");
            parameters.Add(p => p.HorizontalSpacingXxl, "6px");
            parameters.Add(p => p.VerticalSpacingXs, "7px");
            parameters.Add(p => p.VerticalSpacingSm, "8px");
            parameters.Add(p => p.VerticalSpacingMd, "9px");
            parameters.Add(p => p.VerticalSpacingLg, "10px");
            parameters.Add(p => p.VerticalSpacingXl, "11px");
            parameters.Add(p => p.VerticalSpacingXxl, "12px");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap-xs:1px", style!);
        Assert.Contains("--bit-grd-cgap-sm:2px", style);
        Assert.Contains("--bit-grd-cgap-md:3px", style);
        Assert.Contains("--bit-grd-cgap-lg:4px", style);
        Assert.Contains("--bit-grd-cgap-xl:5px", style);
        Assert.Contains("--bit-grd-cgap-xxl:6px", style);

        Assert.Contains("--bit-grd-rgap-xs:7px", style);
        Assert.Contains("--bit-grd-rgap-sm:8px", style);
        Assert.Contains("--bit-grd-rgap-md:9px", style);
        Assert.Contains("--bit-grd-rgap-lg:10px", style);
        Assert.Contains("--bit-grd-rgap-xl:11px", style);
        Assert.Contains("--bit-grd-rgap-xxl:12px", style);
    }

    [TestMethod]
    public void BitGridShouldLetTheResponsiveSpacingOfAnAxisWinOverTheOneOfBothAxes()
    {
        // The two ways of naming the room at one breakpoint stand in the same relation as the two at the base of
        // the chain, so a grid that widens both axes at a size and then narrows one of them is written once.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.SpacingMd, "2rem");
            parameters.Add(p => p.HorizontalSpacingMd, "1rem");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap-md:1rem", style!);
        Assert.Contains("--bit-grd-rgap-md:2rem", style);
    }

    [TestMethod]
    public void BitGridShouldReadAResponsiveSpacingWithNoUnitAsPixels()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.SpacingSm, "12");
            parameters.Add(p => p.VerticalSpacingLg, "-4");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap-sm:12px", style!);
        Assert.Contains("--bit-grd-rgap-sm:12px", style);
        Assert.Contains("--bit-grd-rgap-lg:0px", style);
    }

    [TestMethod]
    [DataRow("-1rem")]
    [DataRow("-16px")]
    [DataRow("-2%")]
    [DataRow("-8")]
    public void BitGridShouldReadALengthWrittenWithAMinusAsNoLengthAtAll(string spacing)
    {
        // A negative gap widens the items past their tracks instead of narrowing them, and the unit it is
        // written with makes no difference to that, so a minus is turned away wherever it is written.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, spacing);
            parameters.Add(p => p.MinItemWidth, spacing);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap:0px", style!);
        Assert.Contains("--bit-grd-rgap:0px", style);
        Assert.Contains("--bit-grd-mnw:0px", style);
    }

    [TestMethod]
    public void BitGridShouldKeepTheBaseSpacingApartFromTheResponsiveOnes()
    {
        // The base is what every breakpoint below the first one that was named falls back to, so naming a wider
        // breakpoint must not take the room away from the sizes underneath it.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, "8px");
            parameters.Add(p => p.HorizontalSpacing, "16px");
            parameters.Add(p => p.SpacingLg, "24px");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap:16px", style!);
        Assert.Contains("--bit-grd-rgap:8px", style);
        Assert.Contains("--bit-grd-cgap-lg:24px", style);
        Assert.Contains("--bit-grd-rgap-lg:24px", style);
    }

    [TestMethod]
    public void BitGridShouldRelayoutWhenAResponsiveSpacingChanges()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.SpacingMd, "1rem");
        });

        Assert.Contains("--bit-grd-cgap-md:1rem", component.Find(".bit-grd").GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.SpacingMd, "2rem"));

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap-md:2rem", style!);
        Assert.Contains("--bit-grd-rgap-md:2rem", style);
    }

    [TestMethod]
    public void BitGridShouldFallBackToTheThemeSpacingWhenTheSpacingIsBlank()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, string.Empty);
        });

        var style = component.Find(".bit-grd").GetAttribute("style") ?? string.Empty;

        Assert.DoesNotContain("--bit-grd-cgap:", style);
        Assert.DoesNotContain("--bit-grd-rgap:", style);
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "flex-start")]
    [DataRow(BitAlignment.End, "flex-end")]
    [DataRow(BitAlignment.Center, "center")]
    [DataRow(BitAlignment.SpaceBetween, "space-between")]
    [DataRow(BitAlignment.SpaceAround, "space-around")]
    [DataRow(BitAlignment.SpaceEvenly, "space-evenly")]
    public void BitGridShouldSetJustifyContentBasedOnTheHorizontalAlign(BitAlignment alignment, string expected)
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.HorizontalAlign, alignment);
        });

        Assert.Contains($"justify-content:{expected}", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    [DataRow(BitAlignment.Baseline, "baseline")]
    [DataRow(BitAlignment.Stretch, "stretch")]
    public void BitGridShouldMoveTheHorizontalAlignsThatDistributeNothingToTheCrossAxis(BitAlignment alignment, string expected)
    {
        // justify-content: baseline is not a value the browser accepts, and stretch behaves like the start
        // of the line there, so both only mean something on the cross axis.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.HorizontalAlign, alignment);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains($"align-items:{expected}", style!);
        Assert.DoesNotContain("justify-content", style!);
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "flex-start")]
    [DataRow(BitAlignment.End, "flex-end")]
    [DataRow(BitAlignment.Center, "center")]
    [DataRow(BitAlignment.Baseline, "baseline")]
    [DataRow(BitAlignment.Stretch, "stretch")]
    public void BitGridShouldSetAlignItemsBasedOnTheVerticalAlign(BitAlignment alignment, string expected)
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.VerticalAlign, alignment);
        });

        Assert.Contains($"align-items:{expected}", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    [DataRow(BitAlignment.SpaceBetween)]
    [DataRow(BitAlignment.SpaceAround)]
    [DataRow(BitAlignment.SpaceEvenly)]
    public void BitGridShouldIgnoreTheVerticalAlignsThatHaveNoMeaningOnTheCrossAxis(BitAlignment alignment)
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.VerticalAlign, alignment);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.DoesNotContain("align-items", (style ?? string.Empty));
    }

    [TestMethod]
    public void BitGridShouldLetTheVerticalAlignWinOverAHorizontalAlignThatFellThrough()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.HorizontalAlign, BitAlignment.Stretch);
            parameters.Add(p => p.VerticalAlign, BitAlignment.Center);
        });

        Assert.Contains("align-items:center", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldApplyTheAlignmentShorthandToBothAxes()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Center);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("justify-content:center", style!);
        Assert.Contains("align-items:center", style);
    }

    [TestMethod]
    public void BitGridShouldLetEachAxisWinOverTheAlignmentShorthand()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Center);
            parameters.Add(p => p.HorizontalAlign, BitAlignment.End);
            parameters.Add(p => p.VerticalAlign, BitAlignment.Start);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("justify-content:flex-end", style!);
        Assert.Contains("align-items:flex-start", style);
    }

    [TestMethod]
    public void BitGridShouldOnlyReachTheAxisAnAlignmentShorthandMeansSomethingOn()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.SpaceBetween);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("justify-content:space-between", style!);
        Assert.DoesNotContain("align-items", style!);
    }

    [TestMethod]
    [DataRow(BitAlignment.Baseline, "baseline")]
    [DataRow(BitAlignment.Stretch, "stretch")]
    public void BitGridShouldKeepTheAlignmentShorthandOnTheMainAxisWhenTheHorizontalAlignLeftIt(BitAlignment alignment, string expected)
    {
        // A HorizontalAlign of Baseline or Stretch is a cross axis value spelled on the horizontal parameter, so
        // it takes nothing away from the main axis and the shorthand is what is left to distribute the items.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.SpaceBetween);
            parameters.Add(p => p.HorizontalAlign, alignment);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("justify-content:space-between", style!);
        Assert.Contains($"align-items:{expected}", style);
    }

    [TestMethod]
    [DataRow(BitAlignment.SpaceBetween, "space-between")]
    [DataRow(BitAlignment.SpaceAround, "space-around")]
    [DataRow(BitAlignment.SpaceEvenly, "space-evenly")]
    public void BitGridShouldKeepTheAlignmentShorthandOnTheCrossAxisWhenTheVerticalAlignLeftIt(BitAlignment alignment, string expected)
    {
        // A VerticalAlign of one of the three distributions is ignored on this axis, so it takes nothing away
        // from it either and the shorthand is what is left to align the items within their row.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Center);
            parameters.Add(p => p.VerticalAlign, alignment);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("justify-content:center", style!);
        Assert.Contains("align-items:center", style);
        Assert.DoesNotContain($"align-items:{expected}", style!);
    }

    [TestMethod]
    [DataRow(BitAlignment.Start, "flex-start")]
    [DataRow(BitAlignment.End, "flex-end")]
    [DataRow(BitAlignment.Center, "center")]
    [DataRow(BitAlignment.SpaceBetween, "space-between")]
    [DataRow(BitAlignment.SpaceAround, "space-around")]
    [DataRow(BitAlignment.SpaceEvenly, "space-evenly")]
    [DataRow(BitAlignment.Baseline, "baseline")]
    [DataRow(BitAlignment.Stretch, "stretch")]
    public void BitGridShouldSetAlignContentBasedOnTheAlignContent(BitAlignment alignment, string expected)
    {
        // Unlike the two axes, every member means something to the rows of a wrapping grid: three of them park
        // the block of rows and three of them spread the rows apart.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.AlignContent, alignment);
        });

        Assert.Contains($"align-content:{expected}", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldNotRenderAnAlignContentThatWasNotAskedFor()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.VerticalAlign, BitAlignment.Center);
        });

        Assert.DoesNotContain("align-content", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldKeepTheAlignContentOutOfTheAlignmentShorthand()
    {
        // The shorthand stands for the two axes the children are laid out on. The rows of the grid are a third
        // thing, so a grid that centers its children must not silently center its rows as well.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Alignment, BitAlignment.Center);
        });

        Assert.DoesNotContain("align-content", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldRelayoutWhenTheAlignContentChanges()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.AlignContent, BitAlignment.Start);
        });

        Assert.Contains("align-content:flex-start", component.Find(".bit-grd").GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.AlignContent, BitAlignment.SpaceBetween));

        Assert.Contains("align-content:space-between", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    [DataRow("0", "0px")]
    [DataRow("8", "8px")]
    [DataRow("0.5", "0.5px")]
    [DataRow("-2", "0px")]
    [DataRow("+6", "6px")]
    [DataRow("4.", "4px")]
    public void BitGridShouldReadASpacingWithNoUnitAsPixels(string spacing, string expected)
    {
        // The width of every item is worked out from the horizontal gap inside a calc(), which a bare number
        // makes invalid: the whole width is thrown away and the items fall back to the size of their content.
        // The very common Spacing="0" used to take the layout down with it.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, spacing);
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains($"--bit-grd-cgap:{expected}", style!);
        Assert.Contains($"--bit-grd-rgap:{expected}", style);
    }

    [TestMethod]
    [DataRow("0px")]
    [DataRow("1rem")]
    [DataRow("2%")]
    [DataRow("clamp(4px, 1vw, 16px)")]
    [DataRow("calc(1rem + 2px)")]
    [DataRow("var(--app-gap)")]
    public void BitGridShouldLeaveASpacingThatIsAlreadyALengthAlone(string spacing)
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, spacing);
        });

        Assert.Contains($"--bit-grd-cgap:{spacing}", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldReadTheSpacingOfEachAxisWithNoUnitAsPixels()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.HorizontalSpacing, "12");
            parameters.Add(p => p.VerticalSpacing, "24");
        });

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap:12px", style!);
        Assert.Contains("--bit-grd-rgap:24px", style);
    }

    [TestMethod]
    public void BitGridShouldTrimTheSpacingItWasGiven()
    {
        // A padded value is a valid enough length to survive on its own, but not inside the calc() the width of
        // every item is worked out with.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, " 1rem ");
        });

        Assert.Contains("--bit-grd-cgap:1rem", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldRespectNoWrap()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.NoWrap, true);
        });

        Assert.IsTrue(component.Find(".bit-grd").ClassList.Contains("bit-grd-nwr"));
    }

    [TestMethod]
    public void BitGridShouldRespectReversed()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Reversed, true);
        });

        Assert.IsTrue(component.Find(".bit-grd").ClassList.Contains("bit-grd-rev"));
    }

    [TestMethod]
    public void BitGridShouldRespectContainer()
    {
        // The class is what turns the grid into a query container, which is what moves every breakpoint of the
        // grid and of its items off the window and onto the width of the grid itself.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Container, true);
        });

        Assert.IsTrue(component.Find(".bit-grd").ClassList.Contains("bit-grd-cnq"));
    }

    [TestMethod]
    public void BitGridShouldRespectGrow()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Grow, true);
        });

        Assert.IsTrue(component.Find(".bit-grd").ClassList.Contains("bit-grd-grw"));
    }

    [TestMethod]
    public void BitGridShouldRespectMinItemWidth()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.MinItemWidth, "12rem");
        });

        var root = component.Find(".bit-grd");

        Assert.Contains("--bit-grd-mnw:12rem", root.GetAttribute("style")!);
        Assert.IsTrue(root.ClassList.Contains("bit-grd-mnw"));
    }

    [TestMethod]
    [DataRow("240", "240px")]
    [DataRow("0", "0px")]
    [DataRow("-8", "0px")]
    [DataRow("+16", "16px")]
    public void BitGridShouldReadAMinItemWidthWithNoUnitAsPixels(string minItemWidth, string expected)
    {
        // A bare number is not a CSS length, and a flex-basis that is not a length is thrown away, which would
        // leave the items at the size of their content rather than at the width that was asked for.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.MinItemWidth, minItemWidth);
        });

        Assert.Contains($"--bit-grd-mnw:{expected}", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(null)]
    public void BitGridShouldNotClaimTheWidthOfTheItemsOfAGridThatAskedForNoMinimum(string? minItemWidth)
    {
        // The class is what the sizing hangs off, so a grid that was given nothing to read must not carry it and
        // leave its items sized by a custom property that was never declared.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.MinItemWidth, minItemWidth);
        });

        var root = component.Find(".bit-grd");

        Assert.IsFalse(root.ClassList.Contains("bit-grd-mnw"));
        Assert.DoesNotContain("--bit-grd-mnw", root.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldRelayoutWhenTheMinItemWidthChanges()
    {
        var component = RenderComponent<BitGrid>();

        Assert.IsFalse(component.Find(".bit-grd").ClassList.Contains("bit-grd-mnw"));

        component.Render(parameters => parameters.Add(p => p.MinItemWidth, "10rem"));

        var root = component.Find(".bit-grd");

        Assert.IsTrue(root.ClassList.Contains("bit-grd-mnw"));
        Assert.Contains("--bit-grd-mnw:10rem", root.GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.MinItemWidth, "20rem"));

        Assert.Contains("--bit-grd-mnw:20rem", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldNotRenderTheModifierClassesByDefault()
    {
        var component = RenderComponent<BitGrid>();

        var classList = component.Find(".bit-grd").ClassList;

        Assert.IsFalse(classList.Contains("bit-grd-nwr"));
        Assert.IsFalse(classList.Contains("bit-grd-rev"));
        Assert.IsFalse(classList.Contains("bit-grd-cnq"));
        Assert.IsFalse(classList.Contains("bit-grd-grw"));
        Assert.IsFalse(classList.Contains("bit-grd-mnw"));
    }

    [TestMethod]
    public void BitGridShouldRenderEveryWayOfSizingItsItemsTogether()
    {
        // None of the four is exclusive of any other: a fluid grid of cards that fills out its last row inside a
        // panel that measures its own width is all of them at once.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Grow, true);
            parameters.Add(p => p.NoWrap, true);
            parameters.Add(p => p.Reversed, true);
            parameters.Add(p => p.Container, true);
            parameters.Add(p => p.MinItemWidth, "8rem");
        });

        var classList = component.Find(".bit-grd").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-grw"));
        Assert.IsTrue(classList.Contains("bit-grd-nwr"));
        Assert.IsTrue(classList.Contains("bit-grd-rev"));
        Assert.IsTrue(classList.Contains("bit-grd-cnq"));
        Assert.IsTrue(classList.Contains("bit-grd-mnw"));
    }

    [TestMethod]
    public void BitGridShouldRespectDir()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-grd");

        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitGridShouldRespectAriaLabel()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.AriaLabel, "Product list");
        });

        Assert.AreEqual("Product list", component.Find(".bit-grd").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitGridShouldRespectVisibility()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        Assert.Contains("display:none", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldRespectDisabled()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
        });

        Assert.IsTrue(component.Find(".bit-grd").ClassList.Contains("bit-dis"));
    }

    [TestMethod]
    public void BitGridShouldRespectStyleAndClass()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Style, "padding:1rem");
            parameters.Add(p => p.Class, "custom-grid");
        });

        var root = component.Find(".bit-grd");

        Assert.Contains("padding:1rem", root.GetAttribute("style")!);
        Assert.IsTrue(root.ClassList.Contains("custom-grid"));
    }

    [TestMethod]
    public void BitGridShouldRenderTheModifierClassesTogether()
    {
        // Neither of the two touches the other: a strip that must not break can still be painted backwards.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.NoWrap, true);
            parameters.Add(p => p.Reversed, true);
        });

        var classList = component.Find(".bit-grd").ClassList;

        Assert.IsTrue(classList.Contains("bit-grd-nwr"));
        Assert.IsTrue(classList.Contains("bit-grd-rev"));
    }

    [TestMethod]
    [DataRow("ul")]
    [DataRow("ol")]
    [DataRow("menu")]
    [DataRow("UL")]
    public void BitGridRenderedAsAListShouldBeAListOfListItems(string element)
    {
        var component = RenderComponent<BitGridListTest>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        Assert.AreEqual("list", component.Find(".bit-grd").GetAttribute("role"));
        Assert.AreEqual("LI", component.Find(".inferred").TagName);
        Assert.AreEqual("SECTION", component.Find(".own").TagName);
    }

    [TestMethod]
    public void BitGridShouldLetAnExplicitRoleReplaceTheListRole()
    {
        var component = RenderComponent<BitGridListTest>(parameters =>
        {
            parameters.Add(p => p.Element, "ul");
            parameters.Add(p => p.Role, "tablist");
        });

        Assert.AreEqual("tablist", component.Find(".bit-grd").GetAttribute("role"));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("section")]
    public void BitGridNotRenderedAsAListShouldKeepDivItems(string? element)
    {
        var component = RenderComponent<BitGridListTest>(parameters =>
        {
            parameters.Add(p => p.Element, element);
        });

        Assert.IsFalse(component.Find(".bit-grd").HasAttribute("role"));
        Assert.AreEqual("DIV", component.Find(".inferred").TagName);
    }

    [TestMethod]
    public void BitGridShouldKeepAnAriaLabelWrittenInTheMarkup()
    {
        // An unset AriaLabel used to be written as a null attribute, which drops the aria-label that was given
        // through the markup.
        var component = RenderComponent<BitGridAttributesTest>();

        var root = component.Find(".bit-grd");

        Assert.AreEqual("Products", root.GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitGridShouldRenderTheTabIndex()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.TabIndex, "0");
        });

        Assert.AreEqual("0", component.Find(".bit-grd").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitGridShouldKeepEverythingItRendersOnTheElementItWasGiven()
    {
        // The custom element only replaces the tag name: every attribute the component renders has to survive it.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Element, "ul");
            parameters.Add(p => p.Class, "plain-list");
            parameters.Add(p => p.Style, "padding:0");
            parameters.Add(p => p.Columns, 4);
            parameters.Add(p => p.AriaLabel, "Fruits");
            parameters.Add(p => p.Dir, BitDir.Rtl);
        });

        var root = component.Find(".bit-grd");

        Assert.AreEqual("UL", root.TagName);
        Assert.AreEqual("Fruits", root.GetAttribute("aria-label"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("plain-list"));
        Assert.Contains("padding:0", root.GetAttribute("style")!);
        Assert.Contains("--bit-grd-cols:4", root.GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldRespectVisibilityHidden()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Visibility, BitVisibility.Hidden);
        });

        Assert.Contains("visibility:hidden", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldRespectHtmlAttributes()
    {
        var component = RenderComponent<BitGridHtmlAttributesTest>();

        Assert.AreEqual("bit", component.Find(".bit-grd").GetAttribute("data-val-test"));
    }

    [TestMethod]
    public void BitGridShouldRelayoutWhenTheColumnCountChanges()
    {
        // The column count is a custom property of the container and used to be one of every item, so a
        // grid that changed it at runtime had no way of telling its items to resize themselves.
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Columns, 4);
        });

        Assert.Contains("--bit-grd-cols:4", component.Find(".bit-grd").GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.Columns, 6));

        Assert.Contains("--bit-grd-cols:6", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldRelayoutWhenAResponsiveColumnCountChanges()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.ColumnsMd, 4);
        });

        Assert.Contains("--bit-grd-cols-md:4", component.Find(".bit-grd").GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.ColumnsMd, 3));

        Assert.Contains("--bit-grd-cols-md:3", component.Find(".bit-grd").GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridShouldRelayoutWhenTheSpacingChanges()
    {
        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.Add(p => p.Spacing, "4px");
        });

        Assert.Contains("--bit-grd-cgap:4px", component.Find(".bit-grd").GetAttribute("style")!);

        component.Render(parameters => parameters.Add(p => p.Spacing, "2rem"));

        var style = component.Find(".bit-grd").GetAttribute("style");

        Assert.Contains("--bit-grd-cgap:2rem", style!);
        Assert.Contains("--bit-grd-rgap:2rem", style);
    }

    [TestMethod]
    [DataRow(typeof(BitGrid), typeof(BitGridParams))]
    [DataRow(typeof(BitGridItem), typeof(BitGridItemParams))]
    public void BitGridParametersShouldAllBeReachableThroughTheCascadingParameters(Type component, Type parameters)
    {
        // The cascading parameters are the shared configuration of every grid of an application, so a parameter
        // that is added to the component and forgotten here is one that cannot be set once for the whole of it.
        var missing = component.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Where(p => p.DeclaringType == component)
                               .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                               .Where(p => p.PropertyType != typeof(RenderFragment))
                               .Where(p => parameters.GetProperty(p.Name) is null)
                               .Select(p => p.Name)
                               .ToArray();

        Assert.IsEmpty(missing, $"{parameters.Name} is missing: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void BitGridShouldReadEveryOneOfItsCascadingParameters()
    {
        // Declaring the parameter is only half of it: the one that is never copied across in UpdateParameters is
        // a parameter an application can set and watch do nothing at all.
        var cascaded = new BitGridParams();

        var expected = Fill(cascaded);

        var component = RenderComponent<BitGrid>(parameters =>
        {
            parameters.AddCascadingValue(BitGridParams.ParamName, cascaded);
        });

        AssertWasRead(component.Instance, expected);
    }

    [TestMethod]
    public void BitGridItemShouldReadEveryOneOfItsCascadingParameters()
    {
        var cascaded = new BitGridItemParams();

        var expected = Fill(cascaded);

        var component = RenderComponent<BitGridItem>(parameters =>
        {
            parameters.AddCascadingValue(BitGridItemParams.ParamName, cascaded);
        });

        AssertWasRead(component.Instance, expected);
    }

    // Gives every parameter of the cascading object a value of its own type, and hands back what each of them
    // was given so that the component can be asked for it afterwards.
    private static Dictionary<string, object> Fill(object cascaded)
    {
        var expected = new Dictionary<string, object>();

        foreach (var property in cascaded.GetType()
                                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.DeclaringType == cascaded.GetType() && p.CanWrite))
        {
            object value = property.PropertyType == typeof(bool?) ? true
                         : property.PropertyType == typeof(int?) ? 3
                         : property.PropertyType == typeof(BitAlignment?) ? BitAlignment.Center
                         : property.Name == "Element" ? "section"
                         : "2rem";

            property.SetValue(cascaded, value);

            expected[property.Name] = value;
        }

        // Auto and Grow are two ways of saying the same thing and Auto wins over Grow in the layout, but each of
        // them is copied across on its own and read back from its own property, so both are asserted here: what
        // is being proved is that neither was forgotten in UpdateParameters, not what they lay out together.
        return expected;
    }

    private static void AssertWasRead(object component, Dictionary<string, object> expected)
    {
        var type = component.GetType();

        // Guards the guard: a reflection walk that found nothing would let every parameter through in silence.
        Assert.IsGreaterThan(10, expected.Count, $"Only {expected.Count} cascading parameters were found on {type.Name}.");

        foreach (var (name, value) in expected)
        {
            Assert.AreEqual(value, type.GetProperty(name)!.GetValue(component), $"{name} was not read from the cascading parameters.");
        }
    }

    [TestMethod]
    public void BitGridShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitGridCascadingParamsTest>();

        var grids = component.FindAll(".bit-grd");

        // The first grid takes everything from the cascading parameters.
        var cascaded = grids[0].GetAttribute("style");
        Assert.Contains("--bit-grd-cols:6", cascaded!);
        Assert.Contains("--bit-grd-cols-xs:2", cascaded);
        Assert.Contains("--bit-grd-cols-sm:3", cascaded);
        Assert.Contains("--bit-grd-cols-md:4", cascaded);
        Assert.Contains("--bit-grd-cols-lg:5", cascaded);
        Assert.Contains("--bit-grd-cols-xl:8", cascaded);
        Assert.Contains("--bit-grd-cols-xxl:10", cascaded);
        Assert.Contains("--bit-grd-span:2", cascaded);
        Assert.Contains("--bit-grd-cgap:1rem", cascaded);
        Assert.Contains("--bit-grd-rgap:2rem", cascaded);
        Assert.Contains("--bit-grd-cgap-lg:3rem", cascaded);
        Assert.Contains("--bit-grd-rgap-lg:3rem", cascaded);
        Assert.Contains("--bit-grd-cgap-xl:4rem", cascaded);
        Assert.Contains("--bit-grd-rgap-xxl:5rem", cascaded);
        Assert.Contains("justify-content:center", cascaded);
        Assert.Contains("align-items:stretch", cascaded);
        Assert.Contains("align-content:space-between", cascaded);
        Assert.Contains("--bit-grd-mnw:12rem", cascaded);
        Assert.AreEqual("SECTION", grids[0].TagName);
        Assert.IsTrue(grids[0].ClassList.Contains("bit-grd-rev"));
        Assert.IsTrue(grids[0].ClassList.Contains("bit-grd-nwr"));
        Assert.IsTrue(grids[0].ClassList.Contains("bit-grd-cnq"));
        Assert.IsTrue(grids[0].ClassList.Contains("bit-grd-grw"));
        Assert.IsTrue(grids[0].ClassList.Contains("bit-grd-mnw"));

        // The second one sets its own column count, element and wrapping, none of which the cascading
        // parameters may overwrite, while everything it left alone still comes from them.
        var own = grids[1].GetAttribute("style");
        Assert.Contains("--bit-grd-cols:3", own!);
        Assert.Contains("--bit-grd-cgap:1rem", own);
        Assert.Contains("--bit-grd-cgap-lg:9rem", own);
        Assert.Contains("--bit-grd-rgap-lg:9rem", own);
        Assert.Contains("--bit-grd-cgap-xl:4rem", own);
        Assert.AreEqual("DIV", grids[1].TagName);
        Assert.IsFalse(grids[1].ClassList.Contains("bit-grd-nwr"));
    }

    [TestMethod]
    public void BitGridItemShouldRespectCascadingParams()
    {
        var component = RenderComponent<BitGridCascadingParamsTest>();

        var items = component.FindAll(".bit-grd-itm");

        // The first item takes everything from the cascading parameters.
        var cascaded = items[0].GetAttribute("style");
        Assert.Contains("--bit-grd-span:2", cascaded!);
        Assert.Contains("--bit-grd-xs:12", cascaded);
        Assert.Contains("--bit-grd-sm:6", cascaded);
        Assert.Contains("--bit-grd-md:4", cascaded);
        Assert.Contains("--bit-grd-lg:3", cascaded);
        Assert.Contains("--bit-grd-xl:2", cascaded);
        Assert.Contains("--bit-grd-xxl:1", cascaded);
        Assert.Contains("--bit-grd-off:1", cascaded);
        Assert.Contains("--bit-grd-off-md:2", cascaded);
        Assert.Contains("--bit-grd-ord:3", cascaded);
        Assert.Contains("--bit-grd-ord-md:4", cascaded);
        Assert.Contains("align-self:center", cascaded);
        Assert.AreEqual("ARTICLE", items[0].TagName);
        Assert.IsTrue(items[0].ClassList.Contains("bit-grd-itm-off"));
        Assert.IsTrue(items[0].ClassList.Contains("bit-grd-itm-ord"));

        // The second one sets its own span, which the cascading parameters must not overwrite.
        Assert.Contains("--bit-grd-span:5", items[1].GetAttribute("style")!);
    }

    [TestMethod]
    public void BitGridItemShouldRespectTheCascadedAutoAndAutoOffset()
    {
        // Both of these claim a slot another parameter of the item already fills, so a cascaded one has to be
        // read the same way an own one is: Auto beats Grow, and the auto offset beats a counted one.
        var component = RenderComponent<BitGridCascadingParamsTest>();

        var items = component.FindAll(".bit-grd-itm");

        // Auto beats Grow below the medium breakpoint, the cascaded AutoMd of false hands the columns back
        // there, and the cascaded GrowLg takes over from the large one.
        Assert.IsTrue(items[2].ClassList.Contains("bit-grd-itm-atc-xs"));
        Assert.IsTrue(items[2].ClassList.Contains("bit-grd-itm-spn-md"));
        Assert.IsTrue(items[2].ClassList.Contains("bit-grd-itm-grw-lg"));
        Assert.IsTrue(items[2].ClassList.Contains("bit-grd-itm-aof"));

        // The last item turned the auto sizing and the auto offset off itself, so it keeps the growth and the
        // counted offset the cascading parameters could not take away from it, and its own AutoMd stands.
        Assert.IsTrue(items[3].ClassList.Contains("bit-grd-itm-grw-xs"));
        Assert.IsTrue(items[3].ClassList.Contains("bit-grd-itm-atc-md"));
        Assert.IsTrue(items[3].ClassList.Contains("bit-grd-itm-off"));
        Assert.IsFalse(items[3].ClassList.Contains("bit-grd-itm-aof"));
    }
}
