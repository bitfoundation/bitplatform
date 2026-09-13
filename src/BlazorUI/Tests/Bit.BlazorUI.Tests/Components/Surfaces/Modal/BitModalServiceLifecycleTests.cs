using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

/// <summary>
/// The lifecycle half of the modal service: what a reference reports about a modal that has been shown, what
/// closes a modal and what is allowed to refuse that, and what happens to the modals a container leaves behind.
/// </summary>
[TestClass]
public class BitModalServiceLifecycleTests : BunitTestContext
{
    private BitModalService ModalService => Services.GetRequiredService<BitModalService>();

    [TestInitialize]
    public void SetupServices()
    {
        Services.AddSingleton<BitModalService>();
    }



    [TestMethod]
    public async Task BitModalServiceShouldShowAComponentTypeOnlyKnownAtRunTime()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show(typeof(TestModalContent), new Dictionary<string, object>
        {
            { nameof(TestModalContent.Message), "runtime type" }
        });

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
            Assert.IsTrue(container.Markup.Contains("runtime type"));
        });

        Assert.IsFalse(modalRef.IsClosed);
    }

    [TestMethod]
    public async Task BitModalServiceShouldRefuseAContentTypeThatIsNotAComponent()
    {
        RenderComponent<BitModalContainer>();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => ModalService.Show(typeof(string)));
    }

    [TestMethod]
    public async Task BitModalServiceShouldListTheModalsItHasOpenInTheOrderTheyWereOpened()
    {
        RenderComponent<BitModalContainer>();

        Assert.AreEqual(0, ModalService.OpenModals.Count);

        var first = await ModalService.Show<TestModalContent>();
        var second = await ModalService.Show<TestModalContent>();

        CollectionAssert.AreEqual(new[] { first, second }, ModalService.OpenModals.ToArray());

        await first.Close();

        CollectionAssert.AreEqual(new[] { second }, ModalService.OpenModals.ToArray());
    }

    [TestMethod]
    public async Task BitModalServiceShouldListAPersistentModalItIsHoldingWithNoContainerYet()
    {
        var modalRef = await ModalService.Show<TestModalContent>(persistent: true);

        // Open, only not on the screen yet: no container has mounted to render it.
        CollectionAssert.AreEqual(new[] { modalRef }, ModalService.OpenModals.ToArray());
    }

    [TestMethod]
    public async Task BitModalServiceShouldFindAnOpenModalByItsId()
    {
        RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        Assert.AreSame(modalRef, ModalService.GetModal(modalRef.Id));
        Assert.IsNull(ModalService.GetModal("not-an-id"));
        Assert.IsNull(ModalService.GetModal(null));

        await modalRef.Close();

        Assert.IsNull(ModalService.GetModal(modalRef.Id));
    }

    [TestMethod]
    public async Task BitModalReferenceShouldReportItHasBeenRenderedAndHandBackItsContent()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new Dictionary<string, object>
        {
            { nameof(TestModalContent.Message), "the content" }
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        Assert.IsTrue(await modalRef.Rendered);

        var content = await modalRef.GetContentAsync<TestModalContent>();

        Assert.IsNotNull(content);
        Assert.AreEqual("the content", content.Message);
        Assert.AreSame(modalRef.Content, content);
    }

    [TestMethod]
    public async Task BitModalReferenceShouldLetGoOfWhoeverWaitsForAModalThatNeverRenders()
    {
        // No container is mounted, so this modal is never rendered - and whoever is waiting for its content
        // has to be let go rather than left waiting on a render that is not coming.
        var modalRef = await ModalService.Show<TestModalContent>();

        await modalRef.Close();

        Assert.IsFalse(await modalRef.Rendered);
        Assert.IsNull(await modalRef.GetContentAsync<TestModalContent>());
    }

    [TestMethod]
    public async Task BitModalReferenceShouldLetGoOfWhoeverWaitsForAModalShownWithNothingToRenderIt()
    {
        // A modal shown while no container is mounted is never going to be rendered, and nothing is going to
        // close it either - it is not tracked anywhere. Whoever is waiting on its content has to be let go
        // there and then, rather than on a close that nobody is ever going to call.
        var modalRef = await ModalService.Show<TestModalContent>();

        // Bounded, so that a modal left waiting fails this test instead of hanging it.
        await Task.WhenAny(modalRef.Rendered, Task.Delay(2000));

        Assert.IsTrue(modalRef.Rendered.IsCompleted);
        Assert.IsFalse(await modalRef.Rendered);
        Assert.IsNull(await modalRef.GetContentAsync<TestModalContent>());

        // And nobody closed it to get that answer.
        Assert.IsFalse(modalRef.IsClosed);
    }

    [TestMethod]
    public async Task BitModalReferenceShouldHandBackItsResultTyped()
    {
        RenderComponent<BitModalContainer>();

        var confirmed = await ModalService.Show<TestModalContent>();
        await confirmed.CloseWith(true);
        Assert.IsTrue(await confirmed.GetResult<bool>());

        // A modal dismissed rather than answered has no result, so the type's default is what it answers with
        // instead of throwing on the null.
        var dismissed = await ModalService.Show<TestModalContent>();
        await dismissed.Close();
        Assert.IsFalse(await dismissed.GetResult<bool>());

        // And so does one answered with something else entirely.
        var other = await ModalService.Show<TestModalContent>();
        await other.CloseWith("not a bool");
        Assert.AreEqual(0, await other.GetResult<int>());
    }

    [TestMethod]
    public async Task BitModalReferenceShouldReportADismissalApartFromAProgrammaticClose()
    {
        var container = RenderComponent<BitModalContainer>();

        var dismissed = await ModalService.Show<TestModalContent>();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));
        container.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        container.WaitForAssertion(() => Assert.IsTrue(dismissed.IsClosed));
        Assert.IsTrue(dismissed.IsDismissed);

        var closed = await ModalService.Show<TestModalContent>();
        await closed.CloseWith("answered");
        Assert.IsFalse(closed.IsDismissed);
    }

    [TestMethod]
    public async Task BitModalReferenceShouldReportTheContentsOwnCancelAsADismissal()
    {
        RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        Assert.IsTrue(await modalRef.Dismiss());
        Assert.IsTrue(modalRef.IsDismissed);
        Assert.IsNull(await modalRef.Result);
    }

    [TestMethod]
    public async Task BitModalServiceShouldKeepAModalWhoseGuardTurnsTheCloseDown()
    {
        var container = RenderComponent<BitModalContainer>();

        var asked = 0;
        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            CanClose = () => { asked++; return Task.FromResult(false); }
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        Assert.IsFalse(await modalRef.TryClose());

        Assert.AreEqual(1, asked);
        Assert.IsFalse(modalRef.IsClosed);
        Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public async Task BitModalServiceShouldCloseAModalWhoseGuardAllowsIt()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            CanClose = () => Task.FromResult(true)
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        Assert.IsTrue(await modalRef.TryClose("answered"));

        Assert.AreEqual("answered", await modalRef.Result);
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public async Task BitModalServiceShouldKeepAModalOnTheScreenWhenItsGuardTurnsDownAnEscape()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            CanClose = () => Task.FromResult(false)
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        container.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // The Modal takes itself off the screen before reporting the dismissal, so a refusal has to put it
        // back: the modal the user was not allowed to leave is still there.
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));
        Assert.IsFalse(modalRef.IsClosed);
    }

    [TestMethod]
    public async Task BitModalServiceShouldKeepAModalOnTheScreenWhenItsGuardTurnsDownTheCloseButton()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            ShowCloseButton = true,
            CanClose = () => Task.FromResult(false)
        });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        container.Find(".bit-mdl-cls").Click();

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));
        Assert.IsFalse(modalRef.IsClosed);
    }

    [TestMethod]
    public async Task BitModalServiceShouldKeepTheContentOfAModalWhoseGuardTurnsDownADismissal()
    {
        var container = RenderComponent<BitModalContainer>();

        var log = new List<string>();
        var asked = 0;
        var dismissals = 0;

        var modalRef = await ModalService.Show<TestModalStateContent>(
            new Dictionary<string, object> { { nameof(TestModalStateContent.Log), log } },
            new BitModalParameters
            {
                CanClose = () => { asked++; return Task.FromResult(false); },
                OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(this, () => dismissals++)
            });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));
        Assert.AreEqual(1, log.Count);

        container.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        // The guard has had its say by now, so whatever the dismissal was going to do it has done.
        container.WaitForAssertion(() => Assert.AreEqual(1, asked));
        await Task.Delay(100);

        // The half-filled form the guard is there to protect is the one the user was filling in: a dismissal
        // that was turned down never takes the content away and builds it again from scratch.
        Assert.AreEqual(1, log.Count);

        // And a dismissal that did not go through is not one the consumer is told about.
        Assert.AreEqual(0, dismissals);

        Assert.IsFalse(modalRef.IsClosed);
        Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public async Task BitModalServiceShouldNotOpenAModalAgainWhenItsAsyncGuardTurnsDownADismissal()
    {
        var container = RenderComponent<BitModalContainer>();

        var log = new List<string>();
        var asked = 0;
        var opens = 0;

        // A guard that has to go and ask - a confirmation of its own, a save still running - answers later
        // than the click that set it off, which is the case the modal has to stay put through.
        var modalRef = await ModalService.Show<TestModalStateContent>(
            new Dictionary<string, object> { { nameof(TestModalStateContent.Log), log } },
            new BitModalParameters
            {
                CanClose = async () => { await Task.Delay(1); asked++; return false; },
                OnOpen = EventCallback.Factory.Create(this, () => opens++)
            });

        container.WaitForAssertion(() => Assert.AreEqual(1, opens));

        container.Find(".bit-mdl-ovl").Click();

        container.WaitForAssertion(() => Assert.AreEqual(1, asked));
        await Task.Delay(100);

        // The modal never left the screen, so it never arrived on it a second time either: an opening
        // reported twice for one modal is a consumer running its opening work twice.
        Assert.AreEqual(1, opens);
        Assert.AreEqual(1, log.Count);

        Assert.IsFalse(modalRef.IsClosed);
        Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public async Task BitModalServiceShouldNotAskTheGuardWhenTheApplicationClosesTheModal()
    {
        RenderComponent<BitModalContainer>();

        var asked = false;
        var parameters = new BitModalParameters
        {
            CanClose = () => { asked = true; return Task.FromResult(false); }
        };

        var closed = await ModalService.Show<TestModalContent>(parameters);
        await closed.Close();
        Assert.IsTrue(closed.IsClosed);

        var closedAll = await ModalService.Show<TestModalContent>(parameters);
        await ModalService.CloseAll();
        Assert.IsTrue(closedAll.IsClosed);

        // A sign-out or a navigation is not something a half-filled form gets to turn down.
        Assert.IsFalse(asked);
    }

    [TestMethod]
    public async Task BitModalServiceShouldCloseItsModalsWhenTheAppNavigatesSomewhereElse()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        Services.GetRequiredService<NavigationManager>().NavigateTo("/somewhere-else");

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-mdl").Count));

        Assert.IsTrue(modalRef.IsClosed);
        Assert.IsNull(await modalRef.Result);
    }

    [TestMethod]
    public async Task BitModalServiceShouldKeepAModalThatAskedToOutliveTheNavigation()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters { CloseOnNavigation = false });

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        Services.GetRequiredService<NavigationManager>().NavigateTo("/somewhere-else");

        Assert.IsFalse(modalRef.IsClosed);
        Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public async Task BitModalServiceShouldKeepAPersistentModalWhenTheAppNavigatesSomewhereElse()
    {
        var container = RenderComponent<BitModalContainer>();

        // A persistent modal is the one thing that outlives the container it happens to be rendering in, so
        // it is not something a route change gets to end: closing it would take it out of the service's
        // hands for good, and nothing could ever bring it back.
        var persistent = await ModalService.Show<TestModalContent>(persistent: true);
        var ordinary = await ModalService.Show<TestModalContent>();

        // Being persistent is not the same as saying so, though: a persistent modal that asked to be closed
        // on a route change is closed on a route change.
        var persistentClosing = await ModalService.Show<TestModalContent>(new BitModalParameters { CloseOnNavigation = true }, persistent: true);

        container.WaitForAssertion(() => Assert.AreEqual(3, container.FindAll(".bit-mdl").Count));

        Services.GetRequiredService<NavigationManager>().NavigateTo("/somewhere-else");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        Assert.IsFalse(persistent.IsClosed);
        Assert.IsTrue(ordinary.IsClosed);
        Assert.IsTrue(persistentClosing.IsClosed);

        // Still tracked, which is what a persistent modal being open means: the next container to mount takes
        // it back on, and it is the only one that does.
        CollectionAssert.AreEqual(new[] { persistent }, ModalService.OpenModals.ToArray());

        await Context.DisposeComponentsAsync();

        var remounted = RenderComponent<BitModalContainer>();

        remounted.WaitForAssertion(() => Assert.AreEqual(1, remounted.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public async Task BitModalContainerShouldSetTheCloseOnNavigationPolicyForEveryModalItRenders()
    {
        var container = RenderComponent<BitModalContainer>(parameters =>
        {
            parameters.Add(p => p.ModalParameters, new BitModalParameters { CloseOnNavigation = false });
        });

        var kept = await ModalService.Show<TestModalContent>();
        var closed = await ModalService.Show<TestModalContent>(new BitModalParameters { CloseOnNavigation = true });

        container.WaitForAssertion(() => Assert.AreEqual(2, container.FindAll(".bit-mdl").Count));

        Services.GetRequiredService<NavigationManager>().NavigateTo("/somewhere-else");

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        Assert.IsFalse(kept.IsClosed);
        Assert.IsTrue(closed.IsClosed);
    }

    [TestMethod]
    public async Task BitModalServiceShouldKeepItsModalsWhenOnlyTheQueryStringChanges()
    {
        var container = RenderComponent<BitModalContainer>();

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/the-page");

        var modalRef = await ModalService.Show<TestModalContent>();

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl").Count));

        // Still the same page, so still the page the modal belongs to.
        navigationManager.NavigateTo("/the-page?sort=name");

        Assert.IsFalse(modalRef.IsClosed);
        Assert.AreEqual(1, container.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public async Task BitModalContainerShouldCloseTheModalsItLeavesBehindWhenItIsDisposed()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();
        var persistentRef = await ModalService.Show<TestModalContent>(persistent: true);

        container.WaitForAssertion(() => Assert.AreEqual(2, container.FindAll(".bit-mdl").Count));

        await Context.DisposeComponentsAsync();

        // The modal is off the screen the moment the container is gone, so leaving it unclosed would leave
        // whoever awaits its Result waiting forever.
        Assert.IsTrue(modalRef.IsClosed);
        Assert.IsNull(await modalRef.Result);

        // A persistent modal is meant to survive the remount, so it is left open for the next container.
        Assert.IsFalse(persistentRef.IsClosed);

        var remounted = RenderComponent<BitModalContainer>();
        remounted.WaitForAssertion(() => Assert.AreEqual(1, remounted.FindAll(".bit-mdl").Count));
    }

    [TestMethod]
    public async Task BitModalServiceShouldRenderAModalOnceWhenMoreThanOneContainerIsMounted()
    {
        var first = RenderComponent<BitModalContainer>();
        var second = RenderComponent<BitModalContainer>();

        await ModalService.Show<TestModalContent>();

        // The last container to initialize is the one the service renders through; the one before it stands
        // down rather than rendering the same modal a second time.
        second.WaitForAssertion(() => Assert.AreEqual(1, second.FindAll(".bit-mdl").Count));
        Assert.AreEqual(0, first.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public async Task BitModalReferenceShouldReplaceTheParametersOfAnOpenModal()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters { HeaderText = "before" });

        container.WaitForAssertion(() => Assert.AreEqual("before", container.Find(".bit-mdl-hdr").TextContent));

        await modalRef.Update(new BitModalParameters { HeaderText = "after", ShowCloseButton = true });

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual("after", container.Find(".bit-mdl-hdr").TextContent);
            Assert.AreEqual(1, container.FindAll(".bit-mdl-cls").Count);
        });
    }

    [TestMethod]
    public async Task BitModalServiceShouldReportTheMissingContainerOnce()
    {
        var loggerFactory = new TestModalLoggerFactory();
        var modalService = new BitModalService(loggerFactory);

        await modalService.Show<TestModalContent>();
        await modalService.Show<TestModalContent>();

        // One clear line rather than one per modal: an app with no container shows every one of them into
        // nothing, and the message is the same every time.
        Assert.AreEqual(1, loggerFactory.Errors.Count);
        StringAssert.Contains(loggerFactory.Errors[0], "modal container");
    }

    [TestMethod]
    public async Task BitModalServiceShouldNotReportAPersistentModalWaitingForItsContainer()
    {
        var loggerFactory = new TestModalLoggerFactory();
        var modalService = new BitModalService(loggerFactory);

        // A persistent modal shown before the container mounts is the supported way of doing it, not a mistake.
        await modalService.Show<TestModalContent>(persistent: true);

        Assert.AreEqual(0, loggerFactory.Errors.Count);
    }

    [TestMethod]
    public async Task BitModalContainerShouldSetTheCloseGuardForEveryModalItRenders()
    {
        var asked = 0;
        var container = RenderComponent<BitModalContainer>(parameters =>
        {
            parameters.Add(p => p.ModalParameters, new BitModalParameters
            {
                CanClose = () => { asked++; return Task.FromResult(false); }
            });
        });

        var guarded = await ModalService.Show<TestModalContent>();

        // The container's guard is the default for every modal it renders.
        Assert.IsFalse(await guarded.TryClose());
        Assert.AreEqual(1, asked);
        Assert.IsFalse(guarded.IsClosed);

        // And a modal that declares one of its own is asked that one instead - not both.
        var ownGuard = await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            CanClose = () => Task.FromResult(true)
        });

        Assert.IsTrue(await ownGuard.TryClose());
        Assert.AreEqual(1, asked);
    }

    [TestMethod]
    public async Task BitModalServiceShouldLetGoOfAModalWhoseShowFailed()
    {
        RenderComponent<BitModalContainer>();

        Func<BitModalReference, Task> throwing = _ => throw new InvalidOperationException("the handler said no");
        ModalService.OnAddModal += throwing;

        try
        {
            var failure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => ModalService.Show<TestModalContent>());
            Assert.AreEqual("the handler said no", failure.Message);
        }
        finally
        {
            ModalService.OnAddModal -= throwing;
        }

        // The reference never reaches the caller of a failed Show, but a persistent one shown the same way
        // would still be tracked - and nothing is left holding a Result that will never complete.
        Assert.AreEqual(0, ModalService.OpenModals.Count);
    }

    [TestMethod]
    public async Task BitModalReferenceShouldReportAnOverlayDismissal()
    {
        var container = RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-mdl-ovl").Count));

        container.Find(".bit-mdl-ovl").Click();

        container.WaitForAssertion(() => Assert.IsTrue(modalRef.IsClosed));
        Assert.IsTrue(modalRef.IsDismissed);
    }

    [TestMethod]
    public async Task BitModalReferenceShouldReportADismissalItsGuardTurnedDown()
    {
        RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>(new BitModalParameters
        {
            CanClose = () => Task.FromResult(false)
        });

        Assert.IsFalse(await modalRef.Dismiss());

        Assert.IsFalse(modalRef.IsClosed);
        Assert.IsFalse(modalRef.IsDismissed);
    }

    [TestMethod]
    public async Task BitModalServiceShouldNotCloseAModalThatIsAlreadyClosed()
    {
        RenderComponent<BitModalContainer>();

        var modalRef = await ModalService.Show<TestModalContent>();

        await modalRef.CloseWith("the answer");

        // Only the first close is the close, so neither of these is one - and the original answer stands.
        Assert.IsFalse(await modalRef.TryClose("another"));
        Assert.IsFalse(await modalRef.Dismiss());
        Assert.AreEqual("the answer", await modalRef.Result);
    }

    [TestMethod]
    public async Task BitModalServiceShouldRefuseToCloseNothing()
    {
        RenderComponent<BitModalContainer>();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => ModalService.Close(null!));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => ModalService.TryClose(null!));
    }

    [TestMethod]
    public async Task BitModalServiceShouldRerenderTheModalsWhenTheirParametersAreMutatedInPlace()
    {
        var container = RenderComponent<BitModalContainer>();

        var parameters = new BitModalParameters { HeaderText = "before" };
        await ModalService.Show<TestModalContent>(parameters);

        container.WaitForAssertion(() => Assert.AreEqual("before", container.Find(".bit-mdl-hdr").TextContent));

        // Mutating the members changes no object reference, so nothing notices it on its own.
        parameters.HeaderText = "after";

        await ModalService.Refresh();

        container.WaitForAssertion(() => Assert.AreEqual("after", container.Find(".bit-mdl-hdr").TextContent));
    }

    [TestMethod]
    public async Task BitModalServiceShouldBuildTheContentParametersOfARuntimeTypeFromTheReference()
    {
        var container = RenderComponent<BitModalContainer>();

        BitModalReference? handedToTheFactory = null;

        var modalRef = await ModalService.Show(typeof(TestModalContent), modalRef =>
        {
            handedToTheFactory = modalRef;

            return new Dictionary<string, object> { { nameof(TestModalContent.Message), modalRef.Id } };
        });

        // The reference reaches the factory before the content is built, which is what lets a parameter of the
        // content close this very modal.
        Assert.AreSame(modalRef, handedToTheFactory);

        container.WaitForAssertion(() => Assert.IsTrue(container.Markup.Contains(modalRef.Id)));
    }
}
