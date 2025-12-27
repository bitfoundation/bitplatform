using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bunit;

namespace Bit.BlazorUI.Tests.Components.Lists.BasicList;

[TestClass]
public class BitBasicListTests : BunitTestContext
{
    [TestMethod,
        DataRow(true, 3_000_000, null),
        DataRow(true, 3_000_000, 5),
        DataRow(true, null, 5),
        DataRow(true, null, null),

        DataRow(false, 3_000_000, null),
        DataRow(false, 3_000_000, 5),
        DataRow(false, null, 5),
        DataRow(false, null, null),
    ]
    public void BitBasicListShouldRenderExpectedChildElements(bool virtualize, int? itemSize, int? overscanCount)
    {
        //https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html#-jsinterop-emulation
        const double viewportHeight = 1_000_000_000;
        var maxItemCount = 100;

        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // To ensure a consistent display structure in the Virtualize component across .NET 8, .NET 9, and .NET 10,
        // we've set the default value of MaxItemCount to 100. This means that even if a higher value is specified,
        // only a maximum of 100 items will be rendered by default.
        AppContext.SetData("Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize.MaxItemCount", maxItemCount);

        var component = RenderComponent<BitBasicListTest>(parameters =>
        {
            parameters.Add(p => p.Virtualize, virtualize);
            parameters.Add(p => p.Items, GetTestData(500));
            //ItemSize default value is 50.
            parameters.Add(p => p.ItemSize, itemSize ?? 50);
            //OverscanCount default value is 3.
            parameters.Add(p => p.OverscanCount, overscanCount ?? 3);
        });

        var bitList = component.Find(".bit-bsl");

        if (virtualize)
        {
            //When virtualize is true, number of rendered items is greater than number of items show in the list + 2 * overScanCount.
            var overscanItemsCount = 2 * component.Instance.OverscanCount;

#if NET10_0
            maxItemCount += overscanItemsCount;
#endif

            var expectedRenderedItemCount = Math.Ceiling((decimal)(viewportHeight / component.Instance.ItemSize)) + overscanItemsCount;
            expectedRenderedItemCount = Math.Min(expectedRenderedItemCount, maxItemCount);

            var actualRenderedItemCount = bitList.GetElementsByClassName("list-item").Length;

            //When actualRenderedItemCount is smaller than expectedRenderedItemCount, so show all items in viewport then actualRenderedItemCount equals total items count
            if (actualRenderedItemCount < expectedRenderedItemCount)
            {
                Assert.AreEqual(component?.Instance?.Items?.Count, actualRenderedItemCount);
            }
            else
            {
                Assert.AreEqual(expectedRenderedItemCount, actualRenderedItemCount);
            }
        }
        else
        {
            var actualRenderedItemCount = bitList.GetElementsByClassName("list-item").Length;
            Assert.AreEqual(component?.Instance?.Items?.Count, actualRenderedItemCount);
        }

        AppContext.SetData("Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize.MaxItemCount", null);
    }

    [TestMethod,
        DataRow(100, "AssignedRole"),
        DataRow(100, null)
    ]
    public void BitBasicListShouldHaveCorrectRole(int itemCount, string role)
    {
        var component = RenderComponent<BitBasicListTest>(parameters =>
        {
            parameters.Add(p => p.Items, GetTestData(itemCount));
            if (role.HasValue())
            {
                parameters.Add(p => p.Role, role);
            }
        });

        var list = component.Find(".bit-bsl");

        var listRole = list.GetAttribute("role");

        Assert.AreEqual(role.HasValue() ? role : "list", listRole);
    }

    [TestMethod, DataRow(100)]
    public void BitBasicListShouldHaveCorrectClass(int itemCount)
    {
        var component = RenderComponent<BitBasicListTest>(parameters =>
        {
            parameters.Add(p => p.Items, GetTestData(itemCount));
        });

        var bitList = component.Find("div");

        Assert.IsTrue(bitList.ClassList.Contains("bit-bsl"));
    }

    private static List<Person> GetTestData(int itemCount)
    {
        List<Person> people = new();
        for (int i = 0; i < itemCount; i++)
        {
            people.Add(new Person
            {
                Id = i + 1,
                FirstName = $"Person {i + 1.ToString()}",
                LastName = $"Person Family {i + 1.ToString()}",
                Job = $"Programmer {i + 1.ToString()}"
            });
        }
        return people;
    }
}
