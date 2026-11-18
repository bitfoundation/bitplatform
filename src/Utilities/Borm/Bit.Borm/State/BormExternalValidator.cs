namespace Bit.Borm;

/// <summary>
/// Optional external validator (e.g. FluentValidation) invoked alongside the schema's built-in rules.
/// </summary>
public delegate ValueTask<IReadOnlyList<BormValidationMessage>> BormExternalValidator(
    BormDefinition form, IBormDataReader data, IServiceProvider? services);
