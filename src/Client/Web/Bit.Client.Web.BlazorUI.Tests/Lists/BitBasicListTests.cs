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

        [DataTestMethod, DataRow(1000)]
        //When we use virtualization, two additional parts are created,
        //that's why this method use itemCount +2
        public void BitBasicListShoudRenderExpectedChildElements(int itemCount)
        {
            var component = RenderComponent<BitBasicListTest>(parameters => parameters
                .Add(p => p.Items, GetTestData(itemCount)));
            var bitList = component.Find(".bit-bsc-lst");
            int comItemCount = bitList.ChildElementCount;

            Assert.AreEqual(comItemCount + 2, itemCount + 2);
        }


        [DataTestMethod, DataRow(10)]
        //When we use virtualization, two additional parts are created,
        //that's why this method use itemCount +2
        public void BitBasicListShoudRenderExpectedChildElementsInNotVirtulization(int itemCount)
        {
            var component = RenderComponent<BitBasicListTest>(parameters => parameters
                .Add(p => p.Items, GetTestData(itemCount)));
            var bitList = component.Find(".bit-bsc-lst");
            int comItemCount = bitList.ChildElementCount;

            Assert.AreEqual(comItemCount + 2, itemCount + 2);
        }

        [DataTestMethod, DataRow(1000, "RoleItem", true)]
        public void BitBasicListCheckForCorrectRole(int itemCount, string role, bool virtualize)
        {
            var component = RenderComponent<BitBasicListTest>(parameters => parameters
                .Add(p => p.Items, GetTestData(itemCount))
                .Add(p => p.Role, role)
                .Add(p => p.Virtualize, virtualize)
            );
            var bitList = component.Find(".bit-bsc-lst");
            var itemRole = bitList.GetAttribute("role");

            Assert.AreEqual(role, itemRole);
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
