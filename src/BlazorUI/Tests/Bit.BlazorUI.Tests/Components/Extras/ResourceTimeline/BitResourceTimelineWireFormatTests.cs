using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline.BitResourceTimelineTestInterop;

namespace Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline;

/// <summary>
/// Locks the JSON shape the engine relies on. The JS interop of Blazor serializes with <see cref="JsonSerializerDefaults.Web"/>
/// (camelCase, case insensitive), so these tests do the same to catch any accidental change to the wire contract: the
/// property names, the numeric representation of the times and durations, what an unset value leaves out, and the shapes
/// the engine (BitResourceTimelineEngine.ts) declares for what it is sent.
/// </summary>
[TestClass]
public class BitResourceTimelineWireFormatTests
{
    private static readonly JsonSerializerOptions Contracts = new(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    private static JsonElement Serialize<T>(T value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, Web));

        return document.RootElement.Clone();
    }

    private static BitResourceTimelineAllocation Bar(long startMs = 0, long endMs = 1_000) => Allocation("a1", "r1", startMs, endMs);



    // ---------------------------------------------------------------- the bars

    [TestMethod]
    public void BitResourceTimelineAllocationShouldSerializeItsTimesAsUnixMillisecondNumbers()
    {
        var root = Serialize(Allocation("a1", "r1", 1_700_000_000_000, 1_700_003_600_000));

        Assert.AreEqual(JsonValueKind.Number, root.GetProperty("startTime").ValueKind);
        Assert.AreEqual(1_700_000_000_000, root.GetProperty("startTime").GetInt64());
        Assert.AreEqual(1_700_003_600_000, root.GetProperty("endTime").GetInt64());
        Assert.AreEqual("a1", root.GetProperty("id").GetString());
        Assert.AreEqual("r1", root.GetProperty("resourceId").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineAllocationShouldSerializeTheInstantWhateverItsOffset()
    {
        var bar = Bar();
        bar.StartTime = new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.FromHours(3.5));
        bar.EndTime = new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.FromHours(-7));

        var root = Serialize(bar);

        Assert.AreEqual(bar.StartTime.ToUnixTimeMilliseconds(), root.GetProperty("startTime").GetInt64());
        Assert.AreEqual(bar.EndTime.ToUnixTimeMilliseconds(), root.GetProperty("endTime").GetInt64());
    }

    [TestMethod]
    public void BitResourceTimelineAllocationShouldSerializeItsClassName()
    {
        var bar = Bar();
        bar.ClassName = "hatch";

        Assert.AreEqual("hatch", Serialize(bar).GetProperty("className").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineAllocationShouldOmitItsClassNameWhenNull()
    {
        Assert.IsFalse(Serialize(Bar()).TryGetProperty("className", out _));
    }

    [TestMethod]
    public void BitResourceTimelineEdgeBarShouldSerializeItsDurationAsAMillisecondNumber()
    {
        var bar = Bar();
        bar.StartBar = new BitResourceTimelineEdgeBar { Duration = TimeSpan.FromMinutes(15), Color = "red" };

        var startBar = Serialize(bar).GetProperty("startBar");

        Assert.AreEqual(JsonValueKind.Number, startBar.GetProperty("duration").ValueKind);
        Assert.AreEqual(900_000, startBar.GetProperty("duration").GetInt64());
        Assert.AreEqual("red", startBar.GetProperty("color").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineBarSectionsShouldSerializeWithTheExpectedNames()
    {
        var bar = Allocation("a1", "r1", 0, 3_600_000);
        bar.Sections =
        [
            new()
            {
                StartTime = DateTimeOffset.FromUnixTimeMilliseconds(600_000),
                EndPercent = 75.5,
                BackgroundColor = "#e03131",
                BorderColor = "#000",
                ForegroundColor = "#fff",
                Text = "Taxi",
                TextPosition = BitResourceTimelineBarSectionPosition.End,
                Icon = "/icons/plane.svg",
                IconPosition = BitResourceTimelineBarSectionPosition.Center,
                IconSize = 12,
            },
        ];

        var section = Serialize(bar).GetProperty("sections")[0];

        Assert.AreEqual(JsonValueKind.Number, section.GetProperty("startTime").ValueKind);
        Assert.AreEqual(600_000, section.GetProperty("startTime").GetInt64());
        Assert.AreEqual(75.5, section.GetProperty("endPercent").GetDouble());
        Assert.AreEqual("#e03131", section.GetProperty("backgroundColor").GetString());
        Assert.AreEqual("#000", section.GetProperty("borderColor").GetString());
        Assert.AreEqual("#fff", section.GetProperty("foregroundColor").GetString());
        Assert.AreEqual("Taxi", section.GetProperty("text").GetString());
        Assert.AreEqual("end", section.GetProperty("textPosition").GetString(), ignoreCase: true);
        Assert.AreEqual("/icons/plane.svg", section.GetProperty("icon").GetString());
        Assert.AreEqual("center", section.GetProperty("iconPosition").GetString(), ignoreCase: true);
        Assert.AreEqual(12, section.GetProperty("iconSize").GetInt32());
    }

    [TestMethod]
    public void BitResourceTimelineBarSectionsShouldOmitTheirUnsetBoundsAndColors()
    {
        var bar = Bar();
        bar.Sections = [new() { StartPercent = 50 }];

        var section = Serialize(bar).GetProperty("sections")[0];

        // The engine falls back to the bar's own edge and colors for anything missing, so an unset value must be absent
        // rather than a 0 or "".
        Assert.IsFalse(section.TryGetProperty("startTime", out _));
        Assert.IsFalse(section.TryGetProperty("endTime", out _));
        Assert.IsFalse(section.TryGetProperty("endPercent", out _));
        Assert.IsFalse(section.TryGetProperty("backgroundColor", out _));
        Assert.AreEqual("start", section.GetProperty("iconPosition").GetString(), ignoreCase: true);
        Assert.AreEqual("center", section.GetProperty("textPosition").GetString(), ignoreCase: true);
    }

    [TestMethod]
    public void BitResourceTimelineAllocationShouldOmitItsSectionsWhenNull()
    {
        Assert.IsFalse(Serialize(Bar()).TryGetProperty("sections", out _));
    }

    [TestMethod]
    public void BitResourceTimelineAllocationShouldSerializeItsTooltipWhenSet()
    {
        var bar = Bar();
        bar.Tooltip = "LH441\nGate A1";

        Assert.AreEqual("LH441\nGate A1", Serialize(bar).GetProperty("tooltip").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineBarIconShouldSerializeItsPositionAsANameAndInsideAsABool()
    {
        var root = Serialize(new BitResourceTimelineBarIcon
        {
            Source = "/icons/warning.svg",
            Position = BitResourceTimelineBarIconPosition.Center,
            Inside = true,
            Size = 12,
        });

        // The engine lowercases the name, so its casing is not part of the contract - but it must stay a string rather
        // than the ordinal of the enum.
        Assert.AreEqual(JsonValueKind.String, root.GetProperty("position").ValueKind);
        Assert.AreEqual("center", root.GetProperty("position").GetString(), ignoreCase: true);
        Assert.IsTrue(root.GetProperty("inside").GetBoolean());
        Assert.AreEqual(12, root.GetProperty("size").GetInt32());
        Assert.AreEqual("/icons/warning.svg", root.GetProperty("source").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineAllocationShouldRoundTripThroughTheWebSerializer()
    {
        var bar = Allocation("a1", "r1", 1_700_000_000_000, 1_700_003_600_000);
        bar.EndBar = new BitResourceTimelineEdgeBar { Duration = TimeSpan.FromMinutes(5) };

        var restored = JsonSerializer.Deserialize<BitResourceTimelineAllocation>(JsonSerializer.Serialize(bar, Web), Web)!;

        Assert.AreEqual(bar.StartTime, restored.StartTime);
        Assert.AreEqual(bar.EndTime, restored.EndTime);
        Assert.AreEqual(bar.EndBar.Duration, restored.EndBar!.Duration);
    }

    [TestMethod]
    public void BitResourceTimelineAllocationShouldSerializeItsDataAsACamelCasedObjectAndRoundTripIt()
    {
        var bar = Bar();
        bar.Data = JsonSerializer.SerializeToElement(new { flightNo = "LH441" });

        var json = JsonSerializer.Serialize(bar, Web);
        var data = Serialize(bar).GetProperty("data");

        Assert.AreEqual(JsonValueKind.Object, data.ValueKind);
        Assert.AreEqual("LH441", data.GetProperty("flightNo").GetString());

        var restored = JsonSerializer.Deserialize<BitResourceTimelineAllocation>(json, Web)!;
        Assert.IsTrue(restored.Data.HasValue);
        Assert.AreEqual("LH441", restored.Data.Value.GetProperty("flightNo").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineAllocationShouldSerializeLockedWhenTrueAndRoundTripIt()
    {
        var bar = Bar();
        bar.Locked = true;

        var json = JsonSerializer.Serialize(bar, Web);

        Assert.IsTrue(Serialize(bar).GetProperty("locked").GetBoolean());
        Assert.IsTrue(JsonSerializer.Deserialize<BitResourceTimelineAllocation>(json, Web)!.Locked);
    }

    [TestMethod]
    public void BitResourceTimelineAllocationShouldOmitLockedWhenFalse()
    {
        Assert.IsFalse(Serialize(Bar()).TryGetProperty("locked", out _));
    }



    // ---------------------------------------------------------------- the resources

    [TestMethod]
    public void BitResourceTimelineResourceShouldSerializeItsHierarchyWithTheExpectedNames()
    {
        var root = Serialize(new BitResourceTimelineResource { Id = "grp-1", Name = "Servers", ParentId = "root", Collapsed = true });

        Assert.AreEqual("root", root.GetProperty("parentId").GetString());
        Assert.IsTrue(root.GetProperty("collapsed").GetBoolean());
    }

    [TestMethod]
    public void BitResourceTimelineResourceShouldOmitItsParentIdWhenNull()
    {
        Assert.IsFalse(Serialize(new BitResourceTimelineResource { Id = "r1", Name = "Server-01" }).TryGetProperty("parentId", out _));
    }

    [TestMethod]
    public void BitResourceTimelineResourceShouldSerializeItsLineColorWhenSet()
    {
        var root = Serialize(new BitResourceTimelineResource { Id = "r1", Name = "Server-01", LineColor = "#15a" });

        Assert.AreEqual("#15a", root.GetProperty("lineColor").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineResourceShouldSerializeItsBackgroundWhenSet()
    {
        var root = Serialize(new BitResourceTimelineResource { Id = "r1", Name = "Server-01", Background = "#e7f5ff" });

        Assert.AreEqual("#e7f5ff", root.GetProperty("background").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineResourceShouldSerializeOnlyWhatIsSet()
    {
        var root = Serialize(new BitResourceTimelineResource { Id = "r1", Name = "Server-01" });

        CollectionAssert.AreEqual(new[] { "id", "name", "collapsed" }, root.EnumerateObject().Select(p => p.Name).ToArray());
    }



    // ---------------------------------------------------------------- the options

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeTheEditingKeysWithTheExpectedNames()
    {
        var root = Serialize(new BitResourceTimelineOptions
        {
            Editable = true,
            EditSnapMinutes = 30,
            EditResizeHandlePx = 8,
            EditMinDurationMinutes = 10,
            AllowResourceChange = false,
            AllowOverlap = false,
            EmptyDragAction = BitResourceTimelineEmptyDragAction.Create,
            SnapToTimeZone = true,
            Hour12 = true,
            MaxStackLanes = 3,
            AllowDelete = true,
            FirstDayOfWeek = DayOfWeek.Sunday,
            NonWorkingDays = [0, 6],
            WorkingHoursStart = 540,
            WorkingHoursEnd = 1020,
        });

        // The engine reads these exact camelCased keys off its config object.
        Assert.IsTrue(root.GetProperty("editable").GetBoolean());
        Assert.AreEqual(30, root.GetProperty("editSnapMinutes").GetInt32());
        Assert.AreEqual(8, root.GetProperty("editResizeHandlePx").GetInt32());
        Assert.AreEqual(10, root.GetProperty("editMinDurationMinutes").GetInt32());
        Assert.IsFalse(root.GetProperty("allowResourceChange").GetBoolean());
        Assert.IsFalse(root.GetProperty("allowOverlap").GetBoolean());
        Assert.AreEqual(JsonValueKind.String, root.GetProperty("emptyDragAction").ValueKind);
        Assert.AreEqual("create", root.GetProperty("emptyDragAction").GetString(), ignoreCase: true);
        Assert.IsTrue(root.GetProperty("snapToTimeZone").GetBoolean());
        Assert.IsTrue(root.GetProperty("hour12").GetBoolean());
        Assert.AreEqual(3, root.GetProperty("maxStackLanes").GetInt32());
        Assert.IsTrue(root.GetProperty("allowDelete").GetBoolean());
        // The engine takes the first day of the week as 0 (Sunday) to 6, the number of the DayOfWeek.
        Assert.AreEqual(0, root.GetProperty("firstDayOfWeek").GetInt32());
        Assert.AreEqual(JsonValueKind.Array, root.GetProperty("nonWorkingDays").ValueKind);
        Assert.AreEqual(540, root.GetProperty("workingHoursStart").GetInt32());
        Assert.AreEqual(1020, root.GetProperty("workingHoursEnd").GetInt32());
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldOmitTheEditingKeysWhenNull()
    {
        var root = Serialize(new BitResourceTimelineOptions());

        Assert.IsFalse(root.TryGetProperty("editable", out _));
        Assert.IsFalse(root.TryGetProperty("editSnapMinutes", out _));
        Assert.IsFalse(root.TryGetProperty("allowResourceChange", out _));
        Assert.IsFalse(root.TryGetProperty("allowOverlap", out _));
        Assert.IsFalse(root.TryGetProperty("emptyDragAction", out _));
        Assert.IsFalse(root.TryGetProperty("snapToTimeZone", out _));
        Assert.IsFalse(root.TryGetProperty("hour12", out _));
        Assert.IsFalse(root.TryGetProperty("maxStackLanes", out _));
        Assert.IsFalse(root.TryGetProperty("allowDelete", out _));
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeNothingWhileNothingIsSet()
    {
        // Every option left null keeps the default of the engine (and a color or a font, the theme in force).
        Assert.AreEqual("{}", JsonSerializer.Serialize(new BitResourceTimelineOptions(), Web));
        Assert.AreEqual("""{"colors":{},"surface":{},"axisLines":{}}""",
                        JsonSerializer.Serialize(new BitResourceTimelineOptions { Colors = new(), Surface = new(), AxisLines = new() }, Web));
        Assert.AreEqual("""{"columns":{},"rows":{}}""", JsonSerializer.Serialize(new BitResourceTimelineSurface { Columns = new(), Rows = new() }, Web));
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeTheTooltipKeysWithTheExpectedNames()
    {
        var root = Serialize(new BitResourceTimelineOptions
        {
            ShowTooltips = false,
            TooltipDelayMs = 500,
            Colors = new() { TooltipBg = "#000", TooltipText = "#fff", Focus = "#f00", NonWorking = "rgba(0,0,0,0.06)" },
        });

        Assert.IsFalse(root.GetProperty("showTooltips").GetBoolean());
        Assert.AreEqual(500, root.GetProperty("tooltipDelayMs").GetInt32());

        var colors = root.GetProperty("colors");
        Assert.AreEqual("#000", colors.GetProperty("tooltipBg").GetString());
        Assert.AreEqual("#fff", colors.GetProperty("tooltipText").GetString());
        Assert.AreEqual("#f00", colors.GetProperty("focus").GetString());
        Assert.AreEqual("rgba(0,0,0,0.06)", colors.GetProperty("nonWorking").GetString());
        // A color left null keeps the theme's, so it is not sent as one.
        Assert.IsFalse(colors.TryGetProperty("bar", out _));
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeTheWindowingKeysWithTheExpectedNames()
    {
        var root = Serialize(new BitResourceTimelineOptions { WindowBufferFactor = 2, WindowRefetchThreshold = 0.5, WindowDebounceMs = 100 });

        Assert.AreEqual(2, root.GetProperty("windowBufferFactor").GetDouble());
        Assert.AreEqual(0.5, root.GetProperty("windowRefetchThreshold").GetDouble());
        Assert.AreEqual(100, root.GetProperty("windowDebounceMs").GetInt32());
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeTheViewKeysWithTheExpectedNames()
    {
        var root = Serialize(new BitResourceTimelineOptions { AutoScrollToNow = true, PreserveScrollOnReload = true, NowLineRefreshMs = 1000, PanToDayStart = true });

        Assert.IsTrue(root.GetProperty("autoScrollToNow").GetBoolean());
        Assert.IsTrue(root.GetProperty("preserveScrollOnReload").GetBoolean());
        Assert.AreEqual(1000, root.GetProperty("nowLineRefreshMs").GetInt32());
        Assert.IsTrue(root.GetProperty("panToDayStart").GetBoolean());
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeTheResourceAxisResizeKeysWithTheExpectedNames()
    {
        var root = Serialize(new BitResourceTimelineOptions { ResourceAxisWidth = 220, ResourceAxisResizable = false, ResourceAxisMinWidth = 64, ResourceAxisMaxWidth = 400 });

        Assert.AreEqual(220, root.GetProperty("resourceAxisWidth").GetInt32());
        Assert.IsFalse(root.GetProperty("resourceAxisResizable").GetBoolean());
        Assert.AreEqual(64, root.GetProperty("resourceAxisMinWidth").GetInt32());
        Assert.AreEqual(400, root.GetProperty("resourceAxisMaxWidth").GetInt32());
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldOmitTheResourceAxisResizeKeysWhenNull()
    {
        var root = Serialize(new BitResourceTimelineOptions());

        Assert.IsFalse(root.TryGetProperty("resourceAxisResizable", out _));
        Assert.IsFalse(root.TryGetProperty("resourceAxisMinWidth", out _));
        Assert.IsFalse(root.TryGetProperty("resourceAxisMaxWidth", out _));
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeAZeroNowLineRefresh()
    {
        // 0 is what turns the ticking of "now" off, so it must reach the engine rather than be mistaken for "unset" and
        // dropped.
        Assert.AreEqual(0, Serialize(new BitResourceTimelineOptions { NowLineRefreshMs = 0 }).GetProperty("nowLineRefreshMs").GetInt32());
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeTheSurfaceWithTheExpectedNames()
    {
        var start = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero);

        var root = Serialize(new BitResourceTimelineOptions
        {
            Surface = new()
            {
                Columns = new()
                {
                    Unit = BitResourceTimelineSurfaceUnit.Day,
                    Align = BitResourceTimelineStripeAlign.Repeat,
                    Span = 1,
                    Offset = 1,
                    Colors = ["#aaa", "#bbb"],
                },
                Rows = new() { Colors = ["#111", "#222"], Span = 2 },
                Combine = BitResourceTimelineSurfaceCombine.Checker,
                ResourceColors = new Dictionary<string, string> { ["r1"] = "#abc" },
                Bands = [new() { Start = start, End = start.AddHours(2), Color = "#f00", ResourceId = "r1" }],
                ShadeTimeAxis = true,
                ShadeResourceAxis = false,
            },
        });

        var surface = root.GetProperty("surface");
        var columns = surface.GetProperty("columns");

        Assert.AreEqual("day", columns.GetProperty("unit").GetString(), ignoreCase: true);
        Assert.AreEqual("repeat", columns.GetProperty("align").GetString(), ignoreCase: true);
        Assert.AreEqual(1, columns.GetProperty("span").GetInt32());
        Assert.AreEqual(1, columns.GetProperty("offset").GetInt32());
        Assert.AreEqual("#bbb", columns.GetProperty("colors")[1].GetString());
        Assert.AreEqual(2, surface.GetProperty("rows").GetProperty("span").GetInt32());
        Assert.AreEqual("checker", surface.GetProperty("combine").GetString(), ignoreCase: true);
        // A dictionary keeps its keys as they are: they are resource ids, not names to camelCase.
        Assert.AreEqual("#abc", surface.GetProperty("resourceColors").GetProperty("r1").GetString());

        var band = surface.GetProperty("bands")[0];
        Assert.AreEqual(start.ToUnixTimeMilliseconds(), band.GetProperty("start").GetInt64());
        Assert.AreEqual(start.AddHours(2).ToUnixTimeMilliseconds(), band.GetProperty("end").GetInt64());
        Assert.AreEqual("#f00", band.GetProperty("color").GetString());
        Assert.AreEqual("r1", band.GetProperty("resourceId").GetString());
        Assert.IsTrue(surface.GetProperty("shadeTimeAxis").GetBoolean());
        Assert.IsFalse(surface.GetProperty("shadeResourceAxis").GetBoolean());
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldKeepTheCaseOfTheResourceIdsOfTheirColors()
    {
        var root = Serialize(new BitResourceTimelineOptions
        {
            Surface = new() { ResourceColors = new Dictionary<string, string> { ["Server-A"] = "#abc" } },
            AxisLines = new() { ResourceColors = new Dictionary<string, string> { ["Server-A"] = "#def" } },
        });

        Assert.AreEqual("#abc", root.GetProperty("surface").GetProperty("resourceColors").GetProperty("Server-A").GetString());
        Assert.AreEqual("#def", root.GetProperty("axisLines").GetProperty("resourceColors").GetProperty("Server-A").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeTheAxisLinesWithTheExpectedNames()
    {
        var root = Serialize(new BitResourceTimelineOptions
        {
            AxisLines = new()
            {
                BoundariesOnly = true,
                Vertical = new() { Unit = BitResourceTimelineSurfaceUnit.Day, Colors = ["#15a", "#888"] },
                Horizontal = new() { Colors = ["#111", "#222"], Span = 2 },
                ResourceColors = new Dictionary<string, string> { ["r1"] = "#abc" },
            },
        });

        var lines = root.GetProperty("axisLines");

        Assert.IsTrue(lines.GetProperty("boundariesOnly").GetBoolean());
        Assert.AreEqual("day", lines.GetProperty("vertical").GetProperty("unit").GetString(), ignoreCase: true);
        Assert.AreEqual("#888", lines.GetProperty("vertical").GetProperty("colors")[1].GetString());
        Assert.AreEqual(2, lines.GetProperty("horizontal").GetProperty("span").GetInt32());
        Assert.AreEqual("#abc", lines.GetProperty("resourceColors").GetProperty("r1").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldOmitTheSurfaceWhenNull()
    {
        Assert.IsFalse(Serialize(new BitResourceTimelineOptions()).TryGetProperty("surface", out _));
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeTheLocaleWithTheExpectedName()
    {
        Assert.AreEqual("de-DE", Serialize(new BitResourceTimelineOptions { Locale = "de-DE" }).GetProperty("locale").GetString());
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldSerializeTheRendererByName()
    {
        var root = Serialize(new BitResourceTimelineOptions { Renderer = BitResourceTimelineRendererType.Html });

        Assert.AreEqual(JsonValueKind.String, root.GetProperty("renderer").ValueKind);
        Assert.AreEqual("html", root.GetProperty("renderer").GetString(), ignoreCase: true);
    }

    [TestMethod,
        DataRow(typeof(BitResourceTimelineRendererType)),
        DataRow(typeof(BitResourceTimelineEmptyDragAction)),
        DataRow(typeof(BitResourceTimelineSurfaceUnit)),
        DataRow(typeof(BitResourceTimelineStripeAlign)),
        DataRow(typeof(BitResourceTimelineSurfaceCombine)),
        DataRow(typeof(BitResourceTimelineBarIconPosition)),
        DataRow(typeof(BitResourceTimelineBarSectionPosition))]
    public void BitResourceTimelineEnumsShouldCrossTheInteropByName(Type type)
    {
        // The engine compares the lowercased names, so every value has to be written as its name, never its ordinal.
        foreach (var value in Enum.GetValues(type))
        {
            Assert.AreEqual($"\"{Enum.GetName(type, value)}\"", JsonSerializer.Serialize(value, type, Web), $"{type.Name}.{value}");
            Assert.AreEqual(value, JsonSerializer.Deserialize($"\"{Enum.GetName(type, value)!.ToLowerInvariant()}\"", type, Web));
        }
    }



    // ---------------------------------------------------------------- the time converters

    [TestMethod]
    public void BitResourceTimelineUnixMillisecondsConverterShouldWriteABareNumber()
    {
        // A bare JSON number: the engine reads allocation.startTime as milliseconds.
        Assert.AreEqual("1700000000123", JsonSerializer.Serialize(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123), With("BitResourceTimelineUnixMillisecondsConverter")));
    }

    [TestMethod, DataRow(0L), DataRow(1_700_000_000_123L), DataRow(-62_135_596_800_000L)]
    public void BitResourceTimelineUnixMillisecondsConverterShouldRoundTrip(long unixMs)
    {
        var options = With("BitResourceTimelineUnixMillisecondsConverter");
        var original = DateTimeOffset.FromUnixTimeMilliseconds(unixMs);

        Assert.AreEqual(original, JsonSerializer.Deserialize<DateTimeOffset>(JsonSerializer.Serialize(original, options), options));

        // And through the bars that use it.
        var bar = Allocation("a1", "r1", unixMs, unixMs);
        var restored = JsonSerializer.Deserialize<BitResourceTimelineAllocation>(JsonSerializer.Serialize(bar, Web), Web)!;
        Assert.AreEqual(original, restored.StartTime);
        Assert.AreEqual(original, restored.EndTime);
    }

    [TestMethod]
    public void BitResourceTimelineUnixMillisecondsConverterShouldReadANumberWrittenAsAString()
    {
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123),
                        JsonSerializer.Deserialize<DateTimeOffset>("\"1700000000123\"", With("BitResourceTimelineUnixMillisecondsConverter")));

        var bar = JsonSerializer.Deserialize<BitResourceTimelineAllocation>("""{ "id": "a1", "resourceId": "r1", "startTime": "1700000000123", "endTime": 1700000000124 }""", Web)!;
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123), bar.StartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_124), bar.EndTime);
    }

    [TestMethod]
    public void BitResourceTimelineNullableUnixMillisecondsConverterShouldRoundTripAValueAndNull()
    {
        var options = With("BitResourceTimelineNullableUnixMillisecondsConverter");
        DateTimeOffset? value = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_123);

        Assert.AreEqual("1700000000123", JsonSerializer.Serialize(value, options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<DateTimeOffset?>("1700000000123", options));
        Assert.AreEqual(value, JsonSerializer.Deserialize<DateTimeOffset?>("\"1700000000123\"", options));
        Assert.AreEqual("null", JsonSerializer.Serialize<DateTimeOffset?>(null, options));
        Assert.IsNull(JsonSerializer.Deserialize<DateTimeOffset?>("null", options));

        // And through the sections that use it.
        var section = JsonSerializer.Deserialize<BitResourceTimelineBarSection>("""{ "startTime": 1700000000123, "endTime": null }""", Web)!;
        Assert.AreEqual(value, section.StartTime);
        Assert.IsNull(section.EndTime);
    }

    [TestMethod]
    public void BitResourceTimelineMillisecondsTimeSpanConverterShouldWriteABareNumber()
    {
        Assert.AreEqual("5400000", JsonSerializer.Serialize(TimeSpan.FromMinutes(90), With("BitResourceTimelineMillisecondsTimeSpanConverter")));
    }

    [TestMethod, DataRow(0), DataRow(1500), DataRow(3_600_000)]
    public void BitResourceTimelineMillisecondsTimeSpanConverterShouldRoundTrip(int ms)
    {
        var options = With("BitResourceTimelineMillisecondsTimeSpanConverter");
        var original = TimeSpan.FromMilliseconds(ms);

        Assert.AreEqual(original, JsonSerializer.Deserialize<TimeSpan>(JsonSerializer.Serialize(original, options), options));

        // And through the edge bars that use it.
        var restored = JsonSerializer.Deserialize<BitResourceTimelineEdgeBar>(JsonSerializer.Serialize(new BitResourceTimelineEdgeBar { Duration = original }, Web), Web)!;
        Assert.AreEqual(original, restored.Duration);
    }

    [TestMethod]
    public void BitResourceTimelineMillisecondsTimeSpanConverterShouldReadANumberWrittenAsAString()
    {
        Assert.AreEqual(TimeSpan.FromMilliseconds(1500), JsonSerializer.Deserialize<TimeSpan>("\"1500\"", With("BitResourceTimelineMillisecondsTimeSpanConverter")));
    }

    [TestMethod]
    public void BitResourceTimelineConvertersShouldBeInternal()
    {
        // The converters are a detail of the wire format, not a part of the API an app writes against.
        foreach (var name in new[] { "BitResourceTimelineUnixMillisecondsConverter", "BitResourceTimelineNullableUnixMillisecondsConverter", "BitResourceTimelineMillisecondsTimeSpanConverter" })
        {
            var type = Library.GetType("Bit.BlazorUI." + name, throwOnError: true)!;

            Assert.IsFalse(type.IsPublic, $"{name} is public.");
            Assert.IsTrue(typeof(JsonConverter).IsAssignableFrom(type), $"{name} is no JsonConverter.");
        }
    }

    private static JsonSerializerOptions With(string converter) => new()
    {
        Converters = { (JsonConverter)Activator.CreateInstance(Library.GetType("Bit.BlazorUI." + converter, throwOnError: true)!)! },
    };



    // ---------------------------------------------------------------- the shapes the engine declares

    [TestMethod,
        DataRow(typeof(BitResourceTimelineAllocation), "ResourceTimelineAllocation"),
        DataRow(typeof(BitResourceTimelineEdgeBar), "ResourceTimelineEdgeBar"),
        DataRow(typeof(BitResourceTimelineBarIcon), "ResourceTimelineBarIcon"),
        DataRow(typeof(BitResourceTimelineBarSection), "ResourceTimelineBarSection"),
        DataRow(typeof(BitResourceTimelineResource), "ResourceTimelineResource")]
    public void BitResourceTimelineDataShouldCrossTheInteropInTheShapeTheEngineDeclares(Type model, string engineInterface)
    {
        // A key the model writes that the engine does not declare is read by nobody, and one the engine declares that the
        // model does not write is never set: a renamed property on either side.
        CollectionAssert.AreEquivalent(EngineInterfaceKeys(engineInterface), JsonNames(model), $"{model.Name} against {engineInterface}");
    }

    [TestMethod]
    public void BitResourceTimelineOptionsShouldBeOptionsTheEngineKnows()
    {
        // The engine warns about (and ignores) an option its config does not declare.
        var config = EngineInterfaceKeys("ResourceTimelineConfig");

        var unknown = JsonNames(typeof(BitResourceTimelineOptions)).Where(name => config.Contains(name) is false).ToList();

        Assert.IsEmpty(unknown, "Options the engine does not know: " + string.Join(", ", unknown));

        // Besides the options, the facade hands the engine the texts and the disabled state, both declared by its config.
        Assert.IsTrue(config.Contains("texts") && config.Contains("disabled"));
    }

    [TestMethod]
    public void BitResourceTimelineColorsShouldBeTheColorsOfTheEngine()
    {
        var engine = Read("BitResourceTimelineEngine.ts");
        var block = Regex.Match(engine, @"const DEFAULT_COLORS[^=]*= \{(.*?)\n    \};", RegexOptions.Singleline);

        Assert.IsTrue(block.Success, "DEFAULT_COLORS was not found in the engine.");

        var colors = Regex.Matches(block.Groups[1].Value, @"^\s*(\w+):", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToArray();

        // The engine warns about (and ignores) a color it does not know, and a color it knows but the model has no
        // property for cannot be set by an app.
        CollectionAssert.AreEquivalent(colors, JsonNames(typeof(BitResourceTimelineColors)));
    }

    [TestMethod]
    public void BitResourceTimelineLayoutShouldReadTheLayoutTheEngineReports()
    {
        var engine = Read("BitResourceTimelineEngine.ts");
        var getLayout = Regex.Match(engine, @"public getLayout\(\): \{([^}]*)\}");

        Assert.IsTrue(getLayout.Success, "getLayout was not found in the engine.");

        var keys = Regex.Matches(getLayout.Groups[1].Value, @"(\w+):").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(keys, JsonNames(LayoutType));

        var layout = JsonSerializer.Deserialize("""{ "resourceAxisWidth": 220, "timeAxisHeight": 72, "resourceHeight": 36, "resourceIndent": 20 }""", LayoutType, Web)!;

        Assert.AreEqual(220, LayoutType.GetProperty("ResourceAxisWidth")!.GetValue(layout));
        Assert.AreEqual(72, LayoutType.GetProperty("TimeAxisHeight")!.GetValue(layout));
        Assert.AreEqual(36, LayoutType.GetProperty("ResourceHeight")!.GetValue(layout));
        Assert.AreEqual(20, LayoutType.GetProperty("ResourceIndent")!.GetValue(layout));
    }

    private static string[] JsonNames(Type type) => Contracts.GetTypeInfo(type).Properties.Select(p => p.Name).ToArray();

    private static string[] EngineInterfaceKeys(string name)
    {
        var engine = Read("BitResourceTimelineEngine.ts");
        var block = Regex.Match(engine, $@"export interface {name} \{{(.*?)\n    \}}", RegexOptions.Singleline);

        Assert.IsTrue(block.Success, $"The interface {name} was not found in the engine.");

        return Regex.Matches(block.Groups[1].Value, @"^\s*(\w+)\??:", RegexOptions.Multiline).Select(m => m.Groups[1].Value).ToArray();
    }

    private static string Read(string file) => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ResourceTimeline", file);
}
