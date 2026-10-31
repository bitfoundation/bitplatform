namespace Boilerplate.Workbench.Components.Pages.Showcases.Shifts.Data;

public sealed record Shift(string Id, string StaffKey, ShiftRole Role, DateTime Start, DateTime End, string? TitleKey = null, string? Title = null);
