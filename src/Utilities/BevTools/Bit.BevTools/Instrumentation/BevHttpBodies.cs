using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.BevTools;

/// <summary>
/// Request and response bodies, when the app asked for them (<see cref="BitBevToolsOptions.CaptureHttpBodies"/>): text
/// only (JSON, XML, forms, text, GraphQL), at most <see cref="BitBevToolsOptions.MaxHttpBodyBytes"/>, redacted - a JSON
/// or form value under a sensitive name, and anything credential-shaped - and never at the cost of the request: a
/// stream is never read (it is read once, as it is sent), and a response is read only when its length is known and
/// within the limit, so the app reads exactly what it would have read. Only at <see cref="BevValueAccess.Full"/>, and
/// in a process that serves many users only in Development.
/// </summary>
internal static class BevHttpBodies
{
    internal static bool Allowed(BevToolsSession session) =>
        session.Options.CaptureHttpBodies && session.ValueAccess == BevValueAccess.Full && session.AllowsForcedCollection;

    /// <summary>The request's body, or why it was not read.</summary>
    public static async Task<(string? Body, string? Note)> ReadRequestAsync(BevToolsSession session, HttpContent? content)
    {
        if (content is null)
        {
            return (null, null);
        }

        if (!IsText(content.Headers.ContentType))
        {
            return (null, "not text (" + (content.Headers.ContentType?.MediaType ?? "no content type") + ")");
        }

        var max = Math.Max(256, session.Options.MaxHttpBodyBytes);
        try
        {
            // Byte array content (strings, forms) is read again as often as asked; JSON content is serialized into a
            // buffer the send then uses, so the value is serialized once. Anything else is a stream, read once.
            if (content is ByteArrayContent || content.GetType().FullName == "System.Net.Http.Json.JsonContent")
            {
                await content.LoadIntoBufferAsync(max);
                return (Redact(session, await content.ReadAsStringAsync(), content.Headers.ContentType?.MediaType, max), null);
            }

            return (null, "a stream is read once, as it is sent: not read");
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException)
        {
            return (null, "larger than " + BevToolsHttpMessageHandler.FormatBytes(max) + ": not read");
        }
    }

    /// <summary>The response's body, or why it was not read.</summary>
    public static async Task<(string? Body, string? Note)> ReadResponseAsync(BevToolsSession session, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = response.Content;
        if (content is null)
        {
            return (null, null);
        }

        var type = content.Headers.ContentType?.MediaType;
        if (type is not null && type.StartsWith("text/event-stream", StringComparison.OrdinalIgnoreCase))
        {
            return (null, "a stream of server-sent events: not read");
        }

        if (!IsText(content.Headers.ContentType))
        {
            return (null, "not text (" + (type ?? "no content type") + ")");
        }

        var max = Math.Max(256, session.Options.MaxHttpBodyBytes);
        if (content.Headers.ContentLength is not { } length)
        {
            return (null, "no length (chunked): reading it would change how the app reads it, so it is not read");
        }

        if (length > max)
        {
            return (null, BevToolsHttpMessageHandler.FormatBytes(length) + ", larger than " + BevToolsHttpMessageHandler.FormatBytes(max) + ": not read");
        }

        try
        {
            // A known, small length: buffered as HttpClient would buffer it, and the app reads the same bytes after.
            await content.LoadIntoBufferAsync(max, cancellationToken);
            return (Redact(session, await content.ReadAsStringAsync(cancellationToken), type, max), null);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException or OperationCanceledException)
        {
            return (null, "could not be read: " + ex.GetType().Name);
        }
    }

    private static bool IsText(MediaTypeHeaderValue? contentType)
    {
        var type = contentType?.MediaType;
        if (type is null)
        {
            return false;
        }

        return type.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || type.EndsWith("json", StringComparison.OrdinalIgnoreCase)
            || type.EndsWith("+json", StringComparison.OrdinalIgnoreCase)
            || type.EndsWith("xml", StringComparison.OrdinalIgnoreCase)
            || type.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
            || type.Equals("application/graphql", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// JSON with each value under a sensitive name replaced, forms the same by key, everything credential-shaped
    /// redacted, then cut to the limit.
    /// </summary>
    internal static string Redact(BevToolsSession session, string body, string? mediaType, int max)
    {
        string text;
        if (mediaType is not null && mediaType.EndsWith("json", StringComparison.OrdinalIgnoreCase) && TryRedactJson(session, body, out var json))
        {
            text = json;
        }
        else if (string.Equals(mediaType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            text = string.Join("&", body.Split('&').Select(pair =>
            {
                var eq = pair.IndexOf('=');
                var key = eq < 0 ? pair : pair[..eq];
                return eq >= 0 && session.Redactor.IsSensitiveName(Uri.UnescapeDataString(key.Replace('+', ' '))) ? key + "=" + BevRedactor.RedactedValue : pair;
            }));
        }
        else
        {
            text = body;
        }

        text = BevRedactor.RedactText(text);
        return text.Length <= max ? text : text[..max] + "…";
    }

    private static bool TryRedactJson(BevToolsSession session, string body, out string redacted)
    {
        redacted = body;
        try
        {
            var node = JsonNode.Parse(body, documentOptions: new JsonDocumentOptions { MaxDepth = 64 });
            if (node is null)
            {
                return false;
            }

            Walk(session, node, 0);
            redacted = node.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void Walk(BevToolsSession session, JsonNode node, int depth)
    {
        if (depth > 64)
        {
            return;
        }

        switch (node)
        {
            case JsonObject obj:
                foreach (var name in obj.Select(p => p.Key).ToList())
                {
                    if (session.Redactor.IsSensitiveName(name))
                    {
                        obj[name] = BevRedactor.RedactedValue;
                    }
                    else if (obj[name] is { } child)
                    {
                        Walk(session, child, depth + 1);
                    }
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        Walk(session, item, depth + 1);
                    }
                }

                break;
        }
    }

    /// <summary>The GraphQL operation a request body names, when it names one.</summary>
    internal static string? GraphQlOperation(string? body)
    {
        if (body is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("operationName", out var name) && name.ValueKind == JsonValueKind.String
                ? name.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// What the request is beyond plain HTTP, read from its content types and path: gRPC-Web, server-sent events,
    /// GraphQL. Null for anything else.
    /// </summary>
    internal static string? Protocol(HttpRequestMessage request, HttpResponseMessage? response)
    {
        static bool Is(MediaTypeHeaderValue? type, string prefix) => type?.MediaType?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true;
        var requestType = request.Content?.Headers.ContentType;
        var responseType = response?.Content?.Headers.ContentType;
        if (Is(requestType, "application/grpc") || Is(responseType, "application/grpc"))
        {
            return "gRPC";
        }

        if (Is(responseType, "text/event-stream"))
        {
            return "server-sent events";
        }

        var path = request.RequestUri?.AbsolutePath ?? "";
        return Is(requestType, "application/graphql") || path.EndsWith("/graphql", StringComparison.OrdinalIgnoreCase) ? "GraphQL" : null;
    }
}
