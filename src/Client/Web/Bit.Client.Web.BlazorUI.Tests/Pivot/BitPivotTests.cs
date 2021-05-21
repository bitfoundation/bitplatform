using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Client.Web.BlazorUI.Tests.Pivot
{
    [TestClass]
    public class BitPivotTests : BunitTestContext
    {
        [DataTestMethod,
         DataRow(Visual.Fluent, LinkFormat.Links),
         DataRow(Visual.Fluent, LinkFormat.Tabs),

         DataRow(Visual.Cupertino, LinkFormat.Links),
         DataRow(Visual.Cupertino, LinkFormat.Tabs),

         DataRow(Visual.Material, LinkFormat.Links),
         DataRow(Visual.Material, LinkFormat.Tabs)]

        public Task BitPivotShouldRepectLinkFormatClasses(Visual visual, LinkFormat linkFormat)
        {
            var component = RenderComponent<BitPivot>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.LinkFormat, linkFormat);
            });

            var visualClass = visual.ToString().ToLower();
            var LinkFormatClass = $"bit-pvt-{linkFormat.ToString().ToLower()}-{visualClass}";

            var bitPivot = component.Find($".bit-pvt");

            Assert.IsTrue(bitPivot.ClassList.Contains(LinkFormatClass));

            return Task.CompletedTask;
        }

        [DataTestMethod,
         DataRow(Visual.Fluent, LinkSize.Large),
         DataRow(Visual.Fluent, LinkSize.Normal),

         DataRow(Visual.Cupertino, LinkSize.Large),
         DataRow(Visual.Cupertino, LinkSize.Normal),

         DataRow(Visual.Material, LinkSize.Large),
         DataRow(Visual.Material, LinkSize.Normal)]
        public Task BitPivotShouldRepectLinkSizeClasses(Visual visual, LinkSize linkSize)
        {
            var component = RenderComponent<BitPivot>(parameters =>
            {
                parameters.Add(p => p.Visual, visual);
                parameters.Add(p => p.LinkSize, linkSize);
            });

            var visualClass = visual.ToString().ToLower();
            var linkSizeClass = $"bit-pvt-{linkSize.ToString().ToLower()}-{visualClass}";

            var bitPivot = component.Find($".bit-pvt");

            Assert.IsTrue(bitPivot.ClassList.Contains(linkSizeClass));

            return Task.CompletedTask;
        }
    }
}
