namespace Bit.Butil;

/// <summary>
/// The <c>Response</c> half of the fetch object model, as <see cref="Fetch.Send"/> returns it.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Response">https://developer.mozilla.org/en-US/docs/Web/API/Response</see>
/// </summary>
/// <remarks>
/// A network or CORS failure is not an exception here: it comes back as an ordinary response with
/// <see cref="Ok"/> false, <see cref="Status"/> 0 and <see cref="Error"/> set - the same way
/// <c>fetch()</c> distinguishes "the server said no" from "there was no answer".
/// </remarks>
public class FetchResponse
{
    /// <summary>True when the response status is in [200, 300).</summary>
    public bool Ok { get; set; }

    /// <summary>HTTP status (or 0 when the request was aborted/failed before headers).</summary>
    public int Status { get; set; }

    /// <summary>The status text that went with <see cref="Status"/>. Often empty over HTTP/2, which does not carry one.</summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>Final URL after redirects.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Response headers, repeats included - which is what makes <c>Set-Cookie</c> readable here at
    /// all, on the browsers that expose it.
    /// </summary>
    public FetchHeaders Headers { get; set; } = new();

    /// <summary>Body bytes. May be empty for 204/304 or aborted responses.</summary>
    public byte[] Body { get; set; } = [];

    /// <summary>Whether the request went through at least one redirect to get here.</summary>
    public bool Redirected { get; set; }

    /// <summary>
    /// The response type: <c>"basic"</c>, <c>"cors"</c>, <c>"opaque"</c>, <c>"opaqueredirect"</c> or
    /// <c>"error"</c>. An <c>"opaque"</c> response is a <c>no-cors</c> one - its status reads 0 and
    /// its body is unreadable by design.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>True when the request was aborted (via <see cref="AbortableFetch"/> or cancellation).</summary>
    public bool Aborted { get; set; }

    /// <summary>Network/CORS error description, when one occurred.</summary>
    public string? Error { get; set; }
}
