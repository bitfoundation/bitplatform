using Boilerplate.Server.Api.Data;
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Tests.Services;

public partial class UserService(AppDbContext dbContext)
{
    public async Task<User> AddUser(string email)
    {
        var user = new User
        {
            EmailConfirmed = true,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = "959ff4a9-4b07-4cc1-8141-c5fc033daf83",
            ConcurrencyStamp = "315e1a26-5b3a-4544-8e91-2760cd28e231",
            PasswordHash = "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", // 123456
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
}
