using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiArtifactView is the panel an artifact lives in: a title, a version selector once there is more than one
/// version, copy, download and a full-screen toggle, around a body chosen by the kind of the artifact.
/// </summary>
/// <remarks>
/// Copy and download act on the version being viewed rather than always the latest, because the version on screen is
/// the one the user means. The body is the renderer the app registered for the kind
/// (<see cref="BitAiRendererRegistry.RegisterArtifact"/>), run inside a <see cref="BitAiRendererBoundary"/>, or else
/// a built-in one: an image, a document in streaming Markdown, a JSON tree, a table, or the code itself. An HTML page is
/// previewed in a frame that may run scripts but is denied same-origin access, which is the only way to show generated
/// markup without handing it the host page, and an SVG is shown as an image, which never runs a script inside it.
/// <br />
/// The panel follows the artifact as it streams, re-rendering at most once per render interval of the AI options. The
/// full-screen panel is the same element laid over the page, so the body - a running page in its frame above all - is
/// never re-created by the toggle; Escape and a click on the overlay collapse it again. Laid over the page it is a
/// modal dialog that holds the keyboard - the focus moves through its own controls and the page in its frame, and an
/// Escape pressed inside that page collapses it too.
/// </remarks>
public partial class BitAiArtifactView : BitAiComponentBase
{
    private bool _expanded;
    private bool _focusToggle;
    private bool _focusImage;
    private bool _holdsKeyboard;
    private ElementReference _image;
    private ElementReference _panel;
    private BitButton? _expandButton;
    private DotNetObjectReference<BitAiArtifactView>? _dotnetObj;
    private (string Page, string Document)? _frameDocument;
    private int? _selectedVersion;
    private BitAiArtifact? _subscribed;
    private bool _watchingEscape;
    private (int Count, int Version, BitAiStrings Strings)? _versionsKey;
    private BitAiIconClassesCache<BitButtonClassStyles> _downloadButtonClasses;
    private BitAiIconClassesCache<BitButtonClassStyles> _expandButtonClasses;
    private BitAiIconClassesCache<BitActionButtonClassStyles> _imagePlaceholderClasses;
    private List<BitDropdownItem<int>> _versionItems = [];

    // The image source the user asked to load under an image policy that asks first.
    private string? _loadedImage;

