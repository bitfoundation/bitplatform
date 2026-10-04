using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
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

    // Every settable property of the params object is one the boundary takes from it, so a property added to one and
    // not wired to the other - or wired to the wrong parameter - fails here rather than going silently unapplied.
    [TestMethod]
    public void BitErrorBoundaryParamsShouldSupplyEveryOneOfItsProperties()
    {
        var @params = new BitErrorBoundaryParams();
        var supplied = new Dictionary<string, object>();

        foreach (var property in typeof(BitErrorBoundaryParams).GetProperties().Where(p => p.CanWrite))
        {
            var value = SampleValue(property.PropertyType);

            property.SetValue(@params, value);
            supplied.Add(property.Name, value);
        }

        var boundary = new BitErrorBoundary();

        @params.UpdateParameters(boundary);

        foreach (var (name, value) in supplied)
        {
            var parameter = typeof(BitErrorBoundary).GetProperty(name);

            Assert.IsNotNull(parameter, $"{name} is not a property of BitErrorBoundary.");
            Assert.IsTrue(parameter.IsDefined(typeof(ParameterAttribute), true), $"{name} is not a parameter of BitErrorBoundary.");
            Assert.AreEqual(value, parameter.GetValue(boundary), $"{name} is not applied by UpdateParameters.");
        }
    }

    // An empty text is a value, not an absence: an empty Title is how the heading is dropped, so a params object that
    // supplies one drops it on every boundary under it.
    [TestMethod]
    public void BitErrorBoundaryShouldTakeAnEmptyTitleFromBitParams()
    {
        var component = RenderInBitParams(new BitErrorBoundaryParams { Title = string.Empty });

        Assert.Throws<ElementNotFoundException>(() => component.Find(".bit-erb-ttl"));
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

    // The markup taking a parameter over and letting it go again leaves the params object supplying it once more,
    // rather than leaving behind the value the markup last gave it.
    [TestMethod]
    public void BitErrorBoundaryShouldGoBackToTheCascadedValueOnceTheMarkupLetsGo()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitErrorBoundaryParams { Title = "Cascaded title" } });
            parameters.AddChildContent(builder => RenderBoundary(builder, withOwnTitle: true));
        });

        Assert.AreEqual("Own title", component.Find(".bit-erb-ttl").TextContent.Trim());

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitErrorBoundaryParams { Title = "Cascaded title" } });
            parameters.AddChildContent(builder => RenderBoundary(builder, withOwnTitle: false));
        });

        Assert.AreEqual("Cascaded title", component.Find(".bit-erb-ttl").TextContent.Trim());

        static void RenderBoundary(RenderTreeBuilder builder, bool withOwnTitle)
        {
            builder.OpenComponent<BitErrorBoundary>(0);

            if (withOwnTitle)
            {
                builder.AddAttribute(1, nameof(BitErrorBoundary.Title), "Own title");
            }

            builder.AddAttribute(2, nameof(BitErrorBoundary.ChildContent), ThrowingContent("err"));
            builder.CloseComponent();
        }
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



    private static object SampleValue(Type type)
    {
        var valueType = Nullable.GetUnderlyingType(type) ?? type;

        // Each value differs from the boundary's default, so a parameter left unapplied cannot pass for an applied one.
        if (valueType == typeof(bool)) return true;
        if (valueType == typeof(int)) return 5;
        if (valueType == typeof(string)) return "sample";
        if (valueType == typeof(BitDir)) return BitDir.Rtl;
        if (valueType == typeof(BitIconInfo)) return BitIconInfo.Css("sample-icon");
        if (valueType == typeof(RenderFragment)) return (RenderFragment)(_ => { });
        if (valueType == typeof(RenderFragment<BitErrorBoundaryContext>)) return (RenderFragment<BitErrorBoundaryContext>)(_ => _ => { });

        return Activator.CreateInstance(valueType)!;
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
