using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins what a disabled component does with the handlers the page splatted on it: the pointer-events a disabled class
/// turns off stop only a direct pointer hit, while a screen reader activates what it announces by dispatching a click,
/// and an event of an enabled control inside a disabled component bubbles up to its root. A form element the browser
/// disables runs none of them, so a component whose Disabled makes it unavailable drops the splatted handlers of the
/// events an element is activated through (BitComponentBase.DisabledAwareHtmlAttributes) and keeps the ones that only
/// follow the pointer around, the way BitElement does.
/// </summary>
[TestClass]
public class BitDisabledActivationContractTests : BunitTestContext
{
    private const string ROOT = "data-splat-root";

    public static IEnumerable<object[]> Components =>
    [
        [typeof(BitActionButton)],
        [typeof(BitButton)],
        [typeof(BitButtonGroup<BitButtonGroupItem>)],
        [typeof(BitMenuButton<BitMenuButtonItem>)],
        [typeof(BitToggleButton)],
        [typeof(BitCalendar)],
        [typeof(BitCheckbox)],
        [typeof(BitChoiceGroup<BitChoiceGroupItem<string>, string>)],
        [typeof(BitCircularTimePicker)],
        [typeof(BitColorPicker)],
        [typeof(BitDatePicker)],
        [typeof(BitDateRangePicker)],
        [typeof(BitDropdown<BitDropdownItem<string>, string>)],
        [typeof(BitFileInput)],
        [typeof(BitFileUpload)],
        [typeof(BitNumberField<int>)],
        [typeof(BitOtpInput)],
        [typeof(BitRating)],
        [typeof(BitSearchBox)],
        [typeof(BitSlider)],
        [typeof(BitTagsInput)],
        [typeof(BitTextField)],
        [typeof(BitTimePicker)],
        [typeof(BitToggle)],
        [typeof(BitTimeline<BitTimelineItem>)],
        [typeof(BitBreadcrumb<BitBreadcrumbItem>)],
        [typeof(BitDropMenu)],
        [typeof(BitNav<BitNavItem>)],
        [typeof(BitNavBar<BitNavBarItem>)],
        [typeof(BitPagination)],
        [typeof(BitPivotItem)],
        [typeof(BitPersona)],
        [typeof(BitTag)],
        [typeof(BitCard)],
        [typeof(BitIcon)],
        [typeof(BitImage)],
        [typeof(BitLink)],
        [typeof(BitFlag)],
        [typeof(BitPhoneInput)],
        [typeof(BitCountryPicker)],
        [typeof(BitTimeZonePicker<string>)],
        [typeof(BitThemeSwitcher)],
        [typeof(BitAccentColorSwitcher)],
        [typeof(BitRichTextEditor)],
        [typeof(BitMarkdownEditor)],
        [typeof(BitDataGrid<Row>)],
        [typeof(BitChart)],
        [typeof(BitPdfViewer)],
        [typeof(BitAiJsonView)],
        [typeof(BitAiCitationMarker)],
        [typeof(BitAiSourceCard)],
        [typeof(BitAiSourceList)],
        [typeof(BitAiAgentRunView)],
        [typeof(BitAiScrollToLatest)],
        [typeof(BitAiConversationList)],
        [typeof(BitAiToolApproval)],
        [typeof(BitAiErrorView)],
        [typeof(BitAiCopyButton)],
        [typeof(BitAiForm)],
        [typeof(BitAiPromptInput)],
        [typeof(BitAiModelSelector)],
        [typeof(BitAiVoiceInput)],
        [typeof(BitAiAttachmentChip)],
    ];

    // The ones that render a plain ChildContent inside their root, which an enabled control of the page can sit in.
    public static IEnumerable<object[]> ContentComponents =>
    [
        [typeof(BitActionButton)],
        [typeof(BitButton)],
        [typeof(BitToggleButton)],
        [typeof(BitCheckbox)],
        [typeof(BitTag)],
        [typeof(BitCard)],
        [typeof(BitLink)],
    ];

    // The ones whose Disabled only switches one behaviour of theirs off while the content they host stays live: the
    // panel of an accordion, the slides of a carousel, the panel of a pivot's open tab, the overlay of a map, the body
    // of a message box, the header and footer of a nav panel - and the AI surfaces that frame content of the app or of
    // the model (a transcript, a message, a tool card, an artifact, a generated component). What bubbles up to their
    // root from there is an enabled control's own event, so they keep the handlers the page splatted on them.
    public static IEnumerable<object[]> ContentHosts =>
    [
        [typeof(BitBadge)],
        [typeof(BitAccordion)],
        [typeof(BitCarousel)],
        [typeof(BitSwiper)],
        [typeof(BitPivot)],
        [typeof(BitAccordionList<BitAccordionListItem>)],
        [typeof(BitMessageBox)],
        [typeof(BitMap<BitLeafletMapProvider>)],
        [typeof(BitNavPanel<BitNavItem>)],
        [typeof(BitAiCollapsible)],
        [typeof(BitAiToolCallView)],
        [typeof(BitAiArtifactView)],
        [typeof(BitAiGenerativeUiView)],
        [typeof(BitAiChat)],
        [typeof(BitAiMessageList)],
        [typeof(BitAiChatMessage)],
        [typeof(BitAiMessageActions)],
        [typeof(BitAiEmptyState)],
    ];

