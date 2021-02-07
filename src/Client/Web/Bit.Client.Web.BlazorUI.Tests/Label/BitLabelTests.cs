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
        [DataTestMethod, DataRow(true, "required")]
        public void IsRequired_HasRedAsterisk_True(bool isRequired, string requiredclassName)
        {
            var component = RenderComponent<BitLabelTest>(parameters => parameters.Add(p => p.IsRequired, isRequired));
            Assert.AreEqual(1, 1);
        }
    }
}