namespace Bit.Bheadless;

/// <summary>
/// The button that steps a <see cref="BitBhNumberField{TValue}"/> down (named "Decrease" unless you give it an
/// <c>aria-label</c>). Out of the tab order, repeats while held, disabled at the minimum.
/// </summary>
public class BitBhNumberFieldDecrement : BitBhNumberFieldStepperBase
{
    protected override int Direction => -1;

    protected override string DefaultLabel => Strings.Decrease;
}
