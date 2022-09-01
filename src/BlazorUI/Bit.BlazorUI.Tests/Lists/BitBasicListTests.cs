using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Lists.BitBasicListTest;

namespace Bit.BlazorUI.Tests.Lists;

[TestClass]
public class BitBasicListTests : BunitTestContext
{
    [DataTestMethod,
        DataRow(true, 3_000_000, null),
        DataRow(true, 3_000_000, 5),
        DataRow(true, null, 5),
        DataRow(true, null, null),

        DataRow(false, 3_000_000, null),
        DataRow(false, 3_000_000, 5),
        DataRow(false, null, 5),
        DataRow(false, null, null),
    ]
    public void BitBasicListShoudRenderExpectedChildElements(bool virtualize, int? itemSize, int? overscanCount)
    {
        //https://bunit.dev/docs/test-doubles/emulating-ijsruntime.html#-jsinterop-emulation
        const double viewportHeight = 1_000_000_000;

        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        var component = RenderComponent<BitBasicListTest>(parameters =>
        {
            parameters.Add(p => p.Virtualize, virtualize);
            parameters.Add(p => p.Items, GetTestData(500));
            //ItemSize default value is 50.
            parameters.Add(p => p.ItemSize, itemSize ?? 50);
            //OverscanCount default value is 3.
            parameters.Add(p => p.OverscanCount, overscanCount ?? 3);
        });

        var bitList = component.Find(".bit-bsc-lst");

        if (virtualize)
        {
            //When virtualize is true, number of rendered items is greater than number of items showm in the list + 2 * overScanCount.
            var expectedRenderedItemCount = Math.Ceiling((decimal)(viewportHeight / component.Instance.ItemSize)) + (2 * component.Instance.OverscanCount);
            var actualRenderedItemCount = bitList.GetElementsByClassName("list-item").Length;

            //When actualRenderedItemCount is smaller than expectedRenderedItemCount, so show all items in viewport then actualRenderedItemCount equals total items count
            if (actualRenderedItemCount < expectedRenderedItemCount)
            {
                Assert.AreEqual(component.Instance.Items.Count, actualRenderedItemCount);
            }
            else
            {
                Assert.AreEqual(expectedRenderedItemCount, actualRenderedItemCount);
            }
        }
        else
        {
            var actualRenderedItemCount = bitList.GetElementsByClassName("list-item").Length;
            Assert.AreEqual(component.Instance.Items.Count, actualRenderedItemCount);
        }
    }

    [DataTestMethod,
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

        var bitList = component.Find(".bit-bsc-lst");

        var bitLisRole = bitList.GetAttribute("role");

        if (role.HasNoValue())
        {
            Assert.AreEqual("list", bitLisRole);
        }
        else
        {
            Assert.AreEqual(role, bitLisRole);
        }
    }

    [DataTestMethod, DataRow(100)]
    public void BitBasicListShouldHaveCorrectClass(int itemCount)
    {
        var component = RenderComponent<BitBasicListTest>(parameters =>
        {
            parameters.Add(p => p.Visual, Visual.Fluent);
            parameters.Add(p => p.Items, GetTestData(itemCount));
        });

        var bitList = component.Find("div");

        Assert.IsTrue(bitList.ClassList.Contains("bit-bsc-lst"));
    }

    private List<Person> GetTestData(int itemCount)
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
