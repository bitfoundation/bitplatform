using AdminPanel.Client.Core;

namespace AdminPanel.Client.Windows;

public class ClientWindowsSettings : ClientCoreSettings
{
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
