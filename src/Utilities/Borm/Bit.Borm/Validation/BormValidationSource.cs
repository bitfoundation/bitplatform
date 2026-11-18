namespace Bit.Borm;

/// <summary>Where a <see cref="BormValidationMessage"/> came from.</summary>
public enum BormValidationSource
{
    /// <summary>One of the schema's own rules, a field's or the form's.</summary>
    Rule,

    /// <summary>An external validator — FluentValidation, DataAnnotations' <c>IValidatableObject</c>, or one of your own.</summary>
    External,

    /// <summary>The server, through <c>SetServerError</c> or <c>SetServerErrors</c>.</summary>
    Server,

    /// <summary>An entry the model could not accept — <c>abc</c> typed into a number.</summary>
    Conversion
}
