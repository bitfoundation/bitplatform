namespace Bit.Blow;

/// <summary>
/// A validation rule written as a function - one added with
/// <see cref="BlowGraphValidator.Add(Func{BlowValidationContext, IEnumerable{BlowValidationIssue}})"/>, or a built-in one.
/// </summary>
internal sealed class BlowDelegateRule(Func<BlowValidationContext, IEnumerable<BlowValidationIssue>> rule) : IBlowValidationRule
{
    public IEnumerable<BlowValidationIssue> Validate(BlowValidationContext context) => rule(context);
}
