namespace Bit.Balette;

internal sealed class BaletteJsOptions
{
    public List<string> ToggleShortcuts { get; set; } = [];
    public List<string> CommandShortcuts { get; set; } = [];
    public bool SuppressInEditable { get; set; }
    public int SequenceTimeout { get; set; }
    public bool VimBindings { get; set; }
}
