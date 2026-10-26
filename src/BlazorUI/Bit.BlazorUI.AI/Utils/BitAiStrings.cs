using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// Every piece of user-facing text the components render.
/// </summary>
/// <remarks>
/// Kept in one replaceable object rather than scattered through markup, so that localising the
/// library, or simply changing its tone, does not mean forking components. Register a subclass and
/// every component picks it up.
/// </remarks>
public class BitAiStrings
{
    /// <summary>The default English strings.</summary>
    public static BitAiStrings Default { get; } = new();

    /// <summary>Validation message for an integer field that holds a fraction.</summary>
    /// <param name="label">The field label.</param>
    /// <returns>The message.</returns>
    public virtual string FieldNotAnInteger(string label) => $"{label} must be a whole number.";

    /// <summary>Validation message for a number that is not a multiple of the field step.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="step">The step the value must be a multiple of.</param>
    /// <returns>The message.</returns>
    public virtual string FieldNotAMultipleOf(string label, double step) => $"{label} must be a multiple of {step}.";

    /// <summary>Validation message for a number at or below an exclusive minimum.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="minimum">The value the field must exceed.</param>
    /// <returns>The message.</returns>
    public virtual string FieldNotAboveMinimum(string label, double minimum) => $"{label} must be greater than {minimum}.";

    /// <summary>Validation message for a number at or above an exclusive maximum.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="maximum">The value the field must stay below.</param>
    /// <returns>The message.</returns>
    public virtual string FieldNotBelowMaximum(string label, double maximum) => $"{label} must be less than {maximum}.";

    /// <summary>Validation message for text shorter than the field allows.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="minimum">The fewest accepted characters.</param>
    /// <returns>The message.</returns>
    public virtual string FieldTooShort(string label, int minimum) => $"{label} must be at least {minimum} characters.";

    /// <summary>Validation message for text longer than the field allows.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="maximum">The most accepted characters.</param>
    /// <returns>The message.</returns>
    public virtual string FieldTooLong(string label, int maximum) => $"{label} must be at most {maximum} characters.";

    /// <summary>Validation message for a list with fewer entries than the field allows.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="minimum">The fewest accepted entries.</param>
    /// <returns>The message.</returns>
    public virtual string FieldTooFewItems(string label, int minimum) =>
        minimum == 1 ? $"{label} needs at least 1 item." : $"{label} needs at least {minimum} items.";

    /// <summary>Validation message for a list with more entries than the field allows.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="maximum">The most accepted entries.</param>
    /// <returns>The message.</returns>
    public virtual string FieldTooManyItems(string label, int maximum) =>
        maximum == 1 ? $"{label} can have at most 1 item." : $"{label} can have at most {maximum} items.";

    /// <summary>Validation message for a URL field that does not hold a web address.</summary>
    /// <param name="label">The field label.</param>
    /// <returns>The message.</returns>
    public virtual string FieldNotAUrl(string label) => $"{label} must be a web address.";

    /// <summary>Validation message for a date or time field that does not hold one.</summary>
    /// <param name="label">The field label.</param>
    /// <returns>The message.</returns>
    public virtual string FieldNotADate(string label) => $"{label} must be a valid date or time.";

    /// <summary>Label for adding an entry to a list field.</summary>
    public virtual string AddItem => "Add item";

    /// <summary>Accessible label for removing one entry of a list field.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="index">The one-based position of the entry.</param>
    /// <returns>The label.</returns>
    public virtual string RemoveItem(string label, int index) => $"Remove {label} {index}";

    /// <summary>
    /// Accessible name of one entry of a list field: the group of fields of an entry of objects, the input of an entry
    /// of values, and the label its validation messages name.
    /// </summary>
    /// <param name="label">The field label.</param>
    /// <param name="index">The one-based position of the entry.</param>
    /// <returns>The name.</returns>
    public virtual string ListItem(string label, int index) => $"{label} {index}";

