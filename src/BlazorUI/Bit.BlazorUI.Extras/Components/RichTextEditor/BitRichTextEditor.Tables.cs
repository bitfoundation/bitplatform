namespace Bit.BlazorUI;

// Table insertion and structural editing.
public partial class BitRichTextEditor
{
    private async Task InsertTableAsync(int rows, int cols)
    {
        // Guard on ControlsDisabled (ReadOnly || source view) so table insertion can't mutate
        // the hidden editor DOM while source view is active, matching the other command flows.
        if (ControlsDisabled) return;
        if (rows < 1 || rows > 50 || cols < 1 || cols > 50)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-table",
                Label("invalid-table", "Tables must be between 1 and 50 rows/columns.")));
            return;
        }
        await _js.BitRichTextEditorInsertTable(_editorRef, rows, cols);
    }

    private async Task TableOpAsync(string op)
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorTableOp(_editorRef, op);
    }
}
