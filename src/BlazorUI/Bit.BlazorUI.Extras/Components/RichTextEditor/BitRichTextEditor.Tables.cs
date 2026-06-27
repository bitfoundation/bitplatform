namespace Bit.BlazorUI;

// Table insertion and structural editing.
public partial class BitRichTextEditor
{
    private async Task InsertTableAsync(int rows, int cols)
    {
        if (ReadOnly) return;
        if (rows < 1 || rows > 50 || cols < 1 || cols > 50)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-table", "Tables must be between 1 and 50 rows/columns."));
            return;
        }
        await _js.BitRichTextEditorInsertTable(_editorRef, rows, cols);
    }

    private async Task TableOpAsync(string op)
    {
        if (ReadOnly) return;
        await _js.BitRichTextEditorTableOp(_editorRef, op);
    }
}