    /// <summary>Placeholder of the free-text answer box under a waiting agent run.</summary>
    public virtual string AnswerPlaceholder => "Type your answer...";

    /// <summary>Label of the button that sends a free-text answer to a waiting agent run.</summary>
    public virtual string Answer => "Answer";

    /// <summary>Accessible name of the list of tool calls one agent step made.</summary>
    public virtual string ToolCalls => "Tool calls";

    /// <summary>Accessible label for the elapsed time of a running agent.</summary>
    public virtual string Elapsed => "Elapsed";

    /// <summary>Shown when the context window is filling up.</summary>
    public virtual string ContextWarning => "The context window is filling up";

    /// <summary>Shown when the context window is nearly full.</summary>
    public virtual string ContextCritical => "The context window is almost full";

    /// <summary>Placeholder in the prompt composer.</summary>
    public virtual string PromptPlaceholder => "Send a message...";

    /// <summary>Accessible label for the send button.</summary>
    public virtual string Send => "Send";

    /// <summary>Label for the button that stops generation.</summary>
    public virtual string Stop => "Stop";

    /// <summary>Label for regenerating a response.</summary>
    public virtual string Regenerate => "Regenerate";

    /// <summary>Label for retrying after a failure.</summary>
    public virtual string Retry => "Try again";

    /// <summary>Label for copying content.</summary>
    public virtual string Copy => "Copy";

    /// <summary>Confirmation shown after copying.</summary>
    public virtual string Copied => "Copied";

    /// <summary>Label for editing a message.</summary>
    public virtual string Edit => "Edit";

    /// <summary>Label for saving an edit.</summary>
    public virtual string Save => "Save";

    /// <summary>Label for cancelling an action.</summary>
    public virtual string Cancel => "Cancel";

    /// <summary>Label for positive feedback.</summary>
    public virtual string GoodResponse => "Good response";

    /// <summary>Label for negative feedback.</summary>
    public virtual string BadResponse => "Bad response";

    /// <summary>Label on the button that scrolls to the newest message.</summary>
    public virtual string ScrollToLatest => "Scroll to latest";

    /// <summary>Shown while the model is working but has produced nothing yet.</summary>
    public virtual string Thinking => "Thinking";

    /// <summary>Title of the collapsed reasoning panel, and the accessible name of the box its text scrolls in.</summary>
    public virtual string Reasoning => "Reasoning";

    /// <summary>Title of the reasoning panel once finished.</summary>
    /// <param name="duration">How long the model reasoned for.</param>
    /// <returns>The title.</returns>
    public virtual string ReasonedFor(TimeSpan duration) => $"Thought for {FormatDuration(duration)}";

    /// <summary>Heading above the list of sources.</summary>
    public virtual string Sources => "Sources";

    /// <summary>Heading above an attachment list.</summary>
    public virtual string Attachments => "Attachments";

    /// <summary>Label for the attach-file button.</summary>
    public virtual string AttachFile => "Attach files";

    /// <summary>Label for removing an attachment.</summary>
    public virtual string Remove => "Remove";

    /// <summary>Prompt shown over the composer while a file is dragged over it.</summary>
    public virtual string DropFiles => "Drop files to attach";

    /// <summary>Label for the voice input button.</summary>
    public virtual string StartDictation => "Dictate";

    /// <summary>Label shown while dictation is active.</summary>
    public virtual string StopDictation => "Stop dictation";

    /// <summary>Label for the model picker.</summary>
    public virtual string Model => "Model";

    /// <summary>Heading of a tool call card before the tool has run.</summary>
    /// <param name="tool">The tool name.</param>
    /// <returns>The heading.</returns>
    public virtual string CallingTool(string tool) => $"Calling {tool}";

    /// <summary>Heading of a tool call card once the tool has run.</summary>
    /// <param name="tool">The tool name.</param>
    /// <returns>The heading.</returns>
    public virtual string CalledTool(string tool) => $"Used {tool}";

