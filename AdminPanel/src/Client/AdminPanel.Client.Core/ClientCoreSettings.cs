
namespace AdminPanel.Client.Core;

public partial class ClientCoreSettings : SharedSettings
{
    /// <summary>
    /// If you're running AdminPanel.Server.Web project, then you can also use relative urls such as / for Blazor Server and WebAssembly
    /// </summary>
    [Required]
    public string ServerAddress { get; set; } = default!;


    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();


        return validationResults;
    }
}
