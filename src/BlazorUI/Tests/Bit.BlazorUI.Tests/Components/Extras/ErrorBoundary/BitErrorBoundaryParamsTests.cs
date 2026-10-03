using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ErrorBoundary;

public partial class BitErrorBoundaryTests
{
    [TestMethod]
    public void BitErrorBoundaryParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitErrorBoundary)}", BitErrorBoundaryParams.ParamName);
    }

    [TestMethod]
    public void BitErrorBoundaryParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitErrorBoundaryParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitErrorBoundaryParams.ParamName, @params.Name);
    }

    // Every settable property of the params object is one the boundary can take from it, so a property added to one
    // and not wired to the other fails here rather than going silently unapplied.
    [TestMethod]
    public void BitErrorBoundaryParamsShouldSupplyEveryOneOfItsProperties()
    {
        var properties = typeof(BitErrorBoundaryParams).GetProperties()
                                                       .Where(p => p.CanWrite)
                                                       .Select(p => p.Name)
                                                       .Order()
                                                       .ToArray();

        // The table is internal to the library, so it is read the way a test outside it can.
        var table = (IEnumerable<object>)typeof(BitErrorBoundaryParams).GetField("Parameters", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        var supplied = table.Select(p => (string)p.GetType().GetProperty("Name")!.GetValue(p)!).Order().ToArray();

        CollectionAssert.AreEqual(properties, supplied);

        foreach (var name in supplied)
        {
            var parameter = typeof(BitErrorBoundary).GetProperty(name);

            Assert.IsNotNull(parameter, $"{name} is not a property of BitErrorBoundary.");
            Assert.IsTrue(parameter.IsDefined(typeof(ParameterAttribute), true), $"{name} is not a parameter of BitErrorBoundary.");
        }
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyCascadingParametersFromBitParams()
    {
        var component = RenderInBitParams(new BitErrorBoundaryParams
        {
            Title = "Cascaded title",
            Message = "Cascaded message",
            HideIcon = true,
            HideHomeButton = true,
            ShowException = true,
            HeadingLevel = 2,
            Dir = BitDir.Rtl,
            Classes = new() { Root = "cascaded-root" },
            Styles = new() { Title = "color: red;" },
        });

        var errorRoot = component.Find(".bit-erb");
        var title = component.Find(".bit-erb-ttl");

        Assert.AreEqual("Cascaded title", title.TextContent.Trim());
        Assert.AreEqual("H2", title.TagName);
        StringAssert.Contains(title.GetAttribute("style"), "color: red;");
        Assert.AreEqual("Cascaded message", component.Find(".bit-erb-msg").TextContent.Trim());
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-svg"));
        Assert.AreEqual(2, component.FindAll(".bit-erb-ftr .bit-btn").Count);
        component.Find(".bit-erb-exp");
        Assert.AreEqual("rtl", errorRoot.GetAttribute("dir"));
        Assert.IsTrue(errorRoot.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(errorRoot.ClassList.Contains("bit-rtl"));
    }

    [TestMethod]
    public void BitErrorBoundaryDirectParametersShouldOverrideCascadingParameters()
    {
        var component = RenderInBitParams(new BitErrorBoundaryParams
        {
            Title = "Cascaded title",
            Message = "Cascaded message",
        },
        (nameof(BitErrorBoundary.Title), "Own title"));

        Assert.AreEqual("Own title", component.Find(".bit-erb-ttl").TextContent.Trim());
        Assert.AreEqual("Cascaded message", component.Find(".bit-erb-msg").TextContent.Trim());
    }

    [TestMethod]
    public void BitErrorBoundaryShouldApplyACascadedErrorTemplate()
    {
        var component = RenderInBitParams(new BitErrorBoundaryParams
        {
            ErrorTemplate = context => b => b.AddMarkupContent(0, $"<div class=\"cascaded-template\">{context.Exception.Message}</div>"),
        });

        Assert.AreEqual("err", component.Find(".cascaded-template").TextContent);
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldPutBackWhatTheParamsObjectNoLongerSupplies()
    {
        var component = RenderInBitParams(new BitErrorBoundaryParams
        {
            Title = "Cascaded title",
            HideRefreshButton = true,
        });

        Assert.AreEqual("Cascaded title", component.Find(".bit-erb-ttl").TextContent.Trim());
        Assert.AreEqual(2, component.FindAll(".bit-erb-ftr .bit-btn").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitErrorBoundaryParams { HideRefreshButton = true } });
        });

        StringAssert.Contains(component.Find(".bit-erb-ttl").TextContent, "Oops, Something went wrong");
        Assert.AreEqual(2, component.FindAll(".bit-erb-ftr .bit-btn").Count);

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>());
        });

        Assert.AreEqual(3, component.FindAll(".bit-erb-ftr .bit-btn").Count);
    }

    [TestMethod]
    public void BitErrorBoundaryShouldMergeNestedBitParams()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitErrorBoundaryParams { Title = "Outer title", HideIcon = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitParams>(0);
                builder.AddAttribute(1, nameof(BitParams.Parameters), new List<IBitComponentParams> { new BitErrorBoundaryParams { Message = "Inner message" } });
                builder.AddAttribute(2, nameof(BitParams.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<BitErrorBoundary>(0);
                    b.AddAttribute(1, nameof(BitErrorBoundary.ChildContent), ThrowingContent("err"));
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        Assert.AreEqual("Outer title", component.Find(".bit-erb-ttl").TextContent.Trim());
        Assert.AreEqual("Inner message", component.Find(".bit-erb-msg").TextContent.Trim());
        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-svg"));
    }

    [TestMethod]
    public void BitErrorBoundaryShouldRecoverOnNavigationFromACascadedParameter()
    {
        ThrowSwitchComponent.Reset();

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitErrorBoundaryParams { RecoverOnNavigation = true } });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitErrorBoundary>(0);
                builder.AddAttribute(1, nameof(BitErrorBoundary.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<ThrowSwitchComponent>(0);
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        component.Find(".bit-erb");

        ThrowSwitchComponent.ShouldThrow = false;

        Services.GetRequiredService<BunitNavigationManager>().NavigateTo("/another-page");

        component.WaitForAssertion(() => component.Find(".throw-switch-safe"));
    }



    private IRenderedComponent<BitParams> RenderInBitParams(BitErrorBoundaryParams @params, params (string Name, object? Value)[] ownParameters)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { @params });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitErrorBoundary>(0);

                var sequence = 1;

                foreach (var (name, value) in ownParameters)
                {
                    builder.AddAttribute(sequence++, name, value);
                }

                builder.AddAttribute(sequence, nameof(BitErrorBoundary.ChildContent), ThrowingContent("err"));
                builder.CloseComponent();
            });
        });
    }
}
