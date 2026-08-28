using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
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

        var closeCount = 0;
        ModalService.OnCloseModal += _ => { closeCount++; return Task.CompletedTask; };

        var modalRef = await ModalService.Show<TestModalContent>();

        await modalRef.CloseWith("first");
        await modalRef.CloseWith("second");

        // Only the first answer is the answer: a modal can be asked to close more than once.
        Assert.AreEqual("first", await modalRef.Result);

        // And only the first close is a close: the second one is not reported to the close handlers again.
        Assert.AreEqual(1, closeCount);
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


    [TestMethod]
    public async Task BitModalServiceShouldShowMarkupAsTheContentOfAModal()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "markup-content");
            builder.AddContent(2, "Shown with markup");
            builder.CloseElement();
        }, new BitModalParameters { FullWidth = true });

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
            Assert.AreEqual(1, container.FindAll(".markup-content").Count);
            Assert.IsTrue(container.Find(".bit-mdl").ClassList.Contains("bit-mdl-fwi"));
        });

        // Markup is not a component instance, so there is none to hand back on the reference.
        Assert.IsNull(modalRef.Content);

        await modalRef.Close();

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public async Task BitModalServiceShouldRefuseToShowNothingAsMarkup()
    {
        RenderComponent<BitModalContainer>();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => ModalService.Show((RenderFragment)null!));
    }

    [TestMethod]
    public async Task BitModalServiceShouldCompleteTheResultOfAMarkupModalItIsClosedWith()
    {
        RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show(builder => builder.AddContent(0, "Shown with markup"));

        await modalRef.CloseWith("answered");

        Assert.AreEqual("answered", await modalRef.Result);
    }

    [TestMethod]
    public async Task BitModalServiceShouldRenderTheChromeFromTheParametersItWasShownWith()
    {
        var container = RenderComponent<BitModalContainer>();

        await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            HeaderText = "the header",
            FooterText = "the footer",
            ShowCloseButton = true
        });

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual("the header", container.Find(".bit-mdl-hdr").TextContent);
            Assert.AreEqual("the footer", container.Find(".bit-mdl-fcn").TextContent);
            Assert.AreEqual(1, container.FindAll(".bit-mdl-cls").Count);
        });
    }

    [TestMethod]
    public async Task BitModalServiceShouldCloseTheModalFromItsCloseButton()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters { ShowCloseButton = true });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        container.Find(".bit-mdl-cls").Click();

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(0, container.FindAll(".bit-mdl").Count);
            Assert.IsTrue(modalRef.IsClosed);
        });
    }
}
