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

        [DataTestMethod, DataRow(10, true), DataRow(10, false)]
        //When we use virtualization, two additional parts are created,
        //that's why this method use itemCount +2
        public void BitBasicListShoudRenderExpectedChildElements(int itemCount, bool virtualize)
        {
            var component = RenderComponent<BitBasicListTest>();
            var bitList = component.Find(".bit-basic-list");
            int comItemCount = bitList.ChildElementCount;

            if (virtualize)
            {
                Assert.AreEqual(comItemCount + 2, itemCount + 2);
            }
            else
            {
                Assert.AreEqual(comItemCount, itemCount);
            }
        }

        [DataTestMethod, DataRow("RoleItem", true)]
        public void BitBasicListCheckForCorrectRole(string role, bool virtualize)
        {
            var component = RenderComponent<BitBasicListTest>(parameters => parameters
            .Add(p => p.Role, role)
            .Add(p => p.Virtualize, virtualize)
            );
            var bitList = component.Find(".bit-basic-list");
            var itemRole = bitList.GetAttribute("role");

            Assert.AreEqual(role, itemRole);
        }


        //[DataTestMethod]
        //[DynamicData(nameof(GetTestData), DynamicDataSourceType.Method)]
        //public void MyTest(IEnumerable<string> myStrings,int itemCount)
        //{
        //    var component = RenderComponent<BitBasicListTest>();
        //    var bitList = component.Find(".bit-basic-list");
        //    int comItemCount = bitList.ChildElementCount;

        //    Assert.AreEqual(comItemCount + 2, itemCount + 2);
        //}

        //public static IEnumerable<object[]> GetTestData()
        //{
        //    yield return new object[] {
        //        new List<Person>() {
        //            new Person{ FirstName="1",LastName="1",Job="1",Id=1}
        //        }
        //    };
        //}


    }
}
