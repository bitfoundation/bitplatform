//+:cnd:noEmit
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Boilerplate.Server.Api.Features.Identity.Configurations;

public partial class UserConfiguration : IEntityTypeConfiguration<User>
{
    private static readonly string seedPasswordHash = HashSeedPassword("12345678", Guid.Parse("AC87AA5B-4B37-4E52-8468-2D5DF24AF256"));

    /// <summary>Identity's V3 format, salted with the project's own GUID, so the model stays the same on every run.</summary>
    private static string HashSeedPassword(string password, Guid projectId)
    {
        var salt = projectId.ToByteArray();
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA512, 32);
        var hash = new byte[13 + salt.Length + subkey.Length];

        hash[0] = 0x01;
        BinaryPrimitives.WriteUInt32BigEndian(hash.AsSpan(1), 2);
        BinaryPrimitives.WriteUInt32BigEndian(hash.AsSpan(5), 100_000);
        BinaryPrimitives.WriteUInt32BigEndian(hash.AsSpan(9), (uint)salt.Length);
        salt.CopyTo(hash, 13);
        subkey.CopyTo(hash, 13 + salt.Length);

        return Convert.ToBase64String(hash);
    }

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
            PasswordHash = seedPasswordHash,
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
            PasswordHash = seedPasswordHash,
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
            PasswordHash = seedPasswordHash,
        }]);
        //#endif

        // Indexed on the NORMALIZED column, because that is the one every lookup uses (UserStore.FindByEmailAsync
        // queries NormalizedEmail with SingleOrDefaultAsync, and the base EmailIndex is NOT unique). A unique index on
        // the raw Email column would let `victim@x.com` and `Victim@x.com` both be inserted under a case-sensitive
        // collation - SQLite's and PostgreSQL's default - after which every FindByEmailAsync for that address throws
        // "Sequence contains more than one element" for good.
        builder.HasUniqueIndexOnNullable(b => b.NormalizedEmail);

        builder.HasUniqueIndexOnNullable(b => b.PhoneNumber);
    }
}
