namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts.Data;

/// <summary>
/// A team member, their usual role and their week: one letter per day from the culture's first day of the week,
/// O opening, M midday, B bakery, C closing and - a day off.
/// </summary>
public sealed record ShiftStaff(string NameKey, ShiftRole Role, string Pattern);
