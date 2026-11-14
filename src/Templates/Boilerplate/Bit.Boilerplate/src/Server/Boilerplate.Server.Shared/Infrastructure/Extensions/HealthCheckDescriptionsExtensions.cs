using Boilerplate.Server.Shared.Infrastructure.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

public static class HealthCheckDescriptionsExtensions
{
    extension(IHealthChecksBuilder builder)
    {
        /// <summary>
        /// The title, icon (a <c>BitIconName</c>) and about /healthz reports for the check registered as
        /// <paramref name="name"/> (See <see cref="HealthCheckDescriptions"/>).
        /// </summary>
        public IHealthChecksBuilder Describe(string name, string title, string? iconName = null, string? about = null)
        {
            builder.Services.Configure<HealthCheckDescriptions>(descriptions => descriptions[name] = new(title, iconName, about));

            return builder;
        }
    }
}
