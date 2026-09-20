using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using Bit.BlazorUI.Demo.Client.Core.Components;

namespace Bit.BlazorUI.Demo.Client.Core.Pages;

/// <summary>
/// The MCP server, demonstrated by talking to it.
/// <para>
/// A page that only described the protocol would be a page a reader has to take on trust. This one
/// is a working MCP client: it opens a session with this very deployment when it loads, and every
/// section below sends a real request and shows both halves of the exchange. Nothing is mocked, and
/// the request pane is never prettified into something the reader could not have typed themselves -
/// the point being made is that the protocol is a JSON-RPC POST with a JSON body.
/// </para>
/// </summary>
public partial class McpServerPage
{
    // Long enough that a search result or a component's API arrives whole, short enough that one
    // answer does not become the entire screen. The full payload is on the wire either way.
    private const int MaxPaneLength = 3_000;
    private const int MaxResultLength = 8_000;

    /// <summary>How many completion values are shown as chips before the rest are counted instead.</summary>
    private const int MaxCompletionChips = 24;

    /// <summary>The anchor of the section "Try it" scrolls to - the same id the markup gives it.</summary>
    private const string CallSectionId = "call";

    /// <summary>
    /// The arguments each tool is worth trying first. A tool's schema says what is required, not
    /// what makes a good question of it - and a playground whose fields start empty demonstrates
    /// nothing.
    /// </summary>
    private static readonly Dictionary<string, string> _examples = new(StringComparer.Ordinal)
    {
        ["SearchBitBlazorUI"] = """{ "query": "searchable multi select with chips", "limit": 5 }""",
        ["GetBitBlazorUIComponent"] = """{ "name": "BitDropdown" }""",
        ["GetBitBlazorUIComponentExamples"] = """{ "name": "BitButton", "example": "Loading" }""",
        ["GetBitBlazorUIType"] = """{ "typeName": "BitColor" }""",
        ["GetBitBlazorUISetupGuide"] = """{ "hostingModel": "web-app" }""",
        ["GetBitBlazorUIThemingGuide"] = """{ "section": "Design tokens" }""",
        ["FindBitBlazorUIIcons"] = """{ "query": "shopping cart" }""",
    };

    /// <summary>The query string each tool is worth trying first over the plain HTTP mirror.</summary>
    private static readonly Dictionary<string, string> _httpExamples = new(StringComparer.Ordinal)
    {
        ["SearchBitBlazorUI"] = "query=date range picker&limit=5",
        ["GetBitBlazorUIComponent"] = "name=Toast",
        ["GetBitBlazorUIComponentExamples"] = "name=BitDialog&example=Basic",
        ["GetBitBlazorUIType"] = "typeName=BitVariant",
        ["GetBitBlazorUISetupGuide"] = "hostingModel=wasm",
        ["GetBitBlazorUIThemingGuide"] = "section=Presets",
        ["FindBitBlazorUIIcons"] = "query=chevron down",
    };

    /// <summary>The arguments a prompt is worth showing filled in, since that is where it stops being a menu entry.</summary>
    private static readonly Dictionary<string, string> _promptArguments = new(StringComparer.Ordinal)
    {
        ["add-bit-blazorui-to-app"] = """{ "hostingModel": "web-app" }""",
        ["build-bit-blazorui-screen"] = """{ "screen": "a product list with search, filters and a details panel" }""",
        ["migrate-to-bit-blazorui"] = """{ "current": "MudBlazor" }""",
        ["theme-bit-blazorui-app"] = """{ "look": "our brand blue #1B6EC2, dark mode by default" }""",
        ["debug-bit-blazorui-issue"] = """{ "symptom": "every component renders as plain unstyled HTML" }""",
    };

    private static readonly string[] _resourceExamples =
    [
        "bitblazorui://components",
        "bitblazorui://components/BitDataGrid",
        "bitblazorui://types/BitSize",
        "bitblazorui://theming",
    ];

