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

    /// <summary>Label of the button that clears a file field's selection.</summary>
    public const string FileClear = "ui.file.clear";

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
        BormMessageKeys.FileClear => "Clear selection",
        BormMessageKeys.SummaryTitleOne => "There is a problem with this form:",
        BormMessageKeys.SummaryTitleMany => $"There are {Arg(args, 0)} problems with this form:",
        BormMessageKeys.Submit => "Submit",
        BormMessageKeys.Submitting => "Submitting…",
        BormMessageKeys.Reset => "Reset",
        BormMessageKeys.Back => "Back",
        BormMessageKeys.Next => "Next",
        BormMessageKeys.Progress => "Progress",
        BormMessageKeys.StepOf => $"Step {Arg(args, 0)} of {Arg(args, 1)}",
        BormMessageKeys.PasswordShow => "Show password",
        BormMessageKeys.PasswordHide => "Hide password",
        BormMessageKeys.Clear => "Clear",
        BormMessageKeys.CharacterCount => $"{Arg(args, 0)} / {Arg(args, 1)}",
        BormMessageKeys.CharactersRemaining => $"{Arg(args, 0)} characters remaining",
        BormMessageKeys.CharactersOver => $"{Arg(args, 0)} characters over the limit",
        BormMessageKeys.ComboboxAvailable => $"{Arg(args, 0)} choices available",
        BormMessageKeys.TagRemove => $"Remove {Arg(args, 0)}",
        BormMessageKeys.TagEntry => $"Add to {Arg(args, 0)}",
        BormMessageKeys.TagPlaceholder => "Add…",
        BormMessageKeys.TagCount => $"{Arg(args, 0)} selected",
        BormMessageKeys.OneOf => "Choose one of the available options.",
        BormMessageKeys.RequiredHint => "Required",

        _ => key
    };

    private static object? Arg(object?[] args, int index) => index < args.Length ? args[index] : null;
}
