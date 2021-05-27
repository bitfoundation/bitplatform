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
        public async Task BitBasicListShoudReturnExpectedChildElements(int itemCount)
        {
            var component = RenderComponent<BitBasicListTest>();

            var bitList = component.Find(".bit-basic-list");
            int comItemCount = bitList.ChildElementCount;

            Assert.AreEqual(comItemCount, itemCount + 1);
        }
        [DataTestMethod, DataRow(true)]
        public async Task BitBasicListCheckWithVirtualize(bool vitualize)
        {
            var component = RenderComponent<BitBasicListTest>();
            var bitList = component.Find(".bit-basic-list");

            var height = bitList.FirstElementChild.GetAttribute("style").Split(":")[1].Replace("px;", "");

            Assert.AreEqual("0", height.Trim());
        }


        [DataTestMethod, DataRow(false, "testItem")]
        public async Task BitBasicListCheckWithoutVirtualize(bool vitualize, string childClass)
        {
            var component = RenderComponent<BitBasicListTest>(parameters => parameters.Add(p => p.Virtualize, vitualize));
            var bitList = component.Find(".bit-basic-list");

            bool vitualizeIsActive = true;
            var itemCalss = bitList.FirstElementChild.FirstElementChild.GetAttribute("class");
            if (itemCalss == childClass && bitList.FirstElementChild.GetAttribute("style") == null && bitList.FirstElementChild.GetAttribute("class") == null)
                vitualizeIsActive = false;

            Assert.AreEqual(vitualize, vitualizeIsActive);
        }

        [DataTestMethod, DataRow("RoleItem")]
        public async Task BitBasicListCheckForCorrectRole(string role)
        {
            var component = RenderComponent<BitBasicListTest>(parameters => parameters.Add(p => p.Role, role));
            var bitList = component.Find(".bit-basic-list");

            var itemRole = bitList.GetAttribute("role");
            
            Assert.AreEqual(role,itemRole);
        }
    }
}