    /// <summary>Heading of a tool call card that failed.</summary>
    /// <param name="tool">The tool name.</param>
    /// <returns>The heading.</returns>
    public virtual string ToolFailed(string tool) => $"{tool} failed";

    /// <summary>Heading of a tool call card whose call was cancelled before the tool produced a result.</summary>
    /// <param name="tool">The tool name.</param>
    /// <returns>The heading.</returns>
    public virtual string ToolCancelled(string tool) => $"{tool} was cancelled";

    /// <summary>Heading of a tool call card whose call was declined when it asked for approval.</summary>
    /// <param name="tool">The tool name.</param>
    /// <returns>The heading.</returns>
    public virtual string ToolRejected(string tool) => $"{tool} was declined";

    /// <summary>Question asked when a tool call needs approval.</summary>
    /// <param name="tool">The tool name.</param>
    /// <returns>The question.</returns>
    public virtual string ApproveTool(string tool) => $"Allow {tool} to run?";

    /// <summary>
    /// Heading of a tool call card whose call waits for approval. The prompt under it asks the question
    /// (<see cref="ApproveTool"/>), so the heading only says what the call is waiting for.
    /// </summary>
    /// <param name="tool">The tool name.</param>
    /// <returns>The heading.</returns>
    public virtual string ToolAwaitingApproval(string tool) => $"{tool} is waiting for your approval";

    /// <summary>Label for approving a tool call once.</summary>
    public virtual string Approve => "Allow";

    /// <summary>Label for approving a tool and not being asked again.</summary>
    public virtual string ApproveAlways => "Always allow";

    /// <summary>Label for rejecting a tool call.</summary>
    public virtual string Reject => "Decline";

    /// <summary>Label for the arguments section of a tool call.</summary>
    public virtual string Arguments => "Arguments";

    /// <summary>Label for the result section of a tool call.</summary>
    public virtual string Result => "Result";

    /// <summary>Heading of an agent run panel.</summary>
    public virtual string Plan => "Plan";

    /// <summary>Shown when an agent is waiting for the user.</summary>
    public virtual string WaitingForYou => "Waiting for your answer";

    /// <summary>Heading of the artifact panel.</summary>
    public virtual string Artifact => "Artifact";

    /// <summary>Label for downloading an artifact.</summary>
    public virtual string Download => "Download";

    /// <summary>Label for opening an artifact full screen.</summary>
    public virtual string Expand => "Expand";

    /// <summary>Label for leaving full screen.</summary>
    public virtual string Collapse => "Collapse";

    /// <summary>Label for the artifact version selector.</summary>
    public virtual string Version => "Version";

    /// <summary>Heading shown above an empty conversation.</summary>
    public virtual string EmptyTitle => "How can I help?";

    /// <summary>Body shown above an empty conversation.</summary>
    public virtual string EmptyBody => "Ask a question to get started.";

    /// <summary>Label for starting a new conversation.</summary>
    public virtual string NewConversation => "New conversation";

    /// <summary>Label for deleting a conversation.</summary>
    public virtual string Delete => "Delete";

    /// <summary>Fallback title for a conversation with no title yet.</summary>
    public virtual string Untitled => "Untitled";

    /// <summary>Heading of an error panel.</summary>
    public virtual string SomethingWentWrong => "Something went wrong";

    /// <summary>Label for showing error detail.</summary>
    public virtual string ShowDetails => "Show details";

    /// <summary>Label for hiding error detail that is shown.</summary>
    public virtual string HideDetails => "Hide details";

    /// <summary>Label for the token usage readout.</summary>
    public virtual string Tokens => "tokens";

    /// <summary>Label for the context usage meter.</summary>
    public virtual string Context => "Context";

    /// <summary>Tooltip on the context usage meter.</summary>
    /// <param name="used">Tokens used so far.</param>
    /// <param name="window">The size of the context window.</param>
    /// <returns>The tooltip.</returns>
    public virtual string ContextUsage(long used, long window) => $"{used:N0} / {window:N0} {Tokens}";