    /// <summary>
    /// The arguments worth completing, and the reference each one belongs to. A completion is always
    /// about one argument of one prompt or one resource template - there is no "complete anything"
    /// request - so a demonstration has to name both, and these are this server's closed sets.
    /// </summary>
    private static readonly CompletionTarget[] _completionTargets =
    [
        new("bitblazorui://components/{name} → name", """{ "type": "ref/resource", "uri": "bitblazorui://components/{name}" }""", "name"),
        new("bitblazorui://types/{typeName} → typeName", """{ "type": "ref/resource", "uri": "bitblazorui://types/{typeName}" }""", "typeName"),
        new("bitblazorui://theming/{section} → section", """{ "type": "ref/resource", "uri": "bitblazorui://theming/{section}" }""", "section"),
        new("add-bit-blazorui-to-app → hostingModel", """{ "type": "ref/prompt", "name": "add-bit-blazorui-to-app" }""", "hostingModel"),
    ];

    /// <summary>
    /// What the server publishes, and why it publishes it that way. Written here rather than
    /// counted off the wire: these are the design decisions, and a decision does not become true or
    /// false because a handshake has or has not happened yet.
    /// </summary>
    private static readonly Fact[] _facts =
    [
        new("Tools", "7, over MCP and as GET /api/mcp/{tool}",
            "Search across everything; the full API of one component; its worked examples; any public type; the setup per hosting model; the theming reference a chapter at a time; and the icon search. Three of them answer with a listing when called with no argument, which is why there are no listing tools."),
        new("Annotations", "readOnlyHint, idempotentHint, destructiveHint, openWorldHint",
            "Every tool reads, reads only from this process, and answers the same way twice. A client that is told so can consult the documentation without stopping to ask permission first."),
        new("Answers", "one Markdown text block per call",
            "A component's parameter table is sixty rows of four fields: as JSON that is the four field names repeated sixty times. No tool declares an outputSchema either - with one, the SDK sends the object in structuredContent AND the identical payload in the text block the protocol wants there anyway, so every answer would cross the wire twice."),
        new("Instructions", "returned by initialize",
            "The one block of text the server writes into the model's context before it has called anything: which tool to reach for first, and the six rules that separate markup that compiles from markup that looks right."),
        new("Prompts", "add-bit-blazorui-to-app, build-bit-blazorui-screen, migrate-to-bit-blazorui, theme-bit-blazorui-app, debug-bit-blazorui-issue",
            "Ready-made workflows, each spending its words on the order to call the tools in rather than repeating the standing rules the instructions already carry."),
        new("Resources", "bitblazorui://components, /components/{name}, /components/{name}/examples, /types/{typeName}, /setup/{hostingModel}, /theming, /theming/{section}",
            "The same knowledge as documents a client can attach or browse, addressed by URI and read out of the same catalogs the tools answer from."),
        new("Completions", "completion/complete",
            "Every prompt argument and template placeholder is drawn from a closed set - the component names, the public types, the theming chapters, the hosting models - so a client's picker can offer them rather than ask someone to type one blind."),
        new("Transport", "POST /mcp (streamable HTTP, stateless)",
            "JSON-RPC in the body; the result arrives as a text/event-stream frame. No session id - SEP-2567 removed them from this transport, so any replica can answer any request."),
    ];

    // Indented, and with the relaxed encoder: the panes are already a re-serialization rather than
    // the bytes verbatim, and a quote inside a string reads as \" the way anyone would write it -
    // the default encoder's " turns a nested tool call into line noise. Nothing here is
    // interpolated into markup, so the escaping the default encoder exists to provide is not needed.
    private static readonly JsonSerializerOptions _readable = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly List<ToolInfo> _tools = [];
    private readonly List<BitDropdownItem<string>> _toolItems = [];
    private readonly List<BitDropdownItem<string>> _promptItems = [];

    private readonly List<BitDropdownItem<string>> _completionItems =
        [.. _completionTargets.Select(t => new BitDropdownItem<string> { Text = t.Label, Value = t.Label })];

    private McpExchange.Roundtrip? _handshake;
    private McpExchange.Roundtrip? _discovery;
    private McpExchange.Roundtrip? _call;
    private McpExchange.Roundtrip? _extras;
    private McpExchange.Roundtrip? _completion;
    private McpExchange.Roundtrip? _http;

