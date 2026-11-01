namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts.Data;

/// <summary>
/// One shift: who works it, in which role, and from when to when. A sample shift names itself with TitleKey, so it
/// reads in every language; Title is a name the user typed.
/// </summary>
public sealed record Shift(string Id, string StaffKey, ShiftRole Role, DateTime Start, DateTime End, string? TitleKey = null, string? Title = null);
