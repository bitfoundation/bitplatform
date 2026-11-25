namespace Bit.BlazorUI;

/// <summary>
/// BitCodeBlock shows a block of code - a command to run, a snippet to paste - in the theme's monospaced family, with a
/// button that puts it on the clipboard, an optional header naming it, and a terminal look for the commands of a shell.
/// </summary>
/// <remarks>
/// The code is written as it is: there is no syntax highlighting. An app that highlights its code with a library of its
/// own hands the highlighted markup in through <see cref="ChildContent"/> and the plain source through
/// <see cref="Code"/>, which is then what the copy puts on the clipboard.
/// <br />
/// A copy confirms itself: the glyph of the button turns into a check mark, its name says so, and the confirmation is
/// announced to screen readers through a polite live region, since a copy that gives no feedback leaves the user guessing
/// whether it worked. Its words are the Copy and Copied of <see cref="BitStrings.Current"/> unless
/// <see cref="CopyText"/> and <see cref="CopiedText"/> say otherwise.
/// </remarks>
public partial class BitCodeBlock : BitComponentBase
{
    // How long the confirmation stays on the button after a copy.
    private static readonly TimeSpan _confirmationDuration = TimeSpan.FromSeconds(2);

    private bool _copied;
    // The Code the confirmation showing is about, so a block handed another one takes it off.
    private string? _copiedCode;
    // Bumped by every confirmation put up or taken off, so a timer left running by an earlier copy stands down.
    private int _copyToken;
    // Bumped by every copy confirmed, which keys the text of the live region (see the markup).
    private int _copyCount;
    private ElementReference _codeElement;
    private readonly RenderFragment _renderCode;
    // Made once, so the diff keeps being handed the same callback. Left out of the markup (a default callback renders no
    // handler) unless CopyOnClick asks for it.
    private readonly EventCallback<MouseEventArgs> _handleBodyClick;

    // The code with every line ending a "\n" (see NormalizeLineEndings), and the Code it was made from.
    private string _code = string.Empty;
    private string? _codeFor;



