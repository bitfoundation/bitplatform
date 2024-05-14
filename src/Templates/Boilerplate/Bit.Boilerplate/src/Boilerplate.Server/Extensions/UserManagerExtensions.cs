﻿using Boilerplate.Server.Models.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Microsoft.AspNetCore.Identity;

public static class UserManagerExtensions
{
    public static async Task<User?> FindUser(this UserManager<User> userManager, IdentityRequestDto identity)
    {
        User? user = default;

        var (userName, email, phoneNumber) = (identity.UserName, identity.Email, identity.PhoneNumber);

        if (userName is null && email is null && phoneNumber is null)
            throw new InvalidOperationException();

        if (string.IsNullOrEmpty(userName) is false)
        {
            user = await userManager.FindByNameAsync(userName!);
        }
        else if (string.IsNullOrEmpty(email) is false)
        {
            user = await userManager.FindByEmailAsync(email!);
        }
        else if (string.IsNullOrEmpty(phoneNumber) is false)
        {
            user = await userManager.FindByPhoneNumber(phoneNumber);
        }

        return user;
    }

    public static Task<User?> FindByPhoneNumber(this UserManager<User> userManager, string phoneNumber)
    {
        return userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }
}
