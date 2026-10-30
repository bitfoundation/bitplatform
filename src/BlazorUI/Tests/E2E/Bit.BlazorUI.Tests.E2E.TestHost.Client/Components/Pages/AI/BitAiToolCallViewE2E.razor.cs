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

public partial class BitAiToolCallViewE2E : IAsyncDisposable
{
    private const string CustomTool = "e2e_tcv_custom";
    private const string CustomArguments = """{"zone":"north"}""";
    private const string CustomResult = "North zone ready.";
    private const string LifecycleProgressMessage = "Reading 12 of 30 files";
    private const string LifecycleUnknownMessage = "Listing the files";
    private static readonly string[] LifecycleSlices = ["""{"folder":""", "\"docs/\",", "\"recursive\":true}"];

    private readonly BitAiUiOptions _expanded = BitAiUiOptions.Default with { CollapseToolCalls = false };
    private readonly BitAiMessage _customMessage = new(BitAiRole.Assistant, "msg-e2e-custom");
    private readonly BitAiToolCallViewParams[] _disabledParams = [new() { Disabled = true }];

    private BitAiRenderScheduler _sessionRenders = default!;
    private BitAiChatSession _toolSession = default!;
    private BitAiChatSession _approvalSession = default!;
    private BitAiChatSession _parallelSession = default!;

    private BitAiToolCallPart? _lcCall;
    private int _lcSlice;
    private int _approvalSubmits;
    private int _renders;

    private BitAiToolCallPart _customCall = default!;
    private BitAiToolCallPart _plainCall = default!;
    private BitAiToolCallPart _doneCall = default!;
    private BitAiToolCallPart _runningCall = default!;
    private BitAiToolCallPart _failedCall = default!;
    private BitAiToolCallPart _declinedCall = default!;
    private BitAiToolCallPart _cancelledCall = default!;
    private BitAiToolCallPart _awaitingCall = default!;
    private BitAiToolCallPart _iconCall = default!;

