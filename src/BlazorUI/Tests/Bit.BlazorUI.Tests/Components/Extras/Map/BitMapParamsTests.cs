using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Map;

/// <summary>
/// Covers the BitParams cascade of the map: what a BitMapParams carries (the behaviour, the look and the texts of
/// every map, never the data or the state of one), that every property it carries reaches a map that did not set it
/// whatever its provider type, that the map's own markup wins, and that the defaults come back once the params object
/// is withdrawn.
/// </summary>
[TestClass]
public class BitMapParamsTests : BunitTestContext
{
    // The provider, the camera, the markers, the clustering, the content templates of the markers and the callbacks
    // describe one map rather than a default for many, so they have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitMap<BitLeafletMapProvider>.CascadingParameters),
        nameof(BitMap<BitLeafletMapProvider>.Center),
        nameof(BitMap<BitLeafletMapProvider>.ChildContent),
        nameof(BitMap<BitLeafletMapProvider>.Clustering),
        nameof(BitMap<BitLeafletMapProvider>.MarkerListTemplate),
        nameof(BitMap<BitLeafletMapProvider>.MarkerPopupTemplate),
        nameof(BitMap<BitLeafletMapProvider>.Markers),
        nameof(BitMap<BitLeafletMapProvider>.Provider),
        nameof(BitMap<BitLeafletMapProvider>.Zoom),
    ];

    private static bool IsEventCallback(Type type)
        => type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));

    [TestMethod]
    public void BitMapParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitMap<BitLeafletMapProvider>)}", BitMapParams.ParamName);
        // One name for every provider type, since every map reads the same object.
        Assert.AreEqual("BitParams.BitMap", BitMapParams.ParamName);
    }

    [TestMethod]
    public void BitMapParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitMapParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitMapParams.ParamName, @params.Name);
        Assert.IsFalse(typeof(BitMapParams).IsGenericType, "one params type serves every provider type");
    }

    [TestMethod]
    public void BitMapParamsShouldCarryEveryParameterThatIsNotPerMap()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitMap<BitLeafletMapProvider>).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                                .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                                .Where(p => IsEventCallback(p.PropertyType) is false)
                                                                .Select(p => p.Name)
                                                                .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                                .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitMapParams).GetProperty(name), $"BitMapParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitMapParamsShouldCarryNothingTheMapDoesNotHave()
    {
        var own = typeof(BitMapParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                      .Where(p => p.Name != nameof(IBitComponentParams.Name));

        foreach (var property in own)
        {
            var parameter = typeof(BitMap<BitLeafletMapProvider>).GetProperty(property.Name);

            Assert.IsNotNull(parameter, $"BitMap has no {property.Name}.");
            Assert.IsNotNull(parameter.GetCustomAttribute<ParameterAttribute>(), $"{property.Name} is not a parameter of BitMap.");
            Assert.IsFalse(_notCascaded.Contains(property.Name), $"{property.Name} is per map and must not be cascaded.");
        }
    }

    [TestMethod]
    public void BitMapShouldTakeEveryPropertyOfTheParamsObject()
    {
        var cascaded = new BitMapParams
        {
            AnnounceViewChanges = true,
            AutoResize = false,
            Classes = new() { Root = "cascaded-root" },
            CooperativeGestures = true,
            CooperativeGesturesTouchHint = "Two fingers",
            CooperativeGesturesWheelHint = "Ctrl + wheel",
            ErrorLabel = "Broken",
            ErrorTemplate = _ => b => b.AddContent(0, "Broken"),
            EscapeToExit = false,
            KeyboardInstructions = "Cascaded keys",
            LazyLoad = true,
            LazyLoadRootMargin = "50px",
            LoadingLabel = "Wait",
            LoadingTemplate = b => b.AddContent(0, "Wait"),
            MarkerListActionHeader = "Go",
            MarkerListCaption = "Pins",
            MarkerListLatitudeHeader = "Lat",
            MarkerListLongitudeHeader = "Lng",
            MarkerListMode = BitMapMarkerListMode.Visible,
            MarkerListNameHeader = "Pin",
            MarkerListZoom = 7,
            PopupAutoPan = false,
            PopupCloseLabel = "Dismiss",
            PopupLabel = "Pin details",
            ReplayStateOnProviderSwap = true,
            RespectReducedMotion = false,
            RoleDescription = "cascaded map",
            ShowLoading = false,
            Styles = new() { Root = "color: red" },
            UnsupportedLabel = "No WebGL",
            UnsupportedTemplate = b => b.AddContent(0, "No WebGL"),
            ViewAnnouncementFormatter = v => $"zoom {v.Zoom}",
            ViewAnnouncementThrottle = TimeSpan.FromSeconds(5),
        };

        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { cascaded });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitMap<BitLeafletMapProvider>>(0);
                builder.CloseComponent();
            });
        });
        var map = component.FindComponent<BitMap<BitLeafletMapProvider>>().Instance;

        // Read off the params object itself, so a property added to it later is checked here without being named.
        var properties = typeof(BitMapParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                             .Where(p => p.Name != nameof(IBitComponentParams.Name))
                                             .ToArray();

        Assert.IsTrue(properties.Length > 0);

        foreach (var property in properties)
        {
            var supplied = property.GetValue(cascaded);
            Assert.IsNotNull(supplied, $"The test leaves {property.Name} unset, so it cannot tell whether the map takes it.");

            var taken = typeof(BitMap<BitLeafletMapProvider>).GetProperty(property.Name)!.GetValue(map);

            if (property.PropertyType.IsValueType || supplied is string)
            {
                Assert.AreEqual(supplied, taken, $"{property.Name} was not taken from the cascade.");
            }
            else
            {
                Assert.AreSame(supplied, taken, $"{property.Name} was not taken from the cascade as the very object supplied.");
            }
        }
    }

    [TestMethod]
    public void BitMapShouldApplyCascadedBaseParameters()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitMapParams { AriaLabel = "Office", Class = "cascaded", Dir = BitDir.Rtl, Disabled = true, Id = "cascaded-id", TabIndex = "-1" }
            });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitMap<BitLeafletMapProvider>>(0);
                builder.CloseComponent();
            });
        });

        var root = component.Find(".bit-map");
        var canvas = component.Find(".bit-map-canvas");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        // The name and the tab stop go on the canvas, which is the map's one landmark; a disabled map is inert.
        Assert.AreEqual("Office", canvas.GetAttribute("aria-label"));
        Assert.AreEqual("-1", canvas.GetAttribute("tabindex"));
        Assert.IsTrue(canvas.HasAttribute("inert"));
    }

    [TestMethod]
    public void BitMapShouldTakeTheCascadedParamsItDoesNotSetItself()
    {
        var component = RenderComponent<BitMapCascadingParamsTest>();

        var maps = component.FindAll(".bit-map");
        Assert.AreEqual(3, maps.Count);

        // The first map takes everything from the cascade.
        var first = maps[0];
        Assert.IsTrue(first.ClassList.Contains("cascaded"));
        Assert.IsTrue(first.ClassList.Contains("cascaded-root"));
        StringAssert.Contains(first.GetAttribute("style"), "--bit-Map-radius: 1rem");
        Assert.IsNotNull(first.QuerySelector(".bit-map-gesture-hint"), "CooperativeGestures is cascaded");
        Assert.AreEqual("Cascaded keys", first.QuerySelector(".bit-map-help")!.TextContent.Trim());

        var canvas = first.QuerySelector(".bit-map-canvas")!;
        Assert.IsTrue(canvas.ClassList.Contains("cascaded-canvas"));
        Assert.AreEqual("cascaded map", canvas.GetAttribute("aria-roledescription"));
        StringAssert.Contains(canvas.GetAttribute("style"), "--cascaded-canvas: 1");

        // The marker list is a sibling of the map, so it is found by its own caption.
        var captions = component.FindAll(".bit-map-marker-table caption");
        Assert.AreEqual(3, captions.Count, "MarkerListMode is cascaded to every map");
        Assert.AreEqual("Cascaded caption", captions[0].TextContent.Trim());
    }

    [TestMethod]
    public void BitMapShouldKeepWhatItsOwnMarkupSets()
    {
        var component = RenderComponent<BitMapCascadingParamsTest>();

        var second = component.FindAll(".bit-map")[1];

        Assert.IsTrue(second.ClassList.Contains("second"));
        Assert.IsTrue(second.ClassList.Contains("own-root"));
        Assert.IsFalse(second.ClassList.Contains("cascaded"));
        Assert.IsFalse(second.ClassList.Contains("cascaded-root"));
        Assert.IsNull(second.QuerySelector(".bit-map-gesture-hint"), "its own CooperativeGestures=false wins");
        Assert.AreEqual("Own keys", second.QuerySelector(".bit-map-help")!.TextContent.Trim());

        // A parameter the markup leaves unset is still taken from the cascade.
        StringAssert.Contains(second.GetAttribute("style"), "--bit-Map-radius: 1rem");
    }

    [TestMethod]
    public void BitMapShouldReadTheSameParamsWhateverItsProvider()
    {
        var component = RenderComponent<BitMapCascadingParamsTest>();

        var third = component.FindAll(".bit-map")[2];

        Assert.IsTrue(third.ClassList.Contains("cascaded"));
        Assert.IsNotNull(third.QuerySelector(".bit-map-gesture-hint"));
    }

    [TestMethod]
    public void BitMapShouldGoBackToItsOwnDefaultsWhenTheParamsAreWithdrawn()
    {
        var component = RenderComponent<BitMapCascadingParamsTest>();

        component.Render(parameters => parameters.Add(p => p.WithParams, false));

        var first = component.FindAll(".bit-map")[0];

        Assert.IsFalse(first.ClassList.Contains("cascaded"));
        Assert.IsFalse(first.ClassList.Contains("cascaded-root"));
        Assert.IsNull(first.QuerySelector(".bit-map-gesture-hint"));
        Assert.AreEqual("Use the arrow keys to pan the map, plus and minus to zoom, and Escape to leave the map.",
                        first.QuerySelector(".bit-map-help")!.TextContent.Trim());
        Assert.AreEqual(0, component.FindAll(".bit-map-marker-table").Count);
    }

    [TestMethod]
    public void BitMapParamsShouldNotOverwriteAParameterTheMapSet()
    {
        var component = RenderComponent<BitMap<BitLeafletMapProvider>>(parameters =>
        {
            parameters.Add(p => p.PopupCloseLabel, "Own close");
        });

        new BitMapParams { PopupCloseLabel = "Cascaded close", PopupLabel = "Cascaded popup" }.UpdateParameters(component.Instance);

        Assert.AreEqual("Own close", component.Instance.PopupCloseLabel);
        Assert.AreEqual("Cascaded popup", component.Instance.PopupLabel);
    }
}