    /// <summary>Generation speed in the token usage readout.</summary>
    /// <param name="rate">Tokens per second.</param>
    /// <returns>The readout.</returns>
    public virtual string TokensPerSecond(double rate) => $"{rate:0.#} tok/s";

    /// <summary>
    /// Name of the time to the first token in the token usage readout, read to assistive technologies before the duration
    /// the readout shows bare.
    /// </summary>
    public virtual string TimeToFirstToken => "Time to first token";

    /// <summary>Label for prompt tokens in the usage breakdown.</summary>
    public virtual string InputTokens => "Input";

    /// <summary>Label for completion tokens in the usage breakdown.</summary>
    public virtual string OutputTokens => "Output";

    /// <summary>Label for cached prompt tokens in the usage breakdown.</summary>
    public virtual string CachedTokens => "Cached";

    /// <summary>Label for reasoning tokens in the usage breakdown.</summary>
    public virtual string ReasoningTokens => "Reasoning";

    /// <summary>One line of the usage breakdown.</summary>
    /// <param name="label">What is being counted, such as <see cref="InputTokens"/>.</param>
    /// <param name="tokens">The count.</param>
    /// <returns>The line.</returns>
    public virtual string UsageLine(string label, long tokens) => $"{label}: {tokens:N0}";

    /// <summary>Accessible name of the transcript region.</summary>
    public virtual string Transcript => "Conversation";

    /// <summary>Accessible name of the conversation list.</summary>
    public virtual string Conversations => "Conversations";

    /// <summary>
    /// Accessible name of the box a table scrolls in sideways (a data table given none of its own, a Markdown table).
    /// </summary>
    public virtual string Table => "Table";

    /// <summary>Accessible name of the box the code of a code block scrolls in.</summary>
    /// <param name="fileName">The file name the block was given, if any.</param>
    /// <param name="language">The language of the code, if known.</param>
    /// <returns>The name: the file name, else the language, else just Code.</returns>
    public virtual string CodeBlock(string? fileName, string? language) =>
        fileName is { Length: > 0 } ? fileName : language is { Length: > 0 } ? $"{language} code" : "Code";

    /// <summary>Accessible name of the toggle of the outermost object or array of a JSON tree, which has no key.</summary>
    public virtual string JsonRoot => "Root";

    /// <summary>Accessible name of the toggle of an object or array that is an entry of an array of a JSON tree.</summary>
    /// <param name="index">The zero-based position of the entry, as JSON counts it.</param>
    /// <returns>The name.</returns>
    public virtual string JsonArrayItem(int index) => $"[{index}]";

    /// <summary>Group heading for conversations updated today.</summary>
    public virtual string Today => "Today";

    /// <summary>Group heading for conversations updated yesterday.</summary>
    public virtual string Yesterday => "Yesterday";

    /// <summary>Group heading for conversations updated in the last week.</summary>
    public virtual string PreviousSevenDays => "Previous 7 days";

    /// <summary>Group heading for conversations updated in the last month.</summary>
    public virtual string PreviousThirtyDays => "Previous 30 days";

    /// <summary>Group heading for older conversations, one per calendar month.</summary>
    /// <param name="date">A local date in the month.</param>
    /// <returns>The heading.</returns>
    public virtual string MonthGroup(DateTime date) => date.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

    /// <summary>Accessible name of an inline citation marker.</summary>
    /// <param name="number">The marker number, when the citation has one.</param>
    /// <param name="title">The title of the cited source.</param>
    /// <returns>The accessible name.</returns>
    public virtual string CitationLabel(int? number, string title) =>
        number is null ? $"Source: {title}" : $"Source {number}: {title}";

    /// <summary>Accessible name of a link at the end of a footnote that leads back to where the footnote is referenced.</summary>
    /// <param name="number">The footnote number.</param>
    /// <returns>The accessible name.</returns>
    public virtual string BackToReference(int number) => $"Back to reference {number}";

