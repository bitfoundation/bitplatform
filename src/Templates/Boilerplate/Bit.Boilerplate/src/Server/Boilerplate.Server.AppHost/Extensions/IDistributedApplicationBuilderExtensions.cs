using System.Linq;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class IDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// https://github.com/davidfowl/aspire-ai-chat-demo/blob/main/AIChat.AppHost/DashboardExtensions.cs
    /// </summary>
    public static void AddAspireDashboard(this IDistributedApplicationBuilder builder)
    {
        if (builder.ExecutionContext.IsPublishMode)
        {
            // The name aspire-dashboard is special cased and excluded from the default
            var dashboard = builder.AddContainer("dashboard", "mcr.microsoft.com/dotnet/aspire-dashboard:9.0")
                   .WithHttpEndpoint(targetPort: 18888)
                   .WithHttpEndpoint(name: "otlp", targetPort: 18889);

            builder.Eventing.Subscribe<BeforeStartEvent>((e, ct) =>
            {
                // We loop over all resources and set the OTLP endpoint to the dashboard
                // we should make WithOtlpExporter() add an annotation so we can detect this
                // automatically in the future.
                foreach (var r in e.Model.Resources.OfType<IResourceWithEnvironment>())
                {
                    if (r == dashboard.Resource)
                    {
                        continue;
                    }

                    builder.CreateResourceBuilder(r).WithEnvironment(c =>
                    {
                        c.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = dashboard.GetEndpoint("otlp");
                        c.EnvironmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc";
                        c.EnvironmentVariables["OTEL_SERVICE_NAME"] = r.Name;
                    });
                }

                return Task.CompletedTask;
            });
        }
    }
}
