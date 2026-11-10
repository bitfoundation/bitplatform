using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>Why a <see cref="BitBhFileUpload"/> refused a file.</summary>
[Flags]
public enum BitBhFileRejectionReason
{
    /// <summary>Not refused.</summary>
    None = 0,

    /// <summary>Its type or extension isn't in <c>Accept</c>.</summary>
    Type = 1,

    /// <summary>It is larger than <c>MaxFileSize</c>.</summary>
    TooLarge = 2,

    /// <summary>It is smaller than <c>MinFileSize</c>.</summary>
    TooSmall = 4,

    /// <summary>It would make more than <c>MaxFiles</c> files.</summary>
    TooMany = 8,

    /// <summary>The <c>Validate</c> callback refused it.</summary>
    Invalid = 16,
}

/// <summary>A file a <see cref="BitBhFileUpload"/> refused, and why.</summary>
public sealed record BitBhFileRejection(IBrowserFile File, BitBhFileRejectionReason Reasons);

/// <summary>
/// A headless file upload (Ark FileUpload, React Aria DropZone / FileTrigger) built on Blazor's <see cref="InputFile"/>: a
/// drop zone that is also a button, triggers that open the browser's file picker, validation and a list of the chosen
/// files with remove buttons.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Picking</b> — clicking (or Enter / Space on) the <see cref="BitBhFileUploadDropzone"/> or a <see cref="BitBhFileUploadTrigger"/>
///     opens the picker straight from the browser (no round trip, so the click's user activation is never lost).</item>
///   <item><b>Dropping</b> — files dragged over the drop zone set <c>data-dragging</c>; dropped files go through the same input.</item>
///   <item><b>Validation</b> — <see cref="Accept"/> (extensions and MIME types, <c>image/*</c>), <see cref="MaxFileSize"/>,
///     <see cref="MinFileSize"/>, <see cref="MaxFiles"/> and <see cref="Validate"/>; refused files go to <see cref="OnFilesRejected"/>.</item>
///   <item><b>Files</b> — <c>@bind-Files</c> (<see cref="IBrowserFile"/>s). Every selection keeps its own hidden input, so the files of
///     earlier selections stay readable (<c>OpenReadStream</c>) after later ones.</item>
/// </list>
///
/// <code>
/// &lt;BitBhFileUpload @bind-Files="files" Accept="image/*,.pdf" MaxFileSize="5_000_000" Multiple="true" class="upload"&gt;
///     &lt;Template Context="u"&gt;
///         &lt;BitBhFileUploadDropzone class="dropzone"&gt;Drop files here, or click to browse&lt;/BitBhFileUploadDropzone&gt;
///         &lt;ul&gt;
///             @foreach (var file in u.Files)
///             {
///                 &lt;BitBhFileUploadItem File="file"&gt;@file.Name &lt;BitBhFileUploadItemRemove&gt;×&lt;/BitBhFileUploadItemRemove&gt;&lt;/BitBhFileUploadItem&gt;
///             }
///         &lt;/ul&gt;
///     &lt;/Template&gt;
/// &lt;/BitBhFileUpload&gt;
/// </code>
/// </summary>
public class BitBhFileUpload : BitBhComponentBase
{
    private readonly List<Batch> _batches = new();
    private int _nextBatch = 1;
    private readonly HashSet<Batch> _unsynced = new();
    private BitBhFileUploadContext? _context;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    /// <summary>The chosen files. Supplying it makes the list controlled (use <c>@bind-Files</c>).</summary>
    [Parameter]
    public IReadOnlyList<IBrowserFile>? Files { get; set; }

    /// <summary>Two-way binding callback for <see cref="Files"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> FilesChanged { get; set; }

    /// <summary>Fires with the files each selection or drop added (after validation): read them here.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesAdded { get; set; }

    /// <summary>Fires with the files a selection or drop brought that were refused.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<BitBhFileRejection>> OnFilesRejected { get; set; }

    /// <summary>Fires when a file is removed from the list.</summary>
    [Parameter]
    public EventCallback<IBrowserFile> OnFileRemoved { get; set; }

    /// <summary>Accepted types, as the <c>accept</c> attribute writes them: <c>".pdf,image/*,text/csv"</c>.</summary>
    [Parameter]
    public string? Accept { get; set; }

    /// <summary>Allows several files. Without it a new file replaces the chosen one.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>The maximum number of files (with <see cref="Multiple"/>).</summary>
    [Parameter]
    public int? MaxFiles { get; set; }

