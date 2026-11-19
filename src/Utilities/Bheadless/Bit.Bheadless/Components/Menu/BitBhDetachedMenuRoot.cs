namespace Bit.Bheadless;

/// <summary>Root used when a menu panel is rendered outside of a menu (design-time / tests).</summary>
internal sealed class BitBhDetachedMenuRoot : IBitBhMenuRoot
{
    public static readonly BitBhDetachedMenuRoot Instance = new();
    public bool IsOpen => false;
    public bool Disabled => false;
    public BitBhDirection Dir => BitBhDirection.Ltr;
    public string? TriggerId => null;
    public BitBhMenubarContext? Menubar => null;
    public Task CloseAsync() => Task.CompletedTask;
}
