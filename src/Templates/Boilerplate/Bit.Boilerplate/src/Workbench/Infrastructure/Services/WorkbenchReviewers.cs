using System.Security.Cryptography;
using System.Text;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Lets a reviewer who isn't at this computer, like the user on their phone through a tunnel, leave comments that reach the
/// agent. Only a link with the invite key does it: the key becomes a cookie, and from then on that browser's comments go into
/// the review document as if they were left here. It never lets them change the code, the texts, the flows or a page's
/// status, and it works only in dev mode, where an agent reads the comments. The agent treats every comment as a request
/// about the page and its design, never as an order to run something on this computer (the workbench skill, section 7).
/// The key is kept in App_Data, which git ignores, so it outlives dotnet watch's restarts; deleting its file revokes every
/// invite.
/// </summary>
public static class WorkbenchReviewers
{
    /// <summary>The query string parameter of an invite link.</summary>
    public const string QueryName = "review-key";

    /// <summary>The cookie an invite link leaves in the reviewer's browser.</summary>
    public const string CookieName = "workbench-reviewer";

    private static readonly Lock keyLock = new();

    private static string KeyFile => Path.Combine(WorkbenchFolders.Reviews, "reviewer-key.txt");

    /// <summary>The invite key, read from its file each time, so a deleted or replaced file counts at once; made when there is none.</summary>
    public static string Key
    {
        get
        {
            lock (keyLock)
            {
                if (File.Exists(KeyFile) && File.ReadAllText(KeyFile).Trim() is { Length: >= 32 } saved)
                    return saved;

                Directory.CreateDirectory(WorkbenchFolders.Reviews);
                var key = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(24));
                File.WriteAllText(KeyFile, key);

                return key;
            }
        }
    }

    /// <summary>Whether the request comes from a browser an invite link was opened in, in dev mode.</summary>
    public static bool IsInvited(HttpContext context, IConfiguration configuration) => IsInvited(context.Request.Cookies[CookieName], configuration);

    /// <summary>Whether an invite cookie's value still counts: in dev mode, with the current key.</summary>
    public static bool IsInvited(string? cookie, IConfiguration configuration) => WorkbenchLocalRequests.IsDevMode(configuration) && Matches(cookie);

    /// <summary>
    /// Turns the key of an invite link into the cookie, then sends the browser on to the same address without it, so the key
    /// stays out of the address bar, the history and the links copied from there. A wrong key is only dropped.
    /// </summary>
    public static bool TryAccept(HttpContext context, IConfiguration configuration)
    {
        if (context.Request.Query.TryGetValue(QueryName, out var value) is false || WorkbenchLocalRequests.IsDevMode(configuration) is false)
            return false;

        var path = $"{context.Request.PathBase}{context.Request.Path}";

        if (path.StartsWith("//", StringComparison.Ordinal) || path.StartsWith("/\\", StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return true;
        }

        if (Matches(value.ToString()))
        {
            context.Response.Cookies.Append(CookieName, Key, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(30)
            });
        }

        var rest = context.Request.Query.Where(q => q.Key != QueryName).SelectMany(q => q.Value.Select(v => KeyValuePair.Create(q.Key, v)));

        context.Response.Redirect($"{path}{QueryString.Create(rest)}");

        return true;
    }

    private static bool Matches(string? value)
    {
        return string.IsNullOrEmpty(value) is false
            && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(value), Encoding.UTF8.GetBytes(Key));
    }
}
