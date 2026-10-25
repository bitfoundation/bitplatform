//+:cnd:noEmit
using Boilerplate.Client.Core;
using Microsoft.Extensions.Options;

namespace Boilerplate.Client.Web;

public class ClientWebSettings : ClientCoreSettings
{
    //#if (notification == true)
    [ValidateObjectMembers]
    public AdsPushVapidOptions? AdsPushVapid { get; set; }
    //#endif

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();

        //#if (notification == true)
        if (AdsPushVapid is not null)
        {
            if (AppEnvironment.IsDevelopment() is false && AdsPushVapid.PublicKey is "BDSNUvuIISD8NQVByQANEtZ2foKaENIcIGUxsiQs9kDz11fQik8c9WeiMwUHs3iTgNNH4nvXioNQIEsn4OAjTKc")
            {
                validationResults.Add(new ValidationResult("Please set your own AdsPushVapid.PublicKey in Client.Web's appsettings.json"));
            }
        }
        //#endif

        return validationResults;
    }
}

[OptionsValidator]
public partial class ClientWebSettingsValidator : IValidateOptions<ClientWebSettings>
{
}
