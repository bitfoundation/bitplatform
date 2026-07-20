namespace Bit.Brouter.Demos.Core;

/// <summary>
/// One entry of the interactive constraint tester (see ConstraintsPage / ConstraintResultPage).
/// <c>Kind</c> is the literal URL segment identifying the demo route (<c>/c/{Kind}/...</c>),
/// <c>Token</c> the constraint text used in that route's template, and the examples are one value
/// that passes and one that fails, for one-click demonstrations.
/// </summary>
public sealed record ConstraintDemo(
    string Kind,
    string Token,
    string Category,
    string Rule,
    string PassExample,
    string FailExample);

/// <summary>
/// The single source of truth for the constraint-tester routes and the table that documents them:
/// AppRouter declares one <c>/c/{Kind}/{v:Token}</c> route per entry, ConstraintsPage renders the
/// docs/tester from the same list, and ConstraintResultPage looks entries up by <c>Kind</c>.
/// </summary>
public static class ConstraintCatalog
{
    public static readonly IReadOnlyList<ConstraintDemo> All =
    [
        // ---- type constraints: validate AND convert the bound value ----
        new("int", "int", "type", "Parses as int (invariant culture); binds an int.", "42", "abc"),
        new("long", "long", "type", "Parses as long; binds a long.", "9000000000", "abc"),
        new("bool", "bool", "type", "true / false, case-insensitive; binds a bool.", "TRUE", "yes"),
        new("guid", "guid", "type", "Any standard Guid format; binds a Guid.", "0f8fad5b-d9cb-469f-a165-70867728950e", "not-a-guid"),
        new("datetime", "datetime", "type", "Invariant-culture DateTime; binds a DateTime.", "2026-07-19", "tomorrow"),
        new("decimal", "decimal", "type", "Invariant-culture decimal; binds a decimal.", "49.99", "1.2.3"),

        // ---- validation constraints: accept/reject, value stays a string ----
        new("alpha", "alpha", "validation", "ASCII letters A-Z only (empty passes too).", "Hello", "h3llo"),
        new("min", "min(10)", "validation", "Numeric value ≥ 10.", "12", "9"),
        new("max", "max(10)", "validation", "Numeric value ≤ 10.", "9", "11"),
        new("range", "range(1,10)", "validation", "Numeric value between 1 and 10.", "5", "20"),
        new("minlength", "minlength(3)", "validation", "Text length ≥ 3.", "abcd", "ab"),
        new("maxlength", "maxlength(5)", "validation", "Text length ≤ 5.", "abc", "abcdef"),
        new("length", "length(2,4)", "validation", "Text length between 2 and 4 (length(4) = exact).", "abc", "a"),
        new("regex", @"regex(^[a-z]+\d+$)", "validation", "Matches the inline pattern (case-insensitive, invariant).", "abc123", "123abc"),
        new("file", "file", "validation", "Looks like a file name: a '.' with something after it.", "report.pdf", "report"),
        new("nonfile", "nonfile", "validation", "Does NOT look like a file name (classic static-asset filter).", "docs", "app.js"),

        // ---- custom constraint, registered on BrouterOptions.Constraints ----
        new("slug", "slug", "custom", "Custom: ≥ 3 chars, letters/digits/dashes only.", "hello-world", "a!"),

        // ---- chaining: the last TYPE constraint decides the bound value ----
        new("chain", "int:min(1):max(5)", "chain", "int AND 1..5 - binds an int, the validators just gate it.", "3", "9"),
    ];

    public static ConstraintDemo? Find(string? kind) =>
        kind is null ? null : All.FirstOrDefault(c => string.Equals(c.Kind, kind, StringComparison.OrdinalIgnoreCase));
}
