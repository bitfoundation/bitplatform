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

public partial class BitAiToolResultViewE2E
{
    private readonly BitAiToolResultPart _text = new("call-e2e-text", BitAiToolOutcome.FromText("2 results\n1. bitplatform.dev\n2. github.com/bitfoundation")) { ToolName = "web_search" };
    private readonly BitAiToolResultPart _untitled = new("call-e2e-untitled", BitAiToolOutcome.FromText("done"));
    private readonly BitAiToolResultPart _json = new("call-e2e-json", BitAiToolOutcome.Success(E2EAiScriptRun.Json("""{"hits":2,"top":"bitplatform.dev","sources":["web","docs"]}"""))) { ToolName = "web_search" };
    private readonly BitAiToolResultPart _failed = new("call-e2e-failed", BitAiToolOutcome.Failure(new BitAiError("The search provider timed out.") { Kind = BitAiErrorKind.Tool, Detail = "timeout after 30s" })) { ToolName = "web_search" };
    private readonly BitAiToolResultPart _empty = new("call-e2e-empty", BitAiToolOutcome.Success(E2EAiScriptRun.Json("null"))) { ToolName = "noop" };
    private readonly BitAiToolResultPart _long = new("call-e2e-long", BitAiToolOutcome.FromText(new string('x', 120))) { ToolName = "long_token" };
    private readonly BitAiToolResultPart _dispatched = new("call-e2e-dispatched", BitAiToolOutcome.FromText("dispatched")) { ToolName = "web_search" };
    private readonly BitAiMessage _message = new(BitAiRole.Assistant, "msg-e2e-trv");

    protected override void OnInitialized()
    {
        _message.AddPart(_dispatched);
    }
}
