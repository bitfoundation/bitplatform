using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Pivot
{
    [TestClass]
    public class BitPivotTests : BunitTestContext
    {
        [DataTestMethod,
         DataRow(Visual.Fluent, LinkFormat.Links, LinkSize.Large, OverflowBehavior.None),
         DataRow(Visual.Fluent, LinkFormat.Tabs, LinkSize.Normal, OverflowBehavior.Scroll),
         DataRow(Visual.Fluent, LinkFormat.Tabs, LinkSize.Normal, OverflowBehavior.Menu),

         DataRow(Visual.Cupertino, LinkFormat.Links, LinkSize.Large, OverflowBehavior.None),
         DataRow(Visual.Cupertino, LinkFormat.Tabs, LinkSize.Normal, OverflowBehavior.Scroll),
         DataRow(Visual.Cupertino, LinkFormat.Tabs, LinkSize.Normal, OverflowBehavior.Menu),

         DataRow(Visual.Material, LinkFormat.Links, LinkSize.Large, OverflowBehavior.None),
         DataRow(Visual.Material, LinkFormat.Tabs, LinkSize.Normal, OverflowBehavior.Scroll),
         DataRow(Visual.Material, LinkFormat.Tabs, LinkSize.Normal, OverflowBehavior.Menu)]
        public void BitPivotShouldRepectLinkFormatClasses(Visual visual, LinkFormat linkFormat, LinkSize linkSize, OverflowBehavior overflowBehavior)
        {
            var component = RenderComponent<BitPivotTest>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.LinkFormat, linkFormat);
                parameters.Add(p => p.LinkSize, linkSize);
                parameters.Add(p => p.OverflowBehavior, overflowBehavior);
            });

            var visualClass = visual.ToString().ToLower();

            var linkFormatClass = $"bit-pvt-{linkFormat.ToString().ToLower()}-{visualClass}";
            var linkSizeClass = $"bit-pvt-{linkSize.ToString().ToLower()}-{visualClass}";
            var overflowBehaviorClass = $"bit-pvt-{overflowBehavior.ToString().ToLower()}-{visualClass}";

            var bitPivot = component.Find($".bit-pvt");

            Assert.IsTrue(bitPivot.ClassList.Contains(linkFormatClass));
            Assert.IsTrue(bitPivot.ClassList.Contains(linkSizeClass));
            Assert.IsTrue(bitPivot.ClassList.Contains(overflowBehaviorClass));
        }


        [DataTestMethod,
         DataRow(false, false),
         DataRow(true, true)]
        public void BitPivotShouldRespectSelectKey(bool isEnabled, bool expectedResult)
        {
            var component = RenderComponent<BitPivotTest>(parameters =>
            {
                parameters.AddCascadingValue(this);
                parameters.AddChildContent<BitPivotItem>();
                parameters.AddChildContent<BitPivotItem>(parameters => parameters.Add(p => p.IsEnabled, isEnabled));
            });

            //component.FindAll(".bit-pvt > div:first-child > div")[1].Click();
            
            //TODO: bypassed - BUnit 2-way bound parameters issue
            //Assert.AreEqual(component.FindAll(".bit-pvt > div:first-child > div")[1].ClassList.Contains("selected-item"), expectedResult);
        }
    }
}
