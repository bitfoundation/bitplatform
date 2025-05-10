//+:cnd:noEmit
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public partial class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.HasIndex(roleClaim => new { roleClaim.RoleId, roleClaim.ClaimType, roleClaim.ClaimValue });

        // Unlimited privileged sessions for super admins
        var superAdminRoleId = Guid.Parse("8ff71671-a1d6-5f97-abb9-d87d7b47d6e7");
        builder.HasData(new
        {
            Id = 1,
            ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
            ClaimValue = "-1",
            RoleId = superAdminRoleId
        });


        // Assign non admin features to demo role
        var demoRoleId = Guid.Parse("9ff71672-a1d5-4f97-abb7-d87d6b47d5e8");
        builder.HasData(new
        {
            Id = 2,
            ClaimType = AppClaimTypes.FEATURES,
            ClaimValue = AppFeatures.AdminPanel.Dashboard,
            RoleId = demoRoleId
        });
        builder.HasData(new
        {
            Id = 3,
            ClaimType = AppClaimTypes.FEATURES,
            ClaimValue = AppFeatures.AdminPanel.ManageProductCatalog,
            RoleId = demoRoleId
        });
        builder.HasData(new
        {
            Id = 4,
            ClaimType = AppClaimTypes.FEATURES,
            ClaimValue = AppFeatures.Todo.ManageTodo,
            RoleId = demoRoleId
        });

        builder.HasData(new
        {
            Id = 5,
            ClaimType = AppClaimTypes.FEATURES,
            ClaimValue = AppFeatures.Extra.ViewAboutApp,
            RoleId = demoRoleId
        });
        builder.HasData(new
        {
            Id = 6,
            ClaimType = AppClaimTypes.FEATURES,
            ClaimValue = AppFeatures.Communication.Chatbot,
            RoleId = demoRoleId
        });
        builder.HasData(new
        {
            Id = 7,
            ClaimType = AppClaimTypes.FEATURES,
            ClaimValue = AppFeatures.Communication.SignalR,
            RoleId = demoRoleId
        });
        builder.HasData(new
        {
            Id = 8,
            ClaimType = AppClaimTypes.FEATURES,
            ClaimValue = AppFeatures.Communication.PushNotification,
            RoleId = demoRoleId
        });
        builder.HasData(new
        {
            Id = 9,
            ClaimType = AppClaimTypes.FEATURES,
            ClaimValue = AppFeatures.Sales.ProductView,
            RoleId = demoRoleId
        });
    }
}