    /// <summary>The maximum size of a file, in bytes.</summary>
    [Parameter]
    public long? MaxFileSize { get; set; }

    /// <summary>The minimum size of a file, in bytes.</summary>
    [Parameter]
    public long? MinFileSize { get; set; }

    /// <summary>Decides whether a file may be added.</summary>
    [Parameter]
    public Func<IBrowserFile, bool>? Validate { get; set; }

    /// <summary>Lets phones open the camera instead of the files (<c>capture</c>: "user" or "environment").</summary>
    [Parameter]
    public string? Capture { get; set; }

    /// <summary>Picks a whole folder (<c>webkitdirectory</c>).</summary>
    [Parameter]
    public bool Directory { get; set; }

    /// <summary>Form field name of the file inputs, for a plain (static SSR) multipart form post.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Disables picking, dropping and removing.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the upload invalid.</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>The parts, when they don't depend on the files.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The parts, rendered with the upload's state (<see cref="BitBhFileUploadContext.Files"/> …).</summary>
    [Parameter]
    public RenderFragment<BitBhFileUploadContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The chosen files.</summary>
    public IReadOnlyList<IBrowserFile> CurrentFiles => IsProvided(nameof(Files)) ? Files ?? [] : _batches.SelectMany(b => b.Files).ToArray();

