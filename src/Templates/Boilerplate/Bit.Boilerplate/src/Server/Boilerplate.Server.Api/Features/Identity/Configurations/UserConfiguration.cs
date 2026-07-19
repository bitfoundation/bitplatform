//+:cnd:noEmit
using Boilerplate.Server.Api.Features.Identity.Models;
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasMany(user => user.Roles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId);

        builder.HasMany(user => user.Claims)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId);

        builder.HasMany(user => user.Tokens)
            .WithOne(ut => ut.User)
            .HasForeignKey(ut => ut.UserId);

        builder.HasMany(user => user.Logins)
            .WithOne(ul => ul.User)
            .HasForeignKey(ul => ul.UserId);

        const string userName = "test";
        const string email = "test@bitplatform.dev";

        builder.HasData([new User
        {
            Id = Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"),
            EmailConfirmed = true,
            LockoutEnabled = true,
            Gender = Gender.Other,
            BirthDate = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default),
            FullName = "Boilerplate test account",
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailTokenRequestedOn = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default),
            PhoneNumber = "+31684207362",
            PhoneNumberConfirmed = true,
            SecurityStamp = "959ff4a9-4b07-4cc1-8141-c5fc033daf83",
            ConcurrencyStamp = "315e1a26-5b3a-4544-8e91-2760cd28e231",
            PasswordHash = "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", // 123456
        }]);

        //#if (multitenant == true)
        const string storeAdminUserName = "store-admin";
        const string storeAdminEmail = "store-admin@bitplatform.dev";

        // The default store tenant's admin (See TenantUserConfiguration and UserRoleConfiguration).
        builder.HasData([new User
        {
            Id = Guid.Parse("6ff71671-a1d6-4f97-abb9-d87d7b47d6e5"),
            EmailConfirmed = true,
            LockoutEnabled = true,
            Gender = Gender.Other,
            BirthDate = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default),
            FullName = "Store tenant admin",
            UserName = storeAdminUserName,
            NormalizedUserName = storeAdminUserName.ToUpperInvariant(),
            Email = storeAdminEmail,
            NormalizedEmail = storeAdminEmail.ToUpperInvariant(),
            EmailTokenRequestedOn = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default),
            SecurityStamp = "869ff4a9-4b07-4cc1-8141-c5fc033daf82",
            ConcurrencyStamp = "425e1a26-5b3a-4544-8e91-2760cd28e230",
            PasswordHash = "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", // 123456
        }]);

        const string storeUserUserName = "store-user";
        const string storeUserEmail = "store-user@bitplatform.dev";

        // A regular (non-admin) member of the default store tenant, assigned to the demo user-group.
        // (See UserRoleConfiguration and TenantUserConfiguration).
        builder.HasData([new User
        {
            Id = Guid.Parse("4ff71671-a1d6-4f97-abb9-d87d7b47d6e4"),
            EmailConfirmed = true,
            LockoutEnabled = true,
            Gender = Gender.Other,
            BirthDate = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default),
            FullName = "Store tenant user",
            UserName = storeUserUserName,
            NormalizedUserName = storeUserUserName.ToUpperInvariant(),
            Email = storeUserEmail,
            NormalizedEmail = storeUserEmail.ToUpperInvariant(),
            EmailTokenRequestedOn = new DateTimeOffset(new DateOnly(2023, 1, 1), default, default),
            SecurityStamp = "469ff4a9-4b07-4cc1-8141-c5fc033daf84",
            ConcurrencyStamp = "435e1a26-5b3a-4544-8e91-2760cd28e229",
            PasswordHash = "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", // 123456
        }]);
        //#endif

        builder.HasUniqueIndexOnNullable(b => b.Email);

        builder.HasUniqueIndexOnNullable(b => b.PhoneNumber);
    }
}
