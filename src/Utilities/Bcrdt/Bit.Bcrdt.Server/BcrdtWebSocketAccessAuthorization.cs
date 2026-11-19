using Microsoft.AspNetCore.Http;

namespace Bit.Bcrdt;

/// <summary>
/// Decides what a request may do with a document - nothing, read it, or read and change it - before the
/// socket is accepted.
/// </summary>
/// <param name="context">The upgrade request.</param>
/// <param name="documentId">The document the request is asking for.</param>
/// <returns>The access to grant; <see cref="BcrdtDocumentAccess.None"/> refuses the request.</returns>
public delegate ValueTask<BcrdtDocumentAccess> BcrdtWebSocketAccessAuthorization(HttpContext context, string documentId);
