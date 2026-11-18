namespace Bit.Bheadless;

/// <summary>
/// The button that steps a <see cref="BitBhNumberField{TValue}"/> up (named "Increase" unless you give it an
/// <c>aria-label</c>). Out of the tab order, repeats while held, disabled at the maximum.
/// </summary>
public class BitBhNumberFieldIncrement : BitBhNumberFieldStepperBase
{
    protected override int Direction => 1;

    protected override string DefaultLabel => Strings.Increase;
}
