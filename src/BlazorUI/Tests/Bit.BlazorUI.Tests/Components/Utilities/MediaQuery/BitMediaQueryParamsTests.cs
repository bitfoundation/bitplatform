using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Utilities.MediaQuery;

/// <summary>
/// Covers the BitParams cascade of the MediaQuery: what a BitMediaQueryParams fills in, what it leaves alone because
/// the media query wrote it for itself - including the query, which is one decision with the screen query - and that
/// a value handed down is taken back when the cascade stops carrying it.
/// </summary>
[TestClass]
public class BitMediaQueryParamsTests : BunitTestContext
{
    private const int QueryArg = 2;
    private const int ScreenQueryArg = 3;

    private IRenderedComponent<BitParams> RenderWithParams(BitMediaQueryParams mediaQueryParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { mediaQueryParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitMediaQuery>(0);
                extraAttributes?.Invoke(builder);
                builder.AddAttribute(100, nameof(BitMediaQuery.Matched), (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"matched\">Matched</div>")));
                builder.AddAttribute(101, nameof(BitMediaQuery.NotMatched), (RenderFragment)(b => b.AddMarkupContent(0, "<div class=\"notmatched\">NotMatched</div>")));
                builder.CloseComponent();
            });
        });
    }

    private JSRuntimeInvocation LastSetup()
        => Context.JSInterop.Invocations.Last(i => i.Identifier == "BitBlazorUI.MediaQuery.setup");

    [TestMethod]
    public void BitMediaQueryParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitMediaQuery)}", BitMediaQueryParams.ParamName);
        Assert.AreEqual("BitParams.BitMediaQuery", BitMediaQueryParams.ParamName);
    }

    [TestMethod]
    public void BitMediaQueryParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitMediaQueryParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitMediaQueryParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitMediaQueryShouldTakeACascadedScreenQuery()
    {
        var component = RenderWithParams(new BitMediaQueryParams { ScreenQuery = BitScreenQuery.LtMd });

        var setup = LastSetup();

        Assert.IsNull(setup.Arguments[QueryArg]);
        Assert.AreEqual("LtMd", setup.Arguments[ScreenQueryArg]);
        Assert.AreEqual(BitScreenQuery.LtMd, component.FindComponent<BitMediaQuery>().Instance.ScreenQuery);
    }

    [TestMethod]
    public void BitMediaQueryShouldTakeACascadedQuery()
    {
        RenderWithParams(new BitMediaQueryParams { Query = "(orientation: portrait)" });

        Assert.AreEqual("(orientation: portrait)", LastSetup().Arguments[QueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotLetACascadedQueryOverrideItsOwnScreenQuery()
    {
        // A cascaded Query would win over the component's own ScreenQuery if the two were cascaded one by one.
        var component = RenderWithParams(new BitMediaQueryParams { Query = "(orientation: portrait)" }, builder =>
        {
            builder.AddAttribute(1, nameof(BitMediaQuery.ScreenQuery), (BitScreenQuery?)BitScreenQuery.Md);
        });

        var setup = LastSetup();

        Assert.IsNull(setup.Arguments[QueryArg]);
        Assert.AreEqual("Md", setup.Arguments[ScreenQueryArg]);
        Assert.IsNull(component.FindComponent<BitMediaQuery>().Instance.Query);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotTakeACascadedScreenQueryBesideItsOwnQuery()
    {
        var component = RenderWithParams(new BitMediaQueryParams { ScreenQuery = BitScreenQuery.Md }, builder =>
        {
            builder.AddAttribute(1, nameof(BitMediaQuery.Query), "(pointer: coarse)");
        });

        Assert.AreEqual("(pointer: coarse)", LastSetup().Arguments[QueryArg]);
        Assert.IsNull(component.FindComponent<BitMediaQuery>().Instance.ScreenQuery);
    }

    [TestMethod]
    public void BitMediaQueryShouldTakeACascadedDefaultMatchedBeforeItsFirstRender()
    {
        var component = RenderWithParams(new BitMediaQueryParams { ScreenQuery = BitScreenQuery.Md, DefaultMatched = true });

        Assert.AreEqual(1, component.FindAll(".matched").Count);
        Assert.AreEqual(0, component.FindAll(".notmatched").Count);
        Assert.IsTrue(component.FindComponent<BitMediaQuery>().Instance.IsMatched);
    }

    [TestMethod]
    public void BitMediaQueryShouldKeepItsOwnDefaultMatched()
    {
        var component = RenderWithParams(new BitMediaQueryParams { ScreenQuery = BitScreenQuery.Md, DefaultMatched = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitMediaQuery.DefaultMatched), false);
        });

        Assert.AreEqual(0, component.FindAll(".matched").Count);
        Assert.AreEqual(1, component.FindAll(".notmatched").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldTakeACascadedNoWrapper()
    {
        var component = RenderWithParams(new BitMediaQueryParams { ScreenQuery = BitScreenQuery.Md, NoWrapper = true });

        Assert.AreEqual(0, component.FindAll(".bit-mdq").Count);
        Assert.AreEqual(1, component.FindAll(".notmatched").Count);
        Assert.IsNull(LastSetup().Arguments[1]);
    }

    [TestMethod]
    public void BitMediaQueryShouldKeepItsOwnNoWrapper()
    {
        var component = RenderWithParams(new BitMediaQueryParams { ScreenQuery = BitScreenQuery.Md, NoWrapper = true }, builder =>
        {
            builder.AddAttribute(1, nameof(BitMediaQuery.NoWrapper), false);
        });

        Assert.AreEqual(1, component.FindAll(".bit-mdq").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldTakeCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitMediaQueryParams
        {
            ScreenQuery = BitScreenQuery.Md,
            Class = "cascaded-class",
            Style = "color: red;",
            Dir = BitDir.Rtl,
        });

        var root = component.Find(".bit-mdq");

        Assert.IsTrue(root.ClassList.Contains("cascaded-class"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        StringAssert.Contains(root.GetAttribute("style"), "color: red");
    }

    [TestMethod]
    public void BitMediaQueryShouldGiveBackACascadedQueryWhenTheCascadeStopsCarryingIt()
    {
        var component = RenderWithParams(new BitMediaQueryParams { Query = "(orientation: portrait)", NoWrapper = true });

        var instance = component.FindComponent<BitMediaQuery>().Instance;
        Assert.AreEqual("(orientation: portrait)", instance.Query);
        Assert.IsTrue(instance.NoWrapper);

        // The same number of params objects, so BitParams keeps its child instead of building a new one.
        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitMediaQueryParams() }));

        Assert.AreSame(instance, component.FindComponent<BitMediaQuery>().Instance);
        Assert.IsNull(instance.Query);
        Assert.IsNull(instance.ScreenQuery);
        Assert.IsFalse(instance.NoWrapper);
        Assert.AreEqual(1, component.FindAll(".bit-mdq").Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count(i => i.Identifier == "BitBlazorUI.MediaQuery.dispose"));
    }

    [TestMethod]
    public void BitMediaQueryShouldKeepItsOwnQueryWhenTheCascadeStopsCarryingOne()
    {
        var component = RenderWithParams(new BitMediaQueryParams { ScreenQuery = BitScreenQuery.Md }, builder =>
        {
            builder.AddAttribute(1, nameof(BitMediaQuery.Query), "(pointer: coarse)");
        });

        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { new BitMediaQueryParams() }));

        Assert.AreEqual("(pointer: coarse)", component.FindComponent<BitMediaQuery>().Instance.Query);
    }

    [TestMethod]
    public void BitMediaQueryShouldDropACascadedQueryWhenItTakesAScreenQueryOfItsOwn()
    {
        // The cascaded Query would outrank the component's new ScreenQuery if it were left behind.
        BitScreenQuery? ownScreenQuery = null;
        var mediaQueryParams = new BitMediaQueryParams { Query = "(orientation: portrait)" };
        var component = RenderWithParams(mediaQueryParams, builder =>
        {
            if (ownScreenQuery.HasValue)
            {
                builder.AddAttribute(1, nameof(BitMediaQuery.ScreenQuery), ownScreenQuery);
            }
        });

        var instance = component.FindComponent<BitMediaQuery>().Instance;
        Assert.AreEqual("(orientation: portrait)", instance.Query);

        ownScreenQuery = BitScreenQuery.Md;
        component.Render(parameters => parameters.Add(p => p.Parameters, new List<IBitComponentParams> { mediaQueryParams }));

        Assert.AreSame(instance, component.FindComponent<BitMediaQuery>().Instance);
        Assert.IsNull(instance.Query);
        Assert.AreEqual(BitScreenQuery.Md, instance.ScreenQuery);

        var setup = LastSetup();
        Assert.IsNull(setup.Arguments[QueryArg]);
        Assert.AreEqual("Md", setup.Arguments[ScreenQueryArg]);
    }

    [TestMethod]
    public void BitMediaQueryParamsShouldLeaveUnsetValuesAlone()
    {
        var component = RenderWithParams(new BitMediaQueryParams());

        var instance = component.FindComponent<BitMediaQuery>().Instance;

        Assert.IsNull(instance.Query);
        Assert.IsNull(instance.ScreenQuery);
        Assert.IsFalse(instance.DefaultMatched);
        Assert.IsFalse(instance.NoWrapper);
        Assert.AreEqual(1, component.FindAll(".bit-mdq").Count);
    }

    [TestMethod]
    public void BitMediaQueryShouldNotCascadeTheStateOrTheContent()
    {
        // The state is the browser's answer for each query, and the content is what each one renders.
        Assert.IsNull(typeof(BitMediaQueryParams).GetProperty(nameof(BitMediaQuery.IsMatched)));
        Assert.IsNull(typeof(BitMediaQueryParams).GetProperty(nameof(BitMediaQuery.Matched)));
        Assert.IsNull(typeof(BitMediaQueryParams).GetProperty(nameof(BitMediaQuery.NotMatched)));
        Assert.IsNull(typeof(BitMediaQueryParams).GetProperty(nameof(BitMediaQuery.Template)));
        Assert.IsNull(typeof(BitMediaQueryParams).GetProperty(nameof(BitMediaQuery.OnChange)));
    }
}
