using System.Text;
using System.Buffers.Text;

namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchUrls
{
    public const string Home = "/";

    public const string Screenshots = "/screenshots";

    public static string Workbench(string pageKey) => $"/workbench/{pageKey}";

    public static string Preview(string pageKey, WorkbenchScenario scenario) => $"/preview/{pageKey}{scenario.ToQueryString()}";

    public static string Screenshot(string pageKey, WorkbenchScenario scenario) => $"/api/screenshots/{pageKey}{scenario.ToQueryString()}";

    public static string EncodePanes(IEnumerable<WorkbenchScenario> scenarios)
    {
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(string.Join('|', scenarios.Select(s => s.ToQueryString()))));
    }

    public static WorkbenchScenario[] DecodePanes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        try
        {
            return [.. Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value)).Split('|').Select(WorkbenchScenario.Parse)];
        }
        catch (FormatException)
        {
            return [];
        }
    }
}
