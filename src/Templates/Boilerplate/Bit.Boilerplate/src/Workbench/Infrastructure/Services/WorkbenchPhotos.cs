namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>The sample portraits of the demo users and the point-of-sale staff, found by the resource key of their names.</summary>
public static class WorkbenchPhotos
{
    private static readonly Dictionary<string, string> files = new()
    {
        [nameof(WorkbenchStrings.DemoUserYas)] = "yas",
        [nameof(WorkbenchStrings.DemoUserSaleh)] = "saleh",
        [nameof(WorkbenchStrings.DemoUserZahra)] = "zahra",
        [nameof(WorkbenchStrings.PosStaffEmma)] = "emma",
        [nameof(WorkbenchStrings.PosStaffLucas)] = "lucas",
        [nameof(WorkbenchStrings.PosStaffMila)] = "mila",
        [nameof(WorkbenchStrings.PosStaffSem)] = "sem",
        [nameof(WorkbenchStrings.PosStaffNoah)] = "noah",
        [nameof(WorkbenchStrings.PosStaffJulia)] = "julia"
    };

    public static string? Find(string nameKey) => files.TryGetValue(nameKey, out var file) ? $"images/people/{file}.svg" : null;
}
