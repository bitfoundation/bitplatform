namespace Bit.Borm;

/// <summary>
/// Supplies the text of built-in validation messages. Register an implementation in DI to localise
/// Borm without replacing its rules; the provider is resolved from
/// <see cref="BormValidationContext.Services"/> at validation time, so the same schema can render
/// in different languages per request.
/// </summary>
public interface IBormMessageProvider
{
    /// <summary>
    /// Returns the message for <paramref name="key"/> (see <see cref="BormMessageKeys"/>),
    /// formatted with <paramref name="args"/>.
    /// </summary>
    string Get(string key, params object?[] args);
}

/// <summary>The keys used by the built-in validation rules when asking an <see cref="IBormMessageProvider"/> for text.</summary>
public static class BormMessageKeys
{
    public const string Required = "required";
    public const string MinLength = "minLength";
    public const string MaxLength = "maxLength";
    public const string RangeBetween = "range.between";
    public const string RangeMin = "range.min";
    public const string RangeMax = "range.max";

    /// <summary>An exclusive lower bound was not cleared. Arg 0 is the bound.</summary>
    public const string RangeAbove = "range.above";

    /// <summary>An exclusive upper bound was not stayed under. Arg 0 is the bound.</summary>
    public const string RangeBelow = "range.below";
    public const string Pattern = "pattern";
    public const string Email = "email";
    public const string Url = "url";
    public const string Compare = "compare";
    public const string MinItems = "items.min";
    public const string MaxItems = "items.max";
    public const string UniqueItems = "items.unique";
    public const string MultipleOf = "multipleOf";
    public const string FileSize = "file.size";
    public const string FileType = "file.type";
    public const string PatternTimeout = "pattern.timeout";
    public const string Conversion = "conversion";

    /// <summary>A value that should be a UUID (JSON Schema's <c>format: uuid</c>) is not one.</summary>
    public const string Uuid = "uuid";

    /// <summary>
    /// An expression rule (<see cref="BormExpressionRule"/>) that was given no message of its own does
    /// not hold — an <c>x-rules</c> entry in a document that only says what must be true.
    /// </summary>
    public const string Rule = "rule";

    /// <summary>A value that does not fit the field's <see cref="BormMask"/>. Arg 0 is the shape to write it in, <c>(###) ###-####</c>.</summary>
    public const string Mask = "mask";

    // --- UI chrome ---
    // The library renders text of its own — buttons, placeholders, the empty state of a repeater. It
    // goes through the same provider as the validation messages, so translating a form means
    // implementing one interface rather than one interface plus a dozen component parameters.

    /// <summary>Placeholder shown in a select with nothing chosen.</summary>
    public const string SelectPlaceholder = "ui.select.placeholder";

    /// <summary>Placeholder shown while an options provider is loading.</summary>
    public const string SelectLoading = "ui.select.loading";

    /// <summary>Placeholder shown when an options provider failed, so an empty list is explained rather than mysterious.</summary>
    public const string SelectError = "ui.select.error";

    /// <summary>Default noun for one repeater row ("item"), used to build the buttons' labels.</summary>
    public const string ArrayItem = "ui.array.item";

    /// <summary>Label of a repeater's add button. Arg 0 is the item noun.</summary>
    public const string ArrayAdd = "ui.array.add";

    /// <summary>Accessible name of a row's remove button. Args: item noun, 1-based row number.</summary>
    public const string ArrayRemove = "ui.array.remove";

    /// <summary>Visible text of a row's remove button, with no room for the row number.</summary>
    public const string ArrayRemoveShort = "ui.array.remove.short";

    /// <summary>Live-region text announcing a repeater's size. Args: count, item noun.</summary>
    public const string ArrayCount = "ui.array.count";

    /// <summary>Label of a row's duplicate button. Args: item noun, 1-based row number.</summary>
    public const string ArrayDuplicate = "ui.array.duplicate";

    /// <summary>Label of a row's "move up" button. Args: item noun, 1-based row number.</summary>
    public const string ArrayMoveUp = "ui.array.moveUp";

    /// <summary>Label of a row's "move down" button. Args: item noun, 1-based row number.</summary>
    public const string ArrayMoveDown = "ui.array.moveDown";

