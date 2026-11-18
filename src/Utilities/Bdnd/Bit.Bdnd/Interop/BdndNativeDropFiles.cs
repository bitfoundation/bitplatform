namespace Bit.Bdnd;

/// <summary>The files of one native drop the browser module keeps for a target - until the target's next drop, or its disposal.</summary>
internal sealed class BdndNativeDropFiles(BdndInterop interop, string dropId)
{
    public BdndInterop Interop { get; } = interop;

    public string DropId { get; } = dropId;

    /// <summary>The module no longer keeps the files: the target took another drop, or was disposed.</summary>
    public bool Released { get; private set; }

    public void Release() => Released = true;
}
