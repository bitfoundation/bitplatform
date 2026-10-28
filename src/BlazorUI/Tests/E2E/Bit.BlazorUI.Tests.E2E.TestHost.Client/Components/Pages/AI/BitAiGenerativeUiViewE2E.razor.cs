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

public partial class BitAiGenerativeUiViewE2E : IAsyncDisposable
{
    // A finished part, as a session leaves it once the model has written the properties.
    private static BitAiGenerativeUiPart Card(string componentName, string props)
    {
        var part = new BitAiGenerativeUiPart(componentName);
        using var document = JsonDocument.Parse(props);
        part.SetProps(document.RootElement.Clone());
        part.Complete();
        return part;
    }

    private readonly BitAiGenerativeUiPart _cardPart = Card(E2EAiSetup.CardComponent, """{"title":"Visitors","value":1200}""");

    // The type of the registered component, by its own name: never a way to reach it.
    private readonly BitAiGenerativeUiPart _typeNamePart = Card(nameof(E2EAiCard), """{"title":"Visitors","value":1}""");
    private readonly BitAiGenerativeUiPart _fullTypeNamePart = Card(typeof(E2EAiCard).FullName!, """{"title":"Visitors","value":1}""");

    // A value that does not bind (a word for an integer), an undeclared property, and no title at all.
    private readonly BitAiGenerativeUiPart _badPart = Card(E2EAiSetup.CardComponent, """{"value":"many","color":"red"}""");
    private readonly BitAiGenerativeUiPart _badPartQuiet = Card(E2EAiSetup.CardComponent, """{"value":"many","color":"red"}""");
    private readonly BitAiGenerativeUiPart _badPartVars = Card(E2EAiSetup.CardComponent, """{"value":"many","color":"red"}""");

    private readonly BitAiGenerativeUiPart _rtlPart = Card(E2EAiSetup.CardComponent, """{"title":"Visitors","value":7}""");
    private readonly BitAiGenerativeUiPart _rtlUnknownPart = Card("not_registered", """{}""");

    // The props of a card arriving a few characters at a time, the way a model writes them.
    private readonly BitAiGenerativeUiPart _streamingPart = new(E2EAiSetup.CardComponent);
    private readonly BitAiGenerativeUiPart _streamingUnknownPart = new("not_registered");
    private int _propsStep;
    private static readonly string[] PropsDeltas = ["{\"title\":\"Sig", "nups\",\"value\":8", "5}"];

    private void AppendProps()
    {
        if (_propsStep < PropsDeltas.Length)
        {
            _streamingPart.AppendProps(PropsDeltas[_propsStep]);
            _propsStep++;
        }
        else if (_streamingPart.IsStreaming)
        {
            _streamingPart.Complete();
            _propsStep++;
        }
    }

    // The generative scripts of the E2E transport: two e2e_card components between two sentences, and one component
    // nobody registered.
    private readonly BitAiChatSession _generativeSession = new(E2EAiTransport.For(E2EAiScripts.Generative));
    private readonly BitAiChatSession _unknownSession = new(E2EAiTransport.For(E2EAiScripts.GenerativeUnknown));
    private BitAiRenderScheduler? _scheduler;

    protected override void OnInitialized()
    {
        _scheduler = new(() => InvokeAsync(StateHasChanged));
        _generativeSession.StateChanged += _scheduler.Request;
        _unknownSession.StateChanged += _scheduler.Request;
    }

    private Task GenerativeAsync() => _generativeSession.SubmitAsync("Show the numbers");

    private Task UnknownAsync() => _unknownSession.SubmitAsync("Show something");

    private static BitAiMessage? Answer(BitAiChatSession session) =>
        session.Conversation.LastMessage is { Role.IsAssistant: true } last ? last : null;

    private static int Answers(BitAiChatSession session) =>
        session.Conversation.Messages.Count(m => m.Role.IsAssistant && m.Status == BitAiMessageStatus.Complete);

    public async ValueTask DisposeAsync()
    {
        if (_scheduler is not null) await _scheduler.DisposeAsync();

        await _generativeSession.DisposeAsync();
        await _unknownSession.DisposeAsync();
    }
}
