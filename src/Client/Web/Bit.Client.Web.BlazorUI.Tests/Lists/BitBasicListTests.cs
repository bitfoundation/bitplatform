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
        [DataTestMethod, DataRow(10)]
        public void BitBasicListShoudRenderExpectedChildElements(int itemCount)
        {
            var component = RenderComponent<BitBasicListTest>();

            var bitList = component.Find(".bit-basic-list");
            bitList.RemoveChild(bitList.FirstElementChild);
            bitList.RemoveChild(bitList.LastElementChild);
            int comItemCount = bitList.ChildElementCount;

            Assert.AreEqual(comItemCount, itemCount);
        }

        [DataTestMethod, DataRow(true)]
        public void BitBasicListCheckUsingVirtualization(bool vitualize)
        {
            var component = RenderComponent<BitBasicListTest>();
            var bitList = component.Find(".bit-basic-list");

            var height = bitList.FirstElementChild.GetAttribute("style").Split(":")[1].Replace("px;", "");

            Assert.AreEqual("0", height.Trim());
        }

        [DataTestMethod, DataRow("testItem")]
        public void BitBasicListCheckWithoutVirtualization(string childClass)
        {
            var component = RenderComponent<BitBasicListTest>(parameters => parameters.Add(p => p.Virtualize, false));
            var bitList = component.Find(".bit-basic-list");

            bool vitualizeIsActive = true;
            var itemClass = bitList.FirstElementChild.FirstElementChild.GetAttribute("class");
            if (itemClass == childClass && bitList.FirstElementChild.GetAttribute("style") == null && bitList.FirstElementChild.GetAttribute("class") == null)
            {
                vitualizeIsActive = false;
            }

            Assert.AreEqual(false, vitualizeIsActive);
        }

        [DataTestMethod, DataRow("RoleItem")]
        public void BitBasicListCheckForCorrectRole(string role)
        {
            var component = RenderComponent<BitBasicListTest>(parameters => parameters.Add(p => p.Role, role));
            var bitList = component.Find(".bit-basic-list");
            var itemRole = bitList.GetAttribute("role");

            Assert.AreEqual(role, itemRole);
        }
    }
}