    internal bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    internal bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false);
    internal BitBhFieldContext? Field => FieldContext;

    /// <summary>The id of the input the next selection goes into (what the triggers and the drop zone open).</summary>
    internal string InputId => $"{ComponentId}-input";

    internal bool IsMaxReached => Multiple && MaxFiles is { } max && CurrentFiles.Count >= max;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _context ??= new BitBhFileUploadContext(this);
        builder.OpenElement(0, Tag);
        builder.AddAttribute(1, "id", ComponentId);
        builder.AddMultipleAttributes(2, GetFinalAttributes());
        builder.AddElementReferenceCapture(3, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhFileUploadContext>>(4);
        builder.AddComponentParameter(5, "Value", _context);
        builder.AddComponentParameter(6, "IsFixed", false);
        builder.AddComponentParameter(7, "ChildContent", BitBhContent.Pick(Template, ChildContent, _context));
        builder.CloseComponent();

        // One hidden input per earlier selection (its files stay readable), then the one the next selection goes into.
        builder.OpenRegion(10);
        foreach (var batch in _batches)
            RenderInput(builder, batch.Key, $"{ComponentId}-batch-{batch.Key.ToString(CultureInfo.InvariantCulture)}");
        RenderInput(builder, _nextBatch, InputId);
        builder.CloseRegion();

        builder.CloseElement();
    }

    private void RenderInput(RenderTreeBuilder builder, int key, string id)
    {
        builder.OpenComponent<InputFile>(0);
        builder.SetKey(key);
        var attributes = new Dictionary<string, object>
        {
            ["id"] = id,
            ["hidden"] = true,
            ["tabindex"] = "-1",
            ["aria-hidden"] = "true",
        };
        if (!string.IsNullOrEmpty(Accept)) attributes["accept"] = Accept;
        if (Multiple) attributes["multiple"] = true;
        if (!string.IsNullOrEmpty(Capture)) attributes["capture"] = Capture;
        if (Directory) attributes["webkitdirectory"] = true;
        if (!string.IsNullOrEmpty(Name)) attributes["name"] = Name;
        if (IsDisabled) attributes["disabled"] = true;
        builder.AddComponentParameter(1, nameof(InputFile.AdditionalAttributes), attributes);
        builder.AddComponentParameter(2, nameof(InputFile.OnChange), EventCallback.Factory.Create<InputFileChangeEventArgs>(this, e => HandleChangeAsync(key, e)));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "empty", CurrentFiles.Count == 0);
        SetDataFlag(attrs, "max-reached", IsMaxReached);
        return attrs;
    }

    private async Task HandleChangeAsync(int key, InputFileChangeEventArgs e)
    {
        if (IsDisabled || key != _nextBatch) return;
        var incoming = e.FileCount == 0 ? [] : e.GetMultipleFiles(e.FileCount);
        var current = Multiple ? CurrentFiles.ToList() : [];
        var accepted = new List<IBrowserFile>();
        var rejected = new List<BitBhFileRejection>();

        foreach (var file in incoming)
        {
            var reasons = Check(file);
            if (reasons == BitBhFileRejectionReason.None && Multiple && MaxFiles is { } max && current.Count + accepted.Count >= max)
                reasons |= BitBhFileRejectionReason.TooMany;
            if (!Multiple && accepted.Count > 0) reasons |= BitBhFileRejectionReason.TooMany;
            if (reasons == BitBhFileRejectionReason.None) accepted.Add(file);
            else rejected.Add(new BitBhFileRejection(file, reasons));
        }

        if (accepted.Count > 0)
        {
            // The input that brought the files keeps them; the next selection goes into a new one.
            if (!Multiple) _batches.Clear();
            var batch = new Batch(key, accepted, incoming.ToList());
            _batches.Add(batch);
            // Rejected files must not be posted with the form either.
            if (accepted.Count < incoming.Count) _unsynced.Add(batch);
            _nextBatch++;
            await SetFilesAsync([.. current, .. accepted]);
            await OnFilesAdded.InvokeAsync(accepted);
        }
        if (rejected.Count > 0) await OnFilesRejected.InvokeAsync(rejected);
        StateHasChanged();
    }

    private BitBhFileRejectionReason Check(IBrowserFile file)
    {
        var reasons = BitBhFileRejectionReason.None;
        if (!IsAccepted(file, Accept)) reasons |= BitBhFileRejectionReason.Type;
        if (MaxFileSize is { } max && file.Size > max) reasons |= BitBhFileRejectionReason.TooLarge;
        if (MinFileSize is { } min && file.Size < min) reasons |= BitBhFileRejectionReason.TooSmall;
        if (reasons == BitBhFileRejectionReason.None && Validate?.Invoke(file) == false) reasons |= BitBhFileRejectionReason.Invalid;
        return reasons;
    }

    /// <summary>
    /// Whether <paramref name="file"/> matches an <c>accept</c> list (<c>".pdf,image/*,text/csv"</c>): an extension, a MIME
    /// type or a <c>type/*</c> wildcard. An empty list accepts everything. The browser's picker filters by it too, but a
    /// drop doesn't - this is what keeps a dropped file in line.
    /// </summary>
    public static bool IsAccepted(IBrowserFile file, string? accept)
    {
        if (string.IsNullOrWhiteSpace(accept)) return true;
        if (accept.Split(',', StringSplitOptions.TrimEntries).Contains("*/*")) return true;
        var type = file.ContentType ?? string.Empty;
        foreach (var raw in accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw.StartsWith('.'))
            {
                if (file.Name.EndsWith(raw, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (raw.EndsWith("/*", StringComparison.Ordinal))
            {
                if (type.StartsWith(raw[..^1], StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (string.Equals(type, raw, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private async Task SetFilesAsync(IReadOnlyList<IBrowserFile> files)
    {
        await FilesChanged.InvokeAsync(files);
        StateHasChanged();
    }

    /// <summary>Removes a file from the list.</summary>
    public async Task RemoveAsync(IBrowserFile file)
    {
        if (IsDisabled) return;
        foreach (var batch in _batches.ToArray())
        {
            if (!batch.Files.Remove(file)) continue;
            if (batch.Files.Count == 0) _batches.Remove(batch);
            // The batch's input still holds the removed file: narrow it, so a plain form post leaves it out.
            else _unsynced.Add(batch);
        }
        var files = CurrentFiles.Where(f => !ReferenceEquals(f, file)).ToArray();
        await SetFilesAsync(files);
        await OnFileRemoved.InvokeAsync(file);
    }

    /// <summary>Removes every file.</summary>
    public async Task ClearAsync()
    {
        if (IsDisabled) return;
        _batches.Clear();
        _unsynced.Clear();
        await SetFilesAsync([]);
    }

    /// <summary>Opens the browser's file picker. Prefer a trigger: a picker opened from .NET can be blocked when the user's click is too far back.</summary>
    public ValueTask OpenAsync() => Interop.ClickAsync(InputId);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_unsynced.Count == 0) return;
        if (string.IsNullOrEmpty(Name))
        {
            _unsynced.Clear();
            return;
        }
        foreach (var batch in _unsynced.ToArray())
        {
            _unsynced.Remove(batch);
            if (!_batches.Contains(batch)) continue;
            var indices = batch.Files.Select(f => batch.InInput.IndexOf(f)).Where(i => i >= 0).ToArray();
            await Interop.KeepFilesAsync($"{ComponentId}-batch-{batch.Key.ToString(CultureInfo.InvariantCulture)}", indices);
            batch.InInput.Clear();
            batch.InInput.AddRange(batch.Files);
        }
    }

    // Files: the batch's files still listed; InInput: what its input element holds, in order.
    private sealed record Batch(int Key, List<IBrowserFile> Files, List<IBrowserFile> InInput);
}

/// <summary>The state of a <see cref="BitBhFileUpload"/>, cascaded to its parts and given to its <c>Template</c>.</summary>
public sealed class BitBhFileUploadContext
{
    internal BitBhFileUploadContext(BitBhFileUpload owner) => Owner = owner;

    internal BitBhFileUpload Owner { get; }

    /// <summary>The chosen files.</summary>
    public IReadOnlyList<IBrowserFile> Files => Owner.CurrentFiles;

    /// <summary>Whether no more files can be added.</summary>
    public bool IsMaxReached => Owner.IsMaxReached;

    /// <summary>Removes a file.</summary>
    public Task RemoveAsync(IBrowserFile file) => Owner.RemoveAsync(file);

    /// <summary>Removes every file.</summary>
    public Task ClearAsync() => Owner.ClearAsync();
}

/// <summary>
/// The drop zone of a <see cref="BitBhFileUpload"/>: files dropped on it are added, and - unless <see cref="Clickable"/> is
/// off - it is a button (<c>role="button"</c>, Enter / Space) that opens the file picker. <c>data-dragging</c> while files are
/// dragged over it. Don't put a <see cref="BitBhFileUploadTrigger"/> inside a clickable drop zone (a button in a button).
/// </summary>
public class BitBhFileUploadDropzone : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhFileUploadContext? Context { get; set; }

    /// <summary>Whether the zone is itself a button that opens the picker. Default: true.</summary>
    [Parameter]
    public bool Clickable { get; set; } = true;

    /// <summary>The content (instructions, an icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        var disabled = owner.IsDisabled;
        if (!disabled) attrs["data-bit-bh-file-drop"] = owner.InputId;
        if (Clickable)
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            if (disabled) attrs["aria-disabled"] = "true";
            else
            {
                attrs["data-bit-bh-file-trigger"] = owner.InputId;
                attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            }
            if (owner.Field?.LabelledBy is { } label && GetAdditionalAttribute("aria-label") is null) attrs["aria-labelledby"] = $"{label} {ComponentId}";
            if (owner.Field?.DescribedBy is { } description) attrs["aria-describedby"] = description;
            attrs["data-bit-bh-interactive"] = "";
        }
        SetDataFlag(attrs, "disabled", disabled);
        SetDataFlag(attrs, "invalid", owner.IsInvalid);
        return attrs;
    }
}

/// <summary>A button that opens the file picker of a <see cref="BitBhFileUpload"/> (React Aria FileTrigger).</summary>
public class BitBhFileUploadTrigger : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhFileUploadContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = Context?.Owner is not { } owner || owner.IsDisabled;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            if (!disabled) attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        // The runtime opens the picker in the click itself: the browser only allows it with the user's activation.
        if (!disabled && Context?.Owner is { } o) attrs["data-bit-bh-file-trigger"] = o.InputId;
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled);
        return attrs;
    }
}