    /// <summary>
    /// Whether there is an HTTP origin to call at all. A Blazor Hybrid build with no configured API
    /// server has none, and a playground that answers every button with an exception reads as a
    /// broken server rather than as a page that cannot run here.
    /// </summary>
    private bool _live;

    private string _origin = string.Empty;

    // Kept, and still sent when the server does supply one, because a client that only works
    // against a stateless server is not an MCP client. Streamable HTTP dropped Mcp-Session-Id in
    // SEP-2567 and the transport this site runs is stateless, so in practice this stays null - the
    // chip beside the button says "stateless" rather than reporting a header as missing.
    private string? _sessionId;

    // The newest revision this client asks for. The server answers with the revision it settled on
    // rather than the one that was asked for, which is why the field is not a const.
    private string _protocolVersion = "2025-11-25";
    private string? _serverName;
    private string? _instructions;
    private int? _componentCount;

    // The count is read off the server's own instructions, so it is never typed on this page - and
    // the sentence still reads before the handshake that supplies it has answered.
    private string CompletionsDescription
        => $"How a client's picker offers the {(_componentCount is null ? "component" : $"{_componentCount} component")} names, the type names and the theming chapters without anyone typing one blind.";

    private string _selectedTool = string.Empty;
    private string? _selectedDescription;
    private string _arguments = "{}";
    private string _selectedPrompt = string.Empty;
    private string _resourceUri = "bitblazorui://components";
    private string _httpTool = "SearchBitBlazorUI";
    private string _httpQuery = "query=date range picker&limit=5";

    private string _completionTarget = _completionTargets[0].Label;
    private string _completionValue = "Dat";
    private string[] _completions = [];

    // Completions fire per keystroke, so their answers can arrive out of order - "Da" after "Dat"
    // would leave the wrong list on screen for as long as nobody typed again. Each request takes
    // the next number and only the newest one is allowed to write its result.
    private int _completionRequest;

    private bool _busy;
    private int _id;

    private string McpEndpoint => $"{_origin}mcp";
    private string HttpEndpoint => $"{_origin}api/mcp/";

    private string _clientConfig = string.Empty;
    private string _vsCodeConfig = string.Empty;
    private string _initializeSample = string.Empty;

    private string _agentRules = """
        ### bit BlazorUI

        - For all UI work in this Blazor project, you **MUST** use the bit BlazorUI MCP server.
        - Start with `SearchBitBlazorUI`: this library's name for a thing is often not the one the task
          suggests (a select is `BitDropdown`, a toast is `BitSnackBar`, a skeleton is `BitShimmer`).
        - Before writing markup, call `GetBitBlazorUIComponent` for every component you are about to use,
          and `GetBitBlazorUIComponentExamples` for the working code of anything non-obvious.
        - Prefer `Bit*` components over plain HTML/CSS or another UI library.
        """;

    private string _toolsBadge => _tools.Count == 0 ? "7 tools" : $"{_tools.Count} tools";
    private string _promptsBadge => _promptItems.Count == 0 ? "5 prompts" : $"{_promptItems.Count} prompts";
    private string _resourcesBadge => "7 resources";

    protected override Task OnInitAsync()
    {
        // The origin to call, and to print in the configs a reader copies out. On the web that is
        // where the page is being read - the deployment they are looking at is the one that answers
        // - and in a Blazor Hybrid build it is not an http origin at all, so the configured API
        // server stands in. When neither is one, there is nothing here to call and the live client
        // says so rather than answering every button with an exception.
        _origin = NavigationManager.BaseUri;

        if (IsHttp(_origin) is false) _origin = HttpClient.BaseAddress?.ToString() ?? _origin;

        if (_origin.EndsWith('/') is false) _origin += "/";

        _live = IsHttp(_origin);

        _clientConfig = $$"""
            {
              "mcpServers": {
                "bitBlazorUI": {
                  "type": "http",
                  "url": "{{_origin}}mcp"
                }
              }
            }
            """;

        _vsCodeConfig = $$"""
            {
              "servers": {
                "bitBlazorUI": {
                  "type": "http",
                  "url": "{{_origin}}mcp"
                }
              }
            }
            """;

        _initializeSample = $$"""
            POST {{_origin}}mcp
            Content-Type: application/json
            Accept: application/json, text/event-stream

            {
              "jsonrpc": "2.0",
              "id": 1,
              "method": "initialize",
              "params": {
                "protocolVersion": "{{_protocolVersion}}",
                "capabilities": {},
                "clientInfo": { "name": "bit-blazorui-docs", "version": "1.0" }
              }
            }
            """;

        return base.OnInitAsync();
    }

