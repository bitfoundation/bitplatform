using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.BreadList;

[TestClass]
public class BitBreadListTests : BunitTestContext
{
    [DataTestMethod,
     DataRow(Visual.Fluent, true),
     DataRow(Visual.Fluent, false),
     DataRow(Visual.Cupertino, true),
     DataRow(Visual.Cupertino, false),
     DataRow(Visual.Material, true),
     DataRow(Visual.Material, false),
   ]
    public void BitBreadListTest(Visual visual, bool isEnabled)
    {
        var component = RenderComponent<BitBreadList<PageInfoModel>>(parameters =>
        {
            parameters.Add(p => p.IsEnabled, isEnabled);
            parameters.Add(p => p.Visual, visual);
        });

        var breadList = component.Find(".bit-brl");

        var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";

        Assert.IsTrue(breadList.ClassList.Contains($"bit-brl-{visualClass}"));

        if (isEnabled)
        {
            Assert.IsTrue(breadList.ClassList.Contains($"bit-brl-enabled-{visualClass}"));
        }
        else
        {
            Assert.IsTrue(breadList.ClassList.Contains($"bit-brl-disabled-{visualClass}"));
        }
    }

    [DataTestMethod,
        DataRow((uint)1),
        DataRow((uint)2),
        DataRow((uint)3),
        DataRow((uint)4),
        DataRow((uint)9),
    ]
    public void BitBreadListShouldRespectMaxDisplayeItems(uint maxDisplayedItems)
    {
        var component = RenderComponent<BitBreadList<PageInfoModel>>(parameters =>
        {
            parameters.Add(p => p.Items, BreadListItems);
            parameters.Add(p => p.TextField, nameof(PageInfoModel.Name));
            parameters.Add(p => p.MaxDisplayedItems, maxDisplayedItems);
        });

        var breadListItems = component.FindAll(".bit-brl .items-wrapper ul li");

        if (maxDisplayedItems < BreadListItems.Count)
        {
            Assert.AreEqual((uint)breadListItems.Count, maxDisplayedItems + 1);
        }
        else
        {
            Assert.AreEqual(breadListItems.Count, BreadListItems.Count);
        }
    }

    [DataTestMethod,
        DataRow((uint)3, (uint)0),
        DataRow((uint)3, (uint)1),
        DataRow((uint)3, (uint)3),
        DataRow((uint)3, (uint)4),
    ]
    public void BitBreadListShouldRespectOverflowIndex(uint maxDisplayedItems, uint overflowIndex)
    {
        var component = RenderComponent<BitBreadList<PageInfoModel>>(parameters =>
        {
            parameters.Add(p => p.Items, BreadListItems);
            parameters.Add(p => p.TextField, nameof(PageInfoModel.Name));
            parameters.Add(p => p.MaxDisplayedItems, maxDisplayedItems);
            parameters.Add(p => p.OverflowIndex, overflowIndex);
        });

        var breadListItems = component.FindAll(".bit-brl .items-wrapper ul li");

        if (overflowIndex < maxDisplayedItems)
        {
            Assert.IsTrue(breadListItems[(int)overflowIndex].InnerHtml.Contains("span"));
        }
        else if(overflowIndex >= maxDisplayedItems)
        {
            Assert.IsTrue(breadListItems[0].InnerHtml.Contains("span"));
        }
    }

    [DataTestMethod]
    public void BitBreadListShouldRespectItemIsEnabled()
    {
        var component = RenderComponent<BitBreadList<PageInfoModel>>(parameters =>
        {
            parameters.Add(p => p.Items, BreadListItems);
            parameters.Add(p => p.TextField, nameof(PageInfoModel.Name));
            parameters.Add(p => p.IsEnabledField, nameof(PageInfoModel.IsEnabled));
        });

        var breadListItems = component.FindAll(".bit-brl .items-wrapper ul li a");

        Assert.IsTrue(breadListItems[1].ClassList.Contains("disabled-item"));
    }

    [DataTestMethod]
    public void BitBreadListShouldRespectIsSelected()
    {
        var component = RenderComponent<BitBreadList<PageInfoModel>>(parameters =>
        {
            parameters.Add(p => p.Items, BreadListItems);
            parameters.Add(p => p.TextField, nameof(PageInfoModel.Name));
            parameters.Add(p => p.IsSelectedField, nameof(PageInfoModel.IsSelected));
        });

        var breadListItems = component.FindAll(".bit-brl .items-wrapper ul li a");

        Assert.IsTrue(breadListItems[2].GetAttribute("aria-current").Contains("page"));
        Assert.IsTrue(breadListItems[2].ClassList.Contains("selected-item"));
    }

    [DataTestMethod]
    public void BitBreadListShouldRespectItemsClassAndStyle()
    {
        var component = RenderComponent<BitBreadList<PageInfoModel>>(parameters =>
        {
            parameters.Add(p => p.Items, BreadListItems);
            parameters.Add(p => p.TextField, nameof(PageInfoModel.Name));
            parameters.Add(p => p.ClassField, nameof(PageInfoModel.HtmlClass));
            parameters.Add(p => p.StyleField, nameof(PageInfoModel.HtmlStyle));
        });

        var breadListItems = component.FindAll(".bit-brl .items-wrapper ul li a");

        Assert.IsTrue(breadListItems[3].ClassList.Contains("custom-class-name"));
        Assert.IsTrue(breadListItems[4].GetAttribute("style").Contains("background-color: red;"));
    }

    public IList<PageInfoModel> BreadListItems { get; set; } = new List<PageInfoModel>
    {
        new PageInfoModel { Name = "Page 1", Href = "https://components.bitplatform.dev/" },
        new PageInfoModel { Name = "Page 2", Href = "https://components.bitplatform.dev/", IsEnabled = false },
        new PageInfoModel { Name = "Page 3", Href = "https://components.bitplatform.dev/", IsSelected = true },
        new PageInfoModel { Name = "Page 4", Href = "https://components.bitplatform.dev/", HtmlClass = "custom-class-name" },
        new PageInfoModel { Name = "Page 5", Href = "https://components.bitplatform.dev/", HtmlStyle = "background-color: red;" },
    };
}