    protected override void OnInitialized()
    {
        // The registry is the host's own (a singleton on the server), so the renderer captures nothing of this page: it
        // reads everything it shows off the call it is handed, and its tool is one no script calls.
        Renderers.RegisterTool(CustomTool, context => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "e2e-tcv-custom");
            builder.AddAttribute(2, "data-status", context.Call.Status.ToString());
            builder.AddAttribute(3, "data-message", context.Message.MessageId);
            builder.AddAttribute(4, "data-session", context.Session is null ? "none" : "some");
            builder.AddContent(5, $"{context.Call.ToolName} is {context.Call.Status}: {context.Call.RawArguments}{context.Call.Result?.GetDisplayText()}");
            builder.CloseElement();
        });

        // Every session's StateChanged turns into at most one render of the page per frame interval; the cards follow
        // their own calls in between.
        _sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));
        _toolSession = new BitAiChatSession(E2EAiTransport.For(E2EAiScripts.Tool, 400), tools: ToolRegistry);
        _approvalSession = new BitAiChatSession(E2EAiTransport.For(E2EAiScripts.ToolApproval), tools: ToolRegistry, approvalPolicy: new BitAiDefaultToolApprovalPolicy());
        _parallelSession = new BitAiChatSession(E2EAiTransport.For(E2EAiScripts.ToolParallel), tools: ToolRegistry);
        _toolSession.StateChanged += _sessionRenders.Request;
        _approvalSession.StateChanged += _sessionRenders.Request;
        _parallelSession.StateChanged += _sessionRenders.Request;

        _customCall = new BitAiToolCallPart("call-e2e-custom", CustomTool);
        _plainCall = Completed(new BitAiToolCallPart("call-e2e-plain", "plain_tool"), """{"q":1}""", BitAiToolOutcome.FromText("plain"));

        _doneCall = Completed(new BitAiToolCallPart("call-e2e-done", "get_weather") { Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" } },
                              """{"city":"Oslo"}""",
                              BitAiToolOutcome.Success(E2EAiScriptRun.Json("""{"city":"Oslo","degrees":-3}""")) with { Duration = TimeSpan.FromMilliseconds(700) });

        _runningCall = new BitAiToolCallPart("call-e2e-running", "index_docs") { Definition = new BitAiToolDefinition("index_docs") { Title = "Index the docs" } };
        _runningCall.CompleteArguments(E2EAiScriptRun.Json("{}"));
        _runningCall.SetStatus(BitAiToolCallStatus.Running);

        _failedCall = Completed(new BitAiToolCallPart("call-e2e-failed", "get_weather") { Definition = new BitAiToolDefinition("get_weather") { Title = "Weather" } },
                                """{"city":"Atlantis"}""",
                                BitAiToolOutcome.Failure(new BitAiError("No weather station reports for Atlantis.") { Kind = BitAiErrorKind.Tool, Detail = "station lookup: 0 rows" }) with { Duration = TimeSpan.FromMilliseconds(1200) });

        _declinedCall = new BitAiToolCallPart("call-e2e-declined", "delete_records") { Definition = new BitAiToolDefinition("delete_records") { Title = "Delete records", Risk = BitAiToolRisk.Destructive } };
        _declinedCall.CompleteArguments(E2EAiScriptRun.Json("""{"filter":"archived"}"""));
        _declinedCall.SetStatus(BitAiToolCallStatus.AwaitingApproval);
        _declinedCall.SetApproval(BitAiApprovalDecision.Rejected);
        _declinedCall.SetResult(BitAiToolOutcome.Failure(new BitAiError("The user declined this tool call.") { Code = "rejected", Kind = BitAiErrorKind.Tool }));
        _declinedCall.SetStatus(BitAiToolCallStatus.Rejected);

        _cancelledCall = new BitAiToolCallPart("call-e2e-cancelled", "index_docs") { Definition = new BitAiToolDefinition("index_docs") { Title = "Index the docs" } };
        _cancelledCall.CompleteArguments(E2EAiScriptRun.Json("{}"));
        _cancelledCall.SetStatus(BitAiToolCallStatus.Running);
        _cancelledCall.SetStatus(BitAiToolCallStatus.Cancelled);

        _awaitingCall = new BitAiToolCallPart("call-e2e-awaiting", "delete_records") { Definition = new BitAiToolDefinition("delete_records") { Title = "Delete records", Risk = BitAiToolRisk.Destructive } };
        _awaitingCall.CompleteArguments(E2EAiScriptRun.Json("""{"filter":"archived"}"""));
        _awaitingCall.SetStatus(BitAiToolCallStatus.AwaitingApproval);

        _iconCall = Completed(new BitAiToolCallPart("call-e2e-icon", "get_weather") { Definition = new BitAiToolDefinition("get_weather") { Title = "Weather", Icon = "Sunny" } },
                              """{"city":"Lisbon"}""",
                              BitAiToolOutcome.FromText("Lisbon: 17 degrees"));
    }

    private static BitAiToolCallPart Completed(BitAiToolCallPart call, string arguments, BitAiToolOutcome outcome)
    {
        call.CompleteArguments(E2EAiScriptRun.Json(arguments));
        call.SetStatus(BitAiToolCallStatus.Running);
        call.SetResult(outcome);
        return call;
    }

    private static string AnswerOf(BitAiChatSession session) =>
        session.Conversation.Messages.LastOrDefault(m => m.Role.IsAssistant)?.GetText() ?? string.Empty;

    private static IEnumerable<(BitAiMessage Message, BitAiToolCallPart Call)> CallsOf(BitAiChatSession session) =>
        session.Conversation.Messages.Where(m => m.Role.IsAssistant).SelectMany(m => m.PartsOf<BitAiToolCallPart>().Select(c => (m, c)));

    private void StartLifecycle()
    {
        _lcSlice = 0;
        _lcCall = new BitAiToolCallPart($"call-e2e-lc-{Guid.NewGuid():N}", "index_docs") { Definition = new BitAiToolDefinition("index_docs") { Title = "Index the docs" } };
    }

    private void AppendLifecycle()
    {
        if (_lcCall is null || _lcSlice >= LifecycleSlices.Length) return;

        _lcCall.AppendArguments(LifecycleSlices[_lcSlice++]);
    }

    private void CompleteLifecycle() => _lcCall?.CompleteArguments();

    private void RunLifecycle() => _lcCall?.SetStatus(BitAiToolCallStatus.Running);

    private void SucceedLifecycle() =>
        _lcCall?.SetResult(BitAiToolOutcome.Success(E2EAiScriptRun.Json("""{"indexed":30,"passages":412}""")) with { Duration = TimeSpan.FromMilliseconds(1500) });

    private void FailLifecycle() =>
        _lcCall?.SetResult(BitAiToolOutcome.Failure(new BitAiError("The index is locked.") { Kind = BitAiErrorKind.Tool, Detail = "lock held by build 42" }) with { Duration = TimeSpan.FromMilliseconds(2500) });

    // A call driven off the renderer's thread, with no render of the page after it: what the cards show of it, they
    // show by following the call themselves.
    private void StartInBackground()
    {
        StartLifecycle();
        var call = _lcCall!;

        _ = Task.Run(async () =>
        {
            foreach (var slice in LifecycleSlices)
            {
                await Task.Delay(150);
                call.AppendArguments(slice);
            }

            await Task.Delay(150);
            call.CompleteArguments();
            call.SetStatus(BitAiToolCallStatus.Running);
            call.SetProgress(new BitAiToolProgress(0.5, "Halfway there"));
            await Task.Delay(300);
            call.SetResult(BitAiToolOutcome.FromText("Indexed 30 files.") with { Duration = TimeSpan.FromMilliseconds(900) });
        });
    }

    private Task AskWeatherAsync() => _toolSession.SubmitAsync("What is the weather in Oslo?");

    private Task AskApprovalAsync() => _approvalSession.SubmitAsync("Delete the inactive users");

    private Task AskParallelAsync() => _parallelSession.SubmitAsync("Compare Oslo and Lisbon");

    public async ValueTask DisposeAsync()
    {
        if (_toolSession is not null) await _toolSession.DisposeAsync();
        if (_approvalSession is not null) await _approvalSession.DisposeAsync();
        if (_parallelSession is not null) await _parallelSession.DisposeAsync();
        if (_sessionRenders is not null) await _sessionRenders.DisposeAsync();
    }
}
