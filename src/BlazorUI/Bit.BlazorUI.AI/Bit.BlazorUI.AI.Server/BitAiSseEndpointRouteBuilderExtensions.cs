using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BlazorUI;

/// <summary>
/// The server end of <see cref="BitAiSseTransport"/>: an endpoint that reads a <see cref="BitAiRequest"/> and streams the
/// reply's events as server-sent events, and one that takes the decisions on tool calls.
/// </summary>
/// <remarks>
/// <para>
/// The endpoints are plain <see cref="RequestDelegate"/>s, so they are safe to trim and to compile ahead of time, and
/// they return the <see cref="IEndpointConventionBuilder"/> of the route for the app's own conventions - put the stream
/// and the approvals behind the same authorization (<c>.RequireAuthorization()</c>) and rate limits.
/// </para>
/// </remarks>
public static class BitAiSseEndpointRouteBuilderExtensions
{
    /// <summary>The media type of the stream.</summary>
    public const string EventStreamContentType = BitAiSseTransport.EventStreamContentType;

    /// <summary>
    /// Maps a <c>POST</c> endpoint that reads a request in the wire format (<see cref="BitAiWireSerializer"/>) and streams
    /// the events <paramref name="handler"/> produces for it, one server-sent event each.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="pattern">The route, such as <c>/api/ai/chat</c>.</param>
    /// <param name="handler">
    /// Produces the reply, typically <c>StreamBitAiEventsAsync</c> (Bit.BlazorUI.AI.ChatClient) of the app's chat
    /// client. The request carries the endpoint's <see cref="HttpContext.RequestServices"/> as its
    /// <see cref="BitAiRequest.Services"/>, and the token is cancelled when the browser stops the reply or goes away.
    /// </param>
    /// <param name="options">Serializer hooks, for custom parts, and the JSON settings, for the app's own types.</param>
    /// <returns>The endpoint's convention builder.</returns>
    /// <remarks>
    /// <para>
    /// Each event is written as <c>event: {type}</c> and <c>data: {json}</c> and flushed as it comes, with response
    /// buffering turned off. A body that is not a request is answered <c>400</c>. An exception of the handler ends the
    /// stream with a terminal <see cref="BitAiErrorEvent"/> holding a generic message, and is logged; its own text never
    /// reaches the browser.
    /// </para>
    /// <para>
    /// Everything in the request comes from the browser: <c>ToChatOptions</c> (Bit.BlazorUI.AI.ChatClient) lets the
    /// server's options win over what it asks for, and <see cref="BitAiWireSerializer.ToRequest"/> hands the handler no
    /// message with a role above the user's (a <c>system</c> message the browser sent arrives as a user message).
    /// </para>
    /// </remarks>
    public static IEndpointConventionBuilder MapBitAiSse(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<BitAiRequest, HttpContext, CancellationToken, IAsyncEnumerable<BitAiEvent>> handler,
        BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        return endpoints.MapPost(pattern, http => StreamAsync(http, handler, options));
    }

    /// <summary>
    /// Maps a <c>POST</c> endpoint that takes the decisions on tool calls the browser sends through
    /// <see cref="BitAiSseTransport.SendApprovalAsync"/>, and hands each one to <paramref name="handler"/>.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="pattern">The route, by default the stream's with <c>/approvals</c> appended.</param>
    /// <param name="handler">Acts on a decision; returns whether a held call was waiting for it.</param>
    /// <param name="options">The JSON settings.</param>
    /// <returns>The endpoint's convention builder.</returns>
    /// <remarks>Answers <c>204</c> when the decision was taken, <c>404</c> when no call was waiting for it, and <c>400</c> for a body that is not a decision.</remarks>
    public static IEndpointConventionBuilder MapBitAiApprovals(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<BitAiApprovalResponse, HttpContext, CancellationToken, ValueTask<bool>> handler,
        BitAiConversationSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        return endpoints.MapPost(pattern, async http =>
        {
            BitAiApprovalResponse response;
            try
            {
                response = await BitAiWireSerializer.DeserializeApprovalAsync(http.Request.Body, options, http.RequestAborted).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                http.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var taken = await handler(response, http, http.RequestAborted).ConfigureAwait(false);
            http.Response.StatusCode = taken ? StatusCodes.Status204NoContent : StatusCodes.Status404NotFound;
        });
    }

    /// <summary>
    /// Maps the approval endpoint onto the app's <see cref="BitAiApprovalBroker"/> (registered as a singleton), which
    /// hands each decision to the stream holding its call.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="pattern">The route, by default the stream's with <c>/approvals</c> appended.</param>
    /// <returns>The endpoint's convention builder.</returns>
    public static IEndpointConventionBuilder MapBitAiApprovals(this IEndpointRouteBuilder endpoints, string pattern) =>
        endpoints.MapBitAiApprovals(pattern, static (response, http, _) =>
            ValueTask.FromResult(http.RequestServices.GetRequiredService<BitAiApprovalBroker>().TrySubmit(response)));

    private static async Task StreamAsync(
        HttpContext http,
        Func<BitAiRequest, HttpContext, CancellationToken, IAsyncEnumerable<BitAiEvent>> handler,
        BitAiConversationSerializerOptions? options)
    {
        var cancellationToken = http.RequestAborted;

        BitAiRequest request;
        try
        {
            var snapshot = await BitAiWireSerializer.DeserializeRequestAsync(http.Request.Body, options, cancellationToken).ConfigureAwait(false);
            request = BitAiWireSerializer.ToRequest(snapshot, options, http.RequestServices);
        }
        catch (JsonException)
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        http.Response.ContentType = EventStreamContentType;
        http.Response.Headers.CacheControl = "no-cache,no-store";
        http.Response.Headers["X-Accel-Buffering"] = "no";

        var logger = http.RequestServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(BitAiSseEndpointRouteBuilderExtensions).FullName!);
        var body = http.Response.Body;

        try
        {
            await body.FlushAsync(cancellationToken).ConfigureAwait(false);

            await foreach (var evt in BitAiServerStreams.Guard(() => handler(request, http, cancellationToken), logger, cancellationToken).ConfigureAwait(false))
            {
                await body.WriteAsync(Encoding.UTF8.GetBytes(FormatEvent(evt, options)), cancellationToken).ConfigureAwait(false);
                await body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser stopped the reply, or went away.
        }
    }

    /// <summary>
    /// One event as a server-sent event. The JSON is written compact, and a newline in a string is escaped in JSON, so
    /// the data is always one line.
    /// </summary>
    internal static string FormatEvent(BitAiEvent evt, BitAiConversationSerializerOptions? options) =>
        $"event: {evt.Type}\ndata: {BitAiWireSerializer.Serialize(evt, options)}\n\n";
}
