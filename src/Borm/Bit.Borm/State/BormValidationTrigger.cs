namespace Bit.Borm;

/// <summary>
/// When a field revalidates. Mirrors the <c>mode</c> / <c>reValidateMode</c> split popularised by
/// React Hook Form: a form can stay quiet until the first submit and then become eager, which is the
/// combination users find least noisy.
/// </summary>
public enum BormValidationTrigger
{
    /// <summary>Only when the form is submitted (or a wizard step advances).</summary>
    OnSubmit,

    /// <summary>When the field loses focus, and on submit.</summary>
    OnBlur,

    /// <summary>When the value changes, when it loses focus, and on submit. The default.</summary>
    OnChange
}
