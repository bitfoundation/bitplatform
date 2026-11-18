using System.Text.RegularExpressions;

namespace Bit.Cli.Projects;

/// <summary>
/// A git repository's address that bit setup clones before it sets the project up: https://host/owner/name, or the SSH
/// form git@host:owner/name. It takes only letters, digits and . - _ ~ / in its path, as bitfabric.co/setup does, which
/// puts the same address into a line for PowerShell or bash.
/// </summary>
public sealed partial record RepositoryAddress
{
    private RepositoryAddress(string url, string host, string path, bool isHttps)
    {
        Url = url;
        Host = host;
        Path = path;
        IsHttps = isHttps;
    }

    /// <summary>The address as it was given, without a trailing slash.</summary>
    public string Url { get; }

    public string Host { get; }

    /// <summary>The owner and the name, such as acme/Contoso, without .git.</summary>
    public string Path { get; }

    public bool IsHttps { get; }

    /// <summary>The repository's own name, which names the folder it is cloned into.</summary>
    public string Name => Path[(Path.LastIndexOf('/') + 1)..];

    /// <summary>A repository on github.com over HTTPS, which bit can sign in to with the GitHub CLI when it is private.</summary>
    public bool SignsInWithGitHubCli => IsHttps && Host.Equals("github.com", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the argument is meant as an address rather than a folder, valid or not.</summary>
    public static bool LooksLikeOne(string? value) => value is not null && (value.Contains("://", StringComparison.Ordinal) || ScpLikeRegex().IsMatch(value));

    public static RepositoryAddress? Parse(string? value)
    {
        if (value is null)
            return null;

        var trimmed = value.Trim().TrimEnd('/');

        if (HttpsRegex().Match(trimmed) is { Success: true } https)
            return Create(trimmed, https.Groups["host"].Value, https.Groups["path"].Value, isHttps: true);

        if (SshRegex().Match(trimmed) is { Success: true } ssh)
            return Create(trimmed, ssh.Groups["host"].Value, ssh.Groups["path"].Value, isHttps: false);

        return null;
    }

    /// <summary>
    /// Whether another address, such as a clone's origin, points to this repository: over HTTPS or SSH, with or without
    /// .git, a user name or a trailing slash, and in any case.
    /// </summary>
    public bool IsSameAs(string? other)
    {
        return other is not null && string.Equals(Comparable(other), $"{Host}/{Path}", StringComparison.OrdinalIgnoreCase);
    }

    private static RepositoryAddress? Create(string url, string host, string path, bool isHttps)
    {
        if (path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            path = path[..^".git".Length];
        }

        // A . or .. part would name a folder outside the one bit clones into
        return path.Split('/').Any(part => part is "" or "." or "..") ? null : new RepositoryAddress(url, host, path, isHttps);
    }

    private static string Comparable(string address)
    {
        var value = address.Trim().TrimEnd('/');
        var scheme = value.IndexOf("://", StringComparison.Ordinal);

        if (scheme >= 0)
        {
            value = value[(scheme + 3)..];
        }
        else if (ScpLikeRegex().IsMatch(value))
        {
            var colon = value.IndexOf(':', StringComparison.Ordinal);
            value = $"{value[..colon]}/{value[(colon + 1)..]}";
        }

        if (value.IndexOf('@', StringComparison.Ordinal) is var at and >= 0 && at < value.IndexOf('/', StringComparison.Ordinal))
        {
            value = value[(at + 1)..];
        }

        return value.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? value[..^".git".Length] : value;
    }

    [GeneratedRegex(@"^https://(?<host>[A-Za-z0-9.-]+(:[0-9]{1,5})?)/(?<path>[A-Za-z0-9._~-]+(/[A-Za-z0-9._~-]+)+)$")]
    private static partial Regex HttpsRegex();

    [GeneratedRegex(@"^[A-Za-z0-9._-]+@(?<host>[A-Za-z0-9.-]+):(?<path>[A-Za-z0-9._~-]+(/[A-Za-z0-9._~-]+)+)$")]
    private static partial Regex SshRegex();

    [GeneratedRegex(@"^[^/\\\s]+@[^/\\\s:]+:")]
    private static partial Regex ScpLikeRegex();
}