    // What a component needs to render its root at all - the AI ones draw a message, a conversation, a session, a part
    // or a source - handed to it on top of the attributes every render of these tests (and of the markup contract's)
    // gives it.
    private static readonly Dictionary<Type, Func<Dictionary<string, object?>>> RequiredParameters = new()
    {
        [typeof(BitAiJsonView)] = () => new() { [nameof(BitAiJsonView.Value)] = JsonDocument.Parse("""{"a":{"b":1},"c":[1,2]}""").RootElement },
        [typeof(BitAiCitationMarker)] = () => new() { [nameof(BitAiCitationMarker.Citation)] = new BitAiCitation("c1", AiSource()) { Number = 1 } },
        [typeof(BitAiSourceCard)] = () => new() { [nameof(BitAiSourceCard.Source)] = AiSource() },
        [typeof(BitAiSourceList)] = () => new() { [nameof(BitAiSourceList.Citations)] = new List<BitAiCitation> { new("c1", AiSource()) { Number = 1 } } },
        [typeof(BitAiAgentRunView)] = () => new() { [nameof(BitAiAgentRunView.Run)] = AiRun() },
        [typeof(BitAiScrollToLatest)] = () => new() { [nameof(BitAiScrollToLatest.Visible)] = true },
        [typeof(BitAiConversationList)] = () => new()
        {
            [nameof(BitAiConversationList.Conversations)] = new List<BitAiConversationSummary> { new("c1", "Trip", DateTimeOffset.Now) },
        },
        [typeof(BitAiToolApproval)] = () => new() { [nameof(BitAiToolApproval.Call)] = AiToolCall(BitAiToolCallStatus.AwaitingApproval) },
        [typeof(BitAiErrorView)] = () => new() { [nameof(BitAiErrorView.Error)] = new BitAiError("The model is overloaded.") { Detail = "503" } },
        [typeof(BitAiCopyButton)] = () => new() { [nameof(BitAiCopyButton.Content)] = "Copied text" },
        [typeof(BitAiForm)] = () => new()
        {
            [nameof(BitAiForm.Definition)] = new BitAiFormDefinition { Fields = [new BitAiFormField("city") { Required = true }] },
        },
        [typeof(BitAiModelSelector)] = () => new() { [nameof(BitAiModelSelector.Models)] = new List<BitAiModel> { new("alpha", "Alpha"), new("beta", "Beta") } },
        [typeof(BitAiAttachmentChip)] = () => new() { [nameof(BitAiAttachmentChip.Attachment)] = new BitAiAttachment("a1", "report.pdf") { Size = 2048 } },
        [typeof(BitAiToolCallView)] = () => new() { [nameof(BitAiToolCallView.Call)] = AiToolCall(BitAiToolCallStatus.Running) },
        [typeof(BitAiArtifactView)] = () => new() { [nameof(BitAiArtifactView.Artifact)] = AiArtifact() },
        [typeof(BitAiGenerativeUiView)] = () => new() { [nameof(BitAiGenerativeUiView.Part)] = new BitAiGenerativeUiPart("forecast_card") },
        [typeof(BitAiChat)] = () => new() { [nameof(BitAiChat.Session)] = new BitAiChatSession(new SilentTransport()) },
        [typeof(BitAiMessageList)] = () => new() { [nameof(BitAiMessageList.Conversation)] = AiConversation() },
        [typeof(BitAiChatMessage)] = () => new() { [nameof(BitAiChatMessage.Message)] = AiAssistantMessage() },
        [typeof(BitAiMessageActions)] = () => new() { [nameof(BitAiMessageActions.Message)] = AiAssistantMessage() },
    };

    internal static IEnumerable<KeyValuePair<string, object?>> GetRequiredParameters(Type type) =>
        RequiredParameters.TryGetValue(type, out var parameters) ? parameters() : [];

    // The carousels, the theme and the accent color switchers read services of the library's own, the AI components
    // theirs; the dictation of BitAiVoiceInput is offered (and its root rendered) only where the browser supports it.
    internal static void AddServices(BunitContext context)
    {
        context.Services.AddBitBlazorUIExtrasServices();
        context.Services.AddBitBlazorUIAIServices();
        context.JSInterop.Setup<bool>("BitBlazorUI.AiVoiceInput.isDictationSupported", _ => true).SetResult(true);
    }