    private static bool IsHttp(string origin) => origin.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                              || origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Opens the session and fills the four sections that start empty. It needs a live circuit, so
    /// it runs from here: during a prerender pass there is none, and this method does not run then.
    /// </summary>
    protected override async Task OnAfterFirstRenderAsync()
    {
        if (_live is false) return;

        await Handshake();
        await ListTools();

        // The prompt picker in section 4 and the completion demo in section 5 both need a round
        // trip before they have anything to show, and a section that starts empty reads as one that
        // does not work. Both are cheap, and neither blocks the two above.
        await ListPrompts();
        await Complete();

        StateHasChanged();
    }

    private async Task Handshake()
    {
        _sessionId = null;

        var parameters = $$"""
            {
              "protocolVersion": "{{_protocolVersion}}",
              "capabilities": {},
              "clientInfo": { "name": "bit-blazorui-docs", "version": "1.0" }
            }
            """;

        var (call, result) = await Rpc("initialize", parameters);
        _handshake = call;

        if (result is { } payload && payload.TryGetProperty("serverInfo", out var info))
        {
            // The title when the server states one - it is what a person is meant to read - and the
            // name only as the fallback, which is what a server that predates the field will send.
            var identity = Text(info, "title") is { Length: > 0 } title ? title : Text(info, "name");

            _serverName = $"{identity} {Text(info, "version").Split('+')[0]}".Trim();

            _instructions = payload.TryGetProperty("instructions", out var instructions) ? instructions.GetString() : null;
            _componentCount = ComponentCount(_instructions);

            if (payload.TryGetProperty("protocolVersion", out var version) && version.GetString() is { } negotiated)
            {
                // Answering in a different revision than the one asked for is allowed, and every
                // later request has to carry the one that was agreed rather than the one requested.
                _protocolVersion = negotiated;
            }
        }

        // A notification carries no id, so the server answers 202 with an empty body rather than a
        // result. It is not shown as an exchange for that reason - there would be nothing to show.
        await Rpc("notifications/initialized", null, notification: true);
    }

    private async Task ListTools()
    {
        var (call, result) = await Rpc("tools/list", null);
        _discovery = call;

        if (result is not { } payload || payload.TryGetProperty("tools", out var tools) is false) return;

        _tools.Clear();
        foreach (var tool in tools.EnumerateArray())
        {
            _tools.Add(new ToolInfo(
                Name: Text(tool, "name"),
                Description: Text(tool, "description"),
                Arguments: ArgumentNames(tool),
                // Absent means "assume the worst", which is the whole reason a server states it.
                ReadOnly: tool.TryGetProperty("annotations", out var annotations)
                          && annotations.TryGetProperty("readOnlyHint", out var readOnly)
                          && readOnly.ValueKind is JsonValueKind.True));
        }

        // tools/list has no defined order - this one arrives in whatever order the server reflected
        // over its own methods - so the table sorts, or the same server reads differently per build.
        _tools.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        _toolItems.Clear();
        _toolItems.AddRange(_tools.Select(t => new BitDropdownItem<string> { Text = t.Name, Value = t.Name }));

        // Only when there is nothing to keep: Load resets the arguments, and re-sending tools/list
        // to look at the table again would otherwise throw away whatever was typed in section 3.
        if (_tools.Any(t => t.Name == _selectedTool) is false)
        {
            Load(_tools.FirstOrDefault(t => t.Name == "SearchBitBlazorUI")?.Name ?? _tools.FirstOrDefault()?.Name);
        }
        else
        {
            _selectedDescription = _tools.FirstOrDefault(t => t.Name == _selectedTool)?.Description;
        }

        if (_tools.Any(t => t.Name == _httpTool) is false) _httpTool = _tools.FirstOrDefault()?.Name ?? string.Empty;
    }

