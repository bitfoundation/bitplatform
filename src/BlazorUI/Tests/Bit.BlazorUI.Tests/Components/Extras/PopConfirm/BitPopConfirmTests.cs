using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PopConfirm;

[TestClass]
public class BitPopConfirmTests : BunitTestContext
{
    private sealed class Log
    {
        public List<string> Answers { get; } = [];
        public List<bool> OpenChanges { get; } = [];
    }

    private IRenderedComponent<BitPopConfirm> Render(
        Log? log = null,
        string? title = "Delete the file?",
        string? message = "It cannot be brought back.",
        bool disabled = false,
        string? ariaLabel = null,
        Action<ComponentParameterCollectionBuilder<BitPopConfirm>>? more = null)
    {
        return RenderComponent<BitPopConfirm>(parameters =>
        {
            parameters.Add(p => p.Id, "pcf");
            parameters.Add(p => p.Disabled, disabled);
            if (title is not null) parameters.Add(p => p.Title, title);
            if (message is not null) parameters.Add(p => p.Message, message);
            if (ariaLabel is not null) parameters.Add(p => p.AriaLabel, ariaLabel);
            parameters.AddChildContent("<button class=\"trigger\">Delete</button>");
            if (log is not null)
            {
                parameters.Add(p => p.OnConfirm, () => log.Answers.Add("confirm"));
                parameters.Add(p => p.OnCancel, () => log.Answers.Add("cancel"));
                parameters.Add(p => p.IsOpenChanged, (bool isOpen) => log.OpenChanges.Add(isOpen));
            }
            more?.Invoke(parameters);
        });
    }

    private static void Open(IRenderedComponent<BitPopConfirm> com) => com.Find(".bit-clo-acn").Click();

    [TestMethod]
    public void BitPopConfirmShouldRenderTheTriggerAsTheAnchorOfAClosedAlertDialog()
    {
        var com = Render();

        var root = com.Find("#pcf");

        Assert.IsTrue(root.ClassList.Contains("bit-pcf"));
        Assert.IsNotNull(root.QuerySelector(".bit-clo-acn #pcf-trg.bit-pcf-trg > .trigger"));
        Assert.IsFalse(com.Instance.IsOpen);

        // A question that has to be answered before going on, which keeps the focus while it is asked.
        var popup = com.Find(".bit-clo-cal");

        Assert.AreEqual("alertdialog", popup.GetAttribute("role"));
        Assert.AreEqual("true", popup.GetAttribute("aria-modal"));
        Assert.IsFalse(popup.ClassList.Contains("bit-clo-ocl"));

        // Named by its title and described by its message.
        Assert.AreEqual("pcf-ttl", popup.GetAttribute("aria-labelledby"));
        Assert.AreEqual("pcf-msg", popup.GetAttribute("aria-describedby"));
        Assert.AreEqual("Delete the file?", com.Find("#pcf-ttl").TextContent);
        Assert.AreEqual("It cannot be brought back.", com.Find("#pcf-msg").TextContent);
    }

    [TestMethod]
    public void BitPopConfirmWithoutATitleShouldBeNamedByItsMessage()
    {
        var com = Render(title: null);

        var popup = com.Find(".bit-clo-cal");

        Assert.AreEqual("pcf-msg", popup.GetAttribute("aria-labelledby"));
        Assert.IsNull(popup.GetAttribute("aria-describedby"));
        Assert.AreEqual(0, com.FindAll(".bit-pcf-ttl").Count);
    }

    [TestMethod]
    public void BitPopConfirmWithNoTextShouldBeNamedByItsAriaLabel()
    {
        var com = Render(title: null, message: null, ariaLabel: "Confirm the delete");

        var popup = com.Find(".bit-clo-cal");

        Assert.IsNull(popup.GetAttribute("aria-labelledby"));
        Assert.IsNull(popup.GetAttribute("aria-describedby"));
        Assert.AreEqual("Confirm the delete", popup.GetAttribute("aria-label"));
        Assert.AreEqual(0, com.FindAll(".bit-pcf-msg").Count);
    }

    [TestMethod]
    public void BitPopConfirmShouldRenderCancelFirstWithTheFocusOnIt()
    {
        var com = Render();

        var buttons = com.FindAll(".bit-pcf-act button");

        Assert.AreEqual(2, buttons.Count);
        Assert.IsTrue(buttons[0].ClassList.Contains("bit-pcf-cbt"));
        Assert.IsTrue(buttons[1].ClassList.Contains("bit-pcf-obt"));
        Assert.AreEqual("Cancel", buttons[0].TextContent.Trim());
        Assert.AreEqual("Ok", buttons[1].TextContent.Trim());

        // The callout moves the focus to the element marked for it as it opens.
        Assert.IsTrue(buttons[0].HasAttribute("data-autofocus"));
        Assert.IsFalse(buttons[1].HasAttribute("data-autofocus"));
    }

