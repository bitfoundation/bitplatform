using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.Client.Web.BlazorUI.Tests.Lists.BitBasicListTest;

namespace Bit.Client.Web.BlazorUI.Tests.Lists
{
    [TestClass]
    public class BitBasicListTests : BunitTestContext
    {

        [DataTestMethod,
            DataRow(Visual.Fluent, true, 1000, 500, 100),
            DataRow(Visual.Fluent, false, 100, 500, 100),
            DataRow(Visual.Fluent, true, 1000, null, 100),
            DataRow(Visual.Fluent, false, 100, null, 100),
            DataRow(Visual.Fluent, true, 1000, 500, null),
            DataRow(Visual.Fluent, false, 100, 500, null),

            DataRow(Visual.Cupertino, true, 1000, 500, 100),
            DataRow(Visual.Cupertino, false, 100, 500, 100),
            DataRow(Visual.Cupertino, true, 1000, null, 100),
            DataRow(Visual.Cupertino, false, 100, null, 100),
            DataRow(Visual.Cupertino, true, 1000, 500, null),
            DataRow(Visual.Cupertino, false, 100, 500, null),

            DataRow(Visual.Material, true, 1000, 500, 100),
            DataRow(Visual.Material, false, 100, 500, 100),
            DataRow(Visual.Material, true, 1000, null, 100),
            DataRow(Visual.Material, false, 100, null, 100),
            DataRow(Visual.Material, true, 1000, 500, null),
            DataRow(Visual.Material, false, 100, 500, null),
            ]
        //When we use virtualization,extra items renderd base on overScanCount values that by defualt is 3
        public void BitBasicListShoudRenderExpectedChildElements(Visual visual, bool virtualize,
            int itemCount, decimal? listHeight, decimal? itemHeight)
        {
            var component = RenderComponent<BitBasicListTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Virtualize, virtualize);
                parameters.Add(p => p.Items, GetTestData(itemCount));
                parameters.Add(p => p.ListHeight, (listHeight is null) ? "500px" : $"height:{listHeight}px");
                parameters.Add(p => p.ItemHeight, (itemHeight is null) ? "100px" : $"height:{itemHeight}px");
            });

            var bitList = component.Find(".bit-bsc-lst");

            int comItemCount = bitList.ChildElementCount;

            if (virtualize)
            {
                listHeight = (listHeight is null) ? 500 : listHeight;
                itemHeight = (itemHeight is null) ? 100 : itemHeight;

                decimal itemForRender = Math.Ceiling(listHeight.GetValueOrDefault() / itemHeight.GetValueOrDefault());

                var renderedItems = bitList.GetElementsByClassName("list-item").Length;
                Assert.AreEqual(itemForRender + 3, renderedItems);
            }
            else
            {
                Assert.AreEqual(comItemCount, itemCount);
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, 1000, true, 1, 500, 100),
            DataRow(Visual.Fluent, 100, false, 1, 500, 100),
            DataRow(Visual.Fluent, 1000, true, 1, null, 100),
            DataRow(Visual.Fluent, 100, false, 1, null, 100),
            DataRow(Visual.Fluent, 1000, true, 1, 500, null),
            DataRow(Visual.Fluent, 100, false, 1, 500, null),

            DataRow(Visual.Cupertino, 1000, true, 1, 500, 100),
            DataRow(Visual.Cupertino, 100, false, 1, 500, 100),
            DataRow(Visual.Cupertino, 1000, true, 1, null, 100),
            DataRow(Visual.Cupertino, 100, false, 1, null, 100),
            DataRow(Visual.Cupertino, 1000, true, 1, 500, null),
            DataRow(Visual.Cupertino, 100, false, 1, 500, null),

            DataRow(Visual.Material, 1000, true, 1, 500, 100),
            DataRow(Visual.Material, 100, false, 1, 500, 100),
            DataRow(Visual.Material, 1000, true, 1, null, 100),
            DataRow(Visual.Material, 100, false, 1, null, 100),
            DataRow(Visual.Material, 1000, true, 1, 500, null),
            DataRow(Visual.Material, 100, false, 1, 500, null)
        ]
        public void BitBasicListRenderExtraItemsUsingOverScanCount(Visual visual,
             int itemCount, bool virtualize, int overScanCount, decimal? listHeight, decimal? itemHeight)
        {
            var component = RenderComponent<BitBasicListTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Virtualize, virtualize);
                parameters.Add(p => p.Items, GetTestData(itemCount));
                parameters.Add(p => p.OverscanCount, overScanCount);
                parameters.Add(p => p.ListHeight, (listHeight is null) ? "500px" : $"height:{listHeight}px");
                parameters.Add(p => p.ItemHeight, (itemHeight is null) ? "100px" : $"height:{itemHeight}px");
            });

            var bitList = component.Find(".bit-bsc-lst");

            int comItemCount = bitList.ChildElementCount;
            if (virtualize)
            {
                listHeight = (listHeight is null) ? 500 : listHeight;
                itemHeight = (itemHeight is null) ? 100 : itemHeight;

                decimal itemForRender = Math.Ceiling(listHeight.GetValueOrDefault() / itemHeight.GetValueOrDefault());

                var renderedItems = bitList.GetElementsByClassName("list-item").Length;
                Assert.AreEqual(comItemCount, itemCount + (overScanCount + 2));
            }
            else
            {
                Assert.AreEqual(comItemCount, itemCount);
            }
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, 1000, "RoleItem", true),
            DataRow(Visual.Fluent, 100, "RoleItem", false),

            DataRow(Visual.Cupertino, 1000, "RoleItem", true),
            DataRow(Visual.Cupertino, 100, "RoleItem", false),

            DataRow(Visual.Material, 1000, "RoleItem", true),
            DataRow(Visual.Material, 100, "RoleItem", false),
            ]
        public void BitBasicListCheckForCorrectRole(Visual visual, int itemCount, string role, bool virtualize)
        {
            var component = RenderComponent<BitBasicListTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Items, GetTestData(itemCount));
                parameters.Add(p => p.Role, role);
                parameters.Add(p => p.Virtualize, virtualize);
            });
            var bitList = component.Find(".bit-bsc-lst");

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            Assert.IsTrue(bitList.ClassList.Contains($"bit-bsc-lst-{visualClass}"));

            var itemRole = bitList.GetAttribute("role");

            Assert.AreEqual(role, itemRole);
        }

        [DataTestMethod,
            DataRow(Visual.Fluent, 1000, true),
            DataRow(Visual.Fluent, 100, false),

            DataRow(Visual.Cupertino, 1000, true),
            DataRow(Visual.Cupertino, 100, false),

            DataRow(Visual.Material, 1000, true),
            DataRow(Visual.Material, 100, false),
        ]
        public void BitBasicListCheckForCorrectVisual(Visual visual, int itemCount, bool virtualize)
        {
            var component = RenderComponent<BitBasicListTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.Items, GetTestData(itemCount));
                parameters.Add(p => p.Virtualize, virtualize);
            });
            var bitList = component.Find(".bit-bsc-lst");

            var visualClass = visual == Visual.Cupertino ? "cupertino" : visual == Visual.Material ? "material" : "fluent";
            Assert.IsTrue(bitList.ClassList.Contains($"bit-bsc-lst-{visualClass}"));
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