    /// <summary>
    /// The tool table's "Try it": load the tool with its example arguments, bring the section that
    /// holds it into view, and send it. Loading alone would be correct and look like nothing
    /// happened - the controls it changes are a screen below the button.
    /// </summary>
    private async Task TryTool(string name)
    {
        Load(name);

        // Rendered before scrolling, so the section being scrolled to already shows the tool that
        // was picked rather than the previous one.
        StateHasChanged();

        // The anchor DocSection puts on its own heading, which already carries the scroll margin
        // that clears the sticky app header.
        await JSRuntime.ScrollToElement(CallSectionId);

        await CallTool();
    }

    private void Load(string? name)
    {
        if (name is null || _tools.Any(t => t.Name == name) is false) return;

        _selectedTool = name;
        OnToolChanged();
    }

    private void OnToolChanged()
    {
        _selectedDescription = _tools.FirstOrDefault(t => t.Name == _selectedTool)?.Description;

        ResetArguments();
    }

    private void ResetArguments()
    {
        _arguments = _examples.TryGetValue(_selectedTool, out var example) ? example : "{}";
    }

    /// <summary>
    /// The HTTP mirror's picker loads that tool's example query the way the tool picker loads its
    /// arguments - a query string left over from the previous tool is one that answers with an
    /// argument error, which demonstrates the wrong thing.
    /// </summary>
    private void OnHttpToolChanged()
    {
        _httpQuery = _httpExamples.GetValueOrDefault(_httpTool, string.Empty);
    }

    private async Task CallTool()
    {
        if (string.IsNullOrEmpty(_selectedTool)) return;

        // Parsed here rather than shipped and rejected: a JSON-RPC parse error would come back as a
        // protocol error, which reads as "the server broke" instead of "the textarea has a typo".
        try
        {
            using var parsed = JsonDocument.Parse(_arguments);

            // The envelope below drops this in as the value of "arguments", which the protocol says
            // is an object. A number, a string or an array parses perfectly well and then produces
            // exactly the protocol error this check exists to keep off the screen.
            if (parsed.RootElement.ValueKind is not JsonValueKind.Object)
            {
                _call = Invalid("tools/call", "The arguments have to be a JSON object - a name and a value per argument, in braces.");
                return;
            }
        }
        catch (JsonException exception)
        {
            _call = Invalid("tools/call", $"The arguments are not valid JSON: {exception.Message}");
            return;
        }

        var parameters = $$"""
            {
              "name": "{{_selectedTool}}",
              "arguments": {{_arguments.Trim()}}
            }
            """;

        (_call, _) = await Rpc("tools/call", parameters);
    }

    private async Task ListPrompts()
    {
        var (call, result) = await Rpc("prompts/list", null);
        _extras = call;

        if (result is not { } payload || payload.TryGetProperty("prompts", out var prompts) is false) return;

        _promptItems.Clear();
        _promptItems.AddRange(prompts.EnumerateArray()
            .Select(p => Text(p, "name"))
            .Select(name => new BitDropdownItem<string> { Text = name, Value = name }));

        if (_promptItems.Any(p => p.Value == _selectedPrompt) is false)
        {
            _selectedPrompt = _promptItems.FirstOrDefault()?.Value ?? string.Empty;
        }
    }

    private async Task ListResources() => (_extras, _) = await Rpc("resources/list", null);

    /// <summary>
    /// The parameterised resources, which resources/list deliberately leaves out: a template is not
    /// a document, it is the shape of the URIs that address a family of them.
    /// </summary>
    private async Task ListResourceTemplates() => (_extras, _) = await Rpc("resources/templates/list", null);

    /// <summary>
    /// prompts/get, which is where a prompt stops being a menu entry and becomes the workflow. The
    /// arguments an example needs are supplied here so the exchange shows one that was filled in.
    /// </summary>
    private async Task GetPrompt()
    {
        if (string.IsNullOrEmpty(_selectedPrompt)) return;

        var arguments = _promptArguments.GetValueOrDefault(_selectedPrompt, "{}");

        (_extras, _) = await Rpc("prompts/get", $$"""{ "name": "{{_selectedPrompt}}", "arguments": {{arguments}} }""");
    }

