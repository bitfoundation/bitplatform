namespace AdminPanel.Shared;

public partial class SharedSettings : IValidatableObject
{
    /// <summary>
    /// If you are hosting the API and web client on different URLs (e.g., api.company.com and app.company.com), you must set `WebClientUrl` to your web client's address.
    /// This ensures that the API server redirects to the correct URL after social sign-ins and other similar actions.
    /// </summary>
    public string? WebClientUrl { get; set; }

    public ApplicationInsightsOptions? ApplicationInsights { get; set; }

    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        if (ApplicationInsights is not null)
        {
            Validator.TryValidateObject(ApplicationInsights, new ValidationContext(ApplicationInsights), validationResults, true);
        }

        return validationResults;
    }
}

public class ApplicationInsightsOptions
{
    public string? ConnectionString { get; set; }
}
