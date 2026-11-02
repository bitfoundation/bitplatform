using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.Borm.Samples.Core;

/// <summary>
/// Stand-in for a service the forms call out to: the builder sample's async rule asks it whether a username is free.
/// On Server the call runs on the circuit, on WebAssembly in the browser, and in the MAUI app in the app's process.
/// </summary>
public class SampleDirectory
{
    private static readonly string[] TakenUsernames = ["ada", "admin", "root", "grace"];

    public static readonly string[] Destinations = ["Amsterdam", "Lisbon", "Reykjavik", "Tokyo", "Vancouver"];

    public async ValueTask<bool> IsUsernameAvailableAsync(string username, CancellationToken cancellationToken = default)
    {
        // A network call's worth of waiting, cancelled when the user types again before it ends.
        await Task.Delay(400, cancellationToken);
        return TakenUsernames.Contains(username, StringComparer.OrdinalIgnoreCase) is false;
    }

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    /// <summary>What a valid submit handed the page, as the pages show it.</summary>
    public static string ToJson(object? answers) => JsonSerializer.Serialize(answers, Indented);
}
