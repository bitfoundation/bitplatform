namespace Bit.BevTools.Samples.Core;

/// <summary>
/// A service the State page injects, so the Components panel shows it among the page's services: the values that
/// look like secrets are redacted wherever BevTools shows them.
/// </summary>
public sealed class SampleSession
{
    public string UserName { get; set; } = "ada.lovelace";

    // Sensitive by its name: redacted everywhere.
    public string AccessToken { get; set; } = "eyJhbGciOiJIUzI1NiJ9.sample-token-never-shown";

    [BevToolsSensitive]
    public string Nickname { get; set; } = "redacted by attribute";

    // Not listed at all, not even as redacted.
    [BevToolsIgnore]
    public string SupportNotes { get; set; } = "never listed by BevTools";
}