    [TestInitialize]
    public void AddServices() => AddServices(Context);

    [TestMethod]
    [DynamicData(nameof(Components))]
    public void DisabledComponentShouldDropTheSplattedActivationHandlers(Type type)
    {
        var activations = 0;
        var hovers = 0;

        var component = RenderSplatted(type, disabled: true, () => activations++, () => hovers++);

        var root = component.Find($"[{ROOT}]");

        // The component's own handler of an event, written after the splat, may still be there, guarded on its own;
        // what is checked is that the page's never runs, whichever way the event is dispatched.
        Trigger(() => root.Click());
        Trigger(() => root.DoubleClick());
        Trigger(() => root.PointerDown());
        Trigger(() => root.KeyDown(Key.Enter));

        Assert.AreEqual(0, activations, $"{type.Name} ran a splatted activation handler while disabled.");

        // A hover activates nothing, so a disabled component keeps what the page does on one.
        root.MouseOver();

        Assert.AreEqual(1, hovers, $"{type.Name} dropped a splatted hover handler while disabled.");
    }

    [TestMethod]
    [DynamicData(nameof(Components))]
    public void EnabledComponentShouldRunTheSplattedActivationHandlers(Type type)
    {
        var activations = 0;

        var component = RenderSplatted(type, disabled: false, () => activations++, () => { });

        var root = component.Find($"[{ROOT}]");

        root.DoubleClick();
        root.PointerDown();

        Assert.AreEqual(2, activations, $"{type.Name} dropped a splatted activation handler while enabled.");
    }