    public BitCodeBlock()
    {
        // Held in a field rather than created per render, so the diff keeps being handed the same delegate.
        _renderCode = BuildCode;
        _handleBodyClick = EventCallback.Factory.Create<MouseEventArgs>(this, HandleBodyClick);
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the code block component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple code block components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitCodeBlockParams.ParamName)]
    public BitCodeBlockParams? CascadingParameters { get; set; }



    /// <summary>
    /// The code as markup, rendered in place of <see cref="Code"/>: code written right inside the tag, or code an app
    /// highlighted itself.
    /// </summary>
    /// <remarks>
    /// The copy puts <see cref="Code"/> on the clipboard where it is set, and otherwise the text the browser renders of
    /// this content.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the code block.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitCodeBlockClassStyles? Classes { get; set; }

    /// <summary>
    /// The code to show, which is also exactly what the copy puts on the clipboard.
    /// </summary>
    [Parameter] public string? Code { get; set; }

    /// <summary>
    /// The name of the copy button while what it copied is on the clipboard, which is also what screen readers are told
    /// after a copy.
    /// <br />
    /// The default value is <strong>null</strong>, which uses <see cref="BitStrings.Copied"/> of
    /// <see cref="BitStrings.Current"/> ("Copied").
    /// </summary>
    [Parameter] public string? CopiedText { get; set; }

    /// <summary>
    /// Copies the code when the code itself is clicked, as well as through the copy button.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A click that ends a selection of a part of the code copies nothing, and neither does a double or a triple click
    /// selecting a word or a line, so the reader can still pick out a part of it. This is a shortcut for the pointer only: the copy button is what a keyboard and a screen reader reach, so it
    /// is best kept shown beside it.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool CopyOnClick { get; set; }

    /// <summary>
    /// The name of the copy button, which is what a screen reader reads out for it and what the pointer shows as its
    /// tooltip.
    /// <br />
    /// The default value is <strong>null</strong>, which uses <see cref="BitStrings.Copy"/> of
    /// <see cref="BitStrings.Current"/> ("Copy").
    /// </summary>
    [Parameter] public string? CopyText { get; set; }

    /// <summary>
    /// The language of the code, shown as a label in the header. It is a label only: the code is not highlighted.
    /// </summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>
    /// The height (any CSS length) past which the code scrolls.
    /// </summary>
    [Parameter, ResetStyleBuilder] public string? MaxHeight { get; set; }

    /// <summary>
    /// The callback raised after the code has been put on the clipboard.
    /// </summary>
    [Parameter] public EventCallback OnCopy { get; set; }

    /// <summary>
    /// The callback raised when the browser refuses to put the code on the clipboard.
    /// </summary>
    /// <remarks>
    /// A browser writes to the clipboard only shortly after a press of the user, and some (Safari) only while the press
    /// is still being handled - which a copy made after a round trip to the server (Blazor Server) may come too late for.
    /// The button then keeps saying what it offers to do; this callback lets the page say more than that.
    /// </remarks>
    [Parameter] public EventCallback OnCopyFailed { get; set; }

    /// <summary>
    /// Shows the button that copies the code.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool ShowCopyButton { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the code block.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitCodeBlockClassStyles? Styles { get; set; }

    /// <summary>
    /// Draws the code block as a terminal window: a dark surface in both color schemes, the three dots of a title bar,
    /// and a prompt before every line of <see cref="Code"/>.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The prompt is drawn by the stylesheet (the --bit-CodeBlock-prompt variable), so it is neither selected nor copied
    /// with the commands. Content handed in through <see cref="ChildContent"/> is drawn as it is written, with no prompt.
    /// </remarks>
    [Parameter, ResetClassBuilder] public bool Terminal { get; set; }

    /// <summary>
    /// The title shown in the header of the code block - a file name, most of the time - which also names the code to a
    /// screen reader.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    // The confirmation in the words in force.
    private string CopiedLabel => CopiedText ?? BitStrings.Current.Copied;

    // A code block given neither code nor content has nothing to put on the clipboard.
    // A code block given neither code nor content has nothing to put on the clipboard, and an empty Code is no code: a
    // copy of it would only empty the clipboard of the user.
    private bool HasNothingToCopy => string.IsNullOrEmpty(Code) && ChildContent is null;



    protected override string RootElementClass => "bit-cdb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Terminal ? "bit-cdb-trm" : string.Empty);

        ClassBuilder.Register(() => CopyOnClick ? "bit-cdb-coc" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => MaxHeight.HasValue() ? $"--bit-cdb-mxh:{MaxHeight}" : string.Empty);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        NormalizeLineEndings();

        // The confirmation is about the code that was copied: a block handed another code (a reused block of a list of
        // snippets), or made unavailable, no longer has a copy to show for itself.
        if (_copied && (Disabled || HasNothingToCopy || string.Equals(Code, _copiedCode, StringComparison.Ordinal) is false))
        {
            ClearCopied();
        }

        base.OnParametersSet();
    }



    // A line ending of "\r\n" written into the block keeps its "\r" as a character of the text, and an engine that copies
    // a selection of it turns that and the "\n" into a break each (Firefox), doubling every line. So the code is drawn
    // with "\n" alone - once per Code, not per render. The copy button copies the Code as it was given.
    private void NormalizeLineEndings()
    {
        if (ReferenceEquals(_codeFor, Code)) return;

        _codeFor = Code;
        _code = (Code ?? string.Empty).ReplaceLineEndings("\n");
    }

    private void BuildCode(RenderTreeBuilder builder)
    {
        if (Terminal is false)
        {
            builder.AddContent(0, _code);
            return;
        }

        // In a terminal each command line is a span of its own, whose prompt the stylesheet draws before it. The line
        // break stays inside the span it ends, so the text copied out of the block is exactly the code, and the prompt -
        // generated content - is never part of it. A blank line is a gap between the commands, with no prompt.
        var code = _code;
        var start = 0;

        while (start < code.Length)
        {
            var newline = code.IndexOf('\n', start);
            var end = newline < 0 ? code.Length : newline + 1;
            var line = code[start..end];

            if (string.IsNullOrWhiteSpace(line))
            {
                builder.AddContent(1, line);
            }
            else
            {
                builder.OpenElement(2, "span");
                builder.AddAttribute(3, "class", "bit-cdb-lin");
                builder.AddContent(4, line);
                builder.CloseElement();
            }

            start = end;
        }
    }

    private async Task HandleCopyButtonClick()
    {
        await Copy(fromClick: false);
    }

    private async Task HandleBodyClick(MouseEventArgs e)
    {
        if (CopyOnClick is false) return;

        // The second and third click of a double or a triple click are the reader selecting a word or a line. The first
        // one is told apart by the script, which waits out a double click before it copies.
        if (e.Detail > 1) return;

        await Copy(fromClick: true);
    }

    private async Task Copy(bool fromClick)
    {
        // A press that reaches the button while it is unavailable - a screen reader's click, a script - copies nothing.
        if (Disabled || HasNothingToCopy || IsDisposed) return;

        var code = Code;

        bool? copied;

        try
        {
            // An empty Code beside a content is no code, so the copy takes the text the content renders.
            copied = await _js.BitCodeBlockCopy(_codeElement, string.IsNullOrEmpty(code) ? null : code, fromClick);
        }
        catch (JSDisconnectedException)
        {
            return; // the circuit is gone, and nobody is left to tell
        }
        catch (Exception)
        {
            copied = false;
        }

        // A click on the code that made a selection asked for no copy, so whatever shows stays as it is.
        if (IsDisposed || copied is null) return;

        if (copied is false)
        {
            // A clipboard that is not there to be written to leaves the button saying what it still offers to do, which
            // is the truth - a confirmation of an earlier copy still showing would claim this one worked, so it goes.
            if (_copied)
            {
                ClearCopied();

                StateHasChanged();
            }

            await OnCopyFailed.InvokeAsync();

            return;
        }

        // The parameters may have moved on while the browser was writing: a block handed another code, made
        // unavailable, or left with nothing to copy in the meantime has no copy of its current code to confirm - the
        // copy made is still reported.
        if (Disabled || HasNothingToCopy || string.Equals(Code, code, StringComparison.Ordinal) is false)
        {
            if (_copied)
            {
                ClearCopied();

                StateHasChanged();
            }

            await OnCopy.InvokeAsync();

            return;
        }

        // Each copy owns the confirmation it puts up, so the one before it running out does not take down the one that
        // has only just gone up.
        var token = ++_copyToken;

        _copied = true;
        _copiedCode = code;
        _copyCount++;

        StateHasChanged();

        await OnCopy.InvokeAsync();

        await Task.Delay(_confirmationDuration);

        if (IsDisposed || _copyToken != token) return;

        _copied = false;
        _copiedCode = null;

        StateHasChanged();
    }

    private void ClearCopied()
    {
        _copied = false;
        _copiedCode = null;
        _copyToken++;
    }
}
