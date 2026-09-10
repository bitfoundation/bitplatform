namespace Bit.BlazorUI;

// Table insertion and structural editing.
public partial class BitRichTextEditor
{
    // The upper bound is a guard against a mistyped size producing a table with thousands of
    // cells, not a design limit; 50x50 is far past any hand-authored table.
    private const int MaxTableDimension = 50;

    private bool _showTableInput;
    private int _tableRows = 2;
    private int _tableCols = 2;
    private bool _tableHeader = true;
    private ElementReference _tableRowsRef = default!;

    /// <summary>
    /// The structural table operations only mean anything while the caret is inside a table, so
    /// they stay disabled until the selection state reports one - the same way a word processor
    /// greys out its table menu outside a table.
    /// </summary>
    private bool TableOpsDisabled => ControlsDisabled || _state.InTable is false;

    private void ToggleTableInput()
    {
        _showTableInput = !_showTableInput;
        if (_showTableInput)
        {
            CloseOtherPanels("table");
            RequestPanelFocus(() => _tableRowsRef);
        }
        ClearInlineError();
    }

    private async Task ApplyTableAsync()
    {
        await InsertTableAsync(_tableRows, _tableCols, _tableHeader);
        // InsertTableAsync surfaces its own error for an out-of-range size, so only close the
        // panel when nothing is being reported.
        if (_inlineError is null) _showTableInput = false;
    }

    private async Task OnTableKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyTableAsync();
        else if (e.Key == "Escape") ToggleTableInput();
    }

    private async Task InsertTableAsync(int rows, int cols, bool header = false)
    {
        // Guard on ControlsDisabled (ReadOnly || source view) so table insertion can't mutate
        // the hidden editor DOM while source view is active, matching the other command flows.
        if (ControlsDisabled) return;
        if (rows < 1 || rows > MaxTableDimension || cols < 1 || cols > MaxTableDimension)
        {
            await RaiseErrorAsync(new BitRichTextEditorError("invalid-table",
                string.Format(Label("invalid-table", "Tables must be between 1 and {0} rows/columns."), MaxTableDimension)));
            return;
        }
        ClearInlineError();
        await _js.BitRichTextEditorInsertTable(_editorRef, rows, cols, header);
    }

    private async Task TableOpAsync(string op)
    {
        if (ControlsDisabled) return;
        await _js.BitRichTextEditorTableOp(_editorRef, op);
    }
}
