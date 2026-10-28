using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MessageActions;

[TestClass]
public class BitAiMessageActionsTests : BitAiTestBase
{
    [TestMethod]
    public void BitAiMessageActionsShouldOfferCopyAndFeedbackByDefault()
    {
        var component = RenderComponent<BitAiMessageActions>(p => p.Add(c => c.Message, Assistant(new BitAiTextPart("answer"))));

        Assert.IsNotNull(component.Find(".bit-aimac > .bit-aimac-act"));
        Assert.HasCount(1, component.FindComponents<BitAiCopyButton>());
        Assert.IsNotNull(component.Find("button[title='Good response']"));
        Assert.IsNotNull(component.Find("button[title='Bad response']"));
        Assert.IsEmpty(component.FindAll("button[title='Regenerate']"));
    }

    [TestMethod]
    public void BitAiMessageActionsShouldOfferRegenerationOnlyWithASessionThatMayRegenerate()
    {
        var session = new BitAiChatSession(new SilentTransport());

        var component = RenderComponent<BitAiMessageActions>(p => p
            .Add(c => c.Message, Assistant(new BitAiTextPart("answer")))
            .Add(c => c.Session, session));

        Assert.AreEqual("Regenerate", component.Find("button[title='Regenerate']").GetAttribute("aria-label"));

        component.Render(p => p.Add(c => c.CanRegenerate, false));

        Assert.IsEmpty(component.FindAll("button[title='Regenerate']"));
    }

