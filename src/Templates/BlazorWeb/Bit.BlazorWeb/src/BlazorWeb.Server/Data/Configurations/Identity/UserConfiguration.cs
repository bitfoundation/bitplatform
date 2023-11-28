using BlazorWeb.Server.Models.Identity;

namespace BlazorWeb.Server.Data.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        const string userName = "test@bitplatform.dev";

        var appUser = new User
        {
            Id = 1,
            EmailConfirmed = true,
            LockoutEnabled = true,
            Gender = Gender.Other,
            BirthDate = new DateTime(2023, 1, 1),
            FullName = "BlazorWeb test account",
            UserName = userName,
            Email = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            NormalizedEmail = userName.ToUpperInvariant(),
            SecurityStamp = "959ff4a9-4b07-4cc1-8141-c5fc033daf83",
            ConcurrencyStamp = "315e1a26-5b3a-4544-8e91-2760cd28e231",
            PasswordHash = "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", // 123456
        };

        builder.HasData(appUser);
    }
}
