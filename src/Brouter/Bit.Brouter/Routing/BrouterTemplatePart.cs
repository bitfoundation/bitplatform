namespace Bit.Brouter;

/// <summary>
/// One piece of a complex (multi-part) template segment such as <c>{name}.{ext?}</c> or
/// <c>v{major}-{minor}</c>: a literal run of characters, the '.' separator that precedes an
/// optional last parameter, or a parameter.
/// </summary>
internal sealed class BrouterTemplatePart
{
    public bool IsParameter { get; private init; }

    /// <summary>
    /// True for the literal '.' that immediately precedes an optional last parameter. The separator
    /// is dropped together with the optional value when the URL omits it, so <c>{name}.{ext?}</c>
    /// matches both "doc.txt" and "doc".
    /// </summary>
    public bool IsSeparator { get; private init; }

    /// <summary>The literal/separator text, or the parameter name.</summary>
    public string Value { get; private init; } = "";

    /// <summary>True for an optional last part, e.g. <c>{ext?}</c> in <c>{name}.{ext?}</c>.</summary>
    public bool IsOptional { get; private init; }

    /// <summary>
    /// Declared default value; parsed for template fidelity and used by URL generation. Matching
    /// ignores it - a complex-segment parameter always needs at least one character (framework parity).
    /// </summary>
    public string? DefaultValue { get; private init; }

    public BrouterRouteConstraintBinding[] Constraints { get; private init; } = [];

    public static BrouterTemplatePart Literal(string text) => new() { Value = text };

    public static BrouterTemplatePart Separator() => new() { Value = ".", IsSeparator = true };

    public static BrouterTemplatePart Parameter(string name, bool isOptional, string? defaultValue, BrouterRouteConstraintBinding[] constraints) =>
        new() { IsParameter = true, Value = name, IsOptional = isOptional, DefaultValue = defaultValue, Constraints = constraints };
}
