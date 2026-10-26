namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.AttachmentChip;

public partial class BitAiAttachmentChipDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Attachment",
            Type = "BitAiAttachment",
            DefaultValue = "",
            Description = "The attachment to render. Required.",
            LinkType = LinkType.Link,
            Href = "#attachment",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiAttachmentChipClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the attachment chip.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "OnRemove",
            Type = "EventCallback<BitAiAttachment>",
            DefaultValue = "",
            Description = "Raised with the attachment when the user removes it. The remove button - the dismiss button of the tag, named Remove and the file name - is there only while this is set.",
            LinkType = LinkType.Link,
            Href = "#attachment",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiAttachmentChipClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the attachment chip.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiAttachmentChip-max-width", DefaultValue = "spacing(32)", Description = "Widest the chip gets before its name is cut short." },
        new() { Name = "--bit-AiAttachmentChip-thumbnail-size", DefaultValue = "spacing(4.25)", Description = "Width and height of the thumbnail of an image, and of the box holding the glyph of any other file." },
        new() { Name = "--bit-AiAttachmentChip-thumbnail-radius", DefaultValue = "var(--bit-shp-radius-sm)", Description = "Corner of the thumbnail." },
        new() { Name = "--bit-AiAttachmentChip-thumbnail-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Fill behind the glyph, or behind a picture that does not cover its box." },
        new() { Name = "--bit-AiAttachmentChip-thumbnail-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the glyph shown in place of a picture." },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Gives the keyboard focus to the remove button of the chip - what a row of chips hands the focus on with after removing one, since the button that was pressed goes with its chip. A chip without OnRemove has no button and nothing to focus.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiAttachmentChipClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiAttachmentChip."
                },
                new()
                {
                    Name = "Tag",
                    Type = "BitTagClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTag of the BitAiAttachmentChip that carries the name, the details and the remove button.",
                    LinkType = LinkType.Link,
                    Href = "/components/tag#tag-class-styles"
                },
                new()
                {
                    Name = "Thumbnail",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the thumbnail of the BitAiAttachmentChip: the picture of an image, or the box holding the glyph of any other kind of file."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the glyph of the BitAiAttachmentChip shown in place of a picture."
                },
            ]
        },
        new()
        {
            Id = "attachment",
            Title = "BitAiAttachment",
            Description = "A file carried by a message or staged in the composer: new BitAiAttachment(attachmentId, name) { ... }. It holds no byte array of its own, so Blazor Server never keeps a multi-megabyte upload in circuit memory: its content is referenced through Url, Data or ContentFactory and only read when something needs it.",
            Parameters =
            [
                new()
                {
                    Name = "AttachmentId",
                    Type = "string",
                    DefaultValue = "",
                    Description = "An identifier unique within its message or composer, a good @key. Required; it cannot be empty."
                },
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The file name shown to the user."
                },
                new()
                {
                    Name = "ContentType",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The MIME type, for example image/png."
                },
                new()
                {
                    Name = "Size",
                    Type = "long?",
                    DefaultValue = "null",
                    Description = "The size in bytes, when known. The chip shows it, formatted (FormatBytes of the strings in force), once the file is ready."
                },
                new()
                {
                    Name = "Kind",
                    Type = "BitAiAttachmentKind",
                    DefaultValue = "BitAiAttachmentKind.File",
                    Description = "The category that decides the preview: a thumbnail for an image, the glyph of its kind otherwise. InferKind works it out of a MIME type and a file name.",
                    LinkType = LinkType.Link,
                    Href = "#attachment-kind-enum"
                },
                new()
                {
                    Name = "Status",
                    Type = "BitAiAttachmentStatus",
                    DefaultValue = "BitAiAttachmentStatus.Ready",
                    Description = "Where the upload of the file is.",
                    LinkType = LinkType.Link,
                    Href = "#attachment-status-enum"
                },
                new()
                {
                    Name = "Progress",
                    Type = "double?",
                    DefaultValue = "null",
                    Description = "The progress of the upload, from 0 to 1, while the Status is Uploading."
                },
                new()
                {
                    Name = "Error",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Why the upload failed, when the Status is Failed. When null, the UploadFailed text of the strings in force is shown."
                },
                new()
                {
                    Name = "Url",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "An addressable URL for the content, which may be a data URL for a small inline file. The chip shows it for an image without a ThumbnailUrl."
                },
                new()
                {
                    Name = "ThumbnailUrl",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A URL for a small preview image, distinct from the full-size content, which the chip prefers for an image."
                },
                new()
                {
                    Name = "Data",
                    Type = "ReadOnlyMemory<byte>?",
                    DefaultValue = "null",
                    Description = "Inline bytes, for small attachments that are cheap to keep in memory."
                },
                new()
                {
                    Name = "ContentFactory",
                    Type = "Func<CancellationToken, ValueTask<Stream>>?",
                    DefaultValue = "null",
                    Description = "Opens the content on demand. Preferred over Data for anything large; never serialized."
                },
                new()
                {
                    Name = "Metadata",
                    Type = "IReadOnlyDictionary<string, object?>?",
                    DefaultValue = "null",
                    Description = "App-defined values carried through to custom attachment renderers."
                },
                new()
                {
                    Name = "HasInlineData",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the content can be read without a round trip (Data is set)."
                },
                new()
                {
                    Name = "OpenReadAsync(CancellationToken cancellationToken)",
                    Type = "ValueTask<Stream>",
                    DefaultValue = "",
                    Description = "Opens the content from whichever source there is: the ContentFactory, else the Data. Throws an InvalidOperationException for an attachment carrying neither."
                },
                new()
                {
                    Name = "InferKind(string? contentType, string? fileName)",
                    Type = "static BitAiAttachmentKind",
                    DefaultValue = "",
                    Description = "Infers the kind from a MIME type (image/*, audio/*, video/*, application/pdf), or from the extension of the file name when the type is missing or generic.",
                    LinkType = LinkType.Link,
                    Href = "#attachment-kind-enum"
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "attachment-kind-enum",
            Name = "BitAiAttachmentKind",
            Description = "The broad category of an attachment, which decides its default preview.",
            Items =
            [
                new() { Name = "File", Description = "Anything without a more specific category.", Value = "0" },
                new() { Name = "Image", Description = "A raster or vector image, previewed as a thumbnail.", Value = "1" },
                new() { Name = "Audio", Description = "An audio clip, previewed with a player.", Value = "2" },
                new() { Name = "Video", Description = "A video clip, previewed with a player.", Value = "3" },
                new() { Name = "Document", Description = "A PDF or other paginated document.", Value = "4" },
            ]
        },
        new()
        {
            Id = "attachment-status-enum",
            Name = "BitAiAttachmentStatus",
            Description = "Where the upload of an attachment is, so the chip can show its progress or its failure.",
            Items =
            [
                new() { Name = "Pending", Description = "Selected locally and not yet uploaded.", Value = "0" },
                new() { Name = "Uploading", Description = "Upload in progress; see Progress.", Value = "1" },
                new() { Name = "Ready", Description = "Available to the model.", Value = "2" },
                new() { Name = "Failed", Description = "The upload failed; see Error.", Value = "3" },
            ]
        },
    ];



    private const string PhotoUrl = "_content/Bit.BlazorUI.Demo.Client.Core/images/carousel/img1.jpg";

    private const string cssVariablesStyle = "--bit-AiAttachmentChip-max-width:12rem;" +
                                             "--bit-AiAttachmentChip-thumbnail-size:2.5rem;" +
                                             "--bit-AiAttachmentChip-thumbnail-radius:50%;" +
                                             "--bit-AiAttachmentChip-thumbnail-background:var(--bit-clr-pri);" +
                                             "--bit-AiAttachmentChip-thumbnail-color:var(--bit-clr-pri-text);" +
                                             "--bit-Tag-radius:2rem;";

    private readonly BitAiAttachment report = new("att-report", "quarterly-report.pdf")
    {
        ContentType = "application/pdf",
        Size = 482_133,
        Kind = BitAiAttachmentKind.Document,
    };

    private readonly BitAiAttachment longName = new("att-long", "minutes-of-the-planning-meeting-with-the-whole-team.docx")
    {
        Size = 23_552,
        Kind = BitAiAttachmentKind.Document,
    };

    private readonly BitAiAttachment photo = new("att-photo", "aurora.jpg")
    {
        ContentType = "image/jpeg",
        Size = 1_843_200,
        Kind = BitAiAttachmentKind.Image,
        ThumbnailUrl = PhotoUrl,
    };

    private readonly IReadOnlyList<BitAiAttachment> kinds =
    [
        new("att-kind-image", "aurora.jpg") { Size = 1_843_200, Kind = BitAiAttachment.InferKind("image/jpeg"), ThumbnailUrl = PhotoUrl },
        new("att-kind-audio", "voice-note.m4a") { Size = 412_000, Kind = BitAiAttachment.InferKind(null, "voice-note.m4a") },
        new("att-kind-video", "screen-recording.webm") { Size = 9_830_400, Kind = BitAiAttachment.InferKind("video/webm") },
        new("att-kind-document", "contract.pdf") { Size = 220_160, Kind = BitAiAttachment.InferKind("application/pdf") },
        new("att-kind-file", "data.csv") { Size = 5_120, Kind = BitAiAttachment.InferKind("text/csv", "data.csv") },
    ];

    private bool isUploading;
    private BitAiAttachment upload = new("att-upload", "site-survey.zip") { Size = 3_145_728, Status = BitAiAttachmentStatus.Pending };
    private readonly BitAiAttachment failed = new("att-failed", "backup.tar")
    {
        Size = 734_003_200,
        Status = BitAiAttachmentStatus.Failed,
        Error = "Over the 100 MB limit of the server",
    };

    private List<BitAiAttachment> staged = CreateStaged();
    private readonly Dictionary<string, BitAiAttachmentChip> stagedChips = [];
    private BitButton resetButton = default!;

    private readonly BitAiAttachment persianReport = new("att-fa-report", "گزارش-فصلی.pdf")
    {
        Size = 482_133,
        Kind = BitAiAttachmentKind.Document,
    };

    private readonly BitAiAttachment persianPhoto = new("att-fa-photo", "شفق-قطبی.jpg")
    {
        Size = 1_843_200,
        Kind = BitAiAttachmentKind.Image,
        ThumbnailUrl = PhotoUrl,
    };

    private string rtlRemoved = "nothing yet";

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
    }

    private static List<BitAiAttachment> CreateStaged() =>
    [
        new("att-staged-1", "aurora.jpg") { Size = 1_843_200, Kind = BitAiAttachmentKind.Image, ThumbnailUrl = PhotoUrl },
        new("att-staged-2", "notes.txt") { Size = 1_024 },
        new("att-staged-3", "budget.xlsx") { Size = 48_640 },
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
    }

    // Never called: the remove button of a disabled chip cannot be pressed.
    private void RemoveDisabled(BitAiAttachment attachment) { }

    private void RemoveRtl(BitAiAttachment attachment)
    {
        rtlRemoved = attachment.Name;
    }
}
