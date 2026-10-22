//+:cnd:noEmit
using Microsoft.Extensions.Options;

namespace Boilerplate.Shared;

public partial class SharedSettings : IValidatableObject
{
    //#if (appInsights == true)
    [ValidateObjectMembers]
    public ApplicationInsightsOptions? ApplicationInsights { get; set; }
    //#endif

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        return validationResults;
    }
}

[OptionsValidator]
public partial class SharedSettingsValidator : IValidateOptions<SharedSettings>
{
}