    // Which Escapes something inside the panel claimed first (see _OnEscapeVerdict), for HandleOnKeyDown. Made the first
    // time the panel is expanded, so a panel that never is starts no listener.
    private BitEscapeClaimWatch<BitAiArtifactView>? _escapeWatch;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the artifact view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple artifact view components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiArtifactViewParams.ParamName)]
    public BitAiArtifactViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// The artifact to show.
    /// </summary>
    [Parameter, EditorRequired] public BitAiArtifact Artifact { get; set; } = default!;

    /// <summary>
    /// Custom CSS classes for different parts of the artifact view.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiArtifactViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The version to show until the user picks another, for example the one a particular turn produced. The latest
    /// version is shown while it is unset, or when the artifact has no such version.
    /// </summary>
    [Parameter] public int? InitialVersion { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the artifact view.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiArtifactViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Sent by the browser as each Escape goes down inside the expanded panel, ahead of the keydown Blazor then
    /// dispatches, with whether something inside the panel claimed it first - a field of a registered renderer clearing
    /// itself, a dropdown closing its list - so the key handler that runs next leaves that press alone.
    /// <br />
    /// <strong>This method is intended for internal use and should not be called directly.</strong>
    /// </summary>
    [JSInvokable("OnEscapeVerdict")]
    public void _OnEscapeVerdict(bool foreign)
    {
        if (IsDisposing) return;

        _escapeWatch?.SetVerdict(foreign);
    }

    /// <summary>
    /// Sent by the browser when an Escape is pressed inside the page an HTML artifact runs in its frame while the panel is
    /// full screen - a key the page's own document hears, and no listener of the page around it - and the page left it
    /// alone. The panel collapses on it, as it does on an Escape pressed anywhere else in it.
    /// <br />
    /// <strong>This method is intended for internal use and should not be called directly.</strong>
    /// </summary>
    [JSInvokable("OnFrameEscape")]
    public async Task _OnFrameEscape()
    {
        if (IsDisposing || _expanded is false) return;

        Collapse();

        await InvokeAsync(StateHasChanged);
    }



    protected override string RootElementClass => "bit-aiatv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => _expanded ? "bit-aiatv-exp" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        if (ReferenceEquals(_subscribed, Artifact) is false)
        {
            if (_subscribed is not null) _subscribed.Changed -= HandleOnArtifactChanged;

            _subscribed = Artifact;
            _subscribed.Changed += HandleOnArtifactChanged;
            _selectedVersion = null;
            _versionsKey = null;
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // The panel listens for the Escapes claimed inside it only once it has been expanded, the one state in which it
        // acts on the key at all.
        if (_expanded && _watchingEscape is false)
        {
            _watchingEscape = true;
            _escapeWatch ??= new(_js, this);
            await _escapeWatch.WatchAsync(_panel);
        }

        // The full-screen panel holds the keyboard once it is laid over the page, and hands it back once it is in place
        // again.
        if (_expanded != _holdsKeyboard)
        {
            await HoldKeyboardAsync(_expanded);
        }

        // The toggle takes the focus once the panel has been laid out the new way (see ToggleExpanded).
        if (_focusToggle)
        {
            _focusToggle = false;

            await FocusToggleAsync();
        }

        // An image the user loaded takes the focus once it has replaced the button that loaded it (see LoadImage).
        if (_focusImage)
        {
            _focusImage = false;

            await FocusImageAsync();
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        if (_subscribed is not null)
        {
            _subscribed.Changed -= HandleOnArtifactChanged;
            _subscribed = null;
        }

        if (_escapeWatch is not null)
        {
            await _escapeWatch.DisposeAsync();
        }

        // A panel taken off the page while full screen leaves no trap or listener behind it.
        if (_holdsKeyboard)
        {
            await HoldKeyboardAsync(false);
        }

        _dotnetObj?.Dispose();
        _dotnetObj = null;

        await base.DisposeAsync(disposing);
    }



    /// <summary>
    /// The version on screen: the one the user picked, else <see cref="InitialVersion"/>, else the latest one.
    /// </summary>
    private int ViewedVersion
    {
        get
        {
            var wanted = _selectedVersion ?? InitialVersion;

            return wanted is { } version && version != Artifact.Version && Snapshot(version) is not null
                ? version
                : Artifact.Version;
        }
    }

    private bool IsLive => ViewedVersion == Artifact.Version;

    private bool IsStreaming => IsLive && Artifact.IsStreaming;

    private string? ViewedContent => IsLive ? Artifact.Content : Snapshot(ViewedVersion)?.Content;

    // What copy and download act on: the viewed content, or nothing when it is empty - an image shown from its Url
    // carries no content of its own - so neither copies nothing nor saves an empty file.
    private string? _SavableContent => ViewedContent is { Length: > 0 } content ? content : null;

    private bool _DownloadDisabled => Disabled || _SavableContent is null;

    private string _ExpandLabel => _expanded ? Strings.Collapse : Strings.Expand;

    // The Escape that collapses the full-screen panel, claimed on the root (see Utils.claimEscape) so a surface the
    // artifact sits in does not close on the same press. Inline, the panel does nothing on Escape and leaves it alone.
    private string? _EscapeClaim => _expanded ? "claim" : null;

    // The panel - the header and the body, without the overlay - which the full-screen panel keeps the keyboard in.
    private string _PanelId => $"{_Id}-pnl";

    // What the frame of an HTML page is handed: the page, then a script of the panel's own that reports an Escape the page
    // leaves alone - not prevented by it, not part of an input method's composition (as Utils.isComposingKey reads it),
    // and with no modifier - to the panel (BitAiArtifactView.ts). A key pressed inside the page is its own document's,
    // which no listener of the page around it hears, so the full-screen panel would otherwise keep the screen once the
    // focus is in the page. Written after the page rather than ahead of it, where it would come before the doctype and
    // put the page in quirks mode (a page ending inside an unclosed comment or script swallows it, and goes without it).
    // Kept for the page it was built for, so a panel rendered again does not build it again.
    private string FrameDocument(string page)
    {
        if (_frameDocument is { } built && ReferenceEquals(built.Page, page)) return built.Document;

        var document = page + FrameEscapeScript;
        _frameDocument = (page, document);

        return document;
    }

    private const string FrameEscapeScript =
        "<script>addEventListener('keydown',function(e){if(e.key==='Escape'&&!e.defaultPrevented&&!e.isComposing&&e.keyCode!==229" +
        "&&!e.altKey&&!e.ctrlKey&&!e.metaKey&&!e.shiftKey)parent.postMessage({bitAiArtifactView:'escape'},'*')})</script>";

    // The glyph beside the title, by the kind of the artifact, from this package's own icon font (none of them is in the
    // core's). Whole class names, which are what keeps each glyph in a trimmed icon stylesheet.
    private string _KindIcon => Artifact.Kind.Value.ToLowerInvariant() switch
    {
        "code" or "html" or "json" => "bit-icon-ai bit-icon-ai--Code",
        "image" or "svg" or "chart" or "diagram" => "bit-icon-ai bit-icon-ai--Photo2",
        "document" => "bit-icon-ai bit-icon-ai--Page",
        _ => "bit-icon-ai bit-icon-ai--MapLayers",
    };

    // The classes handed to the two buttons: the app's own for each, with the glyph of this package's icon font added to
    // the icon, since neither glyph is in the core's (see BitAiIconClasses). Kept while neither changes, so a panel
    // re-rendered on every streamed delta keeps handing the button the same instance rather than resetting its builder.
    private BitButtonClassStyles _DownloadButtonClasses =>
        _downloadButtonClasses.Get("bit-icon-ai bit-icon-ai--Download", Classes?.DownloadButton, BitAiIconClasses.With);

    private BitButtonClassStyles _ExpandButtonClasses =>
        _expandButtonClasses.Get(_expanded ? "bit-icon-ai bit-icon-ai--BackToWindow" : "bit-icon-ai bit-icon-ai--FullScreen",
                                 Classes?.ExpandButton,
                                 BitAiIconClasses.With);

    private BitActionButtonClassStyles _ImagePlaceholderClasses =>
        _imagePlaceholderClasses.Get("bit-icon-ai bit-icon-ai--Photo2", Classes?.ImagePlaceholder, BitAiIconClasses.With);

    private void HandleOnArtifactChanged(BitAiArtifact artifact) => RequestRender();

    private void HandleOnVersionChange(int version)
    {
        if (Disabled) return;

        _selectedVersion = version;
    }

    private void ToggleExpanded()
    {
        if (_expanded is false && Disabled) return;

        _expanded = _expanded is false;
        _focusToggle = true;

        ClassBuilder.Reset();
    }

    private void HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not "Escape") return;

        // One Escape does one thing: a press something inside the panel claimed first - a dropdown closing its list, a
        // field of a registered renderer clearing itself - is that part's, which Blazor still bubbles up to here. The
        // verdict is read once, so it never outlives the press it was given for.
        if (_escapeWatch?.TakeForeign() is true) return;

        // Only the plain key, the one the panel claims: an Escape with a modifier is left to the surface around it.
        if (e.IsPlainEscape() is false) return;

        if (_expanded is false) return;

        Collapse();
    }

    private void Collapse()
    {
        _expanded = false;
        _focusToggle = true;

        ClassBuilder.Reset();
    }

    // The full-screen panel keeps the keyboard inside itself as a modal dialog does: Tab and Shift+Tab cycle through its
    // controls, the focus a Tab out of the page in its frame hands to the page behind it is brought back, and an Escape
    // pressed inside that page is reported (OnFrameEscape) - see BitAiArtifactView.ts. Released as the panel collapses.
    private async Task HoldKeyboardAsync(bool hold)
    {
        if (hold && IsDisposing) return;

        _holdsKeyboard = hold;

        try
        {
            if (hold)
            {
                _dotnetObj ??= DotNetObjectReference.Create(this);

                await _js.BitAiArtifactViewExpand(_PanelId, _dotnetObj);
            }
            else
            {
                await _js.BitAiArtifactViewCollapse(_PanelId);
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // without it the panel still collapses on its own Escape and its overlay
    }

    // The button that loaded an image is gone once the image is shown, and the focus it held with it - to the body of the
    // document, from where the keyboard and a screen reader start over at the top of the page. So the image takes it.
    private void LoadImage(string source)
    {
        _loadedImage = source;
        _focusImage = true;
    }

    private async Task FocusImageAsync()
    {
        // No image was rendered for the source loaded - another artifact or version took its place in the meantime.
        if (string.IsNullOrEmpty(_image.Id) || IsDisposing) return;

        try
        {
            await _image.FocusAsync();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // an image taken off the page in the meantime has nothing to focus
    }

    // The toggle is the one control of the panel that is there both ways, so it is where the focus goes each time the
    // panel turns: into the full-screen panel as it opens - a click does not focus a button in every engine (WebKit's),
    // and an Escape pressed with the focus left outside the panel would close the dialog or panel around it rather than
    // collapse this one - and back to where the panel sits inline as it collapses, from the overlay or the key as well.
    private async Task FocusToggleAsync()
    {
        if (_expandButton is null || IsDisposing) return;

        try
        {
            await _expandButton.FocusAsync();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // a toggle taken off the page in the meantime has nothing to focus
    }

    private async Task DownloadAsync()
    {
        if (Disabled || _SavableContent is not { } content) return;

        var (fileName, mimeType) = DownloadInfo(Artifact, content, IsLive ? null : ViewedVersion);

        try
        {
            await _js.BitAiArtifactViewDownloadText(fileName, content, mimeType);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // a browser that refuses the download leaves the panel as it was
    }

    // The items of the version dropdown: every committed version plus the current draft. Read as the panel renders,
    // which is also when a version committed while streaming shows up, and rebuilt only when the set can have changed -
    // a streaming artifact re-renders the panel on every coalesced delta - so the dropdown keeps being handed the same
    // list.
    private List<BitDropdownItem<int>> _VersionItems
    {
        get
        {
            var key = (Artifact.Versions.Count, Artifact.Version, Strings);
            if (_versionsKey == key) return _versionItems;

            _versionsKey = key;
            _versionItems = AvailableVersions().Select(v => new BitDropdownItem<int> { Text = Strings.VersionNumber(v), Value = v }).ToList();

            return _versionItems;
        }
    }

    /// <summary>
    /// Every version that can be shown: the committed ones plus the current draft, oldest first.
    /// </summary>
    private List<int> AvailableVersions()
    {
        var versions = new List<int>(Artifact.Versions.Count + 1);

        foreach (var snapshot in Artifact.Versions)
        {
            if (versions.Contains(snapshot.Version) is false) versions.Add(snapshot.Version);
        }

        if (versions.Contains(Artifact.Version) is false) versions.Add(Artifact.Version);

        versions.Sort();

        return versions;
    }

    private BitAiArtifactVersion? Snapshot(int version)
    {
        var versions = Artifact.Versions;

        for (var i = versions.Count - 1; i >= 0; i--)
        {
            if (versions[i].Version == version) return versions[i];
        }

        return null;
    }

    private static string? SvgSource(string? markup, string? url) =>
        markup is { Length: > 0 }
            ? $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(markup))}"
            : url;

    private static bool LooksLikeJson(string content)
    {
        var trimmed = content.AsSpan().TrimStart();

        return trimmed.Length > 0 && trimmed[0] is '[' or '{';
    }

    private static bool IsTabSeparated(string? language) =>
        string.Equals(language, "tsv", StringComparison.OrdinalIgnoreCase);

    private static string? DefaultLanguage(BitAiArtifactKind kind) =>
        kind == BitAiArtifactKind.Html ? "html"
        : kind == BitAiArtifactKind.Json ? "json"
        : kind == BitAiArtifactKind.Svg ? "xml"
        : null;

    /// <summary>
    /// Splits delimited text into rows of fields, honouring double-quoted fields.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="delimiter">The field separator.</param>
    /// <returns>The rows.</returns>
    internal static List<List<string>> ParseDelimited(string text, char delimiter = ',')
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var fieldStarted = false;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c != '"')
                {
                    field.Append(c);
                }
                else if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    quoted = false;
                }

                continue;
            }

            if (c == '"' && fieldStarted is false)
            {
                quoted = true;
                fieldStarted = true;
            }
            else if (c == delimiter)
            {
                row.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
            }
            else if (c == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                fieldStarted = false;
                rows.Add(row);
                row = [];
            }
            else if (c != '\r')
            {
                field.Append(c);
                fieldStarted = true;
            }
        }

        if (fieldStarted || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Chooses a file name and MIME type for downloading an artifact.
    /// </summary>
    /// <param name="artifact">The artifact.</param>
    /// <param name="content">The content being downloaded.</param>
    /// <param name="version">The version, when it is not the latest.</param>
    /// <returns>The file name and MIME type.</returns>
    internal static (string FileName, string MimeType) DownloadInfo(BitAiArtifact artifact, string content, int? version = null)
    {
        var kind = artifact.Kind;
        var (extension, mimeType) =
            kind == BitAiArtifactKind.Document ? ("md", "text/markdown")
            : kind == BitAiArtifactKind.Html ? ("html", "text/html")
            : kind == BitAiArtifactKind.Json ? ("json", "application/json")
            : kind == BitAiArtifactKind.Svg ? ("svg", "image/svg+xml")
            : kind == BitAiArtifactKind.Table ? (LooksLikeJson(content) ? ("json", "application/json")
                : IsTabSeparated(artifact.Language) ? ("tsv", "text/tab-separated-values") : ("csv", "text/csv"))
            : (ExtensionFor(artifact.Language), "text/plain");

        var name = Slug(artifact.Title);
        if (version is { } v) name += string.Create(CultureInfo.InvariantCulture, $"-v{v}");

        return ($"{name}.{extension}", mimeType);
    }

    private static string ExtensionFor(string? language) => language?.ToLowerInvariant() switch
    {
        "csharp" or "cs" or "c#" => "cs",
        "javascript" or "js" => "js",
        "typescript" or "ts" => "ts",
        "python" or "py" => "py",
        "html" => "html",
        "css" => "css",
        "json" => "json",
        "xml" => "xml",
        "yaml" or "yml" => "yaml",
        "markdown" or "md" => "md",
        "sql" => "sql",
        "bash" or "sh" or "shell" => "sh",
        "powershell" or "ps1" or "pwsh" => "ps1",
        "razor" => "razor",
        "java" => "java",
        "go" => "go",
        "rust" or "rs" => "rs",
        "cpp" or "c++" => "cpp",
        "c" => "c",
        "ruby" or "rb" => "rb",
        "php" => "php",
        "kotlin" or "kt" => "kt",
        "swift" => "swift",
        { Length: > 0 } other when other.All(char.IsAsciiLetterOrDigit) && other.Length <= 8 => other,
        _ => "txt",
    };

    // The file name a title makes: its words, lower-cased and joined by hyphens. The letters and digits of every script are
    // words - a Persian or a Chinese title names its file as an English one does, rather than leaving it "artifact" - with
    // the marks that complete a letter where a script writes its vowels as marks (Devanagari's, Arabic's harakat).
    private static string Slug(string title)
    {
        var builder = new StringBuilder(title.Length);

        foreach (var c in title.Trim().ToLowerInvariant())
        {
            if (IsWordChar(c)) builder.Append(c);
            else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }

        var slug = builder.ToString().Trim('-');

        return slug.Length == 0 ? "artifact" : slug;
    }

    private static bool IsWordChar(char c) =>
        char.IsLetterOrDigit(c) ||
        CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
}
