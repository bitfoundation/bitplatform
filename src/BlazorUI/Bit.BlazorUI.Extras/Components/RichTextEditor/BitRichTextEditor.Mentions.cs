using System.Diagnostics;

namespace Bit.BlazorUI;

// Mention menu. The bridge detects the trigger character and reports it; the menu, the lookup and
// the inserted markup all live here, mirroring how the slash menu is put together.
public partial class BitRichTextEditor
{
    private bool _showMention;
    private string _mentionFilter = "";
    private int _mentionIndex;
    private bool _focusMentionPending;
    private int _mentionQueryToken;
    private IReadOnlyList<BitRichTextEditorMention> _mentionItems = [];
    private ElementReference _mentionInputRef = default!;

    /// <summary>
    /// Supplies the suggestions shown after the user types <c>@</c>. Receives the text typed into
    /// the menu's filter box (empty on open) and returns the matches to offer. Leaving it null
    /// disables mentions entirely - the bridge does not even watch for the trigger.
    /// </summary>
    [Parameter] public Func<string, Task<IReadOnlyList<BitRichTextEditorMention>>>? OnMentionSearch { get; set; }

    /// <summary>Callback for when a mention is picked from the menu.</summary>
    [Parameter] public EventCallback<BitRichTextEditorMention> OnMentionSelected { get; set; }

    /// <summary>Called by the bridge when the user types the mention trigger.</summary>
    [JSInvokable("OnMentionTrigger")]
    public async Task _OnMentionTrigger()
    {
        // Match the slash menu: never open while the controls are disabled (ReadOnly, disabled, or
        // source view), where the insert would be refused anyway.
        if (ControlsDisabled || OnMentionSearch is null) return;

        // The two menus answer different triggers and share the same spot, so only one is ever up.
        CloseSlash();
        _mentionFilter = "";
        _mentionIndex = 0;
        _showMention = true;
        // Move focus into the filter box on the next render so typing filters the list instead of
        // continuing into the document.
        _focusMentionPending = true;
        StateHasChanged();

        await QueryMentionsAsync("");
    }

    // Runs the host lookup and adopts the result only when it is still the newest one, so a slow
    // response for an earlier keystroke cannot overwrite the list for a later one.
    private async Task QueryMentionsAsync(string term)
    {
        if (OnMentionSearch is null) return;

        var token = ++_mentionQueryToken;
        IReadOnlyList<BitRichTextEditorMention> results;
        try
        {
            results = await OnMentionSearch(term) ?? [];
        }
        catch (Exception ex)
        {
            // Keep the host lookup's internals out of the user-facing message; log for diagnostics.
            // Trace (not Debug) so the failure is still recorded in Release builds.
            Trace.TraceError($"BitRichTextEditor mention search for \"{term}\" failed: {ex}");
            // A search superseded by a later keystroke, or by the menu closing, reports nothing.
            if (token != _mentionQueryToken) return;
            await RaiseErrorAsync(new BitRichTextEditorError("mention-search-failed",
                Label("mention-search-failed", "Could not load mention suggestions.")));
            return;
        }

        if (token != _mentionQueryToken) return;

        _mentionItems = results;
        _mentionIndex = 0;
        StateHasChanged();
    }

    private async Task OnMentionFilterInput(ChangeEventArgs e)
    {
        _mentionFilter = e.Value?.ToString() ?? "";
        await QueryMentionsAsync(_mentionFilter);
    }

    // Arrows move the highlight, Enter picks the highlighted suggestion, Escape dismisses the menu.
    private async Task OnMentionKeyDownAsync(KeyboardEventArgs e)
    {
        if (_showMention is false) return;

        switch (e.Key)
        {
            case "ArrowDown":
                if (_mentionItems.Count > 0) _mentionIndex = (_mentionIndex + 1) % _mentionItems.Count;
                break;

            case "ArrowUp":
                if (_mentionItems.Count > 0) _mentionIndex = (_mentionIndex - 1 + _mentionItems.Count) % _mentionItems.Count;
                break;

            case "Enter":
                if (_mentionIndex >= 0 && _mentionIndex < _mentionItems.Count)
                {
                    await ApplyMentionAsync(_mentionItems[_mentionIndex]);
                }
                break;

            case "Escape":
                CloseMention();
                break;
        }
    }

    private void CloseMention()
    {
        // Invalidate any search still in flight, so it cannot repopulate the closed menu.
        _mentionQueryToken++;
        _showMention = false;
        _mentionFilter = "";
        _mentionIndex = 0;
        _mentionItems = [];
    }

    private async Task ApplyMentionAsync(BitRichTextEditorMention mention)
    {
        if (ControlsDisabled) return;
        CloseMention();

        await _js.BitRichTextEditorApplyMention(_editorRef, BuildMentionHtml(mention));
        await OnMentionSelected.InvokeAsync(mention);
    }

    /// <summary>
    /// The markup written for a picked mention. A span carrying the id is used rather than a link:
    /// it needs no URL, and both the class and <c>data-mention-id</c> are part of the default
    /// sanitization allowlist, so the mention round-trips through save and reload intact.
    /// </summary>
    private static string BuildMentionHtml(BitRichTextEditorMention mention)
        => $"<span class=\"bit-rte-mention\" data-mention-id=\"{EscapeHtml(mention.Id)}\">@{EscapeHtml(mention.Display)}</span>&nbsp;";

    private static string EscapeHtml(string? value)
        => (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // Focuses the filter box on the render that follows opening the menu, the same way the slash
    // menu does.
    private async Task FocusMentionIfPendingAsync()
    {
        if (_focusMentionPending is false) return;
        _focusMentionPending = false;
        try
        {
            await _mentionInputRef.FocusAsync();
            // Suppress the browser's own handling of the menu's navigation keys on this input while
            // leaving normal typing intact.
            await _js.BitRichTextEditorBindSlashKeys(_mentionInputRef);
        }
        catch (JSDisconnectedException) { } // circuit gone; nothing to focus
        catch (JSException) { } // interop unavailable; ignore like the slash menu does
    }
}
