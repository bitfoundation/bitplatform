using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.Pagination;

/// <summary>
/// Covers what BitPaginationTests and BitPaginationCascadingParamsTest leave of the BitParams cascade of the pagination:
/// the name of the cascade, that the params object carries every parameter that is not per pagination, that an empty
/// one changes nothing, that a false the pagination wrote for itself is a choice the cascade leaves alone, and that the
/// cascaded base parameters reach the landmark and its controls.
/// </summary>
[TestClass]
public class BitPaginationParamsTests : BunitTestContext
{
    // The data a pagination pages through (its count, its items, its page size and its selected page) and its
    // callbacks belong to each instance, so they have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitPagination.CascadingParameters),
        nameof(BitPagination.Count),
        nameof(BitPagination.DefaultSelectedPage),
        nameof(BitPagination.OnChange),
        nameof(BitPagination.OnPageSizeChange),
        nameof(BitPagination.PageSize),
        nameof(BitPagination.PageSizeChanged),
        nameof(BitPagination.SelectedPage),
        nameof(BitPagination.SelectedPageChanged),
        nameof(BitPagination.TotalItems),
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitPaginationParams paginationParams, Action<RenderTreeBuilder>? extraAttributes = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { paginationParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPagination>(0);
                builder.AddAttribute(1, nameof(BitPagination.Count), 5);
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    [TestMethod]
    public void BitPaginationParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitPagination)}", BitPaginationParams.ParamName);
        Assert.AreEqual("BitParams.BitPagination", BitPaginationParams.ParamName);
    }

    [TestMethod]
    public void BitPaginationParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitPaginationParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitPaginationParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitPaginationParamsShouldCarryEveryParameterThatIsNotPerPagination()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitPagination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                              .Select(p => p.Name)
                                              .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                              .ToList();

        Assert.IsTrue(parameters.Count > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitPaginationParams).GetProperty(name), $"BitPaginationParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitPaginationShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var alone = RenderComponent<BitPagination>(parameters => parameters.Add(p => p.Count, 5));

        var cascaded = RenderWithParams(new BitPaginationParams()).FindComponent<BitPagination>();

        // The ids of the two instances are the only thing that may tell their markup apart.
        var expected = WithoutRenderIds(alone.Markup.Replace(alone.Instance.UniqueId, "{id}"));
        var actual = WithoutRenderIds(cascaded.Markup.Replace(cascaded.Instance.UniqueId, "{id}"));

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void BitPaginationShouldKeepAnExplicitFalseOverACascadedTrue()
    {
        var component = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>
            {
                new BitPaginationParams { HideOnSinglePage = true, ShowPageButtons = false, Loop = true }
            });
            parameters.AddChildContent(builder =>
            {
                // A single page the pagination itself asked to keep on the screen, with the page buttons it asked for.
                builder.OpenComponent<BitPagination>(0);
                builder.AddAttribute(1, nameof(BitPagination.Count), 1);
                builder.AddAttribute(2, nameof(BitPagination.HideOnSinglePage), false);
                builder.AddAttribute(3, nameof(BitPagination.ShowPageButtons), true);
                builder.CloseComponent();

                // The last of three pages, with only the navigation buttons the cascade leaves it.
                builder.OpenComponent<BitPagination>(4);
                builder.AddAttribute(5, nameof(BitPagination.Count), 3);
                builder.AddAttribute(6, nameof(BitPagination.DefaultSelectedPage), 3);
                builder.CloseComponent();
            });
        });

        var roots = component.FindAll(".bit-pgn");

        // A false the pagination wrote for itself is a choice, not an absence the cascade may fill in.
        Assert.AreEqual(2, roots.Count);
        Assert.AreEqual(1, roots[0].QuerySelectorAll(".bit-pgn-btn[aria-current='page']").Length);

        // What it left unset still comes from the cascade: no page buttons, and a next button that loops around.
        Assert.AreEqual(0, roots[1].QuerySelectorAll("[aria-current]").Length);

        var next = roots[1].QuerySelector("button[aria-label='Next page']");

        Assert.IsNotNull(next);
        Assert.IsFalse(next.HasAttribute("disabled"));
        Assert.IsNull(next.GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitPaginationShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitPaginationParams
        {
            Id = "cascaded-id",
            Class = "cascaded",
            Style = "margin:1px",
            Dir = BitDir.Rtl,
            Disabled = true,
            AriaLabel = "Results pages",
        });

        var root = component.Find(".bit-pgn");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.AreEqual("Results pages", root.GetAttribute("aria-label"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));

        // Every native control of a disabled pagination says so both ways.
        var buttons = component.FindAll("button");

        Assert.IsTrue(buttons.Count > 0);
        Assert.IsTrue(buttons.All(b => b.HasAttribute("disabled") && b.GetAttribute("aria-disabled") == "true"));
    }

    [TestMethod]
    public void BitPaginationShouldApplyTheCascadedLabelsOfEveryControl()
    {
        var component = RenderWithParams(new BitPaginationParams
        {
            ShowFirstButton = true,
            ShowLastButton = true,
            ShowGoToPage = true,
            ShowPageSizeSelector = true,
            FirstButtonAriaLabel = "Première",
            PreviousButtonAriaLabel = "Précédente",
            NextButtonAriaLabel = "Suivante",
            LastButtonAriaLabel = "Dernière",
            GoToPageText = "Aller à",
            PageSizeText = "Par page",
            GetPageAriaLabel = (page, selected) => selected ? $"Page {page}, actuelle" : $"Page {page}",
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitPagination.DefaultSelectedPage), 2);
        });

        foreach (var label in new[] { "Première", "Précédente", "Suivante", "Dernière" })
        {
            Assert.IsNotNull(component.FindAll("button").FirstOrDefault(b => b.GetAttribute("aria-label") == label), $"No control is named {label}.");
        }

        Assert.AreEqual("Aller à", component.Find(".bit-pgn-gtl").TextContent.Trim());
        Assert.AreEqual("Par page", component.Find(".bit-pgn-psl").TextContent.Trim());
        Assert.AreEqual("Page 2, actuelle", component.Find(".bit-pgn-sel").GetAttribute("aria-label"));
        Assert.AreEqual("Page 1", component.FindAll(".bit-pgn-btn[aria-label]").First(b => b.TextContent.Trim() == "1").GetAttribute("aria-label"));
    }

    // bUnit writes every element reference and event handler as a blazor:* attribute holding an id numbered per
    // render, so two renders of the very same markup only compare equal without them.
    private static string WithoutRenderIds(string markup) => Regex.Replace(markup, @"\s+blazor:[^=\s>]+=""[^""]*""", string.Empty);
}
