namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.AttachmentChip;

public partial class BitAiAttachmentChipDemo
{
    private readonly string example1RazorCode = @"
<BitAiAttachmentChip Attachment=""report"" />

<BitAiAttachmentChip Attachment=""longName"" />";
    private readonly string example1CsharpCode = @"
private readonly BitAiAttachment report = new(""att-report"", ""quarterly-report.pdf"")
{
    ContentType = ""application/pdf"",
    Size = 482_133,
    Kind = BitAiAttachmentKind.Document,
};

private readonly BitAiAttachment longName = new(""att-long"", ""minutes-of-the-planning-meeting-with-the-whole-team.docx"")
{
    Size = 23_552,
    Kind = BitAiAttachmentKind.Document,
};";

    private readonly string example2RazorCode = @"
<div style=""display:flex;flex-wrap:wrap;gap:0.5rem"">
    @foreach (var attachment in kinds)
    {
        <BitAiAttachmentChip @key=""attachment.AttachmentId"" Attachment=""attachment"" />
    }
</div>";
    private readonly string example2CsharpCode = @"
private const string PhotoUrl = ""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"";

private readonly IReadOnlyList<BitAiAttachment> kinds =
[
    new(""att-kind-image"", ""aurora.jpg"") { Size = 1_843_200, Kind = BitAiAttachment.InferKind(""image/jpeg""), ThumbnailUrl = PhotoUrl },
    new(""att-kind-audio"", ""voice-note.m4a"") { Size = 412_000, Kind = BitAiAttachment.InferKind(null, ""voice-note.m4a"") },
    new(""att-kind-video"", ""screen-recording.webm"") { Size = 9_830_400, Kind = BitAiAttachment.InferKind(""video/webm"") },
    new(""att-kind-document"", ""contract.pdf"") { Size = 220_160, Kind = BitAiAttachment.InferKind(""application/pdf"") },
    new(""att-kind-file"", ""data.csv"") { Size = 5_120, Kind = BitAiAttachment.InferKind(""text/csv"", ""data.csv"") },
];";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""UploadAsync"" Disabled=""isUploading"">Upload a file</BitButton>

<div style=""display:flex;flex-wrap:wrap;gap:0.5rem"">
    <BitAiAttachmentChip Attachment=""upload"" />
    <BitAiAttachmentChip Attachment=""failed"" />
</div>";
    private readonly string example3CsharpCode = @"
private bool isUploading;
private BitAiAttachment upload = new(""att-upload"", ""site-survey.zip"") { Size = 3_145_728, Status = BitAiAttachmentStatus.Pending };
private readonly BitAiAttachment failed = new(""att-failed"", ""backup.tar"")
{
    Size = 734_003_200,
    Status = BitAiAttachmentStatus.Failed,
    Error = ""Over the 100 MB limit of the server"",
};

private async Task UploadAsync()
{
    isUploading = true;

    for (var progress = 0.0; progress < 1.0; progress += 0.1)
    {
        upload = upload with { Status = BitAiAttachmentStatus.Uploading, Progress = progress };
        StateHasChanged();

        await Task.Delay(250);
    }

    upload = upload with { Status = BitAiAttachmentStatus.Ready, Progress = null };
    isUploading = false;
}";

    private readonly string example4RazorCode = @"
<div style=""display:flex;flex-wrap:wrap;gap:0.5rem"">
    @foreach (var attachment in staged)
    {
        <BitAiAttachmentChip @key=""attachment.AttachmentId"" @ref=""stagedChips[attachment.AttachmentId]"" Attachment=""attachment"" OnRemove=""Remove"" />
    }
</div>

<BitButton @ref=""resetButton"" Variant=""BitVariant.Outline"" OnClick=""ResetStaged"">Bring them back</BitButton>";
    private readonly string example4CsharpCode = @"
private const string PhotoUrl = ""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"";

private List<BitAiAttachment> staged = CreateStaged();
private readonly Dictionary<string, BitAiAttachmentChip> stagedChips = [];
private BitButton resetButton = default!;

private static List<BitAiAttachment> CreateStaged() =>
[
    new(""att-staged-1"", ""aurora.jpg"") { Size = 1_843_200, Kind = BitAiAttachmentKind.Image, ThumbnailUrl = PhotoUrl },
    new(""att-staged-2"", ""notes.txt"") { Size = 1_024 },
    new(""att-staged-3"", ""budget.xlsx"") { Size = 48_640 },
];

private async Task Remove(BitAiAttachment attachment)
{
    var index = staged.IndexOf(attachment);

    staged.Remove(attachment);
    stagedChips.Remove(attachment.AttachmentId);

    // The remove button goes with its chip: the focus moves to the chip that took its place, or the last one, or the
    // button that brings them back once none is left.
    if (staged.Count > 0)
    {
        await stagedChips[staged[Math.Min(index, staged.Count - 1)].AttachmentId].FocusAsync();
    }
    else
    {
        await resetButton.FocusAsync();
    }
}

private void ResetStaged()
{
    staged = CreateStaged();
}";

