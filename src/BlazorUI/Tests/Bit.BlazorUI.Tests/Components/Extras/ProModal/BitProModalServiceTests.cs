using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.ProModal;

[TestClass]
public class BitProModalServiceTests : BunitTestContext
{
    private BitProModalService ModalService => Services.GetRequiredService<BitProModalService>();

    [TestInitialize]
    public void SetupServices()
    {
        Services.AddSingleton<BitProModalService>();
    }

    [TestMethod]
    public async Task BitProModalServiceShouldRenderModalInContainer()
    {
        var message = "Hello promodal";

        var container = RenderComponent<BitProModalContainer>();

        await ModalService.Show<TestProModalContent>(new Dictionary<string, object>
        {
            { nameof(TestProModalContent.Message), message }
        });

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-pmd").Count);
            Assert.AreEqual(message, container.Find(".test-promodal-content").TextContent);
        });
    }

    [TestMethod]
    public async Task BitProModalServiceShouldCloseModal()
    {
        var container = RenderComponent<BitProModalContainer>();

        var modalRef = await ModalService.Show<TestProModalContent>();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-pmd").Count);
        });

        await modalRef.Close();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, container.FindAll(".bit-pmd").Count);
        });
    }

    [TestMethod]
    public async Task BitProModalContainerShouldApplyContainerLevelModalParameters()
    {
        var container = RenderComponent<BitProModalContainer>(parameters =>
        {
            parameters.Add(p => p.ModalParameters, new BitProModalParameters { FullWidth = true });
        });

        await ModalService.Show<TestProModalContent>();

        container.WaitForAssertion(() =>
        {
            Assert.IsTrue(container.Find(".bit-mdl").ClassList.Contains("bit-mdl-fwi"));
        });
    }

    [TestMethod]
    public async Task BitProModalServiceShouldRenderHeaderAndCloseButtonFromParameters()
    {
        var container = RenderComponent<BitProModalContainer>();

        await ModalService.Show<TestProModalContent>(new BitProModalParameters
        {
            HeaderText = "the header",
            ShowCloseButton = true
        });

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual("the header", container.Find(".bit-pmd-hdr").TextContent);
            Assert.AreEqual(1, container.FindAll(".bit-pmd-cls").Count);
        });
    }

    [TestMethod]
    public async Task BitProModalServiceShouldRenderPersistentModalAfterContainerInit()
    {
        var modalRef = await ModalService.Show<TestProModalContent>(persistent: true);

        var container = RenderComponent<BitProModalContainer>();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-pmd").Count);
        });

        Assert.IsTrue(modalRef.Persistent);
    }

    [TestMethod]
    public async Task BitProModalServiceShouldNotReinjectClosedPersistentModalAfterRemount()
    {
        var modalRef = await ModalService.Show<TestProModalContent>(persistent: true);

        var container = RenderComponent<BitProModalContainer>();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-pmd").Count));

        await modalRef.Close();
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-pmd").Count));

        // Simulate a container remount: a closed persistent modal must not reappear.
        Context.DisposeComponents();
        var container2 = RenderComponent<BitProModalContainer>();

        container2.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, container2.FindAll(".bit-pmd").Count);
        });
    }
}
