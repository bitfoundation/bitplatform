namespace Bit.BlazorUI;

// The color palette: a panel of swatches the text and highlight color buttons open in place of the browser's own
// color picker, so an app can keep its documents on its own colors.
public partial class BitRichTextEditor
{
    private bool _showColor;
    private string _colorKind = "fore";
    private bool _colorGridPending;
    private ElementReference _colorPanelRef = default!;
    private ElementReference _firstSwatchRef = default!;

    /// <summary>
    /// The colors the text and highlight color buttons offer as swatches. Set, each button opens a panel of them (with a
    /// custom color field for anything else); null or empty keeps the browser's own color picker.
    /// </summary>
    [Parameter] public IReadOnlyList<BitRichTextEditorColor>? ColorPalette { get; set; }

    private bool HasColorPalette => ColorPalette is { Count: > 0 };

    private string ColorPanelLabel => _colorKind == "back" ? Loc("highlight-color", "Highlight color") : Loc("text-color", "Text color");

    // The color under the caret, for the swatch that shows it is the current one and to seed the custom field.
    private string? CurrentColor => _colorKind == "back" ? _state.BackColor : _state.ForeColor;

    private bool IsCurrentColor(BitRichTextEditorColor color)
        => CurrentColor is { } current && string.Equals(current, color.Value?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string ColorName(BitRichTextEditorColor color)
        => string.IsNullOrWhiteSpace(color.Name) ? color.Value : color.Name;

    // A swatch is a toggle named by its color, pressed while that color is the one under the caret.
    private Dictionary<string, object> SwatchAttributes(BitRichTextEditorColor color)
    {
        var current = IsCurrentColor(color);
        return new()
        {
            ["type"] = "button",
            ["class"] = current ? "bit-rte-swatch bit-rte-swatch-cur" : "bit-rte-swatch",
            ["style"] = $"background:{color.Value}",
            ["title"] = ColorName(color),
            ["aria-label"] = ColorName(color),
            ["aria-pressed"] = current ? "true" : "false",
        };
    }

    private async Task ToggleColor(string kind)
    {
        // The same button closes its panel; the other one switches the open panel over to its own kind.
        if (_showColor && _colorKind == kind)
        {
            await CloseColor();
            return;
        }

        await CloseOtherPanels("color");
        _colorKind = kind;
        _showColor = true;
        _colorGridPending = true;
        RequestPanelFocus(() => _firstSwatchRef);
        ClearInlineError();
    }

    private Task CloseColor()
    {
        _showColor = false;
        RequestEditorFocus();
        return Task.CompletedTask;
    }

    private async Task PickColorAsync(string? value)
    {
        if (ControlsDisabled || string.IsNullOrWhiteSpace(value)) return;
        await _js.BitRichTextEditorApplyColor(_editorRef, _colorKind, value);
        await CloseColor();
    }

    private async Task RemovePickedColorAsync()
    {
        await ClearColorAsync(_colorKind);
        await CloseColor();
    }

    // The swatches are one tab stop walked with the arrow keys, the way the emoji grid is.
    private async Task EnableColorGridIfPendingAsync()
    {
        if (_colorGridPending is false || _showColor is false) return;
        _colorGridPending = false;
        try
        {
            await _js.BitRichTextEditorEnableGridRoving(_colorPanelRef);
        }
        catch (JSDisconnectedException) { } // circuit gone
        catch (JSException) { } // interop unavailable
    }
}
