namespace Bit.Bcrdt.Blazor;

/// <summary>
/// A multi-line text field - a <c>&lt;textarea&gt;</c> - bound to a <see cref="BcrdtText"/>, so several people can type
/// into it at once without losing each other's words: what the user types becomes the minimal CRDT edit
/// rather than a whole-value replacement, remote edits are merged into the control without moving the caret or
/// overwriting keystrokes still in flight (an input method composition included), and the local caret and
/// selection are published through <see cref="Presence"/>, anchored to the text rather than to an index.
/// </summary>
public partial class BcrdtTextArea
{
}
