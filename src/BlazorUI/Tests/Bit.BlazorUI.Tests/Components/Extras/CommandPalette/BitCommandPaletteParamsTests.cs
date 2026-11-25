using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.CommandPalette;

/// <summary>
/// The BitParams cascade of the command palette: what a BitCommandPaletteParams carries (what the palettes of an app agree
/// on), that the palette takes it, and that what is set on the palette itself wins over it.
/// </summary>
[TestClass]
public class BitCommandPaletteParamsTests : BunitTestContext
{
    // What makes one palette the one it is: whether it is open or in the page, its commands, its templates and callbacks,
    // and whether it lists what the service registered and which shortcut opens it.
    private static readonly string[] _notCascaded =
    [
        nameof(BitCommandPalette.CascadingParameters),
        nameof(BitCommandPalette.ChildContent),
        nameof(BitCommandPalette.Commands),
        nameof(BitCommandPalette.DetailTemplate),
        nameof(BitCommandPalette.EmptyTemplate),
        nameof(BitCommandPalette.EmptyText),
        nameof(BitCommandPalette.FooterTemplate),
        nameof(BitCommandPalette.GroupHeaderTemplate),
        nameof(BitCommandPalette.HeaderTemplate),
        nameof(BitCommandPalette.IconTemplate),
        nameof(BitCommandPalette.IncludeRegisteredCommands),
        nameof(BitCommandPalette.Inline),
        nameof(BitCommandPalette.ItemTemplate),
        nameof(BitCommandPalette.LoadingTemplate),
        nameof(BitCommandPalette.NoResultsTemplate),
        nameof(BitCommandPalette.NoResultsText),
        nameof(BitCommandPalette.OnClosed),
        nameof(BitCommandPalette.OnCommandExecuted),
        nameof(BitCommandPalette.OnOpened),
        nameof(BitCommandPalette.OnQueryChanged),
        nameof(BitCommandPalette.Open),
        nameof(BitCommandPalette.OpenChanged),
        nameof(BitCommandPalette.ToggleShortcut),
    ];

    [TestInitialize]
    public void Initialize()
    {
        Services.AddBitCommandPaletteServices();
        Services.AddBitCommandPaletteInMemoryHistoryStore();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        var handle = Context.JSInterop.SetupModule(BitCommandPaletteTestJs.Setup, _ => true);
        handle.Setup<bool>("isMac").SetResult(false);
    }

    private IRenderedComponent<BitParams> RenderWithParams(BitCommandPaletteParams paletteParams, Action<RenderTreeBuilder>? attributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { paletteParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitCommandPalette>(0);
                builder.AddComponentParameter(1, nameof(BitCommandPalette.Inline), true);
                builder.AddComponentParameter(2, nameof(BitCommandPalette.IncludeRegisteredCommands), false);
                builder.AddComponentParameter(3, nameof(BitCommandPalette.Commands), new BitCommandPaletteCommand[] { BitCommandPaletteCmd.New("Alpha").Group("Letters") });
                attributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitCommandPaletteParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitCommandPalette", BitCommandPaletteParams.ParamName);
        Assert.AreEqual(BitCommandPaletteParams.ParamName, new BitCommandPaletteParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitCommandPaletteParams());
    }

    [TestMethod]
    public void BitCommandPaletteParamsShouldCarryEveryParameterButTheStateOfOnePalette()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitCommandPalette).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                  .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                  .Select(p => p.Name)
                                                  .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                  .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitCommandPaletteParams).GetProperty(name), $"BitCommandPaletteParams has no {name}.");
        }

        foreach (var name in _notCascaded)
        {
            Assert.IsNull(typeof(BitCommandPaletteParams).GetProperty(name), $"BitCommandPaletteParams carries {name}.");
        }
    }

    [TestMethod]
    public void BitCommandPaletteShouldTakeTheCascadedValues()
    {
        var component = RenderWithParams(new BitCommandPaletteParams
        {
            Class = "cascaded-root",
            Placeholder = "Find anything",
            ShowFooter = false,
            GroupWhenSearching = false,
            Classes = new() { Input = "cascaded-input" },
            Styles = new() { List = "margin:1px" },
            Strings = new() { DialogLabel = "Commands" },
        });

        var instance = component.FindComponent<BitCommandPalette>().Instance;

        Assert.IsTrue(component.Find(".bit-cpl").ClassList.Contains("cascaded-root"));
        Assert.AreEqual("Commands", component.Find(".bit-cpl").GetAttribute("aria-label"));
        Assert.IsTrue(component.Find(".bit-cpl-input").ClassList.Contains("cascaded-input"));
        Assert.AreEqual("Find anything", component.Find(".bit-cpl-input").GetAttribute("placeholder"));
        Assert.AreEqual("margin:1px", component.Find(".bit-cpl-list").GetAttribute("style"));
        Assert.AreEqual(0, component.FindAll(".bit-cpl-footer").Count);
        Assert.IsFalse(instance.GroupWhenSearching);
    }

    [TestMethod]
    public void BitCommandPaletteShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitCommandPaletteParams { ShowFooter = false, Placeholder = "Find anything" }, builder =>
        {
            builder.AddComponentParameter(4, nameof(BitCommandPalette.ShowFooter), true);
        });

        Assert.AreEqual(1, component.FindAll(".bit-cpl-footer").Count);
        Assert.AreEqual("Find anything", component.Find(".bit-cpl-input").GetAttribute("placeholder"), "the cascaded value is taken where the palette sets none");
    }
}
