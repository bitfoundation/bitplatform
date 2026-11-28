using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Sortable;

/// <summary>
/// Defaults for every list of an app, or of one part of it, set through a <see cref="BitSortableParams"/> cascaded by a
/// <see cref="BitParams"/>: they replace a parameter's default for every list that does not set the parameter itself,
/// and leave a list's own value alone.
/// </summary>
[TestClass]
public class BitSortableParamsTests : BitSortableTestContext
{
    // What makes one list the one it is: its items, its groups and rules, and its callbacks.
    private static readonly string[] _cascaded =
    [
        nameof(BitSortableParams.Animation),
        nameof(BitSortableParams.Classes),
        nameof(BitSortableParams.Delay),
        nameof(BitSortableParams.DelayTolerance),
        nameof(BitSortableParams.EmptyText),
        nameof(BitSortableParams.ForceFallback),
        nameof(BitSortableParams.Keyboard),
        nameof(BitSortableParams.RevertOnSpill),
        nameof(BitSortableParams.Scroll),
        nameof(BitSortableParams.ScrollSensitivity),
        nameof(BitSortableParams.ScrollSpeed),
        nameof(BitSortableParams.Strings),
        nameof(BitSortableParams.Styles),
        nameof(BitSortableParams.SwapThreshold),
        nameof(BitSortableParams.TouchDelay),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitSortableParams sortableParams, List<string> items, Action<RenderTreeBuilder>? attributes = null)
    {
        return Render<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { sortableParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitSortable<string>>(0);
                builder.AddComponentParameter(1, nameof(BitSortable<string>.Items), items);
                builder.AddComponentParameter(2, nameof(BitSortable<string>.ItemTemplate), (RenderFragment<string>)(item => b => b.AddContent(0, item)));
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitSortableParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitSortable", BitSortableParams.ParamName);
        Assert.AreEqual(BitSortableParams.ParamName, new BitSortableParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitSortableParams());
    }

    [TestMethod]
    public void BitSortableParamsShouldCarryWhatTheListsOfAnAppAgreeOnAsParametersOfTheList()
    {
        var baseParameters = typeof(BitComponentBaseParams).GetProperties().Select(p => p.Name).ToHashSet();
        var own = typeof(BitSortableParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                           .Select(p => p.Name)
                                           .Where(n => n != nameof(BitSortableParams.Name) && baseParameters.Contains(n) is false)
                                           .OrderBy(n => n, StringComparer.Ordinal)
                                           .ToArray();

        Assert.AreSequenceEqual(_cascaded, own);

        foreach (var name in own)
        {
            var parameter = typeof(BitSortable<string>).GetProperty(name);
            Assert.IsNotNull(parameter?.GetCustomAttribute<ParameterAttribute>(), $"BitSortable has no {name} parameter.");
            var type = typeof(BitSortableParams).GetProperty(name)!.PropertyType;
            Assert.AreEqual(Nullable.GetUnderlyingType(type) ?? type, Nullable.GetUnderlyingType(parameter!.PropertyType) ?? parameter.PropertyType, name);
        }
    }

    [TestMethod]
    public async Task TheCascadedValuesApplyToEveryListThatDoesNotSetTheParameter()
    {
        var german = new BitSortableStrings { RoleDescription = "sortierbares Element" };
        var component = RenderWithParams(new BitSortableParams
        {
            Animation = 300,
            TouchDelay = 500,
            Classes = new() { Ghost = "my-ghost" },
            Styles = new() { Item = "padding: 2px" },
            Strings = german,
        }, ["A", "B"]);

        var list = component.FindComponent<BitSortable<string>>();

        Assert.AreEqual(300, list.Instance.Animation);
        Assert.AreEqual(500, list.Instance.TouchDelay);
        Assert.AreSame(german, list.Instance.Strings);
        Assert.AreEqual("sortierbares Element", list.Find("[data-bit-srt-item]").GetAttribute("aria-roledescription"));
        StringAssert.Contains(ItemElement(list, "A").GetAttribute("style"), "padding: 2px");
        Assert.AreEqual(500, SentOptions(list)!.TouchDelay);
        // What is not set keeps the parameter's own default.
        Assert.AreEqual(1d, list.Instance.SwapThreshold);

        await DragStartAsync(list, "A");

        Assert.IsTrue(HasClass(list, "A", "bit-srt-gst"));
        Assert.IsTrue(HasClass(list, "A", "my-ghost"));
    }

    [TestMethod]
    public void TheCascadedEmptyTextIsWhatAnEmptyListShows()
    {
        var component = RenderWithParams(new BitSortableParams { EmptyText = "Nothing here" }, []);

        Assert.AreEqual("Nothing here", component.Find(".bit-srt-emp").TextContent.Trim());
    }

    [TestMethod]
    public void AListThatSetsTheParameterKeepsItsOwnValue()
    {
        var component = RenderWithParams(new BitSortableParams { Animation = 300, EmptyText = "Nothing here" }, [], builder =>
        {
            builder.AddComponentParameter(3, nameof(BitSortable<string>.Animation), 0);
            builder.AddComponentParameter(4, nameof(BitSortable<string>.EmptyText), "Drop here");
        });
        var list = component.FindComponent<BitSortable<string>>();

        component.Render();

        Assert.AreEqual(0, list.Instance.Animation);
        Assert.AreEqual(0, SentOptions(list)!.Animation);
        Assert.AreEqual("Drop here", component.Find(".bit-srt-emp").TextContent.Trim());
    }

    [TestMethod]
    public void WithoutParamsEveryParameterKeepsItsDefault()
    {
        var component = RenderWithParams(new BitSortableParams(), ["A"]);
        var list = component.FindComponent<BitSortable<string>>();

        Assert.AreEqual(150, list.Instance.Animation);
        Assert.AreEqual(200, list.Instance.TouchDelay);
        Assert.IsTrue(list.Instance.Keyboard);
        Assert.IsNull(list.Instance.Strings);
        Assert.IsNull(list.Instance.Classes);
    }
}