    [TestMethod]
    public void BitPopConfirmShouldRenderTheTextsAndTheColorItIsGiven()
    {
        var com = Render(more: p =>
        {
            p.Add(x => x.ConfirmText, "Delete");
            p.Add(x => x.CancelText, "Keep");
            p.Add(x => x.ConfirmColor, BitColor.Error);
        });

        Assert.AreEqual("Keep", com.Find(".bit-pcf-cbt").TextContent.Trim());
        Assert.AreEqual("Delete", com.Find(".bit-pcf-obt").TextContent.Trim());
        Assert.IsTrue(com.Find(".bit-pcf-obt").ClassList.Contains("bit-btn-err"));
        Assert.IsFalse(com.Find(".bit-pcf-cbt").ClassList.Contains("bit-btn-err"));
    }

    [TestMethod]
    public void BitPopConfirmShouldOpenWhenTheTriggerIsClicked()
    {
        var log = new Log();
        var com = Render(log);

        Open(com);

        Assert.IsTrue(com.Instance.IsOpen);
        Assert.IsTrue(com.Find(".bit-clo-cal").ClassList.Contains("bit-clo-ocl"));
        CollectionAssert.AreEqual(new[] { true }, log.OpenChanges);
        Assert.AreEqual(0, log.Answers.Count);
    }

    [TestMethod]
    public void BitPopConfirmShouldRaiseOnConfirmOnceThePopupHasClosed()
    {
        var log = new Log();
        bool? openWhenConfirmed = null;

        var com = Render(log);
        com.Render(p => p.Add(x => x.OnConfirm, () =>
        {
            openWhenConfirmed = com.Instance.IsOpen;
            log.Answers.Add("confirm");
        }));

        Open(com);
        com.Find(".bit-pcf-obt").Click();

        Assert.IsFalse(com.Instance.IsOpen);
        Assert.IsFalse(com.Find(".bit-clo-cal").ClassList.Contains("bit-clo-ocl"));
        CollectionAssert.AreEqual(new[] { "confirm" }, log.Answers);
        CollectionAssert.AreEqual(new[] { true, false }, log.OpenChanges);
        Assert.AreEqual(false, openWhenConfirmed);
    }

    [TestMethod]
    public void BitPopConfirmShouldRaiseOnCancelForTheCancelButton()
    {
        var log = new Log();
        var com = Render(log);

        Open(com);
        com.Find(".bit-pcf-cbt").Click();

        Assert.IsFalse(com.Instance.IsOpen);
        CollectionAssert.AreEqual(new[] { "cancel" }, log.Answers);
        CollectionAssert.AreEqual(new[] { true, false }, log.OpenChanges);
    }

    [TestMethod]
    public async Task BitPopConfirmShouldRaiseOnCancelForEscape()
    {
        var log = new Log();
        var com = Render(log);

        Open(com);

        var callout = com.FindComponent<BitCallout>();
        await callout.InvokeAsync(() => callout.Instance._OnEscape());

        Assert.IsFalse(com.Instance.IsOpen);
        CollectionAssert.AreEqual(new[] { "cancel" }, log.Answers);
    }

    [TestMethod]
    public void BitPopConfirmShouldRaiseOnCancelForAClickOutsideOfThePopup()
    {
        var log = new Log();
        var com = Render(log);

        Open(com);
        com.Find(".bit-clo-ovl").Click();

        Assert.IsFalse(com.Instance.IsOpen);
        CollectionAssert.AreEqual(new[] { "cancel" }, log.Answers);

        // The press on the overlay has already taken the focus out of the popup, where the callout would have found it
        // to hand back: the pop confirm hands it back to its trigger itself.
        Assert.AreEqual(1, TriggerFocuses());
    }

    [TestMethod]
    public void BitPopConfirmShouldLeaveTheFocusToTheCalloutForItsOwnButtons()
    {
        var com = Render(new Log());

        Open(com);
        com.Find(".bit-pcf-obt").Click();
        Open(com);
        com.Find(".bit-pcf-cbt").Click();

        // The focus is on the pressed button inside the popup, which the callout hands back to the trigger on its own.
        Assert.AreEqual(0, TriggerFocuses());
    }