    /// <summary>Text shown in place of an empty repeater. Arg 0 is the item noun.</summary>
    public const string ArrayEmpty = "ui.array.empty";

    /// <summary>
    /// Accessible name of one repeater row, so a screen reader says which row a control belongs to.
    /// Args: item noun, 1-based row number.
    /// </summary>
    public const string ArrayItemGroup = "ui.array.itemGroup";

    /// <summary>Accessible name of a paged repeater's pager. Arg 0 is the list's label.</summary>
    public const string ArrayPages = "ui.array.pages";

    /// <summary>Label of a paged repeater's "previous page" button.</summary>
    public const string ArrayPreviousPage = "ui.array.previousPage";

    /// <summary>Label of a paged repeater's "next page" button.</summary>
    public const string ArrayNextPage = "ui.array.nextPage";

    /// <summary>Accessible name of a paged repeater's page picker. Args: current page, number of pages.</summary>
    public const string ArrayPage = "ui.array.page";

    /// <summary>Which rows a page shows. Args: first row, last row, number of rows.</summary>
    public const string ArrayRange = "ui.array.range";

    /// <summary>How many rows on other pages need attention. Args: how many, item noun.</summary>
    public const string ArrayElsewhere = "ui.array.elsewhere";

    /// <summary>Label of the button that turns to the first row on another page that needs attention.</summary>
    public const string ArrayShowElsewhere = "ui.array.showElsewhere";

    /// <summary>Label of the button that clears a file field's selection.</summary>
    public const string FileClear = "ui.file.clear";

    /// <summary>How far a file's upload has got. Args: file name, percent.</summary>
    public const string UploadProgress = "ui.upload.progress";

    /// <summary>Said once a file has been uploaded. Arg 0 is the file name.</summary>
    public const string UploadDone = "ui.upload.done";

    /// <summary>Said when a file could not be uploaded. Arg 0 is the file name.</summary>
    public const string UploadFailed = "ui.upload.failed";

    /// <summary>The button that stops an upload. Arg 0 is the file name.</summary>
    public const string UploadCancel = "ui.upload.cancel";

    /// <summary>The button that tries a failed upload again. Arg 0 is the file name.</summary>
    public const string UploadRetry = "ui.upload.retry";

    /// <summary>The visible text of the button that stops an upload.</summary>
    public const string UploadCancelShort = "ui.upload.cancel.short";

    /// <summary>The visible text of the button that tries a failed upload again.</summary>
    public const string UploadRetryShort = "ui.upload.retry.short";

    /// <summary>The button that takes back an uploaded file. Arg 0 is the file name.</summary>
    public const string UploadRemove = "ui.upload.remove";

    /// <summary>Heading of the error summary when it lists exactly one problem.</summary>
    public const string SummaryTitleOne = "ui.summary.title.one";

    /// <summary>Heading of the error summary. Arg 0 is the number of problems.</summary>
    public const string SummaryTitleMany = "ui.summary.title.many";

    /// <summary>Label of the submit button.</summary>
    public const string Submit = "ui.submit";

    /// <summary>
    /// Label of the submit button while the submit is in flight. A button that only goes grey reads as
    /// broken rather than busy, and says nothing at all to a screen reader.
    /// </summary>
    public const string Submitting = "ui.submitting";

    /// <summary>Label of the reset button.</summary>
    public const string Reset = "ui.reset";

    /// <summary>Label of a wizard's "back" button.</summary>
    public const string Back = "ui.back";

    /// <summary>Label of a wizard's "next" button.</summary>
    public const string Next = "ui.next";

    /// <summary>Accessible name of a wizard's stepper landmark.</summary>
    public const string Progress = "ui.progress";

    /// <summary>Announced when the wizard's position changes. Args: current step, total visible steps.</summary>
    public const string StepOf = "ui.step.of";

    /// <summary>Read beside a tab or section holding errors the user has been shown. Arg 0 is how many.</summary>
    public const string StepErrors = "ui.step.errors";

    /// <summary>Accessible name of the button that reveals a password. </summary>
    public const string PasswordShow = "ui.password.show";

    /// <summary>Accessible name of the button that hides a revealed password.</summary>
    public const string PasswordHide = "ui.password.hide";

