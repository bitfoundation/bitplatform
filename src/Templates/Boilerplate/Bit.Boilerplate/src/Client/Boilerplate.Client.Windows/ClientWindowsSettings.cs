//+:cnd:noEmit
using Boilerplate.Client.Core;

namespace Boilerplate.Client.Windows;

public class ClientWindowsSettings : ClientCoreSettings
{
    /// <summary>
    /// Specify the web app url in if API server and web app are hosted separately for proper link/url generation (e.g., email confirmation, social sign-in).
    /// </summary>
    public string? WebAppUrl { get; set; }

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