/// <summary>A chosen file of a <see cref="BitBhFileUpload"/> (<c>&lt;li&gt;</c>), holding its name, size and remove button.</summary>
public class BitBhFileUploadItem : BitBhComponentBase
{
    /// <summary>The file.</summary>
    [Parameter, EditorRequired]
    public IBrowserFile File { get; set; } = default!;

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhFileUploadItem>>(30);
        builder.AddComponentParameter(31, "Value", this);
        builder.AddComponentParameter(32, "IsFixed", true);
        builder.AddComponentParameter(33, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-type"] = File.ContentType ?? string.Empty;
        return attrs;
    }
}

/// <summary>The button that removes a <see cref="BitBhFileUploadItem"/>'s file ("Remove report.pdf").</summary>
public class BitBhFileUploadItemRemove : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhFileUploadContext? Context { get; set; }

    [CascadingParameter]
    private BitBhFileUploadItem? Item { get; set; }

    /// <summary>The content (an icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => Item is not null && Context is not null ? Context.RemoveAsync(Item.File) : Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = Context?.Owner is not { } owner || owner.IsDisabled;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        if (Item is not null) attrs["aria-label"] = Strings.RemoveFile(Item.File.Name);
        attrs["data-bit-bh-interactive"] = "";
        return attrs;
    }
}
