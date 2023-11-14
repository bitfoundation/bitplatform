﻿using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Data.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        const string userName = "test@bitplatform.dev";

        var appUser = new User
        {
            Id = 1,
            EmailConfirmed = true,
            Gender = Gender.Other,
            BirthDate = new DateTime(2023, 1, 1),
            FullName = "Boilerplate test account",
            UserName = userName,
            Email = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            NormalizedEmail = userName.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var hasher = new PasswordHasher<User>();

        appUser.PasswordHash = hasher.HashPassword(appUser, "123456");

        builder.HasData(appUser);
    }
}
