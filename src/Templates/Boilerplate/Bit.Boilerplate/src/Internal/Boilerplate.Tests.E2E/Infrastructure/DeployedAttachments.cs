using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// What a deployment's anonymous <c>GetAttachment</c> endpoint still serves - the only way from outside to tell a
/// deleted row from a blob that is merely no longer referenced.
/// </summary>
public static class DeployedAttachments
{
    /// <summary>
    /// A query key of its own on every call: the endpoint is cached at the edge for a week, and what a test asks
    /// about is what the origin holds now.
    /// </summary>
    public static async Task<HttpStatusCode> StatusOf(string host, Guid attachmentId, AttachmentKind kind, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(1) };

        using var response = await httpClient.GetAsync($"{host}api/v1/Attachment/GetAttachment/{attachmentId}/{kind}?e2e={Guid.NewGuid()}", cancellationToken);

        return response.StatusCode;
    }
}