    /// <summary>Accessible name of the button that empties a field.</summary>
    public const string Clear = "ui.clear";

    /// <summary>Character counter beneath a length-limited input. Args: used, limit.</summary>
    public const string CharacterCount = "ui.characters";

    /// <summary>
    /// Announced to assistive technology as a length-limited field approaches its limit. Arg 0 is the
    /// number of characters left. The visible counter is decorative; this is what a screen-reader user
    /// actually hears.
    /// </summary>
    public const string CharactersRemaining = "ui.characters.remaining";

    /// <summary>Announced once a length-limited field is over its limit. Arg 0 is the excess.</summary>
    public const string CharactersOver = "ui.characters.over";

    /// <summary>
    /// Announced by a combobox as its list narrows. Arg 0 is the number of matching choices. The
    /// browser filters a <c>&lt;datalist&gt;</c> silently, so without this a screen-reader user typing
    /// into the box cannot tell whether anything matches.
    /// </summary>
    public const string ComboboxAvailable = "ui.combobox.available";

    /// <summary>
    /// Announced by a combobox that searches its options before enough has been typed for a search to
    /// run. Arg 0 is how many characters it needs.
    /// </summary>
    public const string ComboboxSearchHint = "ui.combobox.searchHint";

    /// <summary>Accessible name of a tag's remove button. Arg 0 is the tag.</summary>
    public const string TagRemove = "ui.tags.remove";

    /// <summary>Accessible name of the box a new tag is typed into. Arg 0 is the field's label.</summary>
    public const string TagEntry = "ui.tags.entry";

    /// <summary>Placeholder shown in the tag entry box.</summary>
    public const string TagPlaceholder = "ui.tags.placeholder";

    /// <summary>Live-region text announcing how many tags there are. Arg 0 is the count.</summary>
    public const string TagCount = "ui.tags.count";

    /// <summary>Message for a combobox answered with something that is not one of its choices.</summary>
    public const string OneOf = "oneOf";

    /// <summary>
    /// Read to assistive technology for a required group of controls — a multi-select or a tag list —
    /// whose role cannot carry <c>aria-required</c> itself, and whose visible asterisk is decorative.
    /// </summary>
    public const string RequiredHint = "ui.required";

    /// <summary>What <c>BormUnsavedChangesGuard</c> asks before the user navigates away from unsaved changes.</summary>
    public const string UnsavedChanges = "ui.unsavedChanges";

    /// <summary>Read before an error message ("Error:"), so it is not told from a warning by colour alone.</summary>
    public const string SeverityError = "ui.severity.error";

    /// <summary>Shown and read before a warning ("Warning:").</summary>
    public const string SeverityWarning = "ui.severity.warning";

    /// <summary>Shown and read before an informational message ("Note:").</summary>
    public const string SeverityInfo = "ui.severity.info";

    /// <summary>A true answer to a yes/no question, as a review reads it back.</summary>
    public const string Yes = "ui.yes";

    /// <summary>A false answer to a yes/no question, as a review reads it back.</summary>
    public const string No = "ui.no";

    /// <summary>What a review shows for a question nobody answered.</summary>
    public const string ReviewEmpty = "ui.review.empty";

    /// <summary>What a review shows in place of a sensitive answer — a password, a card number.</summary>
    public const string ReviewHidden = "ui.review.hidden";

    /// <summary>The visible text of a review's "change this step" button.</summary>
    public const string ReviewChange = "ui.review.change";

    /// <summary>The accessible name of a review's "change this step" button. Arg 0 is the step's title.</summary>
    public const string ReviewChangeStep = "ui.review.changeStep";

    /// <summary>The button that opens a Markdown field's preview.</summary>
    public const string MarkdownShowPreview = "ui.markdown.showPreview";

    /// <summary>The button that closes a Markdown field's preview.</summary>
    public const string MarkdownHidePreview = "ui.markdown.hidePreview";

    /// <summary>The accessible name of a Markdown field's preview.</summary>
    public const string MarkdownPreview = "ui.markdown.preview";

    /// <summary>What the preview of an empty Markdown field says.</summary>
    public const string MarkdownEmpty = "ui.markdown.empty";

