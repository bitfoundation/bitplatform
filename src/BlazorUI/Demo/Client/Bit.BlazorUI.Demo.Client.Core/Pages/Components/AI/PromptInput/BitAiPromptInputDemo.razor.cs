using Bit.BlazorUI.Demo.Client.Core.Services.AI;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.PromptInput;

public partial class BitAiPromptInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accept",
            Type = "string?",
            DefaultValue = "null",
            Description = "The file types to accept, in the syntax of the HTML accept attribute: extensions such as .pdf, MIME types such as image/png, or wildcards such as image/*. Applied to the file picker and also checked for pasted and dropped files, which the browser does not filter. When null, the types follow the capabilities of the Model (or Capabilities) when one is given - image/* for Vision, audio/* for Audio, application/pdf, .pdf and text/* for Documents - and every type is accepted otherwise.",
        },
        new()
        {
            Name = "AllowAttachments",
            Type = "bool",
            DefaultValue = "true",
            Description = "Offers the attach button and accepts dropped or pasted files. A Model (or Capabilities) declaring none of Vision, Documents and Audio hides them whatever this says.",
        },
        new()
        {
            Name = "AllowDictation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Offers the dictation button (a BitAiVoiceInput) where the browser has speech recognition; it renders nothing where it has none. A Model (or Capabilities) that does not declare Audio hides it. Each finished phrase is appended to the text (cut at MaxLength), and the provisional one is shown under the text meanwhile.",
        },
        new()
        {
            Name = "Capabilities",
            Type = "BitAiModelCapabilities?",
            DefaultValue = "null",
            Description = "The capabilities to assume when there is no Model record, or to use in place of the capabilities of the one there is. They decide whether attachments and dictation are offered and which file types the picker accepts.",
            LinkType = LinkType.Link,
            Href = "/components/ai/modelselector#model-capabilities-enum",
        },
        new()
        {
            Name = "Classes",
            Type = "BitAiPromptInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the prompt input.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Commands",
            Type = "IReadOnlyList<BitAiSlashCommand>?",
            DefaultValue = "null",
            Description = "The commands offered when the user types a forward slash at the start of the text, filtered by what is typed after it. Choosing one inserts its Template, or runs its Handler instead.",
            LinkType = LinkType.Link,
            Href = "#slash-command",
        },
        new()
        {
            Name = "DictationLanguage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The dictation language, as a BCP 47 language tag such as en-US. When null, the language of the browser is used.",
        },
        new()
        {
            Name = "DraftKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "A key under which the draft text is kept in the session storage of the browser, so it survives a reload or a dropped circuit. It is restored when the composer first renders with an empty value, saved on every change and cleared on submit. Use a key that identifies the conversation, so drafts do not bleed between them. Drafts are not kept when it is null.",
        },
        new()
        {
            Name = "Generating",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Whether a turn is in flight, which turns the send button into a stop button and lets Escape stop the turn. When null, the state of the Session is used.",
        },
        new()
        {
            Name = "HintTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A hint rendered under the composer, beside the character counter of a MaxLength.",
        },
        new()
        {
            Name = "MaxAttachmentSize",
            Type = "long",
            DefaultValue = "10 * 1024 * 1024",
            Description = "The largest file the composer stages, in bytes (10 MB by default, DefaultMaxAttachmentSize). Checked in the browser before a pasted or dropped file is read, so an oversized file costs nothing to turn away. The files within it are streamed rather than sent in one message, so it is not tied to the SignalR message size of Blazor Server.",
        },
        new()
        {
            Name = "MaxAttachments",
            Type = "int",
            DefaultValue = "20",
            Description = "The most files that can be staged at once (DefaultMaxAttachments). A file past it is turned away as TooMany.",
        },
        new()
        {
            Name = "MaxInlineSize",
            Type = "long",
            DefaultValue = "256 * 1024",
            Description = "The largest file whose bytes are held in memory, in bytes (256 KB by default, DefaultMaxInlineSize). A file within it is buffered and carries BitAiAttachment.Data and a data URL; a larger one is kept in the browser and read through BitAiAttachment.ContentFactory when something opens it, so a circuit never holds megabytes per attachment. Image thumbnails are object URLs made in the browser either way.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int?",
            DefaultValue = "null",
            Description = "The most characters the text may hold. Enforced by the textarea and shown as a counter under the composer, which turns to the warning color as the limit gets close and is then announced politely. A text past it (set through Value) cannot be sent. When null, the text is unlimited.",
        },
        new()
        {
            Name = "Mentions",
            Type = "IReadOnlyList<BitAiMentionItem>?",
            DefaultValue = "null",
            Description = "The items offered when the user types an at sign, filtered by what is typed after it. Choosing one inserts @ and its label.",
            LinkType = LinkType.Link,
            Href = "#mention-item",
        },
        new()
        {
            Name = "Model",
            Type = "BitAiModel?",
            DefaultValue = "null",
            Description = "The model the prompt is for. Its capabilities (unless Capabilities is set) decide whether attachments and dictation are offered and which file types the picker accepts.",
            LinkType = LinkType.Link,
            Href = "/components/ai/modelselector#model",
        },
        new()
        {
            Name = "OnAttachmentRejected",
            Type = "EventCallback<BitAiAttachmentRejection>",
            DefaultValue = "",
            Description = "Raised for each file the composer turns away: too large, too many, not an accepted type, or unreadable. The composer already tells the user with an alert above the text (the last three files it turned away); handle this to log the rejection or to offer another route, such as uploading the file to the app's own storage.",
            LinkType = LinkType.Link,
            Href = "#attachment-rejection",
        },
        new()
        {
            Name = "OnStop",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Raised when the user asks to stop the current turn: the stop button, or Escape in the text. The Session, when there is one, is stopped first.",
        },
        new()
        {
            Name = "OnSubmit",
            Type = "EventCallback<BitAiPromptSubmission>",
            DefaultValue = "",
            Description = "Raised when the user submits, with the trimmed text and the staged attachments; the composer then empties itself. When it is not set, the submission goes to the Session.",
            LinkType = LinkType.Link,
            Href = "#prompt-submission",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder of the text, which also names it for assistive technologies unless an AriaLabel is given. When null, the PromptPlaceholder of the strings in force is used (\"Send a message...\").",
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content placed before the text, such as a model picker.",
        },
        new()
        {
            Name = "Session",
            Type = "BitAiChatSession?",
            DefaultValue = "null",
            Description = "The session, which supplies the generation state and receives the submissions while OnSubmit is not set. Stopping a turn stops it.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitAiPromptInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the prompt input.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content placed between the tools (the attach and dictation buttons) and the send button.",
        },
        new()
        {
            Name = "Value",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The text of the composer. Use @bind-Value to keep it in step.",
        },
        new()
        {
            Name = "ValueChanged",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Raised whenever the text changes - typed, dictated, inserted from the menu, restored from a draft or emptied on submit - so the value can be bound.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Attachments",
            Type = "IReadOnlyList<BitAiAttachment>",
            DefaultValue = "[]",
            Description = "The attachments currently staged in the composer.",
            LinkType = LinkType.Link,
            Href = "/components/ai/attachmentchip#attachment",
        },
        new()
        {
            Name = "EffectiveCapabilities",
            Type = "BitAiModelCapabilities?",
            DefaultValue = "null",
            Description = "The capabilities in force: Capabilities, else those of Model, else null (everything offered).",
            LinkType = LinkType.Link,
            Href = "/components/ai/modelselector#model-capabilities-enum",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Puts the caret in the text of the composer.",
        },
        new()
        {
            Name = "DefaultMaxAttachmentSize",
            Type = "const long",
            DefaultValue = "10 * 1024 * 1024",
            Description = "The default of MaxAttachmentSize: 10 MB.",
        },
        new()
        {
            Name = "DefaultMaxAttachments",
            Type = "const int",
            DefaultValue = "20",
            Description = "The default of MaxAttachments.",
        },
        new()
        {
            Name = "DefaultMaxInlineSize",
            Type = "const long",
            DefaultValue = "256 * 1024",
            Description = "The default of MaxInlineSize: 256 KB.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-AiPromptInput-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Fill of the frame. A disabled composer is filled with --bit-AiPromptInput-disabled-background instead." },
        new() { Name = "--bit-AiPromptInput-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Color of the frame. While files are dragged over the composer it is --bit-AiPromptInput-drop-color, and a disabled one takes the disabled border color of the theme." },
        new() { Name = "--bit-AiPromptInput-border-width", DefaultValue = "var(--bit-shp-brd-width)", Description = "Thickness of the frame." },
        new() { Name = "--bit-AiPromptInput-radius", DefaultValue = "var(--bit-shp-radius-control)", Description = "Corner of the frame, which the drop overlay follows." },
        new() { Name = "--bit-AiPromptInput-shadow", DefaultValue = "none", Description = "Elevation of the frame, kept under the focus ring while the focus is in the composer." },
        new() { Name = "--bit-AiPromptInput-padding", DefaultValue = "spacing(1)", Description = "Inset of the frame." },
        new() { Name = "--bit-AiPromptInput-gap", DefaultValue = "spacing(1)", Description = "Room between the rows inside the frame: the staged files, the alert, the text, the provisional dictation and the footer." },
        new() { Name = "--bit-AiPromptInput-focus-color", DefaultValue = "the global focus ring", Description = "Color of the ring drawn around the frame while the focus is anywhere in the composer (the text has no frame of its own)." },
        new() { Name = "--bit-AiPromptInput-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Color of the typed text and of the options of the menu. A disabled composer writes it in --bit-AiPromptInput-disabled-color instead." },
        new() { Name = "--bit-AiPromptInput-placeholder-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the placeholder." },
        new() { Name = "--bit-AiPromptInput-font-size", DefaultValue = "var(--bit-tpg-fs-md)", Description = "Size of the typed text." },
        new() { Name = "--bit-AiPromptInput-line-height", DefaultValue = "1.5", Description = "Leading of the typed text, the unit the growing text counts its rows in (up to the MaxComposerRows of BitAiUiOptions, 12 by default)." },
        new() { Name = "--bit-AiPromptInput-drop-color", DefaultValue = "var(--bit-clr-pri)", Description = "Color of the dashed frame and of the text of the overlay while files are dragged over the composer." },
        new() { Name = "--bit-AiPromptInput-drop-background", DefaultValue = "var(--bit-clr-bg-pri) at 86%", Description = "Fill of the overlay shown while files are dragged over the composer." },
        new() { Name = "--bit-AiPromptInput-menu-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Fill of the menu of commands and mentions." },
        new() { Name = "--bit-AiPromptInput-menu-border-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Color of the rule around the menu." },
        new() { Name = "--bit-AiPromptInput-menu-shadow", DefaultValue = "var(--bit-shd-popup)", Description = "Elevation of the menu." },
        new() { Name = "--bit-AiPromptInput-menu-max-height", DefaultValue = "var(--bit-siz-popup-max-height)", Description = "Height past which the menu scrolls." },
        new() { Name = "--bit-AiPromptInput-item-hover-background", DefaultValue = "var(--bit-clr-bg-pri-hover)", Description = "Fill of the option under the pointer, and of the one the arrow keys have reached." },
        new() { Name = "--bit-AiPromptInput-hint-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Color of the hint, the character counter, the descriptions of the options and the provisional dictation." },
        new() { Name = "--bit-AiPromptInput-near-limit-color", DefaultValue = "var(--bit-clr-wrn)", Description = "Color of the counter of a MaxLength once the text is close to it." },
        new() { Name = "--bit-AiPromptInput-over-limit-color", DefaultValue = "var(--bit-clr-err)", Description = "Color of the counter of a MaxLength once the text is past it." },
        new() { Name = "--bit-AiPromptInput-disabled-color", DefaultValue = "var(--bit-clr-fg-dis)", Description = "Color of the text and of the placeholder of a composer whose Disabled is true." },
        new() { Name = "--bit-AiPromptInput-disabled-background", DefaultValue = "var(--bit-clr-bg-dis)", Description = "Fill of the frame of a composer whose Disabled is true." },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitAiPromptInputClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAiPromptInput, the frame of the composer."
                },
                new()
                {
                    Name = "DropOverlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitAiPromptInput shown while files are dragged over it."
                },
                new()
                {
                    Name = "Attachments",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the list of the staged attachments of the BitAiPromptInput."
                },
                new()
                {
                    Name = "AttachmentChip",
                    Type = "BitAiAttachmentChipClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each BitAiAttachmentChip of the staged attachments of the BitAiPromptInput.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/attachmentchip#class-styles"
                },
                new()
                {
                    Name = "Notice",
                    Type = "BitMessageClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitMessage of the BitAiPromptInput that names the files it turned away.",
                    LinkType = LinkType.Link,
                    Href = "/components/message#message-class-styles"
                },
                new()
                {
                    Name = "Menu",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the menu of the slash commands and the mentions of the BitAiPromptInput."
                },
                new()
                {
                    Name = "MenuItem",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each option of the menu of the BitAiPromptInput."
                },
                new()
                {
                    Name = "MenuItemIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of each option of the menu of the BitAiPromptInput."
                },
                new()
                {
                    Name = "MenuItemLabel",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of each option of the menu of the BitAiPromptInput."
                },
                new()
                {
                    Name = "MenuItemHint",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the description of each option of the menu of the BitAiPromptInput."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the row of the BitAiPromptInput that holds the prefix, the text and the tools."
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the PrefixTemplate of the BitAiPromptInput."
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the textarea of the BitAiPromptInput."
                },
                new()
                {
                    Name = "Tools",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the tools of the BitAiPromptInput: the attach button, the dictation, the SuffixTemplate and the send button."
                },
                new()
                {
                    Name = "AttachButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitButton of the BitAiPromptInput that opens the file picker.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "VoiceInput",
                    Type = "BitAiVoiceInputClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitAiVoiceInput of the BitAiPromptInput.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/voiceinput#class-styles"
                },
                new()
                {
                    Name = "SendButton",
                    Type = "BitButtonClassStyles?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitButton of the BitAiPromptInput that sends the prompt, or stops the turn in flight.",
                    LinkType = LinkType.Link,
                    Href = "/components/button#button-class-styles"
                },
                new()
                {
                    Name = "Interim",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the provisional text of the dictation of the BitAiPromptInput."
                },
                new()
                {
                    Name = "Footer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer of the BitAiPromptInput that holds the hint and the character counter."
                },
                new()
                {
                    Name = "Hint",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hint of the BitAiPromptInput."
                },
                new()
                {
                    Name = "Counter",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the character counter of the BitAiPromptInput."
                },
                new()
                {
                    Name = "LiveRegion",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the visually hidden live region of the BitAiPromptInput that announces the characters left once the limit is close."
                },
            ]
        },
        new()
        {
            Id = "slash-command",
            Title = "BitAiSlashCommand",
            Description = "A command offered when the user types a forward slash at the start of the text: new BitAiSlashCommand(name, description, icon) { Template = ... } or { Handler = ... }. A Template suits a prompt shortcut, a Handler an app action - clearing the conversation, regenerating the answer - that never reaches the model.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The command word, without the slash. It is what the text typed after the slash is matched against (case-insensitively, anywhere in it), and the menu shows it as /Name."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "What it does, shown beside the name."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The name of a Fluent UI icon of the built-in set (BitIconName), shown before the name.",
                    LinkType = LinkType.Link,
                    Href = "/iconography"
                },
                new()
                {
                    Name = "Template",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Text that replaces the slash and what was typed after it when the command is chosen; the caret lands at its end. When null (and there is no Handler), \"/Name \" is inserted."
                },
                new()
                {
                    Name = "Handler",
                    Type = "Func<Task>?",
                    DefaultValue = "null",
                    Description = "Invoked when the command is chosen, instead of inserting text: the slash and what was typed after it are removed from the text first. It runs inside the composer's own event, so a handler that changes the state of the page renders the page itself (StateHasChanged)."
                },
            ]
        },
        new()
        {
            Id = "mention-item",
            Title = "BitAiMentionItem",
            Description = "Something the user can reference by typing an at sign - a file, a person, a data source: new BitAiMentionItem(id, label, description, icon). The composer inserts the label; resolving it to content is the app's job, done by Id rather than by asking the model to guess what was meant.",
            Parameters =
            [
                new()
                {
                    Name = "Id",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The stable identifier the app resolves the mention by later."
                },
                new()
                {
                    Name = "Label",
                    Type = "string",
                    DefaultValue = "",
                    Description = "What the user sees, what the text typed after the at sign is matched against, and what is inserted (\"@Label \")."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Extra detail shown beside the label."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The name of a Fluent UI icon of the built-in set (BitIconName), shown before the label.",
                    LinkType = LinkType.Link,
                    Href = "/iconography"
                },
            ]
        },
        new()
        {
            Id = "prompt-submission",
            Title = "BitAiPromptSubmission",
            Description = "What the composer hands OnSubmit when the user submits. The submission owns the attachments from then on: the composer no longer releases their browser streams or previews when it is discarded.",
            Parameters =
            [
                new()
                {
                    Name = "Text",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The trimmed text. It may be empty when attachments carry the request."
                },
                new()
                {
                    Name = "Attachments",
                    Type = "IReadOnlyList<BitAiAttachment>",
                    DefaultValue = "",
                    Description = "The files staged alongside it.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/attachmentchip#attachment"
                },
            ]
        },
        new()
        {
            Id = "attachment-rejection",
            Title = "BitAiAttachmentRejection",
            Description = "A file the composer did not stage, and why - the same thing the alert under the composer tells the user, for the app to log or to offer another route for, such as uploading the file to its own storage.",
            Parameters =
            [
                new()
                {
                    Name = "FileName",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The file name. It is empty when the picker handed over more files than could be read at all."
                },
                new()
                {
                    Name = "ContentType",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The MIME type the browser reported, which may be empty."
                },
                new()
                {
                    Name = "Size",
                    Type = "long",
                    DefaultValue = "",
                    Description = "The size in bytes."
                },
                new()
                {
                    Name = "Reason",
                    Type = "BitAiAttachmentRejectionReason",
                    DefaultValue = "",
                    Description = "Why the file was turned away.",
                    LinkType = LinkType.Link,
                    Href = "#attachment-rejection-reason-enum"
                },
            ]
        },
        new()
        {
            Id = "composer-file",
            Title = "BitAiComposerFile",
            Description = "A file the browser hands the composer from a paste or a drop: described first, its bytes streamed only when they are read, since one interop message carrying a whole file would exceed the SignalR message limit of Blazor Server (32 KB by default). The composer makes BitAiAttachments of these itself; an app meets the type only where it writes a composer of its own over the same browser helpers.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The file name."
                },
                new()
                {
                    Name = "ContentType",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The MIME type."
                },
                new()
                {
                    Name = "Size",
                    Type = "long",
                    DefaultValue = "",
                    Description = "The size in bytes."
                },
                new()
                {
                    Name = "Data",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The content, base64 encoded, when it was sent inline. The browser helpers send Content instead."
                },
                new()
                {
                    Name = "Content",
                    Type = "IJSStreamReference?",
                    DefaultValue = "null",
                    Description = "The content as a browser stream, or null when it was sent inline in Data or withheld because the file was over the size limit."
                },
                new()
                {
                    Name = "PreviewUrl",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A short object URL (URL.createObjectURL) for the preview of an image, or null for any other file. It becomes the ThumbnailUrl of the attachment: the bytes stay in the browser, only the URL crosses the circuit."
                },
                new()
                {
                    Name = "ToAttachment()",
                    Type = "BitAiAttachment",
                    DefaultValue = "",
                    Description = "Converts a file sent inline (Data) into an attachment with its content as a data URL. Throws an InvalidOperationException for a file that was not sent inline.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/attachmentchip#attachment"
                },
                new()
                {
                    Name = "ToAttachmentAsync(long maxAllowedSize, long maxInlineSize, CancellationToken cancellationToken)",
                    Type = "Task<BitAiAttachment>",
                    DefaultValue = "",
                    Description = "Turns the file into an attachment: buffered, with the content as a data URL, when it is no larger than maxInlineSize (everything by default), and otherwise read on demand through BitAiAttachment.ContentFactory. A longer stream than maxAllowedSize fails the read. A buffered file releases the browser stream; a lazy one keeps it, and the caller releases it when the attachment is discarded.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/attachmentchip#attachment"
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "attachment-rejection-reason-enum",
            Name = "BitAiAttachmentRejectionReason",
            Description = "Why the BitAiPromptInput turned a file away.",
            Items =
            [
                new() { Name = "TooLarge", Description = "The file is larger than MaxAttachmentSize.", Value = "0" },
                new() { Name = "TypeNotAllowed", Description = "The file does not match Accept (or the types the capabilities of the model take).", Value = "1" },
                new() { Name = "ReadFailed", Description = "The browser could not deliver the content, or the transfer was interrupted.", Value = "2" },
                new() { Name = "TooMany", Description = "Staging the file would exceed MaxAttachments.", Value = "3" },
            ]
        },
    ];



    private const long MaxSize = 1024 * 1024;

    private const string cssVariablesStyle = "--bit-AiPromptInput-radius:1.5rem;" +
                                             "--bit-AiPromptInput-padding:0.5rem 0.75rem;" +
                                             "--bit-AiPromptInput-border-color:transparent;" +
                                             "--bit-AiPromptInput-background:var(--bit-clr-bg-sec);" +
                                             "--bit-AiPromptInput-shadow:0 0.25rem 1rem rgb(0 0 0 / 0.12);";

    [AutoInject] private IBitAiModelCatalog modelCatalog { get; set; } = default!;

    private string basicSubmission = "nothing yet";

    private BitAiPromptInput? valueComposer;
    private string boundValue = string.Empty;
    private string valueSubmission = "nothing yet";

    // Any IBitAiTransport will do; this one streams a long Markdown answer, slow enough to stop halfway.
    // A session made by the BitAiChatSessionFactory service picks up the registered transport and tools instead.
    private readonly BitAiChatSession session = new(AiDemoTransports.Markdown());

    private bool isGenerating;
    private CancellationTokenSource? generationCts;
    private string generationStatus = "Send something to start a turn of your own; it takes five seconds unless you stop it.";

    private string attachmentsSubmission = "nothing yet";
    private IReadOnlyList<BitAiAttachment> sentAttachments = [];

    private string limitsSubmission = "nothing yet";
    private readonly List<BitAiAttachmentRejection> rejections = [];

    private string dictationSubmission = "nothing yet";

    private BitAiModel? FindModel(string? modelId) => modelId is null ? null : modelCatalog.FindModel(modelId);

    private string? capabilityModelId = "demo-model-1";
    private string capabilitiesSubmission = "nothing yet";

    private IReadOnlyList<BitAiSlashCommand> commands = [];
    private readonly List<string> sentPrompts = [];
    private readonly IReadOnlyList<BitAiMentionItem> mentions =
    [
        new("file-roadmap", "roadmap.md", "Product roadmap, updated Monday", BitIconName.Page),
        new("file-pricing", "pricing.xlsx", "Pricing model, three tiers", BitIconName.Page),
        new("user-priya", "priya", "Priya Raman, product lead", BitIconName.Contact),
        new("user-tomas", "tomas", "Tomás Silva, engineering", BitIconName.Contact),
    ];

    private string draftSubmission = "nothing yet";

    private string templatesValue = string.Empty;
    private string? templatesModelId = "demo-model-1";
    private string templatesSubmission = "nothing yet";

    private string cascadeSubmission = "nothing yet";
    private BitAiPromptInputParams[] promptInputParams =>
    [
        new()
        {
            Placeholder = "Ask the team assistant...",
            Accept = "image/*",
            MaxAttachments = 2,
            MaxLength = 200,
        }
    ];

    private string styleSubmission = "nothing yet";

    private string rtlSubmission = "nothing yet";

    protected override void OnInitialized()
    {
        commands =
        [
            new("summarize", "Summarize the conversation so far", BitIconName.BulletedList)
            {
                Template = "Summarize the conversation so far in three bullet points.",
            },
            new("explain", "Explain something for a newcomer", BitIconName.Lightbulb)
            {
                Template = "Explain this as if I were new to the codebase: ",
            },
            new("translate", "Translate the next text", BitIconName.Translate)
            {
                Template = "Translate into French: ",
            },
            new("clear", "Clear the list of sent prompts", BitIconName.Delete)
            {
                Handler = () =>
                {
                    sentPrompts.Clear();
                    StateHasChanged();
                    return Task.CompletedTask;
                },
            },
        ];

        base.OnInitialized();
    }

    private void HandleBasicSubmit(BitAiPromptSubmission submission)
    {
        basicSubmission = submission.Text;
    }

    private void HandleValueSubmit(BitAiPromptSubmission submission)
    {
        valueSubmission = submission.Text;
    }

    private void FillSuggestion()
    {
        boundValue = "Which of my invoices are still unpaid?";
    }

    private async Task FocusComposerAsync()
    {
        if (valueComposer is null) return;

        await valueComposer.FocusAsync();
    }

    private async Task StartGenerationAsync(BitAiPromptSubmission submission)
    {
        generationCts?.Dispose();
        generationCts = new();
        isGenerating = true;
        generationStatus = $"Working on \"{submission.Text}\"...";
        StateHasChanged();

        try
        {
            await Task.Delay(5000, generationCts.Token);
            generationStatus = $"Done with \"{submission.Text}\".";
        }
        catch (OperationCanceledException)
        {
            generationStatus = $"Stopped \"{submission.Text}\".";
        }
        finally
        {
            isGenerating = false;
        }
    }

    private void StopGeneration()
    {
        generationCts?.Cancel();
    }

    private void HandleAttachmentsSubmit(BitAiPromptSubmission submission)
    {
        attachmentsSubmission = $"\"{submission.Text}\" with {submission.Attachments.Count} file(s)";
        sentAttachments = submission.Attachments;
    }

    private void HandleLimitsSubmit(BitAiPromptSubmission submission)
    {
        limitsSubmission = $"\"{submission.Text}\" with {submission.Attachments.Count} file(s)";
    }

    // The composer already tells the user; this is where an app would log or count it.
    private void HandleRejected(BitAiAttachmentRejection rejection)
    {
        rejections.Insert(0, rejection);
        if (rejections.Count > 5) rejections.RemoveAt(rejections.Count - 1);
    }

    private void HandleDictationSubmit(BitAiPromptSubmission submission)
    {
        dictationSubmission = submission.Text;
    }

    private void HandleCapabilitiesSubmit(BitAiPromptSubmission submission)
    {
        capabilitiesSubmission = $"\"{submission.Text}\" with {submission.Attachments.Count} file(s)";
    }

    private void HandleCommandsSubmit(BitAiPromptSubmission submission)
    {
        sentPrompts.Add(submission.Text);
    }

    private void HandleDraftSubmit(BitAiPromptSubmission submission)
    {
        draftSubmission = submission.Text;
    }

    private void SuggestPrompt()
    {
        templatesValue = "Draft a friendly reminder for an overdue invoice.";
    }

    private void HandleTemplatesSubmit(BitAiPromptSubmission submission)
    {
        templatesSubmission = submission.Text;
    }

    private void HandleCascadeSubmit(BitAiPromptSubmission submission)
    {
        cascadeSubmission = submission.Text;
    }

    private void HandleStyleSubmit(BitAiPromptSubmission submission)
    {
        styleSubmission = submission.Text;
    }

    private void HandleRtlSubmit(BitAiPromptSubmission submission)
    {
        rtlSubmission = submission.Text;
    }

    public async ValueTask DisposeAsync()
    {
        generationCts?.Cancel();
        generationCts?.Dispose();

        await session.DisposeAsync();
    }
}
