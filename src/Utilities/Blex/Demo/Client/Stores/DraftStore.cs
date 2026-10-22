namespace Bit.Blex.Demo.Client.Stores;

/// <summary>
/// The store behind the scoped-stores demo: a small note draft that exists once per
/// <c>&lt;BlexScope&gt;</c> rather than once per app. It is deliberately <em>not</em> registered with
/// <c>AddBitBlexStore</c> -- every scope creates, registers and disposes its own instance, named
/// <c>draft:{key}</c>. It persists, so each key keeps its own draft across reloads.
/// </summary>
[BlexStoreAttribute(Name = "draft", Persist = true)]
public partial class DraftStore
{
    [BlexStateAttribute] private string _text = "";
    [BlexStateAttribute] private int _edits;

    [BlexComputedAttribute] private int ComputeLength() => Text.Length;

    [BlexComputedAttribute] private int ComputeWords() => Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    [BlexActionAttribute]
    private void OnWrite(string text)
    {
        Text = text;
        Edits++;
    }

    [BlexActionAttribute]
    private void OnClear()
    {
        Text = "";
        Edits++;
    }
}
