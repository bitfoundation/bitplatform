namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchPhotos
{
    private static readonly Dictionary<string, string> files = new()
    {
        [nameof(WorkbenchStrings.DemoUserSarah)] = "sarah",
        [nameof(WorkbenchStrings.DemoUserDaan)] = "daan",
        [nameof(WorkbenchStrings.DemoUserNiloofar)] = "niloofar",
        [nameof(WorkbenchStrings.PosStaffEmma)] = "emma",
        [nameof(WorkbenchStrings.PosStaffLucas)] = "lucas",
        [nameof(WorkbenchStrings.PosStaffMila)] = "mila",
        [nameof(WorkbenchStrings.PosStaffSem)] = "sem",
        [nameof(WorkbenchStrings.PosStaffNoah)] = "noah",
        [nameof(WorkbenchStrings.PosStaffJulia)] = "julia"
    };

    public static string? Find(string nameKey) => files.TryGetValue(nameKey, out var file) ? $"images/people/{file}.svg" : null;
}
