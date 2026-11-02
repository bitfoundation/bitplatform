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

public partial class BitAiStructuredDataViewE2E : IAsyncDisposable
{
    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private readonly BitAiStructuredDataPart _flightPart = new(Parse("""
        {"flightNumber":"BA 117","from":"LHR","departure_time":"09:40","seats":2,"refundable":false,"meal":null,"gate":{"terminal":5,"door":"B32"},"stops":["KEF"]}
        """));

    private readonly BitAiStructuredDataPart _ordersPart = new(Parse("""
        [{"order":"ord_4821","status":"shipped","total":129.5},{"order":"ord_4822","status":"pending","total":42}]
        """));

    private readonly BitAiStructuredDataPart _tagsPart = new(Parse("""["blazor","webassembly","streaming"]"""));

    private readonly BitAiStructuredDataPart _mixedPart = new(Parse("""[{"a":1},"two"]"""));

    private readonly BitAiStructuredDataPart _stringPart = new(Parse("\"just a string\""));

    private readonly BitAiStructuredDataPart _rtlPart = new(Parse("""{"name":"Ada","city":"London"}"""));

    private readonly BitAiStructuredDataPart _varsPart = new(Parse("""{"name":"Ada","manager":null}"""));

    // A part no delta has reached yet, then the deltas of a table one by one.
    private readonly BitAiStructuredDataPart _partialPart = new("options");
    private int _partialStep;
    private static readonly string[] PartialDeltas = ["[{\"name\":\"Ada\"", ",\"role\":\"Engineer\"}", ",{\"name\":\"Grace\"}]"];

    private void AppendPartial()
    {
        if (_partialStep < PartialDeltas.Length)
        {
            _partialPart.Append(PartialDeltas[_partialStep]);
            _partialStep++;
        }
        else if (_partialPart.IsStreaming)
        {
            _partialPart.Complete();
            _partialStep++;
        }
    }

    // The structured script of the E2E transport: four rows of JSON, 24 characters a delta, paced so the table can be
    // seen filling in.
    private readonly BitAiChatSession _streamSession = new(E2EAiTransport.For(E2EAiScripts.Structured, 150));

    // The form script: a JSON schema streamed under the schema the host renders as a BitAiForm.
    private readonly BitAiChatSession _formSession = new(E2EAiTransport.For(E2EAiScripts.Form));

    private BitAiRenderScheduler? _scheduler;

    protected override void OnInitialized()
    {
        _scheduler = new(() => InvokeAsync(StateHasChanged));
        _streamSession.StateChanged += _scheduler.Request;
        _formSession.StateChanged += _scheduler.Request;
    }

    private Task StreamAsync() => _streamSession.SubmitAsync("Compare the options");

    private Task FormAsync() => _formSession.SubmitAsync("Book a table");

    private static BitAiMessage? Answer(BitAiChatSession session) =>
        session.Conversation.LastMessage is { Role.IsAssistant: true } last ? last : null;

    private static int Answers(BitAiChatSession session) =>
        session.Conversation.Messages.Count(m => m.Role.IsAssistant && m.Status == BitAiMessageStatus.Complete);

    public async ValueTask DisposeAsync()
    {
        if (_scheduler is not null) await _scheduler.DisposeAsync();

        await _streamSession.DisposeAsync();
        await _formSession.DisposeAsync();
    }
}
