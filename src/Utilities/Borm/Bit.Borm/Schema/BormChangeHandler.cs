namespace Bit.Borm;

/// <summary>
/// Runs when a field's value changes — the schema's own "and when this changes, do that". Clearing the
/// city when the country changes, resetting a seat count when the plan does, stamping a timestamp: the
/// things every form ends up needing and that otherwise have to be wired up outside the schema by
/// subscribing to <see cref="BormState.FieldChanged"/> and working out which path fired.
/// </summary>
/// <remarks>
/// This is the missing third of a set. <see cref="BormComputedValue"/> derives a value *from*
/// others, <see cref="BormFieldDefinition.OptionsDependencies"/> reloads choices, and
/// <see cref="BormFieldDefinition.ClearOnHide"/> empties a hidden branch — but "when A changes,
/// write B" fits none of them, because B is still the user's to edit afterwards. TanStack Form calls
/// these listeners; Formly calls them hooks.
/// </remarks>
public delegate void BormChangeHandler(BormChangeContext context);
