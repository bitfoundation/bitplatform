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

public partial class BitAiRendererBoundaryE2E
{
    private const string ToolName = "e2e_rbd_tool";
    private const string PartKind = "e2e-rbd-part";
    private const string PartPartial = "partial";
    private const string PartComplete = "complete";

    private readonly BitAiMessage _message = new(BitAiRole.Assistant, "msg-e2e-rbd");
    private BitAiCustomPart<string> _part = default!;
    private BitAiToolCallPart? _toolCall;

    private bool _throwing;
    private bool _throwingDefault;
    private bool _throwingInline;
    private bool _throwingStyle;
    private int _siblingClicks;

    protected override void OnInitialized()
    {
        // The registry is the host's (a singleton on the server): the renderers capture nothing of this page, and are
        // registered for a tool and a part kind nothing else uses.
        Renderers.RegisterTool(ToolName, context => builder =>
        {
            if (context.Call.Arguments is not { } arguments) throw new InvalidOperationException("The chart arguments are incomplete.");

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "e2e-rbd-tool");
            builder.AddContent(2, $"Chart of {arguments.GetRawText()}");
            builder.CloseElement();
        });

        Renderers.RegisterPart(PartKind, context => builder =>
        {
            var value = ((BitAiCustomPart<string>)context.Part).Data;
            if (value == PartPartial) throw new InvalidOperationException("The value is only partial.");

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "e2e-rbd-part");
            builder.AddContent(2, $"Value: {value}");
            builder.CloseElement();
        });

        // Whole as the page loads, so only the tests of a failure see one (and allow the console error the WebAssembly
        // logger writes for it).
        _part = _message.AddPart(new BitAiCustomPart<string>(PartKind, PartComplete, "part-e2e-rbd"));
    }

    private static RenderFragment Throwing(bool shouldThrow, string message, string contentId) => builder =>
    {
        builder.OpenComponent<Thrower>(0);
        builder.AddComponentParameter(1, nameof(Thrower.ShouldThrow), shouldThrow);
        builder.AddComponentParameter(2, nameof(Thrower.Message), message);
        builder.AddComponentParameter(3, nameof(Thrower.ContentId), contentId);
        builder.CloseComponent();
    };

    private RenderFragment InlineFragment => builder =>
    {
        if (_throwingInline) throw new InvalidOperationException("The inline fragment threw.");

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "id", "rbd-inline-content");
        builder.AddContent(2, "Inline content");
        builder.CloseElement();
    };

    private void StartToolCall()
    {
        _toolCall = new BitAiToolCallPart($"call-e2e-rbd-{Guid.NewGuid():N}", ToolName);
        _toolCall.AppendArguments("""{"series":[1,2""");
    }

    private void CompleteToolCallInBackground()
    {
        var call = _toolCall;
        if (call is null) return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            call.AppendArguments(",3]}");
            call.CompleteArguments();
        });
    }

    private void SetPartInBackground(string value)
    {
        var part = _part;

        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            part.Data = value;
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

    /// <summary>Throws while it renders when told to, and shows a line of its own otherwise.</summary>
    public sealed class Thrower : ComponentBase
    {
        [Parameter] public bool ShouldThrow { get; set; }

        [Parameter] public string Message { get; set; } = string.Empty;

        [Parameter] public string ContentId { get; set; } = string.Empty;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            if (ShouldThrow) throw new InvalidOperationException(Message);

            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "id", ContentId);
            builder.AddContent(2, "Content rendered");
            builder.CloseElement();
        }
    }
}