    /// <summary>The summary of a Markdown field's formatting guide.</summary>
    public const string MarkdownHelp = "ui.markdown.help";

    /// <summary>Bold, in the formatting guide.</summary>
    public const string MarkdownBold = "ui.markdown.bold";

    /// <summary>Italic, in the formatting guide.</summary>
    public const string MarkdownItalic = "ui.markdown.italic";

    /// <summary>Code, in the formatting guide.</summary>
    public const string MarkdownCode = "ui.markdown.code";

    /// <summary>A link, in the formatting guide.</summary>
    public const string MarkdownLink = "ui.markdown.link";

    /// <summary>A bulleted list item, in the formatting guide.</summary>
    public const string MarkdownBulleted = "ui.markdown.bulleted";

    /// <summary>A numbered list item, in the formatting guide.</summary>
    public const string MarkdownNumbered = "ui.markdown.numbered";

    /// <summary>A quote, in the formatting guide.</summary>
    public const string MarkdownQuote = "ui.markdown.quote";

    /// <summary>A heading, in the formatting guide.</summary>
    public const string MarkdownHeading = "ui.markdown.heading";

    /// <summary>The accessible name of the signature pad.</summary>
    public const string SignaturePad = "ui.signature.pad";

    /// <summary>The button that clears a signature.</summary>
    public const string SignatureClear = "ui.signature.clear";

    /// <summary>The toggle that types a signature instead of drawing it.</summary>
    public const string SignatureTypeInstead = "ui.signature.typeInstead";

    /// <summary>The accessible name of the box a signature is typed into.</summary>
    public const string SignatureTypeLabel = "ui.signature.typeLabel";

    /// <summary>The alternative text of a signature shown as an image. Arg 0 is the field's label.</summary>
    public const string SignatureImage = "ui.signature.image";

    // --- The form designer (BormDesigner) ---

    /// <summary>Heading of the designer's list of fields.</summary>
    public const string DesignerFields = "designer.fields";

    /// <summary>Heading of the designer's palette of fields to add.</summary>
    public const string DesignerAdd = "designer.add";

    /// <summary>Heading of the designer's properties of the chosen field.</summary>
    public const string DesignerProperties = "designer.properties";

    /// <summary>Heading of the designer's settings of the form itself.</summary>
    public const string DesignerForm = "designer.form";

    /// <summary>Heading of the designer's live preview.</summary>
    public const string DesignerPreview = "designer.preview";

    /// <summary>Heading of the designer's JSON Schema of the form.</summary>
    public const string DesignerJson = "designer.json";

    /// <summary>The button that loads the JSON Schema typed into the designer.</summary>
    public const string DesignerImport = "designer.import";

    /// <summary>Said when the JSON Schema given to the designer cannot be read. Arg 0 is why.</summary>
    public const string DesignerImportFailed = "designer.importFailed";

    /// <summary>What the designer's list says before any field is added.</summary>
    public const string DesignerEmpty = "designer.empty";

    /// <summary>What the properties panel says while no field is chosen.</summary>
    public const string DesignerNothingSelected = "designer.nothingSelected";

    /// <summary>Accessible name of the button that chooses a field to edit. Arg 0 is the field's label.</summary>
    public const string DesignerEdit = "designer.edit";

    /// <summary>Accessible name of the button that moves a field up. Arg 0 is the field's label.</summary>
    public const string DesignerMoveUp = "designer.moveUp";

    /// <summary>Accessible name of the button that moves a field down. Arg 0 is the field's label.</summary>
    public const string DesignerMoveDown = "designer.moveDown";

    /// <summary>Accessible name of the button that copies a field. Arg 0 is the field's label.</summary>
    public const string DesignerDuplicate = "designer.duplicate";

    /// <summary>Accessible name of the button that deletes a field. Arg 0 is the field's label.</summary>
    public const string DesignerDelete = "designer.delete";

    /// <summary>Announced once a field is added. Arg 0 is its label.</summary>
    public const string DesignerAdded = "designer.added";

    /// <summary>Announced once a field is deleted. Arg 0 is its label.</summary>
    public const string DesignerDeleted = "designer.deleted";

