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

public partial class BitAiToolApprovalE2E : IAsyncDisposable
{
    private readonly List<string> _log = [];
    private readonly List<string> _decisions = [];
    private readonly BitAiToolApprovalParams[] _disabledParams = [new() { Disabled = true }];

    private BitAiRenderScheduler _sessionRenders = default!;
    private BitAiChatSession _session = default!;
    private int _submits;
    private int _requests;
    private int _rootClicks;

    private BitAiToolCallPart _destructiveCall = default!;
    private BitAiToolCallPart _writeCall = default!;
    private BitAiToolCallPart _unnamedCall = default!;
    private BitAiToolCallPart _keyboardCall = default!;
    private BitAiToolCallPart _disabledCall = default!;
    private BitAiToolCallPart _enabledCall = default!;
    private BitAiToolCallPart _paramsCall = default!;

    protected override void OnInitialized()
    {
        _sessionRenders = new BitAiRenderScheduler(() => InvokeAsync(StateHasChanged));
        _session = new BitAiChatSession(E2EAiTransport.For(E2EAiScripts.ToolApproval), tools: ToolRegistry, approvalPolicy: new BitAiDefaultToolApprovalPolicy());
        _session.StateChanged += _sessionRenders.Request;
        _session.ApprovalRequested += _ => Interlocked.Increment(ref _requests);

        Reset();
    }

    private void Reset()
    {
        _log.Clear();
        _destructiveCall = Awaiting("call-e2e-destructive", new BitAiToolDefinition("delete_records") { Title = "Delete records", Risk = BitAiToolRisk.Destructive }, """{"filter":"inactive users"}""");
        _writeCall = Awaiting("call-e2e-write", new BitAiToolDefinition("send_email") { Title = "Send email", Risk = BitAiToolRisk.Write, RequiresApproval = true }, """{"to":"ana@example.com"}""",
                              "Send the summary to ana@example.com?");
        _unnamedCall = Awaiting("call-e2e-unnamed", null, "{}", toolName: "archive_files");
        _keyboardCall = Awaiting("call-e2e-keyboard", new BitAiToolDefinition("delete_records") { Title = "Delete records", Risk = BitAiToolRisk.Destructive }, "{}");
        _disabledCall = Awaiting("call-e2e-disabled", new BitAiToolDefinition("delete_records") { Title = "Delete records", Risk = BitAiToolRisk.Destructive }, "{}");
        _enabledCall = Awaiting("call-e2e-enabled", new BitAiToolDefinition("send_email") { Title = "Send email" }, "{}");
        _paramsCall = Awaiting("call-e2e-params", new BitAiToolDefinition("send_email") { Title = "Send email" }, "{}");
    }

    private static BitAiToolCallPart Awaiting(string id, BitAiToolDefinition? definition, string arguments, string? reason = null, string? toolName = null)
    {
        var call = new BitAiToolCallPart(id, toolName ?? definition!.Name) { Definition = definition, ApprovalReason = reason };
        call.CompleteArguments(E2EAiScriptRun.Json(arguments));
        call.SetStatus(BitAiToolCallStatus.AwaitingApproval);
        return call;
    }

    private void Log(BitAiToolCallPart call, BitAiApprovalDecision decision) =>
        _log.Add($"{call.ToolCallId}:{decision}:{call.Status}");

    private static IEnumerable<BitAiToolCallPart> CallsOf(BitAiChatSession session) =>
        session.Conversation.Messages.Where(m => m.Role.IsAssistant).SelectMany(m => m.PartsOf<BitAiToolCallPart>());

    private Task AskAsync() => _session.SubmitAsync("Delete the inactive users");

    public async ValueTask DisposeAsync()
    {
        if (_session is not null) await _session.DisposeAsync();
        if (_sessionRenders is not null) await _sessionRenders.DisposeAsync();
    }
}
