using Microsoft.AspNetCore.Http;

namespace Bit.Bcrdt;

/// <summary>
/// Decides whether a request may work on a document, before the socket is accepted.
/// </summary>
/// <remarks>
/// Given the <see cref="HttpContext"/> of the upgrade request, so an implementation has the
/// authenticated user, the cookies, the headers and the query string to decide with - everything the
/// connection will ever carry, since a WebSocket presents its credentials once and then only sends
/// frames.
/// </remarks>
/// <param name="context">The upgrade request.</param>
/// <param name="documentId">The document the request is asking for.</param>
/// <returns><see langword="true"/> to accept the socket.</returns>
public delegate ValueTask<bool> BcrdtWebSocketAuthorization(HttpContext context, string documentId);
