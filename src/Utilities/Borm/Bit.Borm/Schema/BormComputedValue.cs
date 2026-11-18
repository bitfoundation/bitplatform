namespace Bit.Borm;

/// <summary>
/// Derives a field's value from the rest of the form. This is the "calculated value" every form
/// builder ends up needing — a line total, a full name, a price after discount — expressed once in
/// the schema instead of being wired up by hand in each page that renders it.
/// </summary>
public delegate object? BormComputedValue(BormComputedContext context);