    [TestMethod]
    [DynamicData(nameof(ContentComponents))]
    public void DisabledComponentShouldNotRunTheSplattedHandlersForAnEventBubblingUpFromItsContent(Type type)
    {
        var activations = 0;
        var inner = 0;

        RenderFragment content = builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "inner");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => inner++));
            builder.AddAttribute(3, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => inner++));
            builder.CloseElement();
        };

        var component = RenderSplatted(type, disabled: true, () => activations++, () => { }, content);

        var element = component.Find(".inner");
        element.Click();
        element.KeyDown(Key.Enter);

        Assert.AreEqual(2, inner);
        Assert.AreEqual(0, activations, $"{type.Name} ran a splatted handler for an event bubbling up from its content.");
    }

    [TestMethod]
    [DynamicData(nameof(ContentComponents))]
    public void EnabledComponentShouldRunTheSplattedHandlersForAnEventBubblingUpFromItsContent(Type type)
    {
        var activations = 0;

        RenderFragment content = builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "inner");
            builder.CloseElement();
        };

        var component = RenderSplatted(type, disabled: false, () => activations++, () => { }, content);

        component.Find(".inner").PointerDown();

        Assert.AreEqual(1, activations);
    }

    [TestMethod]
    [DynamicData(nameof(ContentHosts))]
    public void DisabledContentHostShouldKeepTheSplattedActivationHandlers(Type type)
    {
        var activations = 0;

        var component = RenderSplatted(type, disabled: true, () => activations++, () => { });

        var root = component.Find($"[{ROOT}]");

        root.DoubleClick();
        root.PointerDown();

        Assert.AreEqual(2, activations, $"{type.Name} dropped a splatted activation handler of the content it hosts.");
    }

    [TestMethod]
    [DataRow(typeof(BitBadge))]
    [DataRow(typeof(BitAccordion))]
    public void DisabledContentHostShouldRunTheSplattedHandlersForAnEventBubblingUpFromItsContent(Type type)
    {
        // A badge's Disabled greys the badge, not the control it is pinned to, and an accordion's turns its header
        // away, not its panel, so what bubbles up to the root from there is the enabled control's own event and
        // reaches the page's handler as it always has.
        var activations = 0;

        RenderFragment content = builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "class", "inner");
            builder.CloseElement();
        };

        var component = RenderSplatted(type, disabled: true, () => activations++, () => { }, content);

        component.Find(".inner").Click();

        Assert.AreEqual(1, activations);
    }

    [TestMethod]
    public void TabOfADisabledPivotShouldDropTheSplattedActivationHandlers()
    {
        // A tab of a disabled pivot is announced as disabled like one disabled on its own, so it runs the page's
        // activation handlers no more than one does, while the pivot's root keeps them for the panel it hosts.
        var activations = 0;
        var hovers = 0;

        var component = RenderComponent<BitPivot>(parameters =>
        {
            parameters.Add(p => p.Disabled, true);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitPivotItem>(0);
                builder.AddAttribute(1, nameof(BitPivotItem.HeaderText), "Tab");
                builder.AddAttribute(2, ROOT, "");
                builder.AddAttribute(3, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => activations++));
                builder.AddAttribute(4, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, () => activations++));
                builder.AddAttribute(5, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, () => activations++));
                builder.AddAttribute(6, "onmouseover", EventCallback.Factory.Create<MouseEventArgs>(this, () => hovers++));
                builder.CloseComponent();
            });
        });

        var tab = component.Find($"[{ROOT}]");

        Trigger(() => tab.DoubleClick());
        Trigger(() => tab.PointerDown());
        Trigger(() => tab.KeyDown(Key.Enter));
        tab.MouseOver();

        Assert.AreEqual(0, activations);
        Assert.AreEqual(1, hovers);
    }

    [TestMethod]
    [DataRow(typeof(BitElement))]
    [DataRow(typeof(BitButton))]
    [DataRow(typeof(BitToggle))]
    public void DisabledComponentShouldGetItsActivationHandlersBackOnceEnabled(Type type)
    {
        // The filtered attributes are kept between the renders of a component that stays disabled, never once it is
        // enabled again.
        var activations = 0;
        var disabled = true;

        RenderFragment content = builder =>
        {
            builder.OpenComponent(0, type);
            builder.AddAttribute(1, nameof(BitComponentBase.Disabled), disabled);
            builder.AddAttribute(2, ROOT, "");
            builder.AddAttribute(3, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, () => activations++));
            builder.CloseComponent();
        };

        var host = RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(content);
        });

        Trigger(() => host.Find($"[{ROOT}]").PointerDown());

        host.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(content);
        });

        Trigger(() => host.Find($"[{ROOT}]").PointerDown());

        Assert.AreEqual(0, activations, $"{type.Name} ran a splatted activation handler while it stayed disabled.");

        disabled = false;

        host.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(content);
        });

        host.Find($"[{ROOT}]").PointerDown();

        Assert.AreEqual(1, activations, $"{type.Name} did not get its splatted activation handler back once enabled.");
    }

    private IRenderedComponent<BitParams> RenderSplatted(Type type, bool disabled, Action onActivation, Action onHover, RenderFragment? childContent = null)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, []);
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent(0, type);
                builder.AddAttribute(1, nameof(BitComponentBase.Disabled), disabled);
                builder.AddAttribute(2, ROOT, "");
                builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, onActivation));
                builder.AddAttribute(4, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, onActivation));
                builder.AddAttribute(5, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, onActivation));
                builder.AddAttribute(6, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, onActivation));
                builder.AddAttribute(7, "onmouseover", EventCallback.Factory.Create<MouseEventArgs>(this, onHover));
                if (childContent is not null)
                {
                    builder.AddAttribute(8, "ChildContent", childContent);
                }
                foreach (var (name, value) in GetRequiredParameters(type))
                {
                    builder.AddAttribute(9, name, value);
                }
                builder.CloseComponent();
            });
        });
    }

    // An event the root has no handler of at all is the outcome looked for, not a failure.
    private static void Trigger(Action trigger)
    {
        try
        {
            trigger();
        }
        catch (MissingEventHandlerException) { }
    }

    public class Row
    {
        public string? Name { get; set; }
    }

    private static BitAiCitationSource AiSource() => new("s1", "Handbook") { Url = "https://example.com/handbook" };

    // A run at work whose first step is running, so its plan, its steps and its stop action are all drawn.
    private static BitAiAgentRun AiRun()
    {
        var run = new BitAiAgentRun("run_1", "Researcher");
        run.SetPlan([new BitAiAgentPlanStep("search", "Search"), new BitAiAgentPlanStep("write", "Write")]);
        run.GetOrAddStep("search").Start();
        run.SetStatus(BitAiAgentRunStatus.Running);
        return run;
    }

    private static BitAiToolCallPart AiToolCall(BitAiToolCallStatus status)
    {
        var call = new BitAiToolCallPart("c1", "search_docs");
        call.CompleteArguments(JsonDocument.Parse("""{"query":"disabled"}""").RootElement);
        call.SetStatus(status);
        return call;
    }

    private static BitAiArtifact AiArtifact()
    {
        var artifact = new BitAiConversation().GetOrAddArtifact("a1", BitAiArtifactKind.Document, "README");
        artifact.SetContent("# Hello");
        artifact.Complete();
        return artifact;
    }

    private static BitAiConversation AiConversation()
    {
        var conversation = new BitAiConversation();
        conversation.AddMessage(BitAiRole.User, "Hello");
        conversation.AddMessage(AiAssistantMessage());
        return conversation;
    }

    private static BitAiMessage AiAssistantMessage()
    {
        var message = new BitAiMessage(BitAiRole.Assistant);
        message.AddPart(new BitAiTextPart("Hi there."));
        return message;
    }

    // A transport that answers every turn with an empty reply, for a chat that is only rendered.
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