    /// <summary>
    /// Accessible name of one of the links at the end of a footnote that lead back to where it is referenced: a note
    /// referenced more than once has a link per reference, and every one after the first is told apart by its ordinal
    /// ("Back to reference 1-2"), as GitHub names them.
    /// </summary>
    /// <param name="number">The footnote number.</param>
    /// <param name="ordinal">Which of the footnote's references the link leads to, from one.</param>
    /// <returns>The accessible name.</returns>
    public virtual string BackToReference(int number, int ordinal) =>
        ordinal <= 1 ? BackToReference(number) : $"{BackToReference(number)}-{ordinal}";

    /// <summary>Label of one entry in the artifact version selector.</summary>
    /// <param name="version">The version number.</param>
    /// <returns>The label.</returns>
    public virtual string VersionNumber(int version) => $"{Version} {version}";

    /// <summary>Status of an agent run that has not started.</summary>
    public virtual string AgentIdle => "Idle";

    /// <summary>Status of an agent run that is working out its steps.</summary>
    public virtual string AgentPlanning => "Planning";

    /// <summary>Status of an agent run that is executing.</summary>
    public virtual string AgentRunning => "Running";

    /// <summary>Status of an agent run that finished.</summary>
    public virtual string AgentCompleted => "Done";

    /// <summary>Status of an agent run that failed.</summary>
    public virtual string AgentFailed => "Failed";

    /// <summary>Status of an agent run the user stopped.</summary>
    public virtual string AgentCancelled => "Stopped";

    /// <summary>The count of the steps of an agent run that have finished successfully, beside its progress bar.</summary>
    /// <param name="done">How many steps have finished successfully.</param>
    /// <param name="total">How many steps the plan has.</param>
    /// <returns>The count.</returns>
    public virtual string StepsCompleted(int done, int total) => $"{done} / {total}";

    /// <summary>
    /// Where a step of an agent run stands, said to assistive technologies: the marker of the step only draws it.
    /// </summary>
    /// <param name="status">The status of the step.</param>
    /// <returns>The words.</returns>
    public virtual string AgentStepStatus(BitAiAgentStepStatus status) => status switch
    {
        BitAiAgentStepStatus.Running => "In progress",
        BitAiAgentStepStatus.Blocked => "Waiting for you",
        BitAiAgentStepStatus.Completed => "Done",
        BitAiAgentStepStatus.Failed => "Failed",
        BitAiAgentStepStatus.Skipped => "Skipped",
        _ => "Not started",
    };

    /// <summary>Default label on a form's submit button.</summary>
    public virtual string Submit => "Submit";

    /// <summary>Shown in place of a form's actions once it has been submitted.</summary>
    public virtual string Submitted => "Submitted";

    /// <summary>Validation message for a required field left empty.</summary>
    /// <param name="label">The field label.</param>
    /// <returns>The message.</returns>
    public virtual string FieldRequired(string label) => $"{label} is required.";

    /// <summary>Validation message for a numeric field that does not hold a number.</summary>
    /// <param name="label">The field label.</param>
    /// <returns>The message.</returns>
    public virtual string FieldNotANumber(string label) => $"{label} must be a number.";

    /// <summary>Validation message for a number below the minimum.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="minimum">The smallest accepted value.</param>
    /// <returns>The message.</returns>
    public virtual string FieldBelowMinimum(string label, double minimum) => $"{label} must be at least {minimum}.";

    /// <summary>Validation message for a number above the maximum.</summary>
    /// <param name="label">The field label.</param>
    /// <param name="maximum">The largest accepted value.</param>
    /// <returns>The message.</returns>
    public virtual string FieldAboveMaximum(string label, double maximum) => $"{label} must be at most {maximum}.";

    /// <summary>Validation message for an email field that does not hold an address.</summary>
    /// <param name="label">The field label.</param>
    /// <returns>The message.</returns>
    public virtual string FieldNotAnEmail(string label) => $"{label} must be an email address.";

