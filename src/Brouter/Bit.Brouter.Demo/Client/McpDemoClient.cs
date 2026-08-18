using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;

namespace Bit.Brouter.Demo.Client;

/// <summary>
/// One message sent to the MCP server and whatever came back, kept verbatim so the demo page
/// (Pages/McpPage.razor) can show the traffic instead of describing it.
/// </summary>
public sealed class McpExchange
{
    /// <summary>The JSON-RPC method, or "DELETE /mcp" for the request that ends the session.</summary>
    public required string Method { get; init; }

    /// <summary>The JSON-RPC envelope that was posted, indented.</summary>
    public required string RequestJson { get; init; }

    /// <summary>The message the server answered with, indented - or a note when there was no body.</summary>
    public string? ResponseJson { get; set; }

    public int StatusCode { get; set; }

    /// <summary>Which of the two Streamable HTTP response shapes came back: JSON or an SSE stream.</summary>
    public string? ContentType { get; set; }

    public double ElapsedMs { get; set; }

    /// <summary>Set when the call never produced a JSON-RPC response - a transport failure or an error result.</summary>
    public string? Error { get; set; }

    /// <summary>Notifications carry no id and get a 202 with no body back.</summary>
    public bool IsNotification { get; init; }

    public DateTime StartedAt { get; init; } = DateTime.Now;
}

/// <summary>The outcome of one JSON-RPC call: the <c>result</c> object, or the error that replaced it.</summary>
public sealed record McpCallResult(JsonNode? Result, string? Error, McpExchange Exchange)
{
    public bool IsSuccess => Error is null;
}

/// <summary>
/// A minimal MCP client that speaks the Streamable HTTP transport to this app's own <c>/mcp</c>
/// endpoint (mapped in Server/Program.cs) from the browser.
/// <para>
/// It exists to make the protocol visible rather than to replace a real client: every message is
/// built as plain JSON-RPC and kept in <see cref="Exchanges"/> with its response, its status code
/// and how long it took, which is what the demo page renders. The session rules are the
/// interesting part and are honoured for real - the session id the server hands out at
/// <c>initialize</c> travels on every later request, and so does the negotiated protocol version.
/// </para>
/// </summary>
public sealed class McpDemoClient(HttpClient httpClient, NavigationManager navigationManager)
{
    /// <summary>
    /// The revision of the MCP specification this client asks for. A server that does not have it
    /// refuses the handshake with <c>-32022</c> and names the revisions it does support, which is
    /// how a real client knows what to fall back to.
    /// </summary>
    public const string RequestedProtocolVersion = "2025-11-25";

    // For display only: indented, and without the default encoder's \uXXXX escaping of quotes and
    // angle brackets, which turns a tool result carrying JSON into something no one can read.
    private static readonly JsonSerializerOptions _indented = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private int _lastId;

    /// <summary>
    /// The session the server opened at <c>initialize</c>, echoed back on every later request.
    /// Null against a server running the transport statelessly - which is the default, and the only
    /// mode the newest protocol revisions have, since they dropped sessions from Streamable HTTP.
    /// </summary>
    public string? SessionId { get; private set; }

    /// <summary>The protocol revision both sides settled on, or null while disconnected.</summary>
    public string? ProtocolVersion { get; private set; }

    /// <summary>The server's own name and version, from the <c>initialize</c> result.</summary>
    public JsonNode? ServerInfo { get; private set; }

    /// <summary>Which of tools, resources and prompts this server offers - the client learns it here, not from configuration.</summary>
    public JsonNode? Capabilities { get; private set; }

    public bool IsConnected => ProtocolVersion is not null;

    /// <summary>Every message exchanged so far, oldest first.</summary>
    public List<McpExchange> Exchanges { get; } = [];

    /// <summary>
    /// Performs the handshake: <c>initialize</c>, then the <c>notifications/initialized</c> that
    /// tells the server the client is ready. No other request is legal before both have happened.
    /// </summary>
    public async Task<McpCallResult> ConnectAsync()
    {
        SessionId = null;
        ProtocolVersion = null;
        ServerInfo = null;
        Capabilities = null;

        var call = await SendAsync("initialize", new JsonObject
        {
            ["protocolVersion"] = RequestedProtocolVersion,
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject
            {
                ["name"] = "Bit.Brouter docs demo page",
                ["version"] = "1.0.0"
            }
        }, notification: false);

        if (call.IsSuccess)
        {
            ProtocolVersion = call.Result?["protocolVersion"]?.GetValue<string>();
            ServerInfo = call.Result?["serverInfo"];
            Capabilities = call.Result?["capabilities"];

            await SendAsync("notifications/initialized", null, notification: true);
        }

        return call;
    }

