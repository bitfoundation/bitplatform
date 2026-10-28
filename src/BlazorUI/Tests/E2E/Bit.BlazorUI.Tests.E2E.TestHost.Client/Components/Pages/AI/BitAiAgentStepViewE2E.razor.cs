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
using System.Text.Json;

namespace Bit.BlazorUI.Tests.E2E.TestHost.Components.Pages.AI;

public partial class BitAiAgentStepViewE2E
{
    private readonly BitAiAgentStep _pending = new("pending", "Pending step") { Detail = "Waits its turn" };
    private readonly BitAiAgentStep _running = new("running", "Running step");
    private readonly BitAiAgentStep _completed = new("completed", "Completed step");
    private readonly BitAiAgentStep _blocked = new("blocked", "Blocked step");
    private readonly BitAiAgentStep _failed = new("failed", "Failed step");
    private readonly BitAiAgentStep _skipped = new("skipped", "Skipped step");

    private BitAiAgentStep _live = new("live", "Live step");
    private int _liveCount;

    private readonly BitAiConversation _conversation = new();
    private readonly BitAiAgentStep _toolStep = new("tools", "Search the docs");
    private BitAiToolCallPart _search = default!;

    protected override void OnInitialized()
    {
        _running.Start();

        _completed.Start();
        _completed.Complete(summary: "3 sources found");

        _blocked.Start();
        _blocked.Block();

        _failed.Start();
        _failed.Complete(BitAiAgentStepStatus.Failed, error: new BitAiError("The service timed out"));

        _skipped.Complete(BitAiAgentStepStatus.Skipped);

        // A running search and a failed call, both found in the conversation, and one the conversation does not know.
        var message = _conversation.AddMessage(new BitAiMessage(BitAiRole.Assistant));
        _search = new BitAiToolCallPart("call_search", "search_docs");
        message.AddPart(_search);
        var flaky = new BitAiToolCallPart("call_flaky", "flaky_service");
        message.AddPart(flaky);
        flaky.CompleteArguments(Json("{}"));
        flaky.SetResult(BitAiToolOutcome.Failure("The service is down"));

        _toolStep.Start();
        _toolStep.AddToolCall("call_search");
        _toolStep.AddToolCall("call_flaky");
        _toolStep.AddToolCall("call_unknown");
    }

    private void ResetLive() => _live = new BitAiAgentStep($"live-{++_liveCount}", "Live step");

    private void FinishSearch()
    {
        _search.CompleteArguments(Json("""{"query":"render modes"}"""));
        _search.SetResult(BitAiToolOutcome.Success(Json("""{"hits":3}""")));
        _toolStep.Complete(summary: "Searched");
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