    /// <summary>Announced once a field is moved. Args: its label, its new position, how many fields there are.</summary>
    public const string DesignerMoved = "designer.moved";

    /// <summary>Said of a field holding settings made in code, which the designer keeps as they are.</summary>
    public const string DesignerKeepsCode = "designer.keepsCode";

    /// <summary>Said of a field name that is not a valid one.</summary>
    public const string DesignerInvalidName = "designer.invalidName";

    /// <summary>Said of a field name another field has.</summary>
    public const string DesignerDuplicateName = "designer.duplicateName";

    /// <summary>Said of an expression or a mask that does not read. Arg 0 is why.</summary>
    public const string DesignerInvalid = "designer.invalid";

    /// <summary>Help beneath the designer's expression boxes.</summary>
    public const string DesignerExpressionHelp = "designer.expressionHelp";

    /// <summary>Help beneath the designer's options box.</summary>
    public const string DesignerOptionsHelp = "designer.optionsHelp";

    /// <summary>The label of one of the designer's properties. Arg: the property, as <see cref="DesignerProperty"/> names it.</summary>
    public static string DesignerProperty(string property) => "designer.property." + property;

    /// <summary>The name of one of the kinds of field the designer offers - <c>text</c>, <c>select</c>, <c>signature</c>.</summary>
    public static string DesignerKind(string kind) => "designer.kind." + kind;

    /// <summary>The visible text of the Undo button.</summary>
    public const string Undo = "ui.undo";

    /// <summary>The visible text of the Redo button.</summary>
    public const string Redo = "ui.redo";

    /// <summary>The accessible name of the Undo button. Arg 0 is the label of the field the change was to.</summary>
    public const string UndoField = "ui.undo.field";

    /// <summary>The accessible name of the Redo button. Arg 0 is the label of the field the change was to.</summary>
    public const string RedoField = "ui.redo.field";

    /// <summary>Announced once a change is undone. Arg 0 is the field's label.</summary>
    public const string Undone = "ui.undone";

    /// <summary>Announced once a change is made again. Arg 0 is the field's label.</summary>
    public const string Redone = "ui.redone";
}

/// <summary>The English defaults used when no <see cref="IBormMessageProvider"/> is registered.</summary>
public sealed class BormDefaultMessageProvider : IBormMessageProvider
{
    /// <summary>A shared instance; the rules fall back to this when DI has nothing registered.</summary>
    public static readonly BormDefaultMessageProvider Instance = new();