    /// <summary>Sends a JSON-RPC request - <c>tools/list</c>, <c>tools/call</c>, <c>resources/read</c>, ...</summary>
    public Task<McpCallResult> CallAsync(string method, JsonNode? parameters = null)
        => SendAsync(method, parameters, notification: false);

    /// <summary>
    /// Ends the session with an HTTP DELETE, the transport's own way of saying goodbye. The server
    /// drops the session's state; anything sent afterwards has to handshake again.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (SessionId is null) return;

        var exchange = new McpExchange
        {
            Method = "DELETE /mcp",
            RequestJson = $"DELETE /mcp\nMcp-Session-Id: {SessionId}\nMCP-Protocol-Version: {ProtocolVersion}",
            IsNotification = true
        };

        Exchanges.Add(exchange);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, navigationManager.ToAbsoluteUri("mcp"));

            request.Headers.TryAddWithoutValidation("Mcp-Session-Id", SessionId);
            if (ProtocolVersion is not null) request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", ProtocolVersion);

            using var response = await httpClient.SendAsync(request);

            exchange.StatusCode = (int)response.StatusCode;
            exchange.ResponseJson = "(the session is closed - no body)";
        }
        catch (Exception exception)
        {
            exchange.Error = exception.Message;
        }
        finally
        {
            exchange.ElapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            SessionId = null;
            ProtocolVersion = null;
            ServerInfo = null;
            Capabilities = null;
        }
    }

    public void ClearExchanges() => Exchanges.Clear();

    private async Task<McpCallResult> SendAsync(string method, JsonNode? parameters, bool notification)
    {
        var envelope = new JsonObject { ["jsonrpc"] = "2.0" };

        // A notification is a request without an id: it gets no answer, so nothing can be correlated
        // back to it - which is exactly why the spec allows one only where no answer is needed.
        if (notification is false) envelope["id"] = ++_lastId;

        envelope["method"] = method;
        if (parameters is not null) envelope["params"] = parameters;

        var exchange = new McpExchange
        {
            Method = method,
            RequestJson = envelope.ToJsonString(_indented),
            IsNotification = notification
        };

        Exchanges.Add(exchange);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, navigationManager.ToAbsoluteUri("mcp"))
            {
                Content = new StringContent(exchange.RequestJson, Encoding.UTF8, "application/json")
            };

            // Streamable HTTP lets the server answer either with a single JSON body or with an SSE
            // stream, and it is the server that chooses - a client that does not accept both is
            // rejected outright, before any of its messages are looked at.
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            if (SessionId is not null) request.Headers.TryAddWithoutValidation("Mcp-Session-Id", SessionId);
            if (ProtocolVersion is not null) request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", ProtocolVersion);

            using var response = await httpClient.SendAsync(request);

            exchange.StatusCode = (int)response.StatusCode;
            exchange.ContentType = response.Content.Headers.ContentType?.ToString();

            // The session id exists only on the initialize response; every later request carries it
            // back so the server can find the state it kept for this client.
            if (response.Headers.TryGetValues("Mcp-Session-Id", out var sessionIds))
            {
                SessionId = sessionIds.FirstOrDefault() ?? SessionId;
            }

            var payload = ExtractMessage(await response.Content.ReadAsStringAsync(), exchange.ContentType);

            if (payload.Length == 0)
            {
                exchange.ResponseJson = notification
                    ? $"({exchange.StatusCode} - a notification is answered with an empty body)"
                    : $"({exchange.StatusCode} - empty body)";

                return new McpCallResult(null, notification ? null : "The server sent no JSON-RPC message back.", exchange);
            }

            var message = JsonNode.Parse(payload);

            exchange.ResponseJson = message?.ToJsonString(_indented) ?? payload;

            // A JSON-RPC error is a successful HTTP response carrying an "error" member - the
            // status code says nothing about whether the call worked.
            if (message?["error"] is JsonNode error)
            {
                exchange.Error = error["message"]?.GetValue<string>() ?? error.ToJsonString();

                return new McpCallResult(null, exchange.Error, exchange);
            }

            return new McpCallResult(message?["result"], null, exchange);
        }
        catch (Exception exception)
        {
            exchange.Error = exception.Message;

            return new McpCallResult(null, exception.Message, exchange);
        }
        finally
        {
            exchange.ElapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Pulls the JSON-RPC message out of the body. A plain JSON response is already the message; an
    /// SSE response wraps it in an event whose "data:" lines hold it.
    /// </summary>
    private static string ExtractMessage(string body, string? contentType)
    {
        if (contentType?.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase) is not true) return body.Trim();

        var data = new StringBuilder();

        // The stream answering a single request carries a single event, so the data lines of the
        // whole body belong to one message.
        foreach (var line in body.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');

            if (trimmed.StartsWith("data:", StringComparison.Ordinal))
            {
                data.Append(trimmed[5..].TrimStart());
            }
        }

        return data.ToString().Trim();
    }
}
