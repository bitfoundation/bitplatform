using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Net.Http;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Bit.BlazorUI;
using Bit.BlazorUI.Tests.E2E.TestHost.Components;
using Bit.BlazorUI.Tests.E2E.TestHost.Components.Shared;
using Bit.BlazorUI.Tests.E2E.TestHost.AI;

namespace Bit.BlazorUI.Tests.E2E.TestHost.Components.Pages.AI;

public partial class BitAiMessagePartViewE2E : IAsyncDisposable
{
    private const string NoteKind = "e2e-mpv-note";

    private readonly BitAiMessage _message = new(BitAiRole.Assistant, "msg-e2e-mpv");
    private readonly List<(string Id, BitAiMessagePart Part)> _kinds = [];

    private BitAiRenderScheduler _sessionRenders = default!;
    private BitAiChatSession _session = default!;
    private BitAiCustomPart<Note> _firstNote = default!;
    private BitAiCustomPart<Note> _secondNote = default!;
    private BitAiCustomPart<string> _fallback = default!;
    private BitAiCodePart _wideCode = default!;
    private BitAiCitationPart _citedPart = default!;
    private string _citationClicked = "none";
    private int _citationClicks;
    private int _renders;

    private BitAiMessage? LastAssistant => _session.Conversation.Messages.LastOrDefault(m => m.Role.IsAssistant);

    protected override void OnInitialized()
    {
        // The registry is the host's (a singleton on the server): the renderer captures nothing of this page and is
        // registered for a kind nothing else uses. It counts its renders on the note it draws.
        Renderers.RegisterPart(NoteKind, context => builder =>
        {
            var note = ((BitAiCustomPart<Note>)context.Part).Data;
            note.Renders++;

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "e2e-mpv-note");
            builder.AddAttribute(2, "data-renders", note.Renders.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.AddAttribute(3, "data-message", context.Message.MessageId);
            builder.AddContent(4, note.Text);
            builder.CloseElement();
        });

        _sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));
        _session = new BitAiChatSession(E2EAiTransport.For(E2EAiScripts.Everything), tools: ToolRegistry);
        _session.StateChanged += _sessionRenders.Request;

        var call = new BitAiToolCallPart("call-e2e-mpv", "get_weather") { Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" } };
        call.CompleteArguments(E2EAiScriptRun.Json("""{"city":"Oslo"}"""));
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetResult(BitAiToolOutcome.FromText("Oslo: -3 degrees, Snow"));

        var source = new BitAiCitationSource("src-e2e-mpv", "Render modes") { Kind = BitAiSourceKind.Web, Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes", SiteName = "Microsoft Learn" };
        _message.AddCitation(new BitAiCitation("cit-e2e-mpv", source) { Quote = "Every component runs on the server or in the browser." });

        _fallback = new BitAiCustomPart<string>("e2e-mpv-unknown", "data") { Text = "A kind no view knows,\nshown as the text it carries." };
        _wideCode = new BitAiCodePart(string.Concat(Enumerable.Repeat("var wide = 1; ", 20)), "csharp");

        Add("kind-text", new BitAiTextPart("Plain **text** of the answer."));
        Add("kind-markdown", new BitAiMarkdownPart("A *Markdown* part."));
        Add("kind-reasoning", new BitAiReasoningPart("Thinking it over."));
        Add("kind-code", new BitAiCodePart("var total = 1 + 2;", "csharp"));
        Add("kind-tool-call", call);
        Add("kind-tool-result", new BitAiToolResultPart("call-e2e-provider", BitAiToolOutcome.FromText("2 results")) { ToolName = "web_search" });
        Add("kind-image", new BitAiImagePart("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=") { Alt = "A chart of the visitors", Width = 64, Height = 48 });
        Add("kind-file", new BitAiFilePart(new BitAiAttachment("file-e2e-mpv", "report.pdf") { Size = 2048 }));
        Add("kind-structured", new BitAiStructuredDataPart(E2EAiScriptRun.Json("""[{"option":"Server","effort":"Low"},{"option":"WebAssembly","effort":"Medium"}]"""), "options"));
        Add("kind-sources", new BitAiSourcesPart());
        Add("kind-error", new BitAiErrorPart(new BitAiError("The tool loop stopped.") { Code = "tool_loop_limit", Kind = BitAiErrorKind.Tool }));
        Add("kind-unknown-text", _fallback);
        Add("kind-unknown-silent", new BitAiCustomPart<string>("e2e-mpv-silent", "data"));
        Add("kind-citation", new BitAiCitationPart(new BitAiCitation("cit-e2e-part", source)));
        Add("kind-custom", new BitAiCustomPart<Note>(NoteKind, new Note("A note drawn by the page's own renderer.")));

        // The citation the message holds, so its card takes the number the message gave it (1).
        _citedPart = new BitAiCitationPart(new BitAiCitation("cit-e2e-mpv", source)
        {
            Quote = "Every component runs on the server or in the browser.",
            Locator = "Render modes, section 2",
        });

        _firstNote = new BitAiCustomPart<Note>(NoteKind, new Note("First note"), "part-e2e-first");
        _secondNote = new BitAiCustomPart<Note>(NoteKind, new Note("Second note"), "part-e2e-second");
    }

    private void Add(string id, BitAiMessagePart part) => _kinds.Add((id, _message.AddPart(part)));

    private Task AskAsync() => _session.SubmitAsync("Show me everything");

    private void OnCitationClick(BitAiCitation citation)
    {
        _citationClicked = citation.CitationId;
        _citationClicks++;
    }

    private void ChangeFirstInBackground()
    {
        var part = _firstNote;

        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            part.Data = new Note("First note, changed") { Renders = part.Data.Renders };
        });
    }

    private static RenderFragment Trigger(string id, string text, Action action) => builder =>
    {
        builder.OpenComponent<TriggerButton>(0);
        builder.AddComponentParameter(1, nameof(TriggerButton.ButtonId), id);
        builder.AddComponentParameter(2, nameof(TriggerButton.Text), text);
        builder.AddComponentParameter(3, nameof(TriggerButton.OnTrigger), action);
        builder.CloseComponent();
    };

    /// <summary>
    /// A button whose click is handled by the button itself, so it renders that button and not the page around it - a
    /// plain delegate rather than an EventCallback, which would render the page that handed it in.
    /// </summary>
    public sealed class TriggerButton : ComponentBase
    {
        [Parameter] public string ButtonId { get; set; } = string.Empty;

        [Parameter] public string Text { get; set; } = string.Empty;

        [Parameter] public Action? OnTrigger { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "id", ButtonId);
            builder.AddAttribute(2, "type", "button");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => OnTrigger?.Invoke()));
            builder.AddContent(4, Text);
            builder.CloseElement();
        }
    }

    /// <summary>What a note part holds: its text, and how often the renderer has drawn it.</summary>
    public sealed class Note(string text)
    {
        public string Text { get; } = text;

        public int Renders { get; set; }
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null) await _session.DisposeAsync();
        if (_sessionRenders is not null) await _sessionRenders.DisposeAsync();
    }
}