    public string Get(string key, params object?[] args) => key switch
    {
        BormMessageKeys.Required => "This field is required.",
        BormMessageKeys.MinLength => $"Must be at least {Arg(args, 0)} characters.",
        BormMessageKeys.MaxLength => $"Must be at most {Arg(args, 0)} characters.",
        BormMessageKeys.RangeBetween => $"Must be between {Arg(args, 0)} and {Arg(args, 1)}.",
        BormMessageKeys.RangeMin => $"Must be at least {Arg(args, 0)}.",
        BormMessageKeys.RangeMax => $"Must be at most {Arg(args, 0)}.",
        BormMessageKeys.RangeAbove => $"Must be greater than {Arg(args, 0)}.",
        BormMessageKeys.RangeBelow => $"Must be less than {Arg(args, 0)}.",
        BormMessageKeys.Pattern => "Invalid format.",
        BormMessageKeys.PatternTimeout => "This value could not be checked; please simplify it.",
        BormMessageKeys.Email => "Enter a valid email address.",
        BormMessageKeys.Url => "Enter a valid URL.",
        BormMessageKeys.Compare => $"Must match {Arg(args, 0)}.",
        BormMessageKeys.MinItems => $"Add at least {Arg(args, 0)} item(s).",
        BormMessageKeys.MaxItems => $"No more than {Arg(args, 0)} item(s) allowed.",
        BormMessageKeys.UniqueItems => "Items must be unique.",
        BormMessageKeys.MultipleOf => $"Must be a multiple of {Arg(args, 0)}.",
        BormMessageKeys.FileSize => $"Each file must be {Arg(args, 0)} or smaller.",
        BormMessageKeys.FileType => $"Only these file types are allowed: {Arg(args, 0)}.",
        BormMessageKeys.Conversion => $"'{Arg(args, 0)}' is not a valid value for this field.",
        BormMessageKeys.Uuid => "Enter a valid UUID.",
        BormMessageKeys.Rule => "This value is not valid.",
        BormMessageKeys.Mask => $"Enter it in the form {Arg(args, 0)}.",

        BormMessageKeys.SelectPlaceholder => "-- Select --",
        BormMessageKeys.SelectLoading => "Loading…",
        BormMessageKeys.SelectError => "Options could not be loaded",
        BormMessageKeys.ArrayItem => "item",
        BormMessageKeys.ArrayAdd => $"Add {Arg(args, 0)}",
        BormMessageKeys.ArrayRemove => $"Remove {Arg(args, 0)} {Arg(args, 1)}",
        BormMessageKeys.ArrayRemoveShort => "Remove",
        BormMessageKeys.ArrayCount => $"{Arg(args, 0)} {Arg(args, 1)}{(Arg(args, 0) is 1 ? "" : "s")}",
        BormMessageKeys.ArrayDuplicate => $"Duplicate {Arg(args, 0)} {Arg(args, 1)}",
        BormMessageKeys.ArrayMoveUp => $"Move {Arg(args, 0)} {Arg(args, 1)} up",
        BormMessageKeys.ArrayMoveDown => $"Move {Arg(args, 0)} {Arg(args, 1)} down",
        BormMessageKeys.ArrayEmpty => $"No {Arg(args, 0)}s yet.",
        BormMessageKeys.ArrayItemGroup => $"{Arg(args, 0)} {Arg(args, 1)}",
        BormMessageKeys.ArrayPages => $"Pages of {Arg(args, 0)}",
        BormMessageKeys.ArrayPreviousPage => "Previous page",
        BormMessageKeys.ArrayNextPage => "Next page",
        BormMessageKeys.ArrayPage => $"Page {Arg(args, 0)} of {Arg(args, 1)}",
        BormMessageKeys.ArrayRange => $"{Arg(args, 0)}–{Arg(args, 1)} of {Arg(args, 2)}",
        BormMessageKeys.ArrayElsewhere => Arg(args, 0) is 1
            ? $"1 {Arg(args, 1)} on another page needs attention."
            : $"{Arg(args, 0)} {Arg(args, 1)}s on other pages need attention.",
        BormMessageKeys.ArrayShowElsewhere => "Show the first",
        BormMessageKeys.FileClear => "Clear selection",
        BormMessageKeys.UploadProgress => $"{Arg(args, 0)}: {Arg(args, 1)}% uploaded",
        BormMessageKeys.UploadDone => $"{Arg(args, 0)} uploaded",
        BormMessageKeys.UploadFailed => $"{Arg(args, 0)} could not be uploaded.",
        BormMessageKeys.UploadCancel => $"Cancel the upload of {Arg(args, 0)}",
        BormMessageKeys.UploadRetry => $"Try {Arg(args, 0)} again",
        BormMessageKeys.UploadRemove => $"Remove {Arg(args, 0)}",
        BormMessageKeys.UploadCancelShort => "Cancel",
        BormMessageKeys.UploadRetryShort => "Retry",
        BormMessageKeys.SummaryTitleOne => "There is a problem with this form:",
        BormMessageKeys.SummaryTitleMany => $"There are {Arg(args, 0)} problems with this form:",
        BormMessageKeys.Submit => "Submit",
        BormMessageKeys.Submitting => "Submitting…",
        BormMessageKeys.Reset => "Reset",
        BormMessageKeys.Back => "Back",
        BormMessageKeys.Next => "Next",
        BormMessageKeys.Progress => "Progress",
        BormMessageKeys.StepOf => $"Step {Arg(args, 0)} of {Arg(args, 1)}",
        BormMessageKeys.StepErrors => Arg(args, 0) is 1 ? "1 problem to fix" : $"{Arg(args, 0)} problems to fix",
        BormMessageKeys.PasswordShow => "Show password",
        BormMessageKeys.PasswordHide => "Hide password",
        BormMessageKeys.Clear => "Clear",
        BormMessageKeys.CharacterCount => $"{Arg(args, 0)} / {Arg(args, 1)}",
        BormMessageKeys.CharactersRemaining => $"{Arg(args, 0)} characters remaining",
        BormMessageKeys.CharactersOver => $"{Arg(args, 0)} characters over the limit",
        BormMessageKeys.ComboboxAvailable => $"{Arg(args, 0)} choices available",
        BormMessageKeys.ComboboxSearchHint => $"Type at least {Arg(args, 0)} characters to search",
        BormMessageKeys.TagRemove => $"Remove {Arg(args, 0)}",
        BormMessageKeys.TagEntry => $"Add to {Arg(args, 0)}",
        BormMessageKeys.TagPlaceholder => "Add…",
        BormMessageKeys.TagCount => $"{Arg(args, 0)} selected",
        BormMessageKeys.OneOf => "Choose one of the available options.",
        BormMessageKeys.RequiredHint => "Required",
        BormMessageKeys.UnsavedChanges => "You have unsaved changes. Leave this page anyway?",
        BormMessageKeys.SeverityError => "Error:",
        BormMessageKeys.SeverityWarning => "Warning:",
        BormMessageKeys.SeverityInfo => "Note:",
        BormMessageKeys.Yes => "Yes",
        BormMessageKeys.No => "No",
        BormMessageKeys.ReviewEmpty => "Not answered",
        BormMessageKeys.ReviewHidden => "Not shown",
        BormMessageKeys.ReviewChange => "Change",
        BormMessageKeys.ReviewChangeStep => $"Change {Arg(args, 0)}",
        BormMessageKeys.MarkdownShowPreview => "Show preview",
        BormMessageKeys.MarkdownHidePreview => "Hide preview",
        BormMessageKeys.MarkdownPreview => "Preview",
        BormMessageKeys.MarkdownEmpty => "Nothing to preview yet.",
        BormMessageKeys.MarkdownHelp => "Formatting help",
        BormMessageKeys.MarkdownBold => "bold",
        BormMessageKeys.MarkdownItalic => "italic",
        BormMessageKeys.MarkdownCode => "code",
        BormMessageKeys.MarkdownLink => "link",
        BormMessageKeys.MarkdownBulleted => "bulleted list",
        BormMessageKeys.MarkdownNumbered => "numbered list",
        BormMessageKeys.MarkdownQuote => "quote",
        BormMessageKeys.MarkdownHeading => "heading",
        BormMessageKeys.SignaturePad => "Draw your signature here with a mouse, a finger or a pen",
        BormMessageKeys.SignatureClear => "Clear",
        BormMessageKeys.SignatureTypeInstead => "Type it instead",
        BormMessageKeys.SignatureTypeLabel => "Type your full name",
        BormMessageKeys.SignatureImage => $"{Arg(args, 0)}: signature",
        BormMessageKeys.DesignerFields => "Fields",
        BormMessageKeys.DesignerAdd => "Add a field",
        BormMessageKeys.DesignerProperties => "Properties",
        BormMessageKeys.DesignerForm => "Form",
        BormMessageKeys.DesignerPreview => "Preview",
        BormMessageKeys.DesignerJson => "JSON Schema",
        BormMessageKeys.DesignerImport => "Load this JSON",
        BormMessageKeys.DesignerImportFailed => $"That JSON could not be read: {Arg(args, 0)}",
        BormMessageKeys.DesignerEmpty => "No fields yet. Add one from the list.",
        BormMessageKeys.DesignerNothingSelected => "Choose a field to see its properties.",
        BormMessageKeys.DesignerEdit => $"Edit {Arg(args, 0)}",
        BormMessageKeys.DesignerMoveUp => $"Move {Arg(args, 0)} up",
        BormMessageKeys.DesignerMoveDown => $"Move {Arg(args, 0)} down",
        BormMessageKeys.DesignerDuplicate => $"Duplicate {Arg(args, 0)}",
        BormMessageKeys.DesignerDelete => $"Delete {Arg(args, 0)}",
        BormMessageKeys.DesignerAdded => $"Added {Arg(args, 0)}.",
        BormMessageKeys.DesignerDeleted => $"Deleted {Arg(args, 0)}.",
        BormMessageKeys.DesignerMoved => $"{Arg(args, 0)} is now number {Arg(args, 1)} of {Arg(args, 2)}.",
        BormMessageKeys.DesignerKeepsCode => "This field has settings made in code - an options provider, a condition or a rule - which are kept as they are.",
        BormMessageKeys.DesignerInvalidName => "Start with a letter; then letters, digits or _.",
        BormMessageKeys.DesignerDuplicateName => "Another field already has this name.",
        BormMessageKeys.DesignerInvalid => $"This does not read: {Arg(args, 0)}",
        BormMessageKeys.DesignerExpressionHelp => "An expression, such as {Country} = 'NL' or {Quantity} * {Price}.",
        BormMessageKeys.DesignerOptionsHelp => "One option per line: value = Label, or the value alone.",
        _ when key.StartsWith("designer.property.", StringComparison.Ordinal) => DesignerProperties.GetValueOrDefault(key[18..], key[18..]),
        _ when key.StartsWith("designer.kind.", StringComparison.Ordinal) => DesignerKinds.GetValueOrDefault(key[14..], key[14..]),
        BormMessageKeys.Undo => "Undo",
        BormMessageKeys.Redo => "Redo",
        BormMessageKeys.UndoField => $"Undo the change to {Arg(args, 0)}",
        BormMessageKeys.RedoField => $"Redo the change to {Arg(args, 0)}",
        BormMessageKeys.Undone => $"Undone: {Arg(args, 0)}",
        BormMessageKeys.Redone => $"Redone: {Arg(args, 0)}",

        _ => key
    };

