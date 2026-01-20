using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Server.Api.Features.Attachments;

public partial class Attachment
{
    public Guid Id { get; set; }

    public AttachmentKind Kind { get; set; }

    public string? Path { get; set; }
}
