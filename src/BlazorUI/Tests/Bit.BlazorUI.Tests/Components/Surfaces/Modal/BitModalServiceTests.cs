using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

[TestClass]
public class BitModalServiceTests : BunitTestContext
{
    private BitModalService ModalService => Services.GetRequiredService<BitModalService>();

    [TestInitialize]
    public void SetupServices()
    {
        Services.AddSingleton<BitModalService>();
    }

    [TestMethod]
    public async Task BitModalServiceShouldRenderModalInContainer()
    {
        var message = "Hello modal";

        var container = RenderComponent<BitModalContainer>();

        await ModalService.Show<TestModalContent>(new Dictionary<string, object>
        {
            { nameof(TestModalContent.Message), message }
        });

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
            Assert.IsTrue(container.Markup.Contains(message));
        });
    }

    [TestMethod]
    public async Task BitModalServiceShouldCloseModal()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
        });

        modalRef.Close();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, container.FindAll(".bit-mdl").Count);
        });
    }

    [TestMethod]
    public async Task BitModalContainerShouldApplyBasicModalParameters()
    {
        var container = RenderComponent<BitModalContainer>(parameters =>
        {
            parameters.Add(p => p.ModalParameters, new BitModalParameters { FullWidth = true });
        });

        await ModalService.Show<TestModalContent>();

        container.WaitForAssertion(() =>
        {
            Assert.IsTrue(container.Find(".bit-mdl").ClassList.Contains("bit-mdl-fwi"));
        });
    }

    [TestMethod]
    public async Task BitModalReferenceShouldCompleteItsResultWithWhatItWasClosedWith()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        Assert.IsFalse(modalRef.Result.IsCompleted);

        await modalRef.CloseWith("answered");

        Assert.AreEqual("answered", await modalRef.Result);
        Assert.IsTrue(modalRef.IsClosed);
    }

    [TestMethod]
    public async Task BitModalReferenceShouldCompleteItsResultWithNullWhenItIsClosedWithoutOne()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        await modalRef.Close();

        Assert.IsNull(await modalRef.Result);
    }

    [TestMethod]
    public async Task BitModalReferenceShouldKeepTheFirstResultItWasClosedWith()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        await modalRef.CloseWith("first");
        await modalRef.CloseWith("second");

        // Only the first answer is the answer: a modal can be asked to close more than once.
        Assert.AreEqual("first", await modalRef.Result);
    }

    [TestMethod]
    public async Task BitModalServiceShouldCompleteTheResultOfAModalItClosesThroughTheReference()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        await ModalService.Close(modalRef, 42);

        Assert.AreEqual(42, await modalRef.Result);
    }

    [TestMethod]
    public async Task BitModalServiceShouldCloseEveryOpenModal()
    {
        var container = RenderComponent<BitModalContainer>();

        var first = await ModalService.Show<TestModalContent>();
        var second = await ModalService.Show<TestModalContent>();

        container.WaitForAssertion(() => Assert.AreEqual(2, container.FindAll(".bit-mdl").Count));

        await ModalService.CloseAll();

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));

        Assert.IsTrue(first.IsClosed);
        Assert.IsTrue(second.IsClosed);
        Assert.IsNull(await first.Result);
        Assert.IsNull(await second.Result);
    }

    [TestMethod]
    public async Task BitModalServiceShouldCloseThePersistentModalsItIsStillTracking()
    {
        // Shown before any container mounted, so the service - not a container - is what is holding it.
        var modalRef = await ModalService.Show<TestModalContent>(persistent: true);

        await ModalService.CloseAll();

        Assert.IsTrue(modalRef.IsClosed);

        var container = RenderComponent<BitModalContainer>();

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public async Task BitModalServiceCloseAllShouldBeANoOpWithNothingOpen()
    {
        RenderComponent<BitModalContainer>();

        await ModalService.CloseAll();
    }

    [TestMethod]
    public async Task BitModalServiceShouldDismissAModalWithTheEscapeKey()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        container.Find(".bit-mdl").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));

        Assert.IsTrue(modalRef.IsClosed);
        Assert.IsNull(await modalRef.Result);
    }

    [TestMethod]
    public async Task BitModalServiceShouldNotDismissABlockingModalOnAnOverlayClick()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters { Blocking = true });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        container.Find(".bit-mdl-ovl").Click();

        Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
        Assert.IsFalse(modalRef.IsClosed);
    }

    [TestMethod]
    public async Task BitModalServiceShouldRenderPersistentModalAfterContainerInit()
    {
        var modalRef = await ModalService.Show<TestModalContent>(persistent: true);

        var container = RenderComponent<BitModalContainer>();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
        });

        Assert.IsTrue(modalRef.Persistent);
    }
}
