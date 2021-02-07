using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Tests.Label
{
    [TestClass]
    public class BitLabelTests: BunitTestContext
    {
        [TestMethod]
        public void IsRequired_HasRequiredClass_True()
        {
            var component = RenderComponent<BitLabelTest>(parameters => parameters.Add(p => p.IsRequired, true));
            var bitLabel = component.Find(".bit-label");
            Assert.AreEqual(true,bitLabel.ClassList.Contains("required"));
        }
    }
}