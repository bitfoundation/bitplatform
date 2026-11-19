using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Bheadless;

/// <summary>A file a <see cref="BitBhFileUpload"/> refused, and why.</summary>
public sealed record BitBhFileRejection(IBrowserFile File, BitBhFileRejectionReason Reasons);
