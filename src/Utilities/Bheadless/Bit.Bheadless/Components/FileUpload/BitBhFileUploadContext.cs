using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Bheadless;

/// <summary>The state of a <see cref="BitBhFileUpload"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhFileUploadContext
{
    internal BitBhFileUploadContext(BitBhFileUpload owner) => Owner = owner;

    internal BitBhFileUpload Owner { get; }

    /// <summary>The chosen files.</summary>
    public IReadOnlyList<IBrowserFile> Files => Owner.CurrentFiles;

    /// <summary>Whether no more files can be added.</summary>
    public bool IsMaxReached => Owner.IsMaxReached;

    /// <summary>Removes a file.</summary>
    public Task RemoveAsync(IBrowserFile file) => Owner.RemoveAsync(file);

    /// <summary>Removes every file.</summary>
    public Task ClearAsync() => Owner.ClearAsync();
}
