using System.Collections.Concurrent;
using System.Web;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Tests.E2E.TestHost.AI;

/// <summary>
/// The scripted transport of the E2E host: every reply an AI page streams comes from one of the
/// <see cref="E2EAiScripts"/>, deterministic and - unless a page asks for a pace - with no delay between its events, so a
/// test reads the final state as soon as the round trip lands.
/// </summary>
/// <remarks>
/// <para>
/// It is the transport <c>AddBitBlazorUIAIServices</c> registers in both hosts (see <see cref="E2EAiSetup"/>), so a
/// <c>BitAiChat</c> with no <c>Session</c>, and every session made by <c>BitAiChatSessionFactory</c>, plays a script. Which
/// one, first match wins:
/// </para>
/// <list type="number">
/// <item>The script a transport was made for: <c>new BitAiChatSession(E2EAiTransport.For(E2EAiScripts.Markdown))</c>.</item>
/// <item>The first word of the user message that last named a script: send <c>markdown</c>, or <c>tool-approval delete
/// them</c>, and that script answers - and goes on answering the turns that follow it (an agent's question, a revision of an
/// artifact) until another message names a script.</item>
/// <item>The script the page chose: <c>@inject E2EAiScriptSelection AiScript</c>, then
/// <c>AiScript.Script = E2EAiScripts.Agent;</c> in <c>OnInitialized</c>.</item>
/// <item>The <c>?script=</c> of the page's address (<c>/e2e/aichat?script=reasoning</c>).</item>
/// <item><see cref="E2EAiScripts.Echo"/>.</item>
/// </list>
/// <para>
/// A pace between the events (a streaming state to catch, a stop button to press) is asked for the same way:
/// <c>E2EAiTransport.For(script, delayMs)</c>, <c>E2EAiScriptSelection.DelayMs</c>, or <c>?delay=</c>.
/// <see cref="E2EAiScripts.Hang"/> streams a first sentence and then waits for the turn to be stopped.
/// </para>
/// </remarks>
public sealed class E2EAiTransport : IBitAiTransport
{
    private readonly E2EAiScriptSelection? _selection;
    private readonly NavigationManager? _navigation;
    private readonly string? _script;
    private readonly int? _delayMs;

    // How often a script has been played for a conversation, since the message that chose it: what lets
    // E2EAiScripts.ErrorThenRecover fail the first attempt and answer the retry.
    private readonly ConcurrentDictionary<string, int> _attempts = new(StringComparer.Ordinal);

    /// <summary>The registered transport, resolved per scope (a circuit, or the WebAssembly app).</summary>
    public E2EAiTransport(E2EAiScriptSelection selection, NavigationManager navigation)
    {
        _selection = selection;
        _navigation = navigation;
    }

    private E2EAiTransport(string script, int delayMs)
    {
        if (E2EAiScripts.IsScript(script) is false) throw new ArgumentException($"No E2E AI script is named '{script}'.", nameof(script));

        _script = script;
        _delayMs = Math.Max(0, delayMs);
    }

    /// <summary>A transport that plays one script, whatever the user sends, for a session a page makes itself.</summary>
    /// <param name="script">One of <see cref="E2EAiScripts"/>.</param>
    /// <param name="delayMs">The pause between the events of the script, none by default.</param>
    public static E2EAiTransport For(string script, int delayMs = 0) => new(script, delayMs);

    public IAsyncEnumerable<BitAiEvent> StreamAsync(BitAiRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userTexts = request.Messages.Where(m => m.Role.IsUser).Select(m => m.GetText()).ToList();

        // The user message that last named a script, and so where this script's turns started.
        var promptIndex = -1;
        string? prompted = null;
        for (var i = userTexts.Count - 1; i >= 0 && _script is null; i--)
        {
            if (E2EAiScripts.Named(userTexts[i]) is { } named)
            {
                promptIndex = i;
                prompted = named;
                break;
            }
        }

        var script = _script ?? prompted ?? Valid(_selection?.Script) ?? Valid(Query("script")) ?? E2EAiScripts.Echo;
        var delay = _delayMs ?? (_selection is { DelayMs: > 0 } selection ? selection.DelayMs : int.TryParse(Query("delay"), out var d) ? Math.Max(0, d) : 0);

        var start = Math.Max(0, promptIndex);
        var attempt = _attempts.AddOrUpdate($"{request.ConversationId}|{script}|{start}", 1, (_, n) => n + 1);

        return E2EAiScripts.Play(script, new E2EAiScriptRun(request, userTexts, start, attempt, delay, cancellationToken));
    }

    private static string? Valid(string? script) => script is not null && E2EAiScripts.IsScript(script) ? script : null;

    private string? Query(string name)
    {
        if (_navigation is null) return null;

        try
        {
            return HttpUtility.ParseQueryString(new Uri(_navigation.Uri).Query)[name];
        }
        catch (InvalidOperationException)
        {
            // A navigation manager not initialized yet (a render outside an interactive one) has no address to read.
            return null;
        }
    }
}

/// <summary>
/// The script a page chose for the sessions of its scope (see <see cref="E2EAiTransport"/>), and the pace between the
/// events of every script played there. Scoped: a circuit's on the server, the app's in the browser.
/// </summary>
public sealed class E2EAiScriptSelection
{
    /// <summary>One of <see cref="E2EAiScripts"/>, or null for the <c>?script=</c> of the address, else the echo.</summary>
    public string? Script { get; set; }

    /// <summary>The pause between the events of a script, in milliseconds; 0 (the default) for none.</summary>
    public int DelayMs { get; set; }
}