    /// <summary>
    /// One completion/complete round trip for whatever is currently typed. Called from the picker
    /// and from every keystroke of the input, which is exactly how a client's own picker uses it.
    /// </summary>
    private async Task Complete()
    {
        var target = _completionTargets.FirstOrDefault(t => t.Label == _completionTarget) ?? _completionTargets[0];
        var request = ++_completionRequest;

        var parameters = $$"""
            {
              "ref": {{target.Reference}},
              "argument": { "name": "{{target.Argument}}", "value": {{JsonSerializer.Serialize(_completionValue)}} }
            }
            """;

        // Not marked busy: this runs on every keystroke, and disabling every button on the page
        // while it does would make the rest of the page flicker for as long as someone is typing.
        var (call, result) = await Rpc("completion/complete", parameters, busy: false);

        // A slower earlier request must not overwrite the answer to a later one.
        if (request != _completionRequest) return;

        _completion = call;
        _completions = result is { } payload && payload.TryGetProperty("completion", out var completion)
                       && completion.TryGetProperty("values", out var values) && values.ValueKind is JsonValueKind.Array
            ? [.. values.EnumerateArray().Select(v => v.GetString() ?? string.Empty)]
            : [];

        StateHasChanged();
    }

    private Task ReadResource() => ReadResource(_resourceUri);

    private async Task ReadResource(string uri)
    {
        _resourceUri = uri;

        // Serialized rather than interpolated: the URI is free text from the box above, and a quote
        // or a backslash in it would otherwise close the string and send a malformed request.
        (_extras, _) = await Rpc("resources/read", $$"""{ "uri": {{JsonSerializer.Serialize(uri)}} }""");
    }

