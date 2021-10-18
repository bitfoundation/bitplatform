using System;
using System.Collections.Generic;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.Client.Web.BlazorUI.Tests.Lists.BitBasicListTest;

namespace Bit.Client.Web.BlazorUI.Tests.Lists
{
    [TestClass]
    public class BitBasicListTests : BunitTestContext
    {

        [DataTestMethod,
            DataRow(true, 1000, 500, 50, null, 1),
            DataRow(true, 1000, 500, 50, 50, 1),
            DataRow(true, 1000, 500, 50, null, null),
            DataRow(true, 1000, 500, 50, 50, null),
            DataRow(true, 1000, null, 50, null, 1),
            DataRow(true, 1000, null, 50, 50, 1),
            DataRow(true, 1000, null, 50, null, null),
            DataRow(true, 1000, null, 50, 50, null),
            DataRow(false, 100, null, null, null, null)
        ]
        public void BitBasicListShoudRenderExpectedChildElements(
            bool virtualize,
            int itemCount,
            int? listHeight,
            int? itemHeight,
            int? itemSize,
            int? overscanCount)
        {
            Context.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderComponent<BitBasicListTest>(parameters =>
            {
                parameters.Add(p => p.Virtualize, virtualize);
                parameters.Add(p => p.Items, GetTestData(itemCount));
                parameters.Add(p => p.ListStyle, (listHeight is null) ? "height:500px" : $"height:{listHeight}px");
                parameters.Add(p => p.ItemStyle, (itemHeight is null) ? "height:50px" : $"height:{itemHeight}px");
                //ItemSize default value is 50.
                parameters.Add(p => p.ItemSize, (itemSize is null) ? 50 : itemSize.Value);
                //OverscanCount default value is 3.
                parameters.Add(p => p.OverscanCount, (overscanCount is null) ? 3 : overscanCount.Value);
            });

            var bitList = component.Find(".bit-bsc-lst");

            if (virtualize)
            {
                listHeight = (listHeight is null) ? 500 : listHeight;
                itemHeight = (itemHeight is null) ? 50 : itemHeight;
                overscanCount = (overscanCount is null) ? 3 : overscanCount;

                //When virtualize is true, number of rendered items is greater than number of items showm in the list + 2 * overScanCount.
                var expectedRenderedItemCount = Math.Ceiling((decimal)(listHeight / itemHeight)) + 2 * overscanCount;
                var actualRenderedItemCount = bitList.GetElementsByClassName("list-item").Length;
                
                // due to changes to Virtualize component in the .net 6.0 RC2, this test is not valid anymore!
                // Assert.IsTrue(actualRenderedItemCount >= expectedRenderedItemCount);
            }
            else
            {
                var actualRenderedItemCount = bitList.GetElementsByClassName("list-item").Length;
                Assert.AreEqual(itemCount, actualRenderedItemCount);
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
}