    private static object? Arg(object?[] args, int index) => index < args.Length ? args[index] : null;

    internal static readonly Dictionary<string, string> DesignerProperties = new(StringComparer.Ordinal)
    {
        ["kind"] = "Kind of field", ["name"] = "Name", ["label"] = "Label", ["help"] = "Help text",
        ["placeholder"] = "Placeholder", ["text"] = "Text", ["required"] = "Required", ["readOnly"] = "Read-only",
        ["minLength"] = "Shortest", ["maxLength"] = "Longest", ["min"] = "Smallest", ["max"] = "Largest",
        ["pattern"] = "Pattern", ["mask"] = "Mask", ["options"] = "Options", ["visibleWhen"] = "Show when",
        ["requiredWhen"] = "Required when", ["computed"] = "Computed as", ["group"] = "Group",
        ["columnSpan"] = "Columns spanned", ["title"] = "Title", ["description"] = "Description", ["columns"] = "Columns"
    };

    internal static readonly Dictionary<string, string> DesignerKinds = new(StringComparer.Ordinal)
    {
        ["text"] = "Text", ["textarea"] = "Paragraph", ["markdown"] = "Formatted text", ["email"] = "Email",
        ["password"] = "Password", ["url"] = "Web address", ["tel"] = "Phone", ["number"] = "Number",
        ["integer"] = "Whole number", ["range"] = "Slider", ["date"] = "Date", ["datetime"] = "Date and time",
        ["time"] = "Time", ["month"] = "Month", ["week"] = "Week", ["checkbox"] = "Checkbox", ["switch"] = "Switch",
        ["select"] = "Dropdown", ["radio"] = "Choice", ["combobox"] = "Combobox", ["multiselect"] = "Several choices",
        ["tags"] = "Tags", ["color"] = "Colour", ["file"] = "File", ["signature"] = "Signature", ["static"] = "Heading and text"
    };
}

/// <summary>How the library resolves its own words, in one place.</summary>
internal static class BormMessageText
{
    /// <summary>The registered provider, else the English defaults.</summary>
    internal static IBormMessageProvider Provider(IServiceProvider? services)
        => services?.GetService(typeof(IBormMessageProvider)) as IBormMessageProvider ?? BormDefaultMessageProvider.Instance;

    /// <summary>
    /// The text for <paramref name="key"/> in <paramref name="language"/> when there is one and the provider can answer in
    /// a language of its choosing, else in the provider's own (the current UI culture's, for most).
    /// </summary>
    internal static string Get(IBormMessageProvider provider, string? language, string key, object?[] args)
        => language is not null && provider is IBormLocalizedMessageProvider localized
            ? localized.Get(language, key, args)
            : provider.Get(key, args);
}
