//+:cnd:noEmit
using Boilerplate.Client.Core;

namespace Boilerplate.Client.Windows;

public class ClientWindowsSettings : ClientCoreSettings
{
    /// <summary>
    /// When the Windows app sends a request to the API server, and the API server and web app are hosted on different URLs,
    /// the origin of the generated links (e.g., email confirmation links) will depend on `WebAppUrl` value.
    /// </summary>
    public Uri? WebAppUrl { get; set; }

    public WindowsUpdateOptions? WindowsUpdate { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        if (WindowsUpdate is not null)
        {
            Validator.TryValidateObject(WindowsUpdate, new ValidationContext(WindowsUpdate), validationResults, true);
        }

        return validationResults;
    }
}

public partial class WindowsUpdateOptions
{
    public bool AutoReload { get; set; }

    public string? FilesUrl { get; set; }
}