    /// <summary>Validation message for a value that does not match the field pattern.</summary>
    /// <param name="label">The field label.</param>
    /// <returns>The message.</returns>
    public virtual string FieldInvalidFormat(string label) => $"{label} is not in the expected format.";

    /// <summary>Shown on an attachment whose upload failed without a reason.</summary>
    public virtual string UploadFailed => "Upload failed";

    /// <summary>Shown when a file is larger than the composer accepts.</summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="maxSize">The largest accepted size, in bytes.</param>
    /// <returns>The message.</returns>
    public virtual string AttachmentTooLarge(string fileName, long maxSize) =>
        $"{fileName} is too large. Files can be up to {FormatBytes(maxSize)}.";

    /// <summary>Shown when a file is not one of the types the composer accepts.</summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The message.</returns>
    public virtual string AttachmentTypeNotAllowed(string fileName) => $"{fileName} is not a supported file type.";

    /// <summary>Shown when a file could not be read.</summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>The message.</returns>
    public virtual string AttachmentReadFailed(string fileName) => $"{fileName} could not be read.";

    /// <summary>Label for dismissing a notice.</summary>
    public virtual string Dismiss => "Dismiss";

    /// <summary>The error a turn ends with when it ran out of the session's time (<see cref="BitAiChatSessionOptions.Timeout"/>).</summary>
    public virtual string RequestTimedOut => "The request timed out.";

    /// <summary>The error a turn ends with when the user stopped it.</summary>
    public virtual string GenerationStopped => "Generation stopped.";

    /// <summary>The error left on a reply whose tool calls went round more times than the session allows.</summary>
    /// <param name="roundTrips">How many round trips were allowed (<see cref="BitAiChatSessionOptions.MaxToolRoundtrips"/>).</param>
    /// <returns>The message.</returns>
    public virtual string ToolLoopLimitReached(int roundTrips) => $"Stopped after {roundTrips} tool round trips.";

    /// <summary>The result of a tool call the user declined when it asked for approval.</summary>
    public virtual string ToolCallDeclined => "The user declined this tool call.";

    /// <summary>Shown under an error that can be retried after a delay.</summary>
    /// <param name="wait">How long to wait.</param>
    /// <returns>The message.</returns>
    public virtual string RetryIn(TimeSpan wait) => $"Try again in {FormatDuration(wait)}.";

    /// <summary>Shown in place of content whose renderer threw.</summary>
    public virtual string RenderFailed => "This content could not be rendered";

    /// <summary>Shown in place of content whose custom renderer threw.</summary>
    /// <param name="name">The tool, part kind, schema, or artifact kind the renderer handles.</param>
    /// <returns>The message.</returns>
    public virtual string RendererFailed(string name) => $"The renderer for '{name}' failed";

    /// <summary>Shown in place of a generated component that threw.</summary>
    /// <param name="component">The component name.</param>
    /// <returns>The message.</returns>
    public virtual string ComponentFailed(string component) => $"'{component}' could not be rendered";

    /// <summary>Tooltip and accessible name of a generated component the application has not registered.</summary>
    /// <param name="component">The component name the model asked for.</param>
    /// <returns>The tooltip.</returns>
    public virtual string ComponentNotRegistered(string component) => $"No component named '{component}' is registered.";

    /// <summary>Shown for a generated property the component does not declare, so it was not passed.</summary>
    /// <param name="name">The property name.</param>
    /// <returns>The label.</returns>
    public virtual string IgnoredProperty(string name) => $"Ignored: {name}";

    /// <summary>Tooltip explaining an ignored generated property.</summary>
    public virtual string IgnoredPropertyHint => "The component does not declare this property, so it was not passed.";