    /// <summary>One JSON-RPC round trip to /mcp, formatted for both panes.</summary>
    /// <param name="busy">
    /// Whether the page's controls are disabled while it runs. False for the calls a keystroke
    /// makes, where disabling the whole page for each one is worse than letting two overlap.
    /// </param>
    private async Task<(McpExchange.Roundtrip Call, JsonElement? Result)> Rpc(string method, string? parameters, bool notification = false, bool busy = true)
    {
        // Only a call that claims the page clears the flag afterwards. A completion firing on a
        // keystroke must not re-enable the buttons a tools/call in flight has disabled.
        if (busy)
        {
            _busy = true;
            StateHasChanged();
        }

        var envelope = notification
            ? $$"""{ "jsonrpc": "2.0", "method": "{{method}}" }"""
            : $$"""{ "jsonrpc": "2.0", "id": {{++_id}}, "method": "{{method}}"{{(parameters is null ? "" : $", \"params\": {parameters}")}} }""";

        using var request = new HttpRequestMessage(HttpMethod.Post, McpEndpoint)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "application/json")
        };

        // Both are offered because the transport may answer either way: a single result comes back
        // as one text/event-stream frame, and that is what this server does.
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (_sessionId is not null) request.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);
        if (notification is false || _sessionId is not null) request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", _protocolVersion);

        var stopwatch = Stopwatch.StartNew();

        HttpResponseMessage response;
        string body;

        try
        {
            response = await HttpClient.SendAsync(request, CurrentCancellationToken);
            body = await response.Content.ReadAsStringAsync(CurrentCancellationToken);
        }
        catch (Exception exception)
        {
            return (Invalid(method, exception.Message, Pretty(envelope)), null);
        }
        finally
        {
            stopwatch.Stop();
            if (busy) _busy = false;
        }

        using (response)
        {
            if (response.Headers.TryGetValues("Mcp-Session-Id", out var session)) _sessionId = session.FirstOrDefault();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            var payload = Unframe(body, contentType);
            var (result, resultText, resultLabel, isError) = Interpret(payload);

            var call = new McpExchange.Roundtrip(
                Method: method,
                Verb: "POST",
                Endpoint: McpEndpoint,
                Request: Pretty(envelope),
                Response: $"// {body.Length:N0} bytes\n{Clip(Pretty(payload), MaxPaneLength)}",
                Result: resultText is null ? null : Clip(resultText, MaxResultLength),
                ResultLabel: resultLabel,
                Status: (int)response.StatusCode,
                Elapsed: stopwatch.ElapsedMilliseconds,
                Ok: response.IsSuccessStatusCode && isError is false);

            return (call, result);
        }
    }

    /// <summary>The same tool over its plain HTTP mirror - no envelope, no session, no framing.</summary>
    private async Task CallHttp()
    {
        var url = $"{HttpEndpoint}{_httpTool}{(string.IsNullOrWhiteSpace(_httpQuery) ? "" : $"?{_httpQuery.TrimStart('?')}")}";

        _busy = true;
        StateHasChanged();

        var stopwatch = Stopwatch.StartNew();

        HttpResponseMessage response;
        string body;

        try
        {
            response = await HttpClient.GetAsync(url, CurrentCancellationToken);
            body = await response.Content.ReadAsStringAsync(CurrentCancellationToken);
        }
        catch (Exception exception)
        {
            _http = Invalid(_httpTool, exception.Message, url);
            return;
        }
        finally
        {
            stopwatch.Stop();
            _busy = false;
        }

        using (response)
        {
            // Every tool here answers with a string, so the body is one JSON string - the whole
            // document with its newlines escaped, unreadable in the response pane and the point of
            // the call.
            var text = body.StartsWith('"') ? TryUnquote(body) : null;

            _http = new McpExchange.Roundtrip(
                Method: _httpTool,
                Verb: "GET",
                Endpoint: url,
                Request: url,
                Response: $"// {body.Length:N0} bytes\n{Clip(Pretty(body), MaxPaneLength)}",
                Result: text is null ? null : Clip(text, MaxResultLength),
                ResultLabel: "the response, unescaped",
                Status: (int)response.StatusCode,
                Elapsed: stopwatch.ElapsedMilliseconds,
                Ok: response.IsSuccessStatusCode);
        }
    }

    /// <summary>
    /// The JSON out of a streamable-HTTP response. A result arrives as a server-sent event frame -
    /// "event: message" then "data: {...}" - and only the data lines are the JSON-RPC message.
    /// <para>
    /// Decided by the content type rather than by looking for "data:" in the body: the transport is
    /// allowed to answer either way, and a plain JSON answer that happens to contain those five
    /// characters would otherwise be unframed down to nothing at all.
    /// </para>
    /// </summary>
    private static string Unframe(string body, string contentType)
    {
        if (contentType.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase) is false) return body;

        var data = body.Replace("\r\n", "\n", StringComparison.Ordinal)
                       .Split('\n')
                       .Where(line => line.StartsWith("data:", StringComparison.Ordinal))
                       .Select(line => line[5..].Trim())
                       .ToArray();

        // An event-stream that carried no data line at all is still better shown as it arrived.
        return data.Length == 0 ? body : string.Join('\n', data);
    }

    /// <summary>
    /// The JSON-RPC result, plus the text inside it that a client would hand to the model, plus
    /// whether the envelope carried an error instead. Where that text sits differs per method, and
    /// none of the three places is the response itself.
    /// </summary>
    private static (JsonElement? Result, string? Text, string Label, bool IsError) Interpret(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);

            if (document.RootElement.ValueKind is not JsonValueKind.Object) return (null, null, string.Empty, false);

            var isError = document.RootElement.TryGetProperty("error", out _);

            if (document.RootElement.TryGetProperty("result", out var result) is false)
            {
                return (null, null, string.Empty, isError);
            }

            // Cloned: the element is a window onto the document, which is disposed on the way out.
            var clone = result.Clone();

            if (First(clone, "content") is { } content && content.TryGetProperty("text", out var toolText))
            {
                return (clone, toolText.GetString(), "result → content[0].text (what the model sees)", isError);
            }

            if (First(clone, "contents") is { } contents && contents.TryGetProperty("text", out var resourceText))
            {
                return (clone, resourceText.GetString(), "result → contents[0].text (the resource)", isError);
            }

            if (First(clone, "messages") is { } message &&
                message.TryGetProperty("content", out var messageContent) &&
                messageContent.TryGetProperty("text", out var promptText))
            {
                return (clone, promptText.GetString(), "result → messages[0].content.text (the prompt)", isError);
            }

            return (clone, null, string.Empty, isError);
        }
        catch (JsonException)
        {
            // Not JSON at all - a 202 with no body from a notification, or an HTML error page. The
            // status and the response pane already say what happened; nothing here can add to it.
            return (null, null, string.Empty, false);
        }
    }

    private static JsonElement? First(JsonElement element, string arrayName)
    {
        if (element.TryGetProperty(arrayName, out var array) is false || array.ValueKind != JsonValueKind.Array) return null;

        foreach (var item in array.EnumerateArray()) return item;

        return null;
    }

    /// <summary>
    /// The component count out of the server's instructions, which interpolate it from the catalog.
    /// Read rather than written down here for the same reason it is interpolated there: the number
    /// changes with the library, and the lead of this page should not be the one place it is stale.
    /// </summary>
    private static int? ComponentCount(string? instructions)
    {
        if (instructions is null) return null;

        var marker = instructions.IndexOf("library: ", StringComparison.Ordinal);

        if (marker < 0) return null;

        var digits = instructions[(marker + 9)..].TakeWhile(char.IsDigit).ToArray();

        return digits.Length > 0 && int.TryParse(digits, out var count) ? count : null;
    }

    private McpExchange.Roundtrip Invalid(string method, string message, string? request = null) =>
        new(method, "POST", McpEndpoint, request ?? "-", message, null, string.Empty, 0, 0, false);

    private static string Text(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static string? TryUnquote(string json)
    {
        try
        {
            return JsonDocument.Parse(json).RootElement.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Pretty(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            return JsonSerializer.Serialize(document.RootElement, _readable);
        }
        catch (JsonException)
        {
            // Not JSON at all - an HTML error page, or an empty body. Showing it as it arrived says
            // more than "could not parse" would.
            return json.Length == 0 ? "(empty body)" : json;
        }
    }

    /// <summary>Clips a pane, and says so: a payload that stops mid-word is otherwise a bug report.</summary>
    private static string Clip(string text, int max)
        => text.Length <= max ? text : $"{text[..max]}\n\n... clipped after {max:N0} of {text.Length:N0} characters";

    /// <summary>Clips a table cell, where a sentence about the clipping would be longer than the room saved.</summary>
    private static string Summarize(string text, int max)
        => text.Length <= max ? text : $"{text[..max].TrimEnd()}…";

    /// <summary>
    /// The argument names out of a tool's inputSchema, required ones first and marked. The schema is
    /// what a model reads to build a call; the table only needs enough of it to say what a tool
    /// wants, and the full schema is one tools/list away in the response pane below.
    /// </summary>
    private static string[] ArgumentNames(JsonElement tool)
    {
        if (tool.TryGetProperty("inputSchema", out var schema) is false ||
            schema.TryGetProperty("properties", out var properties) is false ||
            properties.ValueKind is not JsonValueKind.Object) return [];

        var required = new HashSet<string>(StringComparer.Ordinal);

        if (schema.TryGetProperty("required", out var names) && names.ValueKind is JsonValueKind.Array)
        {
            foreach (var name in names.EnumerateArray())
            {
                if (name.GetString() is { } value) required.Add(value);
            }
        }

        return [.. properties.EnumerateObject()
            .Select(p => new { p.Name, Required = required.Contains(p.Name) })
            .OrderByDescending(p => p.Required)
            .Select(p => p.Required ? p.Name : $"{p.Name}?")];
    }

    /// <param name="ReadOnly">The server's readOnlyHint - what lets a client skip asking first.</param>
    private record ToolInfo(string Name, string Description, string[] Arguments, bool ReadOnly);

    /// <param name="Reference">The "ref" object verbatim - a ref/prompt or a ref/resource.</param>
    /// <param name="Argument">The name of the argument being completed within it.</param>
    private record CompletionTarget(string Label, string Reference, string Argument);

    /// <summary>One row of the closing inventory: what the server publishes, and why it publishes it that way.</summary>
    private record Fact(string Name, string Signature, string Description);
}
