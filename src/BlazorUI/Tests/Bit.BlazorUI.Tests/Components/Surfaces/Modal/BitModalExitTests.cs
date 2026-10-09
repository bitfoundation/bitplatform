using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

/// <summary>
/// A Modal leaves the page the way it came in: closed at once - inert, hidden from assistive technologies, the
/// page behind it handed back - and only taken out of the page once its exit animation has played, which the
/// script reports (waitForAnimations). Each test holds that report back to look at the Modal on its way out.
/// </summary>
[TestClass]
public class BitModalExitTests : BunitTestContext
{
    private const string WaitForAnimations = "BitBlazorUI.Utils.waitForAnimations";

    private BitModalService ModalService => Services.GetRequiredService<BitModalService>();

    [TestInitialize]
    public void SetupServices()
    {
        Services.AddSingleton<BitModalService>();
    }

    [TestMethod]
    public async Task BitModalShouldStayInThePageClosedUntilItsExitAnimationHasPlayed()
    {
        var animation = Context.JSInterop.SetupVoid(WaitForAnimations, _ => true);

        var dismissed = 0;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.OnDismiss, EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++));
            parameters.Add(p => p.ChildContent, (RenderFragment)(b => b.AddContent(0, "Content")));
        });

        await com.InvokeAsync(() => com.Instance.Close());

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations[WaitForAnimations].Count));

        // Closed already: out of the tab order, out of the reading order, and reported dismissed.
        var root = com.Find(".bit-mdl");
        Assert.IsTrue(root.ClassList.Contains("bit-mdl-lvg"));
        Assert.IsTrue(root.HasAttribute("inert"));
        Assert.AreEqual("true", root.GetAttribute("aria-hidden"));
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocus"].Count);

        // Still in the page, so a focus the browser has not moved out of it yet counts as lost.
        Assert.AreEqual(root.Id, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocus"][0].Arguments[2]);
        com.WaitForAssertion(() => Assert.AreEqual(1, dismissed));

        await com.InvokeAsync(() => animation.SetVoidResult());

        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public async Task BitModalOpenedAgainOnItsWayOutShouldSimplyStay()
    {
        var animation = Context.JSInterop.SetupVoid(WaitForAnimations, _ => true);

        var com = RenderComponent<BitModal>(parameters => parameters.Add(p => p.DefaultIsOpen, true));

        await com.InvokeAsync(() => com.Instance.Close());

        com.WaitForAssertion(() => Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("bit-mdl-lvg")));

        await com.InvokeAsync(() => com.Instance.Open());

        // The animation of the closing that was overtaken ending later takes nothing away.
        await com.InvokeAsync(() => animation.SetVoidResult());

        var root = com.Find(".bit-mdl");
        Assert.IsFalse(root.ClassList.Contains("bit-mdl-lvg"));
        Assert.IsFalse(root.HasAttribute("inert"));
    }

    [TestMethod]
    public async Task BitModalKeptMountedShouldBeHiddenOnceItsExitAnimationHasPlayed()
    {
        var animation = Context.JSInterop.SetupVoid(WaitForAnimations, _ => true);

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        await com.InvokeAsync(() => com.Instance.Close());

        com.WaitForAssertion(() => Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("bit-mdl-lvg")));
        Assert.IsFalse(com.Find(".bit-mdl").ClassList.Contains("bit-mdl-hid"));

        await com.InvokeAsync(() => animation.SetVoidResult());

        com.WaitForAssertion(() =>
        {
            var root = com.Find(".bit-mdl");
            Assert.IsTrue(root.ClassList.Contains("bit-mdl-hid"));
            Assert.IsFalse(root.ClassList.Contains("bit-mdl-lvg"));
        });
    }

    [TestMethod]
    public async Task BitModalDraggedShouldKeepItsPlaceUntilItsExitAnimationHasPlayed()
    {
        var animation = Context.JSInterop.SetupVoid(WaitForAnimations, _ => true);

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.Draggable, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.setup"].Count));

        await com.InvokeAsync(() => com.Instance.Close());

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations[WaitForAnimations].Count));

        // Taking the drag handlers away puts the Modal back where it was laid out, which is not where it is to fade.
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"].Count);

        await com.InvokeAsync(() => animation.SetVoidResult());

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.DragDrop.remove"].Count));
        com.WaitForAssertion(() => Assert.AreEqual(0, com.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public void BitModalThatWasNeverOnTheScreenShouldHaveNothingToAnimate()
    {
        var com = RenderComponent<BitModal>();

        com.Render(parameters => parameters.Add(p => p.IsOpen, false));

        Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations[WaitForAnimations].Count);
    }

    [TestMethod]
    public async Task BitModalServiceShouldKeepAClosedModalInThePageUntilItsExitAnimationHasPlayed()
    {
        var animation = Context.JSInterop.SetupVoid(WaitForAnimations, _ => true);

        var dismissed = 0;

        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++),
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        await modalRef.Close();

        container.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations[WaitForAnimations].Count));

        // The service is done with it: closed, no longer one of the open modals - only still on its way out.
        Assert.IsTrue(modalRef.IsClosed);
        Assert.AreEqual(0, ModalService.OpenModals.Count);
        var root = container.Find(".bit-mdl");
        Assert.IsTrue(root.ClassList.Contains("bit-mdl-lvg"));
        Assert.IsTrue(root.HasAttribute("inert"));

        // The application closed it, which is not a dismissal.
        Assert.AreEqual(0, dismissed);

        await container.InvokeAsync(() => animation.SetVoidResult());

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public async Task BitModalServiceShouldAnimateAModalTheUserDismissedOutOnce()
    {
        var animation = Context.JSInterop.SetupVoid(WaitForAnimations, _ => true);

        var dismissed = 0;

        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++),
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        _ = container.PressEscape();

        container.WaitForAssertion(() => Assert.IsTrue(container.Find(".bit-mdl").ClassList.Contains("bit-mdl-lvg")));
        Assert.IsTrue(modalRef.IsClosed);
        Assert.IsTrue(modalRef.IsDismissed);
        container.WaitForAssertion(() => Assert.AreEqual(1, dismissed));

        await container.InvokeAsync(() => animation.SetVoidResult());

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));
        Assert.AreEqual(1, Context.JSInterop.Invocations[WaitForAnimations].Count);
        Assert.AreEqual(1, dismissed);
    }

    // bUnit gives up on a script call left unanswered for longer than its wait timeout, which is process-wide, so the
    // test that holds one past the container's deadline raises it and runs on its own.
    [TestMethod, DoNotParallelize]
    public async Task BitModalServiceShouldHearOutAModalThatStartedClosingPastItsOwnDeadline()
    {
        var waitTimeout = BunitContext.DefaultWaitTimeout;

        try
        {
            BunitContext.DefaultWaitTimeout = TimeSpan.FromSeconds(10);

            // The close sequence makes its round trips to the browser before the animation starts; on a slow circuit
            // those alone can outlast the deadline the container keeps for a modal that never starts closing.
            var animation = Context.JSInterop.SetupVoid(WaitForAnimations, _ => true);

            var container = RenderComponent<BitModalContainer>();

            var modalRef = await ModalService.Show<TestModalContent>();

            container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

            await modalRef.Close();

            container.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations[WaitForAnimations].Count));

            await Task.Delay(TimeSpan.FromSeconds(2));

            // Still playing its way out: the modal, not the container's deadline, says when it is out of the way.
            Assert.IsTrue(container.Find(".bit-mdl").ClassList.Contains("bit-mdl-lvg"));

            await container.InvokeAsync(() => animation.SetVoidResult());

            container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));
        }
        finally
        {
            BunitContext.DefaultWaitTimeout = waitTimeout;
        }
    }

    [TestMethod]
    public async Task BitModalServiceShouldTakeAModalOutAtOnceFromAContainerThatCannotHearItOut()
    {
        var dismissed = 0;

        var container = RenderComponent<HandWrittenModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissed++),
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        await modalRef.Close();

        // A container a consumer wrote hands the modal no BitModalExit, so it has no way of hearing that the modal
        // played its way out: the modal goes at once, as it always has, and the application closing it is still not
        // reported as a dismissal.
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));
        Assert.AreEqual(0, dismissed);
        Assert.AreEqual(0, Context.JSInterop.Invocations[WaitForAnimations].Count);
        Assert.AreEqual(0, ModalService.OpenModals.Count);
    }

    // A container written by hand the way a consumer outside the library writes one: it can cascade the reference and
    // the parameters, but not the BitModalExit, which is internal to the library.
    private sealed class HandWrittenModalContainer : BitModalContainerBase<BitModalReference, BitModalParameters>
    {
        [Inject] private BitModalService _modalService { get; set; } = default!;

        protected override BitModalServiceBase<BitModalReference, BitModalParameters> ModalService => _modalService;

        protected override BitModalParameters? MergeParameters(BitModalParameters? modalParameters, BitModalParameters? containerParameters)
        {
            return BitModalParameters.Merge(modalParameters, containerParameters);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            foreach (var modalRef in _modalRefs)
            {
                builder.OpenComponent<CascadingValue<BitModalReference>>(0);
                builder.SetKey(modalRef.Id);
                builder.AddComponentParameter(1, nameof(CascadingValue<BitModalReference>.Value), modalRef);
                builder.AddComponentParameter(2, nameof(CascadingValue<BitModalReference>.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<CascadingValue<BitModalParameters>>(0);
                    b.AddComponentParameter(1, nameof(CascadingValue<BitModalParameters>.Value), GetMergedParameters(modalRef));
                    b.AddComponentParameter(2, nameof(CascadingValue<BitModalParameters>.ChildContent), modalRef.Modal);
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }
    }

    [TestMethod]
    public async Task BitModalDeclaredInsideAServiceModalShouldNotBeMistakenForIt()
    {
        var animation = Context.JSInterop.SetupVoid(WaitForAnimations, _ => true);

        var container = RenderComponent<BitModalContainer>();

        RenderFragment content = b =>
        {
            b.OpenComponent<BitModal>(0);
            b.AddComponentParameter(1, nameof(BitModal.DefaultIsOpen), true);
            b.AddComponentParameter(2, nameof(BitModal.Classes), new BitModalClassStyles { Root = "inner" });
            b.CloseComponent();
        };

        var modalRef = await ModalService.Show(content);

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".inner").Count));

        var inner = container.FindComponents<BitModal>()[1];
        await container.InvokeAsync(() => inner.Instance.Close());
        await container.InvokeAsync(() => animation.SetVoidResult());

        // The inner Modal closing is not the service modal leaving: that one stays open and on the screen.
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".inner").Count));
        Assert.IsFalse(modalRef.IsClosed);
        Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
        Assert.AreEqual(1, ModalService.OpenModals.Count);
    }
}
