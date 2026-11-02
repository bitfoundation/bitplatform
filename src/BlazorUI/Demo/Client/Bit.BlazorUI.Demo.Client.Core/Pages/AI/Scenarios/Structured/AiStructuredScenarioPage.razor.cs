using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Structured;

public partial class AiStructuredScenarioPage
{
    // The shape the second demo asks for: a JSON Schema, written by hand here, and the name the part is given.
    private static readonly BitAiResponseFormat OptionsFormat = BitAiResponseFormat.FromSchema(AiScript.Json("""
        {
          "type": "array",
          "items": {
            "type": "object",
            "required": ["option", "effort", "risk"],
            "properties": {
              "option": { "type": "string" },
              "effort": { "type": "string", "enum": ["Low", "Medium", "High"] },
              "risk": { "type": "string", "enum": ["Low", "Medium", "High"] },
              "notes": { "type": "string" }
            }
          }
        }
        """), "options");

    private readonly BitAiChatSession _session = new(AiDemoContentTransports.Structured());

    private readonly BitAiChatSession _formatSession = new(AiDemoContentTransports.StructuredAsRequested(), options: new BitAiChatSessionOptions
    {
        RequestOptions = new BitAiRequestOptions { ResponseFormat = OptionsFormat },
    });

    private bool _askForJson = true;

    private readonly string[] _suggestions = ["Compare our options"];

    // The options are an immutable record the session reads at the start of every request: a new one is the change.
    private void ApplyResponseFormat() => _formatSession.Options = _formatSession.Options with
    {
        RequestOptions = new BitAiRequestOptions { ResponseFormat = _askForJson ? OptionsFormat : null },
    };

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await JSRuntime.InvokeVoid("highlightSnippet");
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await _session.DisposeAsync();
        await _formatSession.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
