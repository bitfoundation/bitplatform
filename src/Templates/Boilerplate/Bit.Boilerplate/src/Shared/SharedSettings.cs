//+:cnd:noEmit
namespace Boilerplate.Shared;

public partial class SharedSettings : IValidatableObject
{
    //#if (appInsights == true)
    public ApplicationInsightsOptions? ApplicationInsights { get; set; }
    //#endif

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        //#if (appInsights == true)
        if (ApplicationInsights is not null)
        {
            Validator.TryValidateObject(ApplicationInsights, new ValidationContext(ApplicationInsights), validationResults, true);
        }
        //#endif

        return validationResults;
    }
}

//#if (appInsights == true)
public class ApplicationInsightsOptions
{
    public string? ConnectionString { get; set; }
}