    private readonly string example5RazorCode = @"
<BitAiAttachmentChip Disabled Attachment=""report"" OnRemove=""RemoveDisabled"" />";
    private readonly string example5CsharpCode = @"
private readonly BitAiAttachment report = new(""att-report"", ""quarterly-report.pdf"")
{
    ContentType = ""application/pdf"",
    Size = 482_133,
    Kind = BitAiAttachmentKind.Document,
};

// Never called: the remove button of a disabled chip cannot be pressed.
private void RemoveDisabled(BitAiAttachment attachment) { }";

    private readonly string example6RazorCode = @"
<style>
    .custom-class {
        padding: 0.25rem;
        border-radius: 0.5rem;
        background-color: var(--bit-clr-bg-sec);
    }

    .custom-text {
        font-weight: bold;
        color: var(--bit-clr-pri);
    }

    .custom-thumbnail {
        outline: 2px solid var(--bit-clr-pri);
        outline-offset: 1px;
    }
</style>

<div style=""display:flex;flex-direction:column;align-items:flex-start;gap:0.75rem"">
    <BitAiAttachmentChip Attachment=""report"" Style=""padding:0.25rem;border:1px dashed var(--bit-clr-brd-pri)"" />

    <BitAiAttachmentChip Attachment=""photo"" Class=""custom-class"" />

    <BitAiAttachmentChip Attachment=""report""
                         Styles=""@(new() { Tag = new() { Root = ""border-radius:1rem"" }, Thumbnail = ""border-radius:50%"", Icon = ""color:var(--bit-clr-pri)"" })"" />

    <BitAiAttachmentChip Attachment=""photo""
                         Classes=""@(new() { Tag = new() { Text = ""custom-text"" }, Thumbnail = ""custom-thumbnail"" })"" />

    <div style=""@cssVariablesStyle"">
        <BitAiAttachmentChip Attachment=""longName"" />
    </div>
</div>";
    private readonly string example6CsharpCode = @"
private const string PhotoUrl = ""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"";

private const string cssVariablesStyle = ""--bit-AiAttachmentChip-max-width:12rem;"" +
                                         ""--bit-AiAttachmentChip-thumbnail-size:2.5rem;"" +
                                         ""--bit-AiAttachmentChip-thumbnail-radius:50%;"" +
                                         ""--bit-AiAttachmentChip-thumbnail-background:var(--bit-clr-pri);"" +
                                         ""--bit-AiAttachmentChip-thumbnail-color:var(--bit-clr-pri-text);"" +
                                         ""--bit-Tag-radius:2rem;"";

private readonly BitAiAttachment report = new(""att-report"", ""quarterly-report.pdf"")
{
    ContentType = ""application/pdf"",
    Size = 482_133,
    Kind = BitAiAttachmentKind.Document,
};

private readonly BitAiAttachment longName = new(""att-long"", ""minutes-of-the-planning-meeting-with-the-whole-team.docx"")
{
    Size = 23_552,
    Kind = BitAiAttachmentKind.Document,
};

private readonly BitAiAttachment photo = new(""att-photo"", ""aurora.jpg"")
{
    ContentType = ""image/jpeg"",
    Size = 1_843_200,
    Kind = BitAiAttachmentKind.Image,
    ThumbnailUrl = PhotoUrl,
};";

    private readonly string example7RazorCode = @"
<BitAiAttachmentChip Dir=""BitDir.Rtl"" Attachment=""persianReport"" OnRemove=""RemoveRtl"" />

<div dir=""rtl"">
    <BitAiAttachmentChip Attachment=""persianPhoto"" OnRemove=""RemoveRtl"" />
</div>

<div>Removed: <b>@rtlRemoved</b></div>";
    private readonly string example7CsharpCode = @"
private const string PhotoUrl = ""_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg"";

private readonly BitAiAttachment persianReport = new(""att-fa-report"", ""گزارش-فصلی.pdf"")
{
    Size = 482_133,
    Kind = BitAiAttachmentKind.Document,
};

private readonly BitAiAttachment persianPhoto = new(""att-fa-photo"", ""شفق-قطبی.jpg"")
{
    Size = 1_843_200,
    Kind = BitAiAttachmentKind.Image,
    ThumbnailUrl = PhotoUrl,
};

private string rtlRemoved = ""nothing yet"";

private void RemoveRtl(BitAiAttachment attachment)
{
    rtlRemoved = attachment.Name;
}";
}
