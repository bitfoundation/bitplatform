//+:cnd:noEmit
using Boilerplate.Server.Models.Identity;

namespace Boilerplate.Server.Data.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        const string userName = "test";
        const string email = "test@bitplatform.dev";

        builder.HasData([new()
        {
            Id = 1,
            EmailConfirmed = true,
            LockoutEnabled = true,
            Gender = Gender.Other,
            BirthDate = new DateTime(2023, 1, 1),
            FullName = "Boilerplate test account",
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailTokenRequestedOn = new DateTime(2023,1,1),
            PhoneNumber = "+31684207362",
            PhoneNumberConfirmed = true,
            SecurityStamp = "959ff4a9-4b07-4cc1-8141-c5fc033daf83",
            ConcurrencyStamp = "315e1a26-5b3a-4544-8e91-2760cd28e231",
            PasswordHash = "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", // 123456
        }]);

        //#if (database == "Sqlite" || database == "SqlServer")
        builder
            .HasIndex(b => b.Email)
            .HasFilter($"[{nameof(User.Email)}] IS NOT NULL")
            .IsUnique();

        builder
            .HasIndex(b => b.PhoneNumber)
            .HasFilter($"[{nameof(User.PhoneNumber)}] IS NOT NULL")
            .IsUnique();
        //#endif
        //#if (IsInsideProjectTemplate == true)
        return;
        //#endif
        //#if (database == "PostgreSQL")
        builder
            .HasIndex(b => b.Email)
            .HasFilter($"'{nameof(User.Email)}' IS NOT NULL")
            .IsUnique();

        builder
            .HasIndex(b => b.PhoneNumber)
            .HasFilter($"'{nameof(User.PhoneNumber)}' IS NOT NULL")
            .IsUnique();
        //#endif
    }
}
