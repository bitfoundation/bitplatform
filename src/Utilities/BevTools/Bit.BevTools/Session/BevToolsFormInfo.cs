using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BevTools;

/// <summary>A live form - an <see cref="EditContext"/> - and what BevTools observed of it.</summary>
/// <param name="Id">BevTools' number for the form in this session.</param>
/// <param name="ModelType">The type of the form's model.</param>
/// <param name="OwnerInstanceId">The component the form is on: the one rendering its EditForm.</param>
/// <param name="OwnerName">That component's name.</param>
/// <param name="Modified">Whether any field is marked modified.</param>
/// <param name="MessageCount">How many validation messages the form holds now.</param>
/// <param name="Fields">Its fields: the ones the EditContext tracks, and the ones that notified a change.</param>
/// <param name="Validations">How many times the form was validated while BevTools watched it.</param>
/// <param name="LastValidatedAt">When it was last validated.</param>
/// <param name="LastValidationPassed">Whether that validation left no message; null before the first.</param>
/// <param name="Validators">What answers its validation requests (<c>DataAnnotationsValidator</c>, a custom validator); empty when nothing does.</param>
/// <param name="MessageDisplays">The <c>ValidationSummary</c> and <c>ValidationMessage</c> components listening to it.</param>
/// <param name="ValidationAttributes">The model's members carrying validation attributes ("Email: Required, EmailAddress").</param>
public sealed record BevToolsFormInfo(
    int Id,
    string ModelType,
    long? OwnerInstanceId,
    string? OwnerName,
    IReadOnlyList<string> Validators,
    IReadOnlyList<string> MessageDisplays,
    bool Modified,
    int MessageCount,
    IReadOnlyList<BevToolsFormField> Fields,
    int Validations,
    DateTimeOffset? LastValidatedAt,
    bool? LastValidationPassed,
    IReadOnlyList<string> ValidationAttributes)
{
    /// <summary>
    /// The EditForm has an <c>OnSubmit</c> handler: the code decides when and how the form is validated (it may call
    /// <c>Validator.TryValidateObject</c> itself), so no validator component is not a defect.
    /// </summary>
    public bool HandlesSubmit { get; init; }
}