    /// <summary>
    /// Shown for a generated property whose value could not be read as the type the component declares for it, so it
    /// was not passed.
    /// </summary>
    /// <param name="name">The property name, as the model wrote it.</param>
    /// <param name="type">The name of the type the value was to be read as.</param>
    /// <returns>The label.</returns>
    public virtual string UnreadableProperty(string name, string type) => $"'{name}' could not be read as {type}";

    /// <summary>Shown for a property a generated component requires that the model did not supply.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="component">The component name.</param>
    /// <returns>The label.</returns>
    public virtual string MissingProperty(string name, string component) => $"'{name}' is required by '{component}'";

    /// <summary>Formats a duration for display.</summary>
    /// <param name="duration">The duration.</param>
    /// <returns>A short human-readable string.</returns>
    public virtual string FormatDuration(TimeSpan duration) => duration.TotalSeconds switch
    {
        < 1 => $"{duration.TotalMilliseconds:F0}ms",
        // A whole number of seconds is written without a fraction ("30s", not "30.0s").
        < 60 => $"{duration.TotalSeconds:0.#}s",
        _ => $"{(int)duration.TotalMinutes}m {duration.Seconds}s",
    };

    /// <summary>Formats a byte count for display.</summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>A short human-readable string.</returns>
    public virtual string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F0} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
    };

    /// <summary>Formats a token count for display.</summary>
    /// <param name="tokens">The token count.</param>
    /// <returns>A short human-readable string.</returns>
    public virtual string FormatTokens(long tokens) => tokens switch
    {
        < 1000 => tokens.ToString("N0"),
        < 1_000_000 => $"{tokens / 1000.0:0.#}k",
        _ => $"{tokens / 1_000_000.0:0.#}M",
    };

    /// <summary>The character counter under the composer when a length limit is set.</summary>
    /// <param name="used">Characters typed so far.</param>
    /// <param name="max">The limit.</param>
    /// <returns>The counter text.</returns>
    public virtual string CharacterCount(int used, int max) => $"{used:N0} / {max:N0}";

    /// <summary>Announced when the composer text is close to its length limit.</summary>
    /// <param name="remaining">Characters left before the limit.</param>
    /// <returns>The announcement.</returns>
    public virtual string CharactersRemaining(int remaining) =>
        remaining == 1 ? "1 character remaining" : $"{remaining:N0} characters remaining";

    /// <summary>Shown when more files are staged than the composer allows.</summary>
    /// <param name="fileName">The file that was turned away.</param>
    /// <param name="maxCount">The most files the composer accepts at once.</param>
    /// <returns>The message.</returns>
    public virtual string AttachmentTooMany(string fileName, int maxCount) =>
        $"{fileName} was not attached. Up to {maxCount} files can be attached at once.";

    /// <summary>Accessible name of the slash-command menu.</summary>
    public virtual string Commands => "Commands";

    /// <summary>Accessible name of the mention menu.</summary>
    public virtual string Mentions => "Mentions";

    /// <summary>Announced while dictation is running.</summary>
    public virtual string Listening => "Listening";

    /// <summary>Announced when dictation stops.</summary>
    public virtual string DictationStopped => "Dictation stopped";

    /// <summary>Announced when dictation fails.</summary>
    /// <param name="error">The error name the browser reported, such as <c>not-allowed</c>.</param>
    /// <returns>The announcement.</returns>
    public virtual string DictationFailed(string error) => $"Dictation failed: {error}";

    /// <summary>Label of a deprecated model in the picker.</summary>
    /// <param name="name">The model name.</param>
    /// <returns>The label.</returns>
    public virtual string ModelDeprecated(string name) => $"{name} (deprecated)";

    /// <summary>Group heading in the model picker for models with neither a family nor a provider.</summary>
    public virtual string OtherModels => "Other";

    /// <summary>Label of the click-to-load placeholder shown for an image the image policy did not load automatically.</summary>
    /// <param name="description">The image alt text, or its host when there is none.</param>
    /// <returns>The label.</returns>
    public virtual string LoadImage(string description) => $"Load image: {description}";
}