    [TestMethod]
    public void FeedbackIsPressedAndWithdrawnByTheSameButton()
    {
        var message = Assistant(new BitAiTextPart("answer"));
        var recorded = new List<BitAiFeedbackKind>();

        var component = RenderComponent<BitAiMessageActions>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.OnFeedback, kind => recorded.Add(kind)));

        Assert.AreEqual("false", component.Find("button[title='Good response']").GetAttribute("aria-pressed"));

        component.Find("button[title='Good response']").Click();

        Assert.AreEqual(BitAiFeedbackKind.Positive, message.Feedback);
        Assert.AreEqual("true", component.Find("button[title='Good response']").GetAttribute("aria-pressed"));
        Assert.AreEqual("false", component.Find("button[title='Bad response']").GetAttribute("aria-pressed"));
        Assert.IsTrue(component.Find("button[title='Good response']").ClassList.Contains("bit-btn-dft-pri"));

        component.Find("button[title='Good response']").Click();

        Assert.AreEqual(BitAiFeedbackKind.None, message.Feedback);
        Assert.AreEqual("false", component.Find("button[title='Good response']").GetAttribute("aria-pressed"));
        CollectionAssert.AreEqual(new[] { BitAiFeedbackKind.Positive, BitAiFeedbackKind.None }, recorded);
    }

    [TestMethod]
    public void BitAiMessageActionsShouldHideTheFeedbackWhenAskedTo()
    {
        var component = RenderComponent<BitAiMessageActions>(p => p
            .Add(c => c.Message, Assistant(new BitAiTextPart("answer")))
            .Add(c => c.ShowFeedback, false));

        Assert.IsEmpty(component.FindAll("button[aria-pressed]"));
    }

    [TestMethod]
    public void BitAiMessageActionsShouldAppendTheExtraActions()
    {
        var component = RenderComponent<BitAiMessageActions>(p => p
            .Add(c => c.Message, Assistant(new BitAiTextPart("answer")))
            .Add(c => c.ExtraActionsTemplate, b => b.AddMarkupContent(0, "<button class=\"share\">Share</button>")));

        Assert.IsNotNull(component.Find(".bit-aimac-act > .share"));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void BitAiMessageActionsShouldRespectPinned(bool pinned)
    {
        var component = RenderComponent<BitAiMessageActions>(p => p
            .Add(c => c.Message, Assistant(new BitAiTextPart("answer")))
            .Add(c => c.Pinned, pinned));

        Assert.AreEqual(pinned, component.Find(".bit-aimac").ClassList.Contains("bit-aimac-pin"));
    }

    [TestMethod]
    public void ADisabledRowDisablesItsButtons()
    {
        var message = Assistant(new BitAiTextPart("answer"));
        var session = new BitAiChatSession(new SilentTransport());

        var component = RenderComponent<BitAiMessageActions>(p => p
            .Add(c => c.Message, message)
            .Add(c => c.Session, session)
            .Add(c => c.Disabled, true));

        Assert.IsTrue(component.Find(".bit-aimac").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", component.Find("button[title='Regenerate']").GetAttribute("aria-disabled"));
        Assert.AreEqual("true", component.Find("button[title='Good response']").GetAttribute("aria-disabled"));

        component.Find("button[title='Good response']").Click();

        Assert.AreEqual(BitAiFeedbackKind.None, message.Feedback);
    }

    [TestMethod]
    public void TheActionsAreLeftOutWhenTheOptionsSaySo()
    {
        Services.AddSingleton(BitAiUiOptions.Default with { ShowMessageActions = false, ShowTimestamps = true });

        var component = RenderComponent<BitAiMessageActions>(p => p.Add(c => c.Message, Assistant(new BitAiTextPart("answer"))));

        Assert.IsEmpty(component.FindAll(".bit-aimac-act"));
        Assert.IsNotNull(component.Find("time.bit-aimac-tim").GetAttribute("datetime"));
    }

    [TestMethod]
    public void TheModelIsShownWhenTheOptionsSaySo()
    {
        Services.AddSingleton(BitAiUiOptions.Default with { ShowModelName = true });

        var message = Assistant(new BitAiTextPart("answer"));
        message.MergeMetadata(new BitAiGenerationMetadata { ModelId = "gpt-test" });

        var component = RenderComponent<BitAiMessageActions>(p => p.Add(c => c.Message, message));

        Assert.AreEqual("gpt-test", component.Find(".bit-aimac-mdl").TextContent);
    }

    // A reader knows a model by the name the picker shows, the one the registered catalog gives it, not by its id.
    [TestMethod]
    public void TheModelIsNamedByTheCatalog()
    {
        Services.AddSingleton(BitAiUiOptions.Default with { ShowModelName = true });
        Services.AddSingleton<IBitAiModelCatalog>(new BitAiModelCatalog([new BitAiModel("gpt-test", "GPT Test")]));

        var listed = Assistant(new BitAiTextPart("answer"));
        listed.MergeMetadata(new BitAiGenerationMetadata { ModelId = "gpt-test" });

        var unlisted = Assistant(new BitAiTextPart("answer"));
        unlisted.MergeMetadata(new BitAiGenerationMetadata { ModelId = "gpt-other" });

        Assert.AreEqual("GPT Test", RenderComponent<BitAiMessageActions>(p => p.Add(c => c.Message, listed)).Find(".bit-aimac-mdl").TextContent);
        Assert.AreEqual("gpt-other", RenderComponent<BitAiMessageActions>(p => p.Add(c => c.Message, unlisted)).Find(".bit-aimac-mdl").TextContent);
    }

    // Under Blazor Server the local zone is the server's: the options name the reader's, and the time is shown in it,
    // while the instant written for machines stays in UTC.
    [TestMethod]
    public void TheTimeIsShownInTheTimeZoneOfTheOptions()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("bit-test+03:30", new TimeSpan(3, 30, 0), "UTC+03:30", "UTC+03:30");
        Services.AddSingleton(BitAiUiOptions.Default with { ShowTimestamps = true, TimeZone = zone });

        var message = new BitAiMessage(BitAiRole.Assistant) { CreatedAt = new DateTimeOffset(2026, 1, 2, 9, 0, 0, TimeSpan.Zero) };
        message.AddPart(new BitAiTextPart("answer"));

        var component = RenderComponent<BitAiMessageActions>(p => p.Add(c => c.Message, message));

        var time = component.Find("time.bit-aimac-tim");
        Assert.AreEqual(new DateTimeOffset(2026, 1, 2, 12, 30, 0, new TimeSpan(3, 30, 0)).ToString("t"), time.TextContent);
        Assert.AreEqual("2026-01-02T09:00:00Z", time.GetAttribute("datetime"));
    }

    [TestMethod]
    public void BitAiMessageActionsShouldRespectClassesAndStyles()
    {
        Services.AddSingleton(BitAiUiOptions.Default with { ShowTimestamps = true });

        var component = RenderComponent<BitAiMessageActions>(p => p
            .Add(c => c.Message, Assistant(new BitAiTextPart("answer")))
            .Add(c => c.Classes, new() { Root = "custom-root", Actions = "custom-actions", Time = "custom-time", FeedbackButton = new() { Root = "custom-feedback" } })
            .Add(c => c.Styles, new() { Root = "color: red;", Actions = "color: green;", Time = "color: blue;" }));

        Assert.IsTrue(component.Find(".bit-aimac").ClassList.Contains("custom-root"));
        StringAssert.Contains(component.Find(".bit-aimac").GetAttribute("style"), "color: red;");
        Assert.IsTrue(component.Find(".bit-aimac-act").ClassList.Contains("custom-actions"));
        Assert.AreEqual("color: green;", component.Find(".bit-aimac-act").GetAttribute("style"));
        Assert.IsTrue(component.Find(".bit-aimac-tim").ClassList.Contains("custom-time"));
        Assert.AreEqual("color: blue;", component.Find(".bit-aimac-tim").GetAttribute("style"));
        Assert.IsTrue(component.Find("button[title='Good response']").ClassList.Contains("custom-feedback"));
    }

    [TestMethod]
    [DataRow(BitVisibility.Visible, "")]
    [DataRow(BitVisibility.Hidden, "visibility:hidden")]
    [DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiMessageActionsShouldRespectVisibility(BitVisibility visibility, string expectedStyle)
    {
        var component = RenderComponent<BitAiMessageActions>(p => p
            .Add(c => c.Message, Assistant(new BitAiTextPart("answer")))
            .Add(c => c.Visibility, visibility));

        var style = component.Find(".bit-aimac").GetAttribute("style");

        if (expectedStyle.Length == 0)
        {
            Assert.IsTrue(style is null || style.Contains("visibility:hidden") is false);
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    [DataRow(BitDir.Rtl, "bit-rtl")]
    [DataRow(BitDir.Ltr, null)]
    public void BitAiMessageActionsShouldRespectDir(BitDir dir, string? expectedClass)
    {
        var component = RenderComponent<BitAiMessageActions>(p => p
            .Add(c => c.Message, Assistant(new BitAiTextPart("answer")))
            .Add(c => c.Dir, dir));

        var root = component.Find(".bit-aimac");

        Assert.AreEqual(dir.ToString().ToLower(), root.GetAttribute("dir"));
        Assert.AreEqual(expectedClass is not null, root.ClassList.Contains("bit-rtl"));
    }



    private sealed class SilentTransport : IBitAiTransport
    {
        public async IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new BitAiMessageStartedEvent();
            yield return new BitAiMessageCompletedEvent();
            await Task.CompletedTask;
        }
    }
}