    private int TriggerFocuses() => Context.JSInterop.Invocations["BitBlazorUI.Utils.focusFirstElement"].Count(i => Equals(i.Arguments[0], "pcf-trg"));

    [TestMethod]
    public void BitPopConfirmShouldRaiseOneAnswerPerOpening()
    {
        var log = new Log();
        var com = Render(log);

        Open(com);
        com.Find(".bit-pcf-obt").Click();
        Open(com);
        com.Find(".bit-pcf-cbt").Click();
        Open(com);
        com.Find(".bit-clo-ovl").Click();

        CollectionAssert.AreEqual(new[] { "confirm", "cancel", "cancel" }, log.Answers);
        CollectionAssert.AreEqual(new[] { true, false, true, false, true, false }, log.OpenChanges);
    }

    [TestMethod]
    public void BitPopConfirmShouldOpenAndCloseThroughIsOpenWithoutAnAnswer()
    {
        var answers = new List<string>();

        var com = RenderComponent<BitPopConfirm>(parameters =>
        {
            parameters.Add(p => p.Title, "Delete the file?");
            parameters.Add(p => p.IsOpen, true);
            parameters.Add(p => p.IsOpenChanged, (bool _) => { });
            parameters.Add(p => p.OnConfirm, () => answers.Add("confirm"));
            parameters.Add(p => p.OnCancel, () => answers.Add("cancel"));
            parameters.AddChildContent("<button>Delete</button>");
        });

        Assert.IsTrue(com.Find(".bit-clo-cal").ClassList.Contains("bit-clo-ocl"));

        // Closed by the page, which is not an answer to the question.
        com.Render(p => p.Add(x => x.IsOpen, false));

        Assert.IsFalse(com.Find(".bit-clo-cal").ClassList.Contains("bit-clo-ocl"));
        Assert.AreEqual(0, answers.Count);
    }

    [TestMethod]
    public void DisabledBitPopConfirmShouldNotOpenAndShouldLeaveTheTriggerLive()
    {
        var log = new Log();
        var triggerClicks = 0;

        var com = RenderComponent<BitPopConfirm>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.Add(p => p.Title, "Delete the file?");
            parameters.Add(p => p.OnCancel, () => log.Answers.Add("cancel"));
            parameters.Add(p => p.IsOpenChanged, (bool isOpen) => log.OpenChanges.Add(isOpen));
            parameters.AddChildContent(builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "trigger");
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => triggerClicks++));
                builder.CloseElement();
            });
        });

        com.Find(".trigger").Click();

        Assert.AreEqual(1, triggerClicks);
        Assert.IsFalse(com.Instance.IsOpen);
        Assert.AreEqual(0, log.OpenChanges.Count);
        Assert.AreEqual(0, log.Answers.Count);
    }

    [TestMethod]
    public void BitPopConfirmShouldApplyTheClassesAndStylesOfItsParts()
    {
        var com = Render(more: p =>
        {
            p.Add(x => x.Classes, new BitPopConfirmClassStyles
            {
                Root = "c-root",
                Callout = "c-callout",
                Container = "c-container",
                Title = "c-title",
                Message = "c-message",
                Actions = "c-actions",
                CancelButton = "c-cancel",
                ConfirmButton = "c-confirm",
            });
            p.Add(x => x.Styles, new BitPopConfirmClassStyles
            {
                Root = "margin:1px",
                Callout = "margin:2px",
                Container = "margin:3px",
                Title = "margin:4px",
                Message = "margin:5px",
                Actions = "margin:6px",
                CancelButton = "margin:7px",
                ConfirmButton = "margin:8px",
            });
        });

        void AssertPart(string selector, string cssClass, string style)
        {
            var element = com.Find(selector);

            Assert.IsTrue(element.ClassList.Contains(cssClass), $"{selector} has no {cssClass}.");
            StringAssert.Contains(element.GetAttribute("style"), style, $"{selector} has no {style}.");
        }

        AssertPart(".bit-pcf", "c-root", "margin:1px");
        AssertPart(".bit-clo-cal", "c-callout", "margin:2px");
        AssertPart(".bit-pcf-ctn", "c-container", "margin:3px");
        AssertPart(".bit-pcf-ttl", "c-title", "margin:4px");
        AssertPart(".bit-pcf-msg", "c-message", "margin:5px");
        AssertPart(".bit-pcf-act", "c-actions", "margin:6px");
        AssertPart(".bit-pcf-cbt", "c-cancel", "margin:7px");
        AssertPart(".bit-pcf-obt", "c-confirm", "margin:8px");
    }
}
