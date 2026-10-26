using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiCodeBlockView shows a block of code with a header naming its language or file, a copy button, and syntax
/// highlighting.
/// </summary>
/// <remarks>
/// Highlighting is deferred while the block is still streaming. Re-tokenising a growing block on every delta is wasted
/// work, and colors that shift as the lexer changes its mind about an unterminated string are worse than no colors at
/// all; the block is highlighted once, the moment <see cref="IsStreaming"/> turns false.
/// <br />
/// The colors of the tokens are read from the theme - each one a role color tuned to be read on the page, so they
/// follow the preset and the color scheme - and every one of them can be restyled through its public
/// "--bit-AiCodeBlockView-*-color" variable. <see cref="BitAiUiOptions.ShowCodeCopy"/> and
/// <see cref="BitAiUiOptions.ShowLineNumbers"/> turn the copy button and the line numbers on and off for every block at
/// once.
/// </remarks>
public partial class BitAiCodeBlockView : BitAiComponentBase
{
    private IReadOnlyList<BitAiCodeToken> _tokens = [];
    private string? _tokenizedFor;
    private string? _tokenizedLanguage;
    private readonly RenderFragment _renderCode;

    // The code with every line ending a "\n" (see NormalizeLineEndings), and the Code it was made from.
    private string _code = string.Empty;
    private string? _codeFor;



    public BitAiCodeBlockView()
    {
        // Held in a field rather than created per render, so the diff keeps being handed the same delegate.
        _renderCode = BuildCode;
    }



    /// <summary>
    /// Gets or sets the cascading parameters for the code block component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (whether the header bar is shown, the classes
    /// and styles of the parts) to be applied to multiple code block components through the <see cref="BitParams"/>
    /// component.
    /// </remarks>
    [CascadingParameter(Name = BitAiCodeBlockViewParams.ParamName)]
    public BitAiCodeBlockViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the code block.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiCodeBlockViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The source to display.
    /// </summary>
    [Parameter] public string Code { get; set; } = string.Empty;

    /// <summary>
    /// A file name shown in the header in place of the language.
    /// </summary>
    [Parameter] public string? FileName { get; set; }

    /// <summary>
    /// Whether the block is still receiving content, which holds the highlighting back until it is complete.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool IsStreaming { get; set; }

    /// <summary>
    /// The language identifier used to pick a highlighter, also shown in the header when there is no file name.
    /// </summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>
    /// Shows the header bar with the language and the copy button.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the code block.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiCodeBlockViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aicbv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiCodeBlockViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        NormalizeLineEndings();
        Tokenize();

        base.OnParametersSet();
    }



    // A line ending of "\r\n" written into the block keeps its "\r" as a character of the text, and an engine that copies
    // a selection of it turns that and the "\n" into a break each (Firefox), doubling every line. So the code is drawn and
    // highlighted with "\n" alone - once per Code, not per render. The copy button copies the Code as it was given.
    private void NormalizeLineEndings()
    {
        if (ReferenceEquals(_codeFor, Code)) return;

        _codeFor = Code;
        _code = (Code ?? string.Empty).ReplaceLineEndings("\n");
    }

    private void Tokenize()
    {
        if (IsStreaming)
        {
            _tokens = [];
            _tokenizedFor = null;
            return;
        }

        if (ReferenceEquals(_tokenizedFor, _code) && _tokenizedLanguage == Language) return;

        _tokens = BitAiSyntaxHighlighter.Highlight(_code, Language);
        _tokenizedFor = _code;
        _tokenizedLanguage = Language;
    }

    private void BuildCode(RenderTreeBuilder builder)
    {
        var code = _code;
        var tokenIndex = 0;

        if (Options.ShowLineNumbers is false)
        {
            if (_tokens.Count == 0)
            {
                builder.AddContent(0, code);
                return;
            }

            builder.OpenRegion(1);
            BuildRange(builder, code, 0, code.Length, ref tokenIndex);
            builder.CloseRegion();
            return;
        }

        // Each line is a span of its own that the stylesheet numbers with a counter. The line break stays inside the
        // span it ends, so the text copied out of the block is exactly the code, and the numbers - generated content -
        // are never part of it.
        var customLineClass = Classes?.Line;
        var lineClass = customLineClass.HasValue() ? $"bit-aicbv-lin {customLineClass}" : "bit-aicbv-lin";
        var start = 0;

        do
        {
            var newline = code.IndexOf('\n', start);
            var end = newline < 0 ? code.Length : newline + 1;

            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "style", Styles?.Line);
            builder.AddAttribute(4, "class", lineClass);
            builder.OpenRegion(5);
            BuildRange(builder, code, start, end, ref tokenIndex);
            builder.CloseRegion();
            builder.CloseElement();

            start = end;
        }
        while (start < code.Length);
    }

    // Writes code[start..end) as plain text and token spans, moving tokenIndex past the tokens that end inside it. A
    // token running on past the end of the range (a comment or a string over several lines) is split there, and the
    // next range carries it on.
    private void BuildRange(RenderTreeBuilder builder, string code, int start, int end, ref int tokenIndex)
    {
        var position = start;

        while (tokenIndex < _tokens.Count)
        {
            var token = _tokens[tokenIndex];
            var tokenEnd = token.Start + token.Length;

            if (tokenEnd <= position)
            {
                tokenIndex++;
                continue;
            }

            if (token.Start >= end) break;

            var from = Math.Max(token.Start, position);
            var to = Math.Min(tokenEnd, end);

            if (from > position)
            {
                builder.AddContent(0, code[position..from]);
            }

            if (ClassFor(token.Kind) is { } tokenClass)
            {
                builder.OpenElement(1, "span");
                builder.AddAttribute(2, "class", tokenClass);
                builder.AddContent(3, code[from..to]);
                builder.CloseElement();
            }
            else
            {
                builder.AddContent(4, code[from..to]);
            }

            position = to;

            if (tokenEnd > end) break;

            tokenIndex++;
        }

        if (position < end)
        {
            builder.AddContent(5, code[position..end]);
        }
    }

    private static string? ClassFor(BitAiTokenKind kind) => kind switch
    {
        BitAiTokenKind.Keyword => "bit-aicbv-kwd",
        BitAiTokenKind.Type => "bit-aicbv-typ",
        BitAiTokenKind.String => "bit-aicbv-str",
        BitAiTokenKind.Number => "bit-aicbv-num",
        BitAiTokenKind.Comment => "bit-aicbv-cmt",
        BitAiTokenKind.Function => "bit-aicbv-fnc",
        BitAiTokenKind.Operator => "bit-aicbv-opr",
        BitAiTokenKind.Punctuation => "bit-aicbv-pnc",
        BitAiTokenKind.Property => "bit-aicbv-prp",
        BitAiTokenKind.Tag => "bit-aicbv-tag",
        BitAiTokenKind.Attribute => "bit-aicbv-atr",
        BitAiTokenKind.Constant => "bit-aicbv-cst",
        BitAiTokenKind.Inserted => "bit-aicbv-ins",
        BitAiTokenKind.Deleted => "bit-aicbv-del",
        BitAiTokenKind.Meta => "bit-aicbv-met",
        BitAiTokenKind.Label => "bit-aicbv-lab",
        BitAiTokenKind.Variable => "bit-aicbv-vrb",
        // Plain text needs no span of its own.
        _ => null,
    };
}
