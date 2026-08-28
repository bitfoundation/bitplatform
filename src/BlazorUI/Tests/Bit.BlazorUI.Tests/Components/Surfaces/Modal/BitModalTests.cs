using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Modal;

[TestClass]
public class BitModalTests : BunitTestContext
{
    private bool isModalOpen = true;

    [TestMethod,
        DataRow(null),
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsAlertTest(bool? isAlert)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsAlert, isAlert);
            parameters.Add(p => p.IsOpen, true);
        });

        // The role belongs on the box the dialog actually is, not on the layer that also holds the overlay:
        // the pattern asks for every element the dialog is operated through to be a descendant of it.
        var element = com.Find(".bit-mdl-ctn");
        Assert.AreEqual(element?.Attributes?["role"]?.Value, isAlert.HasValue && isAlert.Value ? "alertdialog" : "dialog");

        Assert.IsFalse(com.Find(".bit-mdl").HasAttribute("role"));
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsBlockingTest(bool isBlocking)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.Blocking, isBlocking);
            parameters.Add(p => p.IsOpen, isModalOpen);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
        });

        var bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(1, bitModal.Count);

        var overlayElement = com.Find(".bit-mdl-ovl");
        overlayElement.Click();

        bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(isBlocking ? 1 : 0, bitModal.Count);
    }

    [TestMethod,
        DataRow(false),
        DataRow(true)
    ]
    public void BitModalIsOpenTest(bool isOpen)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isOpen);
        });

        var bitModel = com.FindAll(".bit-mdl");
        Assert.AreEqual(isOpen ? 1 : 0, bitModel.Count);
    }

    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("Test-S-A-Id")
    ]
    public void BitModalSubtitleAriaIdTest(string subtitleAriaId)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.SubtitleAriaId, subtitleAriaId);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-mdl-ctn");

        if (subtitleAriaId == null)
        {
            Assert.IsFalse(element.HasAttribute("aria-describedby"));
        }
        else if (subtitleAriaId == string.Empty)
        {
            Assert.AreEqual(element?.Attributes?["aria-describedby"]?.Value, string.Empty);
        }
        else
        {
            Assert.AreEqual(element?.Attributes?["aria-describedby"]?.Value, subtitleAriaId);
        }
    }

    [TestMethod,
        DataRow(null),
        DataRow(""),
        DataRow("Test-T-A-Id")
    ]
    public void BitModalTitleAriaIdTest(string titleAriaId)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.TitleAriaId, titleAriaId);
            parameters.Add(p => p.IsOpen, true);
        });

        var element = com.Find(".bit-mdl-ctn");

        if (titleAriaId == null)
        {
            Assert.IsFalse(element.HasAttribute("aria-labelledby"));
        }
        else if (titleAriaId == string.Empty)
        {
            Assert.AreEqual(element?.Attributes["aria-labelledby"]?.Value, string.Empty);
        }
        else
        {
            Assert.AreEqual(element?.Attributes["aria-labelledby"]?.Value, titleAriaId);
        }
    }

    [TestMethod]
    public void BitModalContentTest()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent("<div>Test Content</div>");
        });

        var elementContent = com.Find(".bit-mdl-ctn");

        elementContent.MarkupMatches("<div id:ignore tabindex=\"-1\" class=\"bit-mdl-ctn\" aria-modal=\"true\" role=\"dialog\"><div>Test Content</div></div>");
    }

    [TestMethod]
    public void BitModalCloseWhenClickOutOfModalTest()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, isModalOpen);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
        });

        var bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(1, bitModal.Count);

        var overlayElement = com.Find(".bit-mdl-ovl");
        overlayElement.Click();

        bitModal = com.FindAll(".bit-mdl");
        Assert.AreEqual(0, bitModal.Count);
    }

    [TestMethod]
    public void BitModalOnDismissShouldWorkCorrect()
    {
        var isOpen = true;
        var currentCount = 0;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, newValue => isOpen = newValue);
            parameters.Add(p => p.OnDismiss, () => currentCount++);
        });

        var overlayElement = com.Find(".bit-mdl-ovl");

        overlayElement.Click();

        Assert.IsFalse(isOpen);
        Assert.AreEqual(1, currentCount);
    }



    // ------------------------------------------------------------------------------------------------
    // Rendering & structure
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldRenderTheAriaLabelItWasGiven()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaLabel, "Terms of service");
        });

        Assert.AreEqual("Terms of service", com.Find(".bit-mdl-ctn").GetAttribute("aria-label"));
    }

    [TestMethod,
        DataRow(true, "true"),
        DataRow(false, "false")
    ]
    public void BitModalShouldReportWhetherItIsModal(bool ariaModal, string expected)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaModal, ariaModal);
        });

        Assert.AreEqual(expected, com.Find(".bit-mdl-ctn").GetAttribute("aria-modal"));
    }

    [TestMethod]
    public void BitModalShouldMakeItsContentProgrammaticallyFocusable()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        // Without a tab stop of its own the content has nowhere to put the focus in a Modal that holds
        // nothing focusable, which would leave it behind the overlay on whatever opened the Modal.
        Assert.AreEqual("-1", com.Find(".bit-mdl-ctn").GetAttribute("tabindex"));
    }

    [TestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitModalShouldRenderTheOverlayOnlyWhenAskedFor(bool showOverlay, int expectedCount)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ShowOverlay, showOverlay);
        });

        Assert.AreEqual(expectedCount, com.FindAll(".bit-mdl-ovl").Count);
    }

    [TestMethod]
    public void BitModalShouldApplyTheFullSizeClasses()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.FullWidth, true);
            parameters.Add(p => p.FullHeight, true);
        });

        var root = com.Find(".bit-mdl");

        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fwi"));
        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fhe"));
    }

    [TestMethod]
    public void BitModalShouldApplyTheClassesAndStylesOfEachPart()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitModalClassStyles { Root = "root-class", Overlay = "overlay-class", Content = "content-class" });
            parameters.Add(p => p.Styles, new BitModalClassStyles { Root = "color:red", Overlay = "color:green", Content = "color:blue" });
        });

        Assert.IsTrue(com.Find(".bit-mdl").ClassList.Contains("root-class"));
        Assert.IsTrue(com.Find(".bit-mdl").GetAttribute("style")!.Contains("color:red"));

        Assert.IsTrue(com.Find(".bit-mdl-ovl").ClassList.Contains("overlay-class"));
        Assert.AreEqual("color:green", com.Find(".bit-mdl-ovl").GetAttribute("style"));

        Assert.IsTrue(com.Find(".bit-mdl-ctn").ClassList.Contains("content-class"));
        Assert.AreEqual("color:blue", com.Find(".bit-mdl-ctn").GetAttribute("style"));
    }

    [TestMethod]
    public void BitModalShouldKeepTwoStyleSourcesApartWithASemicolon()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters
            {
                Styles = new BitModalClassStyles { Overlay = "margin:1rem", Content = "padding:1rem" }
            });
            parameters.Add(p => p.IsOpen, true);
            // Neither part carries a trailing semicolon: without one being spliced in, the declaration
            // that follows is swallowed and the CSS parser drops both.
            parameters.Add(p => p.Styles, new BitModalClassStyles { Overlay = "color:green", Content = "color:blue" });
        });

        Assert.AreEqual("color:green;margin:1rem", com.Find(".bit-mdl-ovl").GetAttribute("style"));
        Assert.AreEqual("color:blue;padding:1rem", com.Find(".bit-mdl-ctn").GetAttribute("style"));
    }

    [TestMethod]
    public void BitModalShouldJoinTheOwnAndCascadedClassesOfEachPart()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters
            {
                Classes = new BitModalClassStyles { Overlay = "cascaded-overlay", Content = "cascaded-content" }
            });
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitModalClassStyles { Overlay = "own-overlay", Content = "own-content" });
        });

        Assert.AreEqual("bit-mdl-ovl own-overlay cascaded-overlay", com.Find(".bit-mdl-ovl").GetAttribute("class"));
        Assert.AreEqual("bit-mdl-ctn own-content cascaded-content", com.Find(".bit-mdl-ctn").GetAttribute("class"));
    }

    [TestMethod]
    public void BitModalShouldJoinASingleClassSourceWithoutAnEmptySlot()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters
            {
                Classes = new BitModalClassStyles { Overlay = "cascaded-overlay" }
            });
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Classes, new BitModalClassStyles { Content = "own-content" });
        });

        Assert.AreEqual("bit-mdl-ovl cascaded-overlay", com.Find(".bit-mdl-ovl").GetAttribute("class"));
        Assert.AreEqual("bit-mdl-ctn own-content", com.Find(".bit-mdl-ctn").GetAttribute("class"));
    }

    [TestMethod]
    public void BitModalShouldNotRenderAnEmptyStyleAttributeOnItsParts()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.IsFalse(com.Find(".bit-mdl-ovl").HasAttribute("style"));
        Assert.IsFalse(com.Find(".bit-mdl-ctn").HasAttribute("style"));
    }

    [TestMethod]
    public void BitModalShouldNotLeaveEmptyClassSlotsInTheRenderedAttribute()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual("bit-mdl-ovl", com.Find(".bit-mdl-ovl").GetAttribute("class"));
        Assert.AreEqual("bit-mdl-ctn", com.Find(".bit-mdl-ctn").GetAttribute("class"));
    }



    // ------------------------------------------------------------------------------------------------
    // Dismissal
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldBeDismissedByTheEscapeKey()
    {
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(isOpen);
        Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public void BitModalShouldNotBeDismissedByTheEscapeKeyWhenItWasToldNotToBe()
    {
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.NoDismissOnEscape, true);
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(1, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public void BitModalShouldIgnoreEveryKeyButEscape()
    {
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(1, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public void BitModalShouldStillBeDismissedByTheEscapeKeyWhileItBlocksTheOverlay()
    {
        // Blocking takes the pointer's way out away; a keyboard user would otherwise have none at all
        // unless the content offers one.
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.Blocking, true);
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitModalShouldFireOnDismissWhenItIsDismissedByTheEscapeKey()
    {
        var isOpen = true;
        var dismissCount = 0;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.OnDismiss, () => dismissCount++);
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        com.WaitForAssertion(() => Assert.AreEqual(1, dismissCount));
    }

    [TestMethod]
    public void BitModalShouldNotBeDismissedWhileItIsDisabled()
    {
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.IsEnabled, false);
        });

        com.Find(".bit-mdl-ovl").Click();
        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
        Assert.AreEqual(1, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public void BitModalShouldReportEveryOverlayClickEvenTheOnesItRefusesToBeDismissedBy()
    {
        var isOpen = true;
        var clickCount = 0;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.Blocking, true);
            parameters.Add(p => p.OnOverlayClick, () => clickCount++);
        });

        com.Find(".bit-mdl-ovl").Click();

        Assert.AreEqual(1, clickCount);
        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitModalShouldNotBeDismissableWhileTheConsumerDrivesIsOpenOneWay()
    {
        // A one-way IsOpen is the consumer holding the state: the Modal has nowhere to report a
        // dismissal to, so it does not act on one either.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.Find(".bit-mdl-ovl").Click();
        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, com.FindAll(".bit-mdl").Count);
    }



    // ------------------------------------------------------------------------------------------------
    // Open state
    // ------------------------------------------------------------------------------------------------

    [TestMethod,
        DataRow(true, 1),
        DataRow(false, 0)
    ]
    public void BitModalShouldStartInTheStateDefaultIsOpenAsksFor(bool defaultIsOpen, int expectedCount)
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, defaultIsOpen);
        });

        Assert.AreEqual(expectedCount, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public void BitModalShouldIgnoreDefaultIsOpenWhenIsOpenIsSet()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.DefaultIsOpen, true);
            parameters.Add(p => p.IsOpen, false);
        });

        Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public async Task BitModalShouldOpenAndCloseThroughItsOwnMethods()
    {
        var com = RenderComponent<BitModal>();

        Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);

        await com.InvokeAsync(() => com.Instance.Open());
        Assert.AreEqual(1, com.FindAll(".bit-mdl").Count);

        await com.InvokeAsync(() => com.Instance.Close());
        Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public async Task BitModalShouldToggleThroughItsOwnMethod()
    {
        var com = RenderComponent<BitModal>();

        await com.InvokeAsync(() => com.Instance.Toggle());
        Assert.AreEqual(1, com.FindAll(".bit-mdl").Count);

        await com.InvokeAsync(() => com.Instance.Toggle());
        Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public async Task BitModalShouldReportTheOpeningThroughIsOpenChanged()
    {
        var isOpen = false;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        await com.InvokeAsync(() => com.Instance.Open());

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitModalShouldFireOnOpenOnceItIsInThePage()
    {
        var openCount = 0;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.OnOpen, () => openCount++);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, openCount));
    }

    [TestMethod]
    public void BitModalShouldNotFireOnOpenWhileItIsClosed()
    {
        var openCount = 0;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.OnOpen, () => openCount++);
        });

        Assert.AreEqual(0, openCount);

        com.Render(parameters => parameters.Add(p => p.IsOpen, true));

        com.WaitForAssertion(() => Assert.AreEqual(1, openCount));
    }



    // ------------------------------------------------------------------------------------------------
    // Focus handling
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldTakeTheFocusAndTheTabSequenceWhenItOpens()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        var containerId = com.Find(".bit-mdl-ctn").Id;

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.storeFocus"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count);
        });

        Assert.AreEqual(containerId, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"][^1].Arguments[0]);
        Assert.AreEqual(containerId, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"][^1].Arguments[0]);
    }

    [TestMethod]
    public void BitModalShouldHandTheFocusBackWhenItCloses()
    {
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        com.Render(parameters => parameters.Add(p => p.IsOpen, false));

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocus"].Count);
        });

        // The focus is only handed back while nothing else has taken it, which after the Modal left the
        // page is the state the browser leaves behind.
        Assert.AreEqual(true, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocus"][^1].Arguments[1]);
    }

    [TestMethod]
    public void BitModalShouldNotMoveTheFocusWhenItWasToldNotTo()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoAutoFocus, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count);
    }

    [TestMethod]
    public void BitModalShouldNotTrapTheFocusWhenItWasToldNotTo()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoFocusTrap, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
    }

    [TestMethod]
    public void BitModalShouldNotTrapTheFocusWhileItIsNotModal()
    {
        // Holding the keyboard inside a surface the pointer is free to leave would only be half a
        // barrier, so a modeless Modal leaves the tab sequence alone.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaModal, false);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
    }

    [TestMethod]
    public void BitModalShouldNotRememberTheFocusWhenItWillNotHandItBack()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoRestoreFocus, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.storeFocus"].Count);
    }

    [TestMethod]
    public async Task BitModalShouldTakeBackWhatItRegisteredWhenItIsDisposedWhileOpen()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        await Context.DisposeComponentsAsync();

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.forgetFocus"].Count);
    }

    [TestMethod]
    public void BitModalShouldRegisterTheFocusTrapWhenItIsTurnedOnWhileTheModalIsOpen()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoFocusTrap, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);

        com.Render(parameters => parameters.Add(p => p.NoFocusTrap, false));

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));
    }



    // ------------------------------------------------------------------------------------------------
    // Cascaded parameters
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldTakeTheValuesOfTheCascadedParameters()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters
            {
                AriaLabel = "Cascaded label",
                FullWidth = true,
                IsAlert = true,
                ShowOverlay = false,
            });
            parameters.Add(p => p.IsOpen, true);
        });

        var root = com.Find(".bit-mdl");
        var content = com.Find(".bit-mdl-ctn");

        Assert.AreEqual("Cascaded label", content.GetAttribute("aria-label"));
        Assert.AreEqual("alertdialog", content.GetAttribute("role"));
        Assert.IsTrue(root.ClassList.Contains("bit-mdl-fwi"));
        Assert.AreEqual(0, com.FindAll(".bit-mdl-ovl").Count);
    }

    [TestMethod]
    public void BitModalShouldPreferItsOwnValuesOverTheCascadedOnes()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters { AriaLabel = "Cascaded label", IsAlert = false });
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaLabel, "Own label");
            parameters.Add(p => p.IsAlert, true);
        });

        var content = com.Find(".bit-mdl-ctn");

        Assert.AreEqual("Own label", content.GetAttribute("aria-label"));
        Assert.AreEqual("alertdialog", content.GetAttribute("role"));
    }

    [TestMethod]
    public void BitModalShouldHonorTheCascadedNoDismissOnEscape()
    {
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters { NoDismissOnEscape = true });
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitModalShouldHonorTheCascadedBlocking()
    {
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters { Blocking = true });
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        com.Find(".bit-mdl-ovl").Click();

        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitModalShouldFireTheCascadedCallbacksAlongsideItsOwn()
    {
        var isOpen = true;
        var ownDismiss = 0;
        var cascadedDismiss = 0;
        var ownOverlayClick = 0;
        var cascadedOverlayClick = 0;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters
            {
                OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(new object(), () => cascadedDismiss++),
                OnOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(new object(), () => cascadedOverlayClick++),
            });
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.OnDismiss, () => ownDismiss++);
            parameters.Add(p => p.OnOverlayClick, () => ownOverlayClick++);
        });

        com.Find(".bit-mdl-ovl").Click();

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, ownOverlayClick);
            Assert.AreEqual(1, cascadedOverlayClick);
            Assert.AreEqual(1, ownDismiss);
            Assert.AreEqual(1, cascadedDismiss);
        });
    }

    [TestMethod]
    public void BitModalShouldMergeTheCascadedHtmlAttributesWithItsOwn()
    {
        var com = RenderComponent<BitModalHtmlAttributesTest>(parameters =>
        {
            parameters.Add(p => p.ModalParameters, new BitModalParameters
            {
                HtmlAttributes = new Dictionary<string, object> { ["data-cascaded"] = "yes", ["data-shared"] = "cascaded" }
            });
        });

        var root = com.Find(".bit-mdl");

        Assert.AreEqual("yes", root.GetAttribute("data-cascaded"));
        Assert.AreEqual("yes", root.GetAttribute("data-own"));
        Assert.AreEqual("own", root.GetAttribute("data-shared"));
    }

    [TestMethod]
    public void BitModalShouldTolerateANullCascadedParameters()
    {
        // A cascaded null is what a consumer writing ModalParameters="null" hands down; the Modal falls
        // back to a set of its own rather than failing on it.
        var com = RenderComponent<BitModalHtmlAttributesTest>();

        Assert.AreEqual(1, com.FindAll(".bit-mdl").Count);
        Assert.AreEqual("yes", com.Find(".bit-mdl").GetAttribute("data-own"));
    }



    // ------------------------------------------------------------------------------------------------
    // Dialog semantics
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldCarryTheDialogSemanticsOnTheContentRatherThanTheLayerAroundIt()
    {
        // Every element the dialog is operated through has to be a descendant of the element carrying the
        // role, and the layer around the content also holds the overlay - which is not part of the dialog
        // but part of the page it is covering.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaLabel, "Terms of service");
            parameters.Add(p => p.TitleAriaId, "title");
            parameters.Add(p => p.SubtitleAriaId, "subtitle");
        });

        var root = com.Find(".bit-mdl");
        var content = com.Find(".bit-mdl-ctn");

        Assert.IsFalse(root.HasAttribute("role"));
        Assert.IsFalse(root.HasAttribute("aria-modal"));
        Assert.IsFalse(root.HasAttribute("aria-label"));
        Assert.IsFalse(root.HasAttribute("aria-labelledby"));
        Assert.IsFalse(root.HasAttribute("aria-describedby"));

        Assert.AreEqual("dialog", content.GetAttribute("role"));
        Assert.AreEqual("true", content.GetAttribute("aria-modal"));
        Assert.AreEqual("Terms of service", content.GetAttribute("aria-label"));
        Assert.AreEqual("title", content.GetAttribute("aria-labelledby"));
        Assert.AreEqual("subtitle", content.GetAttribute("aria-describedby"));
    }



    // ------------------------------------------------------------------------------------------------
    // Scroll lock
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldHoldThePageWhileItIsOpen()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        var containerId = com.Find(".bit-mdl-ctn").Id;

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        // Keyed by the same id the focus registrations use, so that every hold a Modal takes is given back
        // under the one name.
        Assert.AreEqual(containerId, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1].Arguments[0]);
    }

    [TestMethod]
    public void BitModalShouldHandThePageBackWhenItCloses()
    {
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        com.Render(parameters => parameters.Add(p => p.IsOpen, false));

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count));
    }

    [TestMethod]
    public void BitModalShouldNotHoldThePageWhenItWasToldNotTo()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoScrollLock, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }

    [TestMethod]
    public void BitModalShouldNotHoldThePageWhileItIsNotModal()
    {
        // A modeless Modal is meant to leave the page behind it usable, and a page held still behind a
        // surface the pointer is free to leave reads as a page that broke rather than one that is covered.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaModal, false);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }

    [TestMethod]
    public void BitModalShouldHonorTheCascadedNoScrollLock()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters { NoScrollLock = true });
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
    }

    [TestMethod]
    public void BitModalShouldTakeTheHoldOnThePageWhenItIsTurnedOnWhileTheModalIsOpen()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.NoScrollLock, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);

        com.Render(parameters => parameters.Add(p => p.NoScrollLock, false));

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));
    }

    [TestMethod]
    public async Task BitModalShouldHandThePageBackWhenItIsDisposedWhileOpen()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        await Context.DisposeComponentsAsync();

        Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
    }

    [TestMethod]
    public void BitModalShouldNeitherHoldTheKeyboardNorThePageWhileItIsNotVisible()
    {
        // A Modal taken out of view carries none of the behaviors that only make sense for one the user can
        // see: holding either of them would leave the page unusable behind a surface nobody can find.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.storeFocus"].Count));

        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count);
    }



    // ------------------------------------------------------------------------------------------------
    // Keeping the content
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldRenderNothingOfAKeptModalBeforeItIsFirstOpened()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.KeepMounted, true);
        });

        Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public void BitModalShouldStayInThePageWhileItIsClosedWhenItIsKeptMounted()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        var root = com.Find(".bit-mdl");
        Assert.IsFalse(root.ClassList.Contains("bit-mdl-hid"));
        Assert.IsFalse(root.HasAttribute("inert"));
        Assert.IsFalse(root.HasAttribute("aria-hidden"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.KeepMounted, true);
        });

        // Still there, but out of the way of the page: out of the layout, out of the tab sequence and out of
        // the reading order.
        root = com.Find(".bit-mdl");
        Assert.IsTrue(root.ClassList.Contains("bit-mdl-hid"));
        Assert.IsTrue(root.HasAttribute("inert"));
        Assert.AreEqual("true", root.GetAttribute("aria-hidden"));
    }

    [TestMethod]
    public void BitModalShouldTakeAModalThatIsNotKeptOutOfThePageWhenItCloses()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual(1, com.FindAll(".bit-mdl").Count);

        com.Render(parameters => parameters.Add(p => p.IsOpen, false));

        Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
    }

    [TestMethod]
    public void BitModalShouldKeepTheStateOfItsContentWhileItIsKeptMounted()
    {
        var log = new List<string>();

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.KeepMounted, true);
            parameters.AddChildContent<TestModalStateContent>(content => content.Add(c => c.Log, log));
        });

        Assert.AreEqual(1, log.Count);

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.KeepMounted, true);
        });
        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        // The content was never taken away, so it was never built a second time - and whatever state it was
        // holding is the state it is still in.
        Assert.AreEqual(1, log.Count);
    }

    [TestMethod]
    public void BitModalShouldBuildItsContentAgainOnEveryOpeningWhenItIsNotKept()
    {
        var log = new List<string>();

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.AddChildContent<TestModalStateContent>(content => content.Add(c => c.Log, log));
        });

        Assert.AreEqual(1, log.Count);

        com.Render(parameters => parameters.Add(p => p.IsOpen, false));
        com.Render(parameters => parameters.Add(p => p.IsOpen, true));

        Assert.AreEqual(2, log.Count);
    }

    [TestMethod]
    public void BitModalShouldStillTakeAndHandBackTheFocusOfAKeptModal()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.KeepMounted, true);
        });

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.disposeFocusTrap"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.restoreFocus"].Count);
        });

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));
    }



    // ------------------------------------------------------------------------------------------------
    // The Escape callback and the answer to a refused dismissal
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldReportEveryEscapeEvenTheOnesItRefusesToBeDismissedBy()
    {
        var escapes = 0;
        var isOpen = true;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.NoDismissOnEscape, true);
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++));
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, escapes);
        Assert.IsTrue(isOpen);
    }

    [TestMethod]
    public void BitModalShouldReportTheEscapeItIsDismissedBy()
    {
        var escapes = 0;
        var isOpen = true;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++));
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, escapes);
        Assert.IsFalse(isOpen);
    }

    [TestMethod]
    public void BitModalShouldNotReportAnyKeyButEscapeAsAnEscape()
    {
        var escapes = 0;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++));
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.AreEqual(0, escapes);
    }

    [TestMethod]
    public void BitModalShouldFireTheCascadedEscapeCallbackAlongsideItsOwn()
    {
        var ownEscape = 0;
        var cascadedEscape = 0;

        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters
            {
                NoDismissOnEscape = true,
                OnEscapeKeyDown = EventCallback.Factory.Create<KeyboardEventArgs>(this, () => cascadedEscape++),
            });
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.OnEscapeKeyDown, EventCallback.Factory.Create<KeyboardEventArgs>(this, () => ownEscape++));
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, ownEscape);
            Assert.AreEqual(1, cascadedEscape);
        });
    }

    [TestMethod]
    public void BitModalShouldAnswerAnOverlayClickItRefusesWithAMovement()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
            parameters.Add(p => p.Blocking, true);
        });

        Assert.IsFalse(com.Find(".bit-mdl-ctn").ClassList.Contains("bit-mdl-bna"));

        com.Find(".bit-mdl-ovl").Click();

        Assert.IsTrue(com.Find(".bit-mdl-ctn").ClassList.Contains("bit-mdl-bna"));

        // The second refusal in a row has to be answered too, which is why the two markers are alternated
        // rather than one being added and taken away again.
        com.Find(".bit-mdl-ovl").Click();

        Assert.IsFalse(com.Find(".bit-mdl-ctn").ClassList.Contains("bit-mdl-bna"));
        Assert.IsTrue(com.Find(".bit-mdl-ctn").ClassList.Contains("bit-mdl-bnb"));
    }

    [TestMethod]
    public void BitModalShouldAnswerAnEscapeItRefusesWithAMovement()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
            parameters.Add(p => p.NoDismissOnEscape, true);
        });

        com.Find(".bit-mdl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.IsTrue(com.Find(".bit-mdl-ctn").ClassList.Contains("bit-mdl-bna"));
    }

    [TestMethod]
    public void BitModalShouldNotAnswerADismissalItAccepts()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.KeepMounted, true);
            parameters.Add(p => p.IsOpenChanged, EventCallback.Factory.Create<bool>(this, () => { }));
        });

        com.Find(".bit-mdl-ovl").Click();

        var content = com.Find(".bit-mdl-ctn");
        Assert.IsFalse(content.ClassList.Contains("bit-mdl-bna"));
        Assert.IsFalse(content.ClassList.Contains("bit-mdl-bnb"));
    }

    [TestMethod]
    public void BitModalShouldStartFromTheEntryAnimationWhenAModalThatRefusedIsOpenedAgain()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsOpenChanged, HandleIsOpenChanged);
            parameters.Add(p => p.Blocking, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        com.Find(".bit-mdl-ovl").Click();
        Assert.IsTrue(com.Find(".bit-mdl-ctn").ClassList.Contains("bit-mdl-bna"));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, false);
            parameters.Add(p => p.Blocking, true);
            parameters.Add(p => p.KeepMounted, true);
        });
        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Blocking, true);
            parameters.Add(p => p.KeepMounted, true);
        });

        var content = com.Find(".bit-mdl-ctn");
        Assert.IsFalse(content.ClassList.Contains("bit-mdl-bna"));
        Assert.IsFalse(content.ClassList.Contains("bit-mdl-bnb"));
    }


    [TestMethod]
    public void BitModalShouldHoldThePageItWasPointedAt()
    {
        // Holding a page that never scrolls holds nothing, so a layout that scrolls a region of its own
        // names that region instead.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".app-main");
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        Assert.AreEqual(".app-main", Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1].Arguments[1]);
    }

    [TestMethod]
    public void BitModalShouldHoldThePageItselfWhenItWasPointedAtNothing()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        Assert.IsNull(Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1].Arguments[1]);
    }

    [TestMethod]
    public void BitModalShouldTakeTheCascadedScrollerSelector()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters { ScrollerSelector = ".cascaded-main" });
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        Assert.AreEqual(".cascaded-main", Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1].Arguments[1]);
    }

    [TestMethod]
    public void BitModalShouldMoveTheFocusOnlyOncePerOpening()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));

        // An unrelated parameter change is not a reason to take the focus off whatever the user has since
        // moved it to inside the Modal.
        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.AriaLabel, "Something else");
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count));
    }

    [TestMethod]
    public void BitModalShouldTakeTheKeyboardAndThePageOnceItBecomesVisibleWhileOpen()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Visibility, BitVisibility.Collapsed);
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.storeFocus"].Count));
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Visibility, BitVisibility.Visible);
        });

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count);
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count);
        });
    }


    [TestMethod]
    public void BitModalShouldTakeTheHoldOnTheNewScrollerWhenTheSelectorChangesWhileItIsOpen()
    {
        // The hold is registered against the element the selector resolved to, so a selector changed while
        // the Modal is open has to be let go of and taken again - the Modal would otherwise be holding the
        // element it was pointed at before while the one it is pointed at now carries on scrolling.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".first-main");
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".second-main");
        });

        com.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
            Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count);
        });

        Assert.AreEqual(".second-main", Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][^1].Arguments[1]);
    }

    [TestMethod]
    public void BitModalShouldNotTakeTheHoldAgainWhileTheScrollerItWasPointedAtIsUnchanged()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".app-main");
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        com.Render(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.ScrollerSelector, ".app-main");
            parameters.Add(p => p.AriaLabel, "Something else");
        });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));
        Assert.AreEqual(0, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count);
    }



    // ------------------------------------------------------------------------------------------------
    // Sizing
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldWriteTheSizeItWasGivenOntoItsContent()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.Width, "24rem");
            parameters.Add(p => p.Height, "18rem");
            parameters.Add(p => p.MaxWidth, "32rem");
            parameters.Add(p => p.MaxHeight, "16rem");
        });

        Assert.AreEqual("width:24rem;height:18rem;max-width:32rem;max-height:16rem;", com.Find(".bit-mdl-ctn").GetAttribute("style"));

        // The size belongs to the box that is the dialog, not to the layer that also holds the overlay.
        Assert.IsFalse(com.Find(".bit-mdl").GetAttribute("style")?.Contains("max-width") ?? false);
    }

    [TestMethod]
    public void BitModalShouldWriteOnlyTheSizesItWasGiven()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MaxWidth, "32rem");
        });

        Assert.AreEqual("max-width:32rem;", com.Find(".bit-mdl-ctn").GetAttribute("style"));
    }

    [TestMethod]
    public void BitModalShouldNotWriteASizeItWasNotGiven()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            // Whitespace is not a length, so it is treated as nothing rather than written out as one.
            parameters.Add(p => p.Width, "  ");
        });

        Assert.IsFalse(com.Find(".bit-mdl-ctn").HasAttribute("style"));
    }

    [TestMethod]
    public void BitModalShouldLetTheContentStylesHaveTheLastWordOverTheSizeParameters()
    {
        // Within one style attribute the later declaration is the one that stands, so the styles the
        // consumer gave the content are written after the size parameters.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MaxWidth, "32rem");
            parameters.Add(p => p.Styles, new BitModalClassStyles { Content = "max-width:40rem" });
        });

        Assert.AreEqual("max-width:32rem; max-width:40rem", com.Find(".bit-mdl-ctn").GetAttribute("style"));
    }

    [TestMethod]
    public void BitModalShouldTakeTheCascadedSize()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters
            {
                Width = "20rem",
                Height = "10rem",
                MaxWidth = "28rem",
                MaxHeight = "14rem",
            });
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.AreEqual("width:20rem;height:10rem;max-width:28rem;max-height:14rem;", com.Find(".bit-mdl-ctn").GetAttribute("style"));
    }

    [TestMethod]
    public void BitModalShouldPreferItsOwnSizeOverTheCascadedOne()
    {
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.AddCascadingValue(new BitModalParameters { MaxWidth = "28rem", MaxHeight = "14rem" });
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.MaxWidth, "32rem");
        });

        // The one the component was given wins; the one it was not given still comes from the cascade.
        Assert.AreEqual("max-width:32rem;max-height:14rem;", com.Find(".bit-mdl-ctn").GetAttribute("style"));
    }



    // ------------------------------------------------------------------------------------------------
    // The overlay
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldRefuseTheDefaultOfAPressOnItsOverlay()
    {
        // Pressing something that cannot hold the focus is what takes the focus off whatever had it, and a
        // press on the overlay would otherwise leave the keyboard on the body - out of reach of the Escape
        // handler on the Modal and of the focus trap on its content.
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Add(p => p.IsOpen, true);
        });

        Assert.IsTrue(com.Find(".bit-mdl-ovl").HasAttribute("blazor:onmousedown:preventdefault"));
    }

    [TestMethod]
    public void BitModalShouldStillBeDismissedByAClickOnTheOverlayItRefusedTheDefaultOf()
    {
        var isOpen = true;
        var com = RenderComponent<BitModal>(parameters =>
        {
            parameters.Bind(p => p.IsOpen, isOpen, value => isOpen = value);
        });

        com.Find(".bit-mdl-ovl").Click();

        Assert.IsFalse(isOpen);
        Assert.AreEqual(0, com.FindAll(".bit-mdl").Count);
    }




    // ------------------------------------------------------------------------------------------------
    // Nesting
    // ------------------------------------------------------------------------------------------------

    [TestMethod]
    public void BitModalShouldDismissOnlyTheInnermostOfTwoNestedModalsOnEscape()
    {
        // A Modal opened from inside another one renders inside that one's content, so an Escape left to
        // carry on up would dismiss the Modal the keyboard is still working inside of along with the one it
        // was meant for.
        var com = RenderComponent<BitModalNestedTest>(parameters =>
        {
            parameters.Add(p => p.IsOuterOpen, true);
            parameters.Add(p => p.IsInnerOpen, true);
        });

        Assert.AreEqual(1, com.FindAll(".inner-modal").Count);
        Assert.AreEqual(1, com.FindAll(".outer-modal").Count);

        com.Find(".inner-modal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(0, com.FindAll(".inner-modal").Count);
        Assert.AreEqual(1, com.FindAll(".outer-modal").Count);
    }

    [TestMethod]
    public void BitModalShouldHoldThePageOnceForEachOfTwoNestedModals()
    {
        // The holds are counted rather than toggled: both Modals hold the page and it is only handed back
        // once the last of them closes.
        var com = RenderComponent<BitModalNestedTest>(parameters =>
        {
            parameters.Add(p => p.IsOuterOpen, true);
            parameters.Add(p => p.IsInnerOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"].Count));

        // Each Modal holds under a key of its own, so one letting go never releases what the other holds.
        var firstKey = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][0].Arguments[0];
        var secondKey = Context.JSInterop.Invocations["BitBlazorUI.Utils.lockScroll"][1].Arguments[0];
        Assert.AreNotEqual(firstKey, secondKey);

        com.Find(".inner-modal").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        com.WaitForAssertion(() => Assert.AreEqual(1, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"].Count));
        Assert.AreEqual(secondKey, Context.JSInterop.Invocations["BitBlazorUI.Utils.unlockScroll"][^1].Arguments[0]);
    }

    [TestMethod]
    public void BitModalShouldTrapTheFocusOfEachOfTwoNestedModalsSeparately()
    {
        var com = RenderComponent<BitModalNestedTest>(parameters =>
        {
            parameters.Add(p => p.IsOuterOpen, true);
            parameters.Add(p => p.IsInnerOpen, true);
        });

        com.WaitForAssertion(() => Assert.AreEqual(2, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"].Count));

        // The inner Modal's trap is registered against its own content box, not against the one it renders
        // inside of, so Tab cycles within the Modal the keyboard is actually in.
        var innerContainerId = com.Find(".inner-modal .bit-mdl-ctn").Id;
        Assert.AreEqual(innerContainerId, Context.JSInterop.Invocations["BitBlazorUI.Utils.setupFocusTrap"][^1].Arguments[0]);
    }


    private void HandleIsOpenChanged(bool isOpen) => isModalOpen = isOpen;
}
