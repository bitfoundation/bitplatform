using System.Globalization;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.OptionsProvider;

public partial class BitAiOptionsProviderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content the options are provided to. It is all the provider renders: there is no element of its own around it.",
        },
        new()
        {
            Name = "UiOptions",
            Type = "BitAiUiOptions?",
            DefaultValue = "null",
            Description = "The options to cascade. Left unset, the options of an enclosing provider are cascaded, else the ones registered with AddBitBlazorUIAIServices (ai.ConfigureUi), else BitAiUiOptions.Default. A new instance handed in re-renders the components that read it; the options are immutable, so build one with \"options with { ... }\".",
            LinkType = LinkType.Link,
            Href = "#ui-options",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "ui-options",
            Title = "BitAiUiOptions",
            Description = "The presentation settings every AI component reads, a sealed record: BitAiUiOptions.Default with { ShowUsage = true }. Everything here is a default, not a constraint - a component's own parameters override it. It is set for the whole app with AddBitBlazorUIAIServices(ai => ai.ConfigureUi(options => options with { ... })), for a region with a BitAiOptionsProvider, and for one chat with the UiOptions of a BitAiChat. Density is deliberately not one of the settings: the components are spaced in the units of the bit theme, so a compact chat is a region with a lower --bit-layout-density-scale (BitThemeDensityPresets.CreateCompactOverlay).",
            Parameters =
            [
                new() { Name = "Default", Type = "BitAiUiOptions", DefaultValue = "", Description = "Static: the default settings, the values of this table." },
                new()
                {
                    Name = "Markdown",
                    Type = "BitAiMarkdownOptions",
                    DefaultValue = "BitAiMarkdownOptions.Default",
                    Description = "Which Markdown extensions the Markdown views enable - tables, strikethrough, task lists, auto links, math, footnotes, citation markers, raw HTML and the rest. A parser is built from them, and built again when other options are cascaded.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/markdownview#markdown-options",
                },
                new()
                {
                    Name = "Images",
                    Type = "BitAiImagePolicy",
                    DefaultValue = "BitAiImagePolicy.Allow",
                    Description = "Which images are fetched and shown - the images of Markdown, image parts, image and SVG artifacts, and the favicons of sources (a favicon only when the policy shows it outright, never as a placeholder): allowed by default; a host handling sensitive conversations can restrict them to listed hosts (AllowListedHosts), load them only on request (ClickToLoad) or block them. Script-capable URL schemes are neutralized whatever the policy.",
                    LinkType = LinkType.Link,
                    Href = "/components/ai/markdownview#image-policy",
                },
                new() { Name = "RenderInterval", Type = "TimeSpan", DefaultValue = "TimeSpan.FromMilliseconds(50)", Description = "The minimum time between two renders of a component following a stream; every change in between is folded into the next render. The single most important performance dial: raising it reduces circuit traffic on Blazor Server, lowering it makes WebAssembly marginally smoother at a real CPU cost." },
                new() { Name = "ShowTimestamps", Type = "bool", DefaultValue = "false", Description = "Shows a timestamp under each message." },
                new() { Name = "ShowUsage", Type = "bool", DefaultValue = "false", Description = "Shows the token usage under assistant messages, and the total usage of a run in the header of a BitAiAgentRunView." },
                new() { Name = "ShowModelName", Type = "bool", DefaultValue = "false", Description = "Shows the model name under assistant messages." },
                new() { Name = "ShowMessageActions", Type = "bool", DefaultValue = "true", Description = "Shows the copy, regenerate and feedback actions under assistant messages." },
                new() { Name = "ShowCodeCopy", Type = "bool", DefaultValue = "true", Description = "Shows a copy button on code blocks." },
                new() { Name = "ShowLineNumbers", Type = "bool", DefaultValue = "false", Description = "Shows line numbers in code blocks." },
                new() { Name = "CollapseReasoning", Type = "bool", DefaultValue = "true", Description = "Collapses the reasoning panel until the user opens it." },
                new() { Name = "CollapseToolCalls", Type = "bool", DefaultValue = "true", Description = "Collapses tool call cards until the user opens them." },
                new() { Name = "AutoScroll", Type = "bool", DefaultValue = "true", Description = "Follows the newest content while streaming, until the user scrolls away." },
                new() { Name = "VirtualizeThreshold", Type = "int", DefaultValue = "60", Description = "The number of messages at which the message list switches to virtualized rendering. Below it every message is rendered, which is both simpler and faster; above it, only what fits on screen is." },
                new() { Name = "SmoothStreaming", Type = "bool", DefaultValue = "true", Description = "Evens out bursty text so it renders at a steady rate." },
                new() { Name = "ShowStreamingCaret", Type = "bool", DefaultValue = "true", Description = "Shows the caret that blinks at the end of streaming text." },
                new() { Name = "InlineCitations", Type = "bool", DefaultValue = "true", Description = "Turns the bracketed numbers of the text ([1]) into interactive citation markers; off, they stay text." },
                new() { Name = "ShowSourceList", Type = "bool", DefaultValue = "true", Description = "Shows the source list under a message that cites anything." },
                new() { Name = "SubmitOnEnter", Type = "bool", DefaultValue = "true", Description = "Sends the prompt when Enter is pressed without a modifier." },
                new() { Name = "MaxComposerRows", Type = "int", DefaultValue = "12", Description = "The largest number of rows the prompt textarea grows to before it scrolls." },
                new()
                {
                    Name = "Strings",
                    Type = "BitAiStrings",
                    DefaultValue = "BitAiStrings.Default",
                    Description = "The user-facing text, for localization: a subclass of BitAiStrings that overrides what it translates.",
                    LinkType = LinkType.Link,
                    Href = "#strings",
                },
            ]
        },
        new()
        {
            Id = "strings",
            Title = "BitAiStrings",
            Description = "Every piece of user-facing text the AI components render - labels, accessible names, announcements, validation messages, and the formats of durations, sizes and token counts - kept in one replaceable object rather than scattered through markup, so that localizing the library, or changing its tone, does not mean forking components. Subclass it, override the members to translate (each is virtual), and hand an instance to BitAiUiOptions.Strings; every member left alone keeps its English. The default values below are the English expressions, written in C#.",
            Parameters =
            [
                new() { Name = "Default", Type = "BitAiStrings", DefaultValue = "", Description = "Static: The default English strings. Subclass BitAiStrings and override the members to translate; every member left alone keeps its English." },
                new() { Name = "AddItem", Type = "string", DefaultValue = "\"Add item\"", Description = "Label for adding an entry to a list field." },
                new() { Name = "AgentCancelled", Type = "string", DefaultValue = "\"Stopped\"", Description = "Status of an agent run the user stopped." },
                new() { Name = "AgentCompleted", Type = "string", DefaultValue = "\"Done\"", Description = "Status of an agent run that finished." },
                new() { Name = "AgentFailed", Type = "string", DefaultValue = "\"Failed\"", Description = "Status of an agent run that failed." },
                new() { Name = "AgentIdle", Type = "string", DefaultValue = "\"Idle\"", Description = "Status of an agent run that has not started." },
                new() { Name = "AgentPlanning", Type = "string", DefaultValue = "\"Planning\"", Description = "Status of an agent run that is working out its steps." },
                new() { Name = "AgentRunning", Type = "string", DefaultValue = "\"Running\"", Description = "Status of an agent run that is executing." },
                new() { Name = "Answer", Type = "string", DefaultValue = "\"Answer\"", Description = "Label of the button that sends a free-text answer to a waiting agent run." },
                new() { Name = "AnswerPlaceholder", Type = "string", DefaultValue = "\"Type your answer...\"", Description = "Placeholder of the free-text answer box under a waiting agent run." },
                new() { Name = "Approve", Type = "string", DefaultValue = "\"Allow\"", Description = "Label for approving a tool call once." },
                new() { Name = "ApproveAlways", Type = "string", DefaultValue = "\"Always allow\"", Description = "Label for approving a tool and not being asked again." },
                new() { Name = "ApproveTool", Type = "string", DefaultValue = "$\"Allow {tool} to run?\"", Description = "ApproveTool(string tool): Question asked when a tool call needs approval." },
                new() { Name = "Arguments", Type = "string", DefaultValue = "\"Arguments\"", Description = "Label for the arguments section of a tool call." },
                new() { Name = "Artifact", Type = "string", DefaultValue = "\"Artifact\"", Description = "Heading of the artifact panel." },
                new() { Name = "AttachFile", Type = "string", DefaultValue = "\"Attach files\"", Description = "Label for the attach-file button." },
                new() { Name = "AttachmentReadFailed", Type = "string", DefaultValue = "$\"{fileName} could not be read.\"", Description = "AttachmentReadFailed(string fileName): Shown when a file could not be read." },
                new() { Name = "Attachments", Type = "string", DefaultValue = "\"Attachments\"", Description = "Heading above an attachment list." },
                new() { Name = "AttachmentTooLarge", Type = "string", DefaultValue = "$\"{fileName} is too large. Files can be up to {FormatBytes(maxSize)}.\"", Description = "AttachmentTooLarge(string fileName, long maxSize): Shown when a file is larger than the composer accepts." },
                new() { Name = "AttachmentTooMany", Type = "string", DefaultValue = "$\"{fileName} was not attached. Up to {maxCount} files can be attached at once.\"", Description = "AttachmentTooMany(string fileName, int maxCount): Shown when more files are staged than the composer allows." },
                new() { Name = "AttachmentTypeNotAllowed", Type = "string", DefaultValue = "$\"{fileName} is not a supported file type.\"", Description = "AttachmentTypeNotAllowed(string fileName): Shown when a file is not one of the types the composer accepts." },
                new() { Name = "BackToReference", Type = "string", DefaultValue = "$\"Back to reference {number}\"", Description = "BackToReference(int number): Accessible name of a link at the end of a footnote that leads back to where the footnote is referenced." },
                new() { Name = "BadResponse", Type = "string", DefaultValue = "\"Bad response\"", Description = "Label for negative feedback." },
                new() { Name = "CachedTokens", Type = "string", DefaultValue = "\"Cached\"", Description = "Label for cached prompt tokens in the usage breakdown." },
                new() { Name = "CalledTool", Type = "string", DefaultValue = "$\"Used {tool}\"", Description = "CalledTool(string tool): Heading of a tool call card once the tool has run." },
                new() { Name = "CallingTool", Type = "string", DefaultValue = "$\"Calling {tool}\"", Description = "CallingTool(string tool): Heading of a tool call card before the tool has run." },
                new() { Name = "Cancel", Type = "string", DefaultValue = "\"Cancel\"", Description = "Label for cancelling an action." },
                new() { Name = "CharacterCount", Type = "string", DefaultValue = "$\"{used:N0} / {max:N0}\"", Description = "CharacterCount(int used, int max): The character counter under the composer when a length limit is set." },
                new() { Name = "CharactersRemaining", Type = "string", DefaultValue = "remaining == 1 ? \"1 character remaining\" : $\"{remaining:N0} characters remaining\"", Description = "CharactersRemaining(int remaining): Announced when the composer text is close to its length limit." },
                new() { Name = "CitationLabel", Type = "string", DefaultValue = "number is null ? $\"Source: {title}\" : $\"Source {number}: {title}\"", Description = "CitationLabel(int? number, string title): Accessible name of an inline citation marker." },
                new() { Name = "Collapse", Type = "string", DefaultValue = "\"Collapse\"", Description = "Label for leaving full screen." },
                new() { Name = "Commands", Type = "string", DefaultValue = "\"Commands\"", Description = "Accessible name of the slash-command menu." },
                new() { Name = "ComponentFailed", Type = "string", DefaultValue = "$\"'{component}' could not be rendered\"", Description = "ComponentFailed(string component): Shown in place of a generated component that threw." },
                new() { Name = "ComponentNotRegistered", Type = "string", DefaultValue = "$\"No component named '{component}' is registered.\"", Description = "ComponentNotRegistered(string component): Tooltip on a generated component the application has not registered." },
                new() { Name = "Context", Type = "string", DefaultValue = "\"Context\"", Description = "Label for the context usage meter." },
                new() { Name = "ContextCritical", Type = "string", DefaultValue = "\"The context window is almost full\"", Description = "Shown when the context window is nearly full." },
                new() { Name = "ContextUsage", Type = "string", DefaultValue = "$\"{used:N0} / {window:N0} {Tokens}\"", Description = "ContextUsage(long used, long window): Tooltip on the context usage meter." },
                new() { Name = "ContextWarning", Type = "string", DefaultValue = "\"The context window is filling up\"", Description = "Shown when the context window is filling up." },
                new() { Name = "Conversations", Type = "string", DefaultValue = "\"Conversations\"", Description = "Accessible name of the conversation list." },
                new() { Name = "Copied", Type = "string", DefaultValue = "\"Copied\"", Description = "Confirmation shown after copying." },
                new() { Name = "Copy", Type = "string", DefaultValue = "\"Copy\"", Description = "Label for copying content." },
                new() { Name = "Delete", Type = "string", DefaultValue = "\"Delete\"", Description = "Label for deleting a conversation." },
                new() { Name = "DictationFailed", Type = "string", DefaultValue = "$\"Dictation failed: {error}\"", Description = "DictationFailed(string error): Announced when dictation fails." },
                new() { Name = "DictationStopped", Type = "string", DefaultValue = "\"Dictation stopped\"", Description = "Announced when dictation stops." },
                new() { Name = "Dismiss", Type = "string", DefaultValue = "\"Dismiss\"", Description = "Label for dismissing a notice." },
                new() { Name = "Download", Type = "string", DefaultValue = "\"Download\"", Description = "Label for downloading an artifact." },
                new() { Name = "DropFiles", Type = "string", DefaultValue = "\"Drop files to attach\"", Description = "Prompt shown over the composer while a file is dragged over it." },
                new() { Name = "Edit", Type = "string", DefaultValue = "\"Edit\"", Description = "Label for editing a message." },
                new() { Name = "Elapsed", Type = "string", DefaultValue = "\"Elapsed\"", Description = "Accessible label for the elapsed time of a running agent." },
                new() { Name = "EmptyBody", Type = "string", DefaultValue = "\"Ask a question to get started.\"", Description = "Body shown above an empty conversation." },
                new() { Name = "EmptyTitle", Type = "string", DefaultValue = "\"How can I help?\"", Description = "Heading shown above an empty conversation." },
                new() { Name = "Expand", Type = "string", DefaultValue = "\"Expand\"", Description = "Label for opening an artifact full screen." },
                new() { Name = "FieldAboveMaximum", Type = "string", DefaultValue = "$\"{label} must be at most {maximum}.\"", Description = "FieldAboveMaximum(string label, double maximum): Validation message for a number above the maximum." },
                new() { Name = "FieldBelowMinimum", Type = "string", DefaultValue = "$\"{label} must be at least {minimum}.\"", Description = "FieldBelowMinimum(string label, double minimum): Validation message for a number below the minimum." },
                new() { Name = "FieldInvalidFormat", Type = "string", DefaultValue = "$\"{label} is not in the expected format.\"", Description = "FieldInvalidFormat(string label): Validation message for a value that does not match the field pattern." },
                new() { Name = "FieldNotAboveMinimum", Type = "string", DefaultValue = "$\"{label} must be greater than {minimum}.\"", Description = "FieldNotAboveMinimum(string label, double minimum): Validation message for a number at or below an exclusive minimum." },
                new() { Name = "FieldNotADate", Type = "string", DefaultValue = "$\"{label} must be a valid date or time.\"", Description = "FieldNotADate(string label): Validation message for a date or time field that does not hold one." },
                new() { Name = "FieldNotAMultipleOf", Type = "string", DefaultValue = "$\"{label} must be a multiple of {step}.\"", Description = "FieldNotAMultipleOf(string label, double step): Validation message for a number that is not a multiple of the field step." },
                new() { Name = "FieldNotAnEmail", Type = "string", DefaultValue = "$\"{label} must be an email address.\"", Description = "FieldNotAnEmail(string label): Validation message for an email field that does not hold an address." },
                new() { Name = "FieldNotAnInteger", Type = "string", DefaultValue = "$\"{label} must be a whole number.\"", Description = "FieldNotAnInteger(string label): Validation message for an integer field that holds a fraction." },
                new() { Name = "FieldNotANumber", Type = "string", DefaultValue = "$\"{label} must be a number.\"", Description = "FieldNotANumber(string label): Validation message for a numeric field that does not hold a number." },
                new() { Name = "FieldNotAUrl", Type = "string", DefaultValue = "$\"{label} must be a web address.\"", Description = "FieldNotAUrl(string label): Validation message for a URL field that does not hold a web address." },
                new() { Name = "FieldNotBelowMaximum", Type = "string", DefaultValue = "$\"{label} must be less than {maximum}.\"", Description = "FieldNotBelowMaximum(string label, double maximum): Validation message for a number at or above an exclusive maximum." },
                new() { Name = "FieldRequired", Type = "string", DefaultValue = "$\"{label} is required.\"", Description = "FieldRequired(string label): Validation message for a required field left empty." },
                new() { Name = "FieldTooFewItems", Type = "string", DefaultValue = "$\"{label} needs at least {minimum} items.\"", Description = "FieldTooFewItems(string label, int minimum): Validation message for a list with fewer entries than the field allows." },
                new() { Name = "FieldTooLong", Type = "string", DefaultValue = "$\"{label} must be at most {maximum} characters.\"", Description = "FieldTooLong(string label, int maximum): Validation message for text longer than the field allows." },
                new() { Name = "FieldTooManyItems", Type = "string", DefaultValue = "$\"{label} can have at most {maximum} items.\"", Description = "FieldTooManyItems(string label, int maximum): Validation message for a list with more entries than the field allows." },
                new() { Name = "FieldTooShort", Type = "string", DefaultValue = "$\"{label} must be at least {minimum} characters.\"", Description = "FieldTooShort(string label, int minimum): Validation message for text shorter than the field allows." },
                new() { Name = "GenerationStopped", Type = "string", DefaultValue = "\"Generation stopped.\"", Description = "The error a turn ends with when the user stopped it." },
                new() { Name = "FormatBytes", Type = "string", DefaultValue = "512 B, 12 KB, 3.4 MB, 1.2 GB", Description = "FormatBytes(long bytes): Formats a byte count for display." },
                new() { Name = "FormatDuration", Type = "string", DefaultValue = "850ms, 4.2s, 2m 5s", Description = "FormatDuration(TimeSpan duration): Formats a duration for display." },
                new() { Name = "FormatTokens", Type = "string", DefaultValue = "950, 1.6k, 2.4M", Description = "FormatTokens(long tokens): Formats a token count for display." },
                new() { Name = "GoodResponse", Type = "string", DefaultValue = "\"Good response\"", Description = "Label for positive feedback." },
                new() { Name = "IgnoredProperty", Type = "string", DefaultValue = "$\"Ignored: {name}\"", Description = "IgnoredProperty(string name): Shown for a generated property the component does not declare, so it was not passed." },
                new() { Name = "IgnoredPropertyHint", Type = "string", DefaultValue = "\"The component does not declare this property, so it was not passed.\"", Description = "Tooltip explaining an ignored generated property." },
                new() { Name = "InputTokens", Type = "string", DefaultValue = "\"Input\"", Description = "Label for prompt tokens in the usage breakdown." },
                new() { Name = "Listening", Type = "string", DefaultValue = "\"Listening\"", Description = "Announced while dictation is running." },
                new() { Name = "LoadImage", Type = "string", DefaultValue = "$\"Load image: {description}\"", Description = "LoadImage(string description): Label of the click-to-load placeholder shown for an image the image policy did not load automatically." },
                new() { Name = "Mentions", Type = "string", DefaultValue = "\"Mentions\"", Description = "Accessible name of the mention menu." },
                new() { Name = "MissingProperty", Type = "string", DefaultValue = "$\"'{name}' is required by '{component}'\"", Description = "MissingProperty(string name, string component): Shown for a property a generated component requires that the model did not supply." },
                new() { Name = "Model", Type = "string", DefaultValue = "\"Model\"", Description = "Label for the model picker." },
                new() { Name = "ModelDeprecated", Type = "string", DefaultValue = "$\"{name} (deprecated)\"", Description = "ModelDeprecated(string name): Label of a deprecated model in the picker." },
                new() { Name = "MonthGroup", Type = "string", DefaultValue = "date.ToString(\"MMMM yyyy\", CultureInfo.CurrentCulture)", Description = "MonthGroup(DateTime date): Group heading for older conversations, one per calendar month." },
                new() { Name = "NewConversation", Type = "string", DefaultValue = "\"New conversation\"", Description = "Label for starting a new conversation." },
                new() { Name = "OtherModels", Type = "string", DefaultValue = "\"Other\"", Description = "Group heading in the model picker for models with neither a family nor a provider." },
                new() { Name = "OutputTokens", Type = "string", DefaultValue = "\"Output\"", Description = "Label for completion tokens in the usage breakdown." },
                new() { Name = "Plan", Type = "string", DefaultValue = "\"Plan\"", Description = "Heading of an agent run panel." },
                new() { Name = "PreviousSevenDays", Type = "string", DefaultValue = "\"Previous 7 days\"", Description = "Group heading for conversations updated in the last week." },
                new() { Name = "PreviousThirtyDays", Type = "string", DefaultValue = "\"Previous 30 days\"", Description = "Group heading for conversations updated in the last month." },
                new() { Name = "PromptPlaceholder", Type = "string", DefaultValue = "\"Send a message...\"", Description = "Placeholder in the prompt composer." },
                new() { Name = "ReasonedFor", Type = "string", DefaultValue = "$\"Thought for {FormatDuration(duration)}\"", Description = "ReasonedFor(TimeSpan duration): Title of the reasoning panel once finished." },
                new() { Name = "Reasoning", Type = "string", DefaultValue = "\"Reasoning\"", Description = "Title of the collapsed reasoning panel." },
                new() { Name = "ReasoningTokens", Type = "string", DefaultValue = "\"Reasoning\"", Description = "Label for reasoning tokens in the usage breakdown." },
                new() { Name = "Regenerate", Type = "string", DefaultValue = "\"Regenerate\"", Description = "Label for regenerating a response." },
                new() { Name = "Reject", Type = "string", DefaultValue = "\"Decline\"", Description = "Label for rejecting a tool call." },
                new() { Name = "Remove", Type = "string", DefaultValue = "\"Remove\"", Description = "Label for removing an attachment." },
                new() { Name = "RemoveItem", Type = "string", DefaultValue = "$\"Remove {label} {index}\"", Description = "RemoveItem(string label, int index): Accessible label for removing one entry of a list field." },
                new() { Name = "RendererFailed", Type = "string", DefaultValue = "$\"The renderer for '{name}' failed\"", Description = "RendererFailed(string name): Shown in place of content whose custom renderer threw." },
                new() { Name = "RenderFailed", Type = "string", DefaultValue = "\"This content could not be rendered\"", Description = "Shown in place of content whose renderer threw." },
                new() { Name = "RequestTimedOut", Type = "string", DefaultValue = "\"The request timed out.\"", Description = "The error a turn ends with when it ran out of the session's time (BitAiChatSessionOptions.Timeout)." },
                new() { Name = "Result", Type = "string", DefaultValue = "\"Result\"", Description = "Label for the result section of a tool call." },
                new() { Name = "Retry", Type = "string", DefaultValue = "\"Try again\"", Description = "Label for retrying after a failure." },
                new() { Name = "RetryIn", Type = "string", DefaultValue = "$\"Try again in {FormatDuration(wait)}.\"", Description = "RetryIn(TimeSpan wait): Shown under an error that can be retried after a delay." },
                new() { Name = "Save", Type = "string", DefaultValue = "\"Save\"", Description = "Label for saving an edit." },
                new() { Name = "ScrollToLatest", Type = "string", DefaultValue = "\"Scroll to latest\"", Description = "Label on the button that scrolls to the newest message." },
                new() { Name = "Send", Type = "string", DefaultValue = "\"Send\"", Description = "Accessible label for the send button." },
                new() { Name = "ShowDetails", Type = "string", DefaultValue = "\"Show details\"", Description = "Label for showing error detail." },
                new() { Name = "SomethingWentWrong", Type = "string", DefaultValue = "\"Something went wrong\"", Description = "Heading of an error panel." },
                new() { Name = "Sources", Type = "string", DefaultValue = "\"Sources\"", Description = "Heading above the list of sources." },
                new() { Name = "StartDictation", Type = "string", DefaultValue = "\"Dictate\"", Description = "Label for the voice input button." },
                new() { Name = "Stop", Type = "string", DefaultValue = "\"Stop\"", Description = "Label for the button that stops generation." },
                new() { Name = "StopDictation", Type = "string", DefaultValue = "\"Stop dictation\"", Description = "Label shown while dictation is active." },
                new() { Name = "Submit", Type = "string", DefaultValue = "\"Submit\"", Description = "Default label on a form's submit button." },
                new() { Name = "Submitted", Type = "string", DefaultValue = "\"Submitted\"", Description = "Shown in place of a form's actions once it has been submitted." },
                new() { Name = "Thinking", Type = "string", DefaultValue = "\"Thinking\"", Description = "Shown while the model is working but has produced nothing yet." },
                new() { Name = "Today", Type = "string", DefaultValue = "\"Today\"", Description = "Group heading for conversations updated today." },
                new() { Name = "Tokens", Type = "string", DefaultValue = "\"tokens\"", Description = "Label for the token usage readout." },
                new() { Name = "TokensPerSecond", Type = "string", DefaultValue = "$\"{rate:0.#} tok/s\"", Description = "TokensPerSecond(double rate): Generation speed in the token usage readout." },
                new() { Name = "ToolCalls", Type = "string", DefaultValue = "\"Tool calls\"", Description = "Accessible name of the list of tool calls one agent step made." },
                new() { Name = "ToolCallDeclined", Type = "string", DefaultValue = "\"The user declined this tool call.\"", Description = "The result of a tool call the user declined when it asked for approval." },
                new() { Name = "ToolCancelled", Type = "string", DefaultValue = "$\"{tool} was cancelled\"", Description = "ToolCancelled(string tool): Heading of a tool call card whose call was cancelled before the tool produced a result." },
                new() { Name = "ToolFailed", Type = "string", DefaultValue = "$\"{tool} failed\"", Description = "ToolFailed(string tool): Heading of a tool call card that failed." },
                new() { Name = "ToolLoopLimitReached", Type = "string", DefaultValue = "$\"Stopped after {roundTrips} tool round trips.\"", Description = "ToolLoopLimitReached(int roundTrips): The error left on a reply whose tool calls went round more times than the session allows (BitAiChatSessionOptions.MaxToolRoundtrips)." },
                new() { Name = "ToolRejected", Type = "string", DefaultValue = "$\"{tool} was declined\"", Description = "ToolRejected(string tool): Heading of a tool call card whose call was declined when it asked for approval." },
                new() { Name = "Transcript", Type = "string", DefaultValue = "\"Conversation\"", Description = "Accessible name of the transcript region." },
                new() { Name = "UnreadableProperty", Type = "string", DefaultValue = "$\"'{name}' could not be read as {type}\"", Description = "UnreadableProperty(string name, string type): Shown for a generated property whose value could not be read as the type the component declares for it, so it was not passed." },
                new() { Name = "Untitled", Type = "string", DefaultValue = "\"Untitled\"", Description = "Fallback title for a conversation with no title yet." },
                new() { Name = "UploadFailed", Type = "string", DefaultValue = "\"Upload failed\"", Description = "Shown on an attachment whose upload failed without a reason." },
                new() { Name = "UsageLine", Type = "string", DefaultValue = "$\"{label}: {tokens:N0}\"", Description = "UsageLine(string label, long tokens): One line of the usage breakdown." },
                new() { Name = "Version", Type = "string", DefaultValue = "\"Version\"", Description = "Label for the artifact version selector." },
                new() { Name = "VersionNumber", Type = "string", DefaultValue = "$\"{Version} {version}\"", Description = "VersionNumber(int version): Label of one entry in the artifact version selector." },
                new() { Name = "WaitingForYou", Type = "string", DefaultValue = "\"Waiting for your answer\"", Description = "Shown when an agent is waiting for the user." },
                new() { Name = "Yesterday", Type = "string", DefaultValue = "\"Yesterday\"", Description = "Group heading for conversations updated yesterday." },
            ]
        },
    ];



    // Basic: inline citations on or off.
    private static readonly BitAiCitation[] citations =
    [
        new("c1", new BitAiCitationSource("render-modes", "Blazor render modes")
        {
            Kind = BitAiSourceKind.Web,
            Url = "https://learn.microsoft.com/aspnet/core/blazor/components/render-modes",
            SiteName = "Microsoft Learn",
        }) { Number = 1 },
        new("c2", new BitAiCitationSource("circuits", "SignalR circuit lifetime")
        {
            Kind = BitAiSourceKind.Document,
            Url = "https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr",
            SiteName = "Microsoft Learn",
        }) { Number = 2 },
    ];

    private const string answer = "Interactive Server keeps component state on the server [1], and loses it with the circuit [2].";

    private bool inlineCitations = true;
    private BitAiUiOptions basicOptions = BitAiUiOptions.Default;

    private void BuildBasicOptions() => basicOptions = BitAiUiOptions.Default with { InlineCitations = inlineCitations };



    // Localized strings: a few of the strings in German, the rest left in English.
    private static readonly BitAiUiOptions germanOptions = BitAiUiOptions.Default with { Strings = new GermanAiStrings() };

    private readonly BitAiTokenUsage usage = new() { InputTokens = 12_400, OutputTokens = 1_860, CachedInputTokens = 9_800 };

    private sealed class GermanAiStrings : BitAiStrings
    {
        private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

        public override string AgentRunning => "Läuft";
        public override string WaitingForYou => "Wartet auf Ihre Antwort";
        public override string AgentCompleted => "Fertig";
        public override string Sources => "Quellen";
        public override string Context => "Kontext";
        public override string Tokens => "Token";
        public override string InputTokens => "Eingabe";
        public override string OutputTokens => "Ausgabe";
        public override string CachedTokens => "Zwischengespeichert";
        public override string ContextWarning => "Das Kontextfenster füllt sich";
        public override string ContextCritical => "Das Kontextfenster ist fast voll";
        public override string CitationLabel(int? number, string title) => number is null ? $"Quelle: {title}" : $"Quelle {number}: {title}";
        public override string ContextUsage(long used, long window) => string.Format(German, "{0:N0} / {1:N0} {2}", used, window, Tokens);
        public override string UsageLine(string label, long tokens) => string.Format(German, "{0}: {1:N0}", label, tokens);
        public override string FormatTokens(long tokens) => tokens < 1000
            ? tokens.ToString("N0", German)
            : string.Format(German, "{0:0.0} Tsd.", tokens / 1000.0);
    }



    // Nested providers: the outer options, with one setting changed for the inner region.
    private static readonly BitAiUiOptions innerOptions = germanOptions with { InlineCitations = false };



    // Registered options: a run with usage, outside any provider and inside one that brings the defaults back.
    private readonly BitAiAgentRun usageRun = CreateUsageRun();

    private static BitAiAgentRun CreateUsageRun()
    {
        var run = new BitAiAgentRun(agentName: "Research agent") { Goal = "Summarize the release notes" };

        run.SetPlan(
        [
            new BitAiAgentPlanStep("fetch", "Fetch the release notes"),
            new BitAiAgentPlanStep("summarize", "Summarize them"),
        ]);
        run.SetStatus(BitAiAgentRunStatus.Running);
        run.Steps[0].Start();
        run.AddUsage(new BitAiTokenUsage { InputTokens = 1_850, OutputTokens = 420, Cost = 0.0119m, Currency = "USD" });

        return run;
    }



    // Render interval: one run whose usage changes every 50ms, followed at two intervals.
    private readonly BitAiUiOptions fastOptions = BitAiUiOptions.Default with { ShowUsage = true };
    private readonly BitAiUiOptions slowOptions = BitAiUiOptions.Default with { ShowUsage = true, RenderInterval = TimeSpan.FromSeconds(1) };
    private BitAiAgentRun? streamingRun;
    private bool isStreaming;

    private async Task StartStreamingRun()
    {
        isStreaming = true;

        var run = new BitAiAgentRun(agentName: "Writer") { Goal = "Draft the announcement" };
        streamingRun = run;

        run.SetPlan([new BitAiAgentPlanStep("draft", "Draft it")]);
        run.SetStatus(BitAiAgentRunStatus.Running);
        run.Steps[0].Start();

        for (var i = 0; i < 100; i++)
        {
            await Task.Delay(50);
            run.AddUsage(new BitAiTokenUsage { OutputTokens = 7 });
        }

        run.Steps[0].Complete(summary: "700 tokens written");
        run.SetStatus(BitAiAgentRunStatus.Completed);

        isStreaming = false;
    }



    // Density & scheme: the bit theme, not the AI options.
    private readonly BitTheme compactTheme = BitThemeDensityPresets.CreateCompactOverlay();
    private readonly BitTheme spaciousTheme = BitThemeDensityPresets.CreateSpaciousOverlay();
}
