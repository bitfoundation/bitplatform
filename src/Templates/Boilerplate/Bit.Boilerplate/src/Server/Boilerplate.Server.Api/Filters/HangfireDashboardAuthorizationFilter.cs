using Hangfire.Dashboard;
using Hangfire.Annotations;

namespace Boilerplate.Server.Api.Filters;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        return context.GetHttpContext().User.HasClaim(AppPermissions.Management.ManageJobs, string.Empty);
    }
}
