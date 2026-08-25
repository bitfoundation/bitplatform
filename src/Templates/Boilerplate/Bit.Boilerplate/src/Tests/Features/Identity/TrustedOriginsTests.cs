using Boilerplate.Server.Shared;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Unit tests for <see cref="ServerSharedSettings.IsTrustedOrigin(Uri)"/> and its helpers.
/// These guard the trust boundary used by CORS, external sign-in / email-confirmation return URLs and Web Auth,
/// including the <c>*</c> wildcard support that lets a single entry trust every tenant subdomain.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class TrustedOriginsTests
{
    private static bool IsTrusted(string requestOrigin, params string[] trustedOrigins)
    {
        var settings = new ServerSharedSettings { TrustedOrigins = trustedOrigins };
        return settings.IsTrustedOrigin(new Uri(requestOrigin));
    }

    /// <summary>
    /// A <c>*</c> in an entry stands in for any run of characters within the authority, so <c>https://*.myapp.com</c>
    /// trusts every tenant subdomain while still refusing the apex, sibling domains and look-alike suffixes.
    /// </summary>
    [TestMethod]
    // Subdomain wildcard trusts tenant subdomains (single and multi-level)...
    [DataRow("https://*.myapp.com", "https://tenant1.myapp.com", true)]
    [DataRow("https://*.myapp.com", "https://a.b.myapp.com", true)]
    // ...tolerating a trailing slash or path on the incoming origin (only the authority is compared)...
    [DataRow("https://*.myapp.com", "https://tenant1.myapp.com/", true)]
    [DataRow("https://*.myapp.com", "https://tenant1.myapp.com/some/path", true)]
    // ...but the wildcard does NOT cover the bare apex...
    [DataRow("https://*.myapp.com", "https://myapp.com", false)]
    // ...nor a different registrable domain...
    [DataRow("https://*.myapp.com", "https://tenant1.other.com", false)]
    // ...nor a look-alike that merely ends with the trusted domain (suffix-injection attempt)...
    [DataRow("https://*.myapp.com", "https://evil.myapp.com.attacker.com", false)]
    // ...nor a host that glues the domain onto another label without a dot boundary...
    [DataRow("https://*.myapp.com", "https://x-myapp.com", false)]
    // ...and a wildcard placed mid-label still respects the surrounding literal text.
    [DataRow("https://tenant-*.myapp.com", "https://tenant-42.myapp.com", true)]
    [DataRow("https://tenant-*.myapp.com", "https://other-42.myapp.com", false)]
    public void IsTrustedOrigin_Should_HonorWildcardPatterns(string trustedOrigin, string requestOrigin, bool expected)
    {
        Assert.AreEqual(expected, IsTrusted(requestOrigin, trustedOrigin),
            $"'{trustedOrigin}' matching '{requestOrigin}' should be {expected}.");
    }

    /// <summary>
    /// An entry without a <c>*</c> keeps its original exact-match behavior: the scheme and host must line up (host
    /// case-insensitively), so a different scheme or host is rejected.
    /// </summary>
    [TestMethod]
    [DataRow("https://adminpanel.bitplatform.dev", "https://adminpanel.bitplatform.dev", true)]
    // Host comparison is case-insensitive...
    [DataRow("https://adminpanel.bitplatform.dev", "https://ADMINPANEL.bitplatform.dev", true)]
    // ...a trailing slash on the request is ignored...
    [DataRow("https://adminpanel.bitplatform.dev", "https://adminpanel.bitplatform.dev/", true)]
    // ...but the scheme must match...
    [DataRow("https://adminpanel.bitplatform.dev", "http://adminpanel.bitplatform.dev", false)]
    // ...and so must the host.
    [DataRow("https://adminpanel.bitplatform.dev", "https://other.bitplatform.dev", false)]
    public void IsTrustedOrigin_Should_MatchExactEntries(string trustedOrigin, string requestOrigin, bool expected)
    {
        Assert.AreEqual(expected, IsTrusted(requestOrigin, trustedOrigin),
            $"'{trustedOrigin}' matching '{requestOrigin}' should be {expected}.");
    }

    /// <summary>
    /// A port is part of an origin, so a port-less pattern must not trust a request that carries an explicit port
    /// (and vice-versa). Callers that need a specific port include it in the pattern.
    /// </summary>
    [TestMethod]
    [DataRow("https://*.myapp.com", "https://tenant1.myapp.com:8443", false)]
    [DataRow("https://*.myapp.com:8443", "https://tenant1.myapp.com:8443", true)]
    [DataRow("https://*.myapp.com:8443", "https://tenant1.myapp.com", false)]
    public void IsTrustedOrigin_Should_TreatPortAsPartOfOrigin(string trustedOrigin, string requestOrigin, bool expected)
    {
        Assert.AreEqual(expected, IsTrusted(requestOrigin, trustedOrigin),
            $"'{trustedOrigin}' matching '{requestOrigin}' should be {expected}.");
    }

    /// <summary>
    /// Local development origins (e.g. localhost) are trusted out of the box via the built-in regex, even when no
    /// <see cref="ServerSharedSettings.TrustedOrigins"/> are configured.
    /// </summary>
    [TestMethod]
    [DataRow("http://localhost")]
    [DataRow("https://localhost")]
    [DataRow("https://localhost:5000")]
    [DataRow("http://127.0.0.1:8080")]
    public void IsTrustedOrigin_Should_TrustLocalhostByDefault(string requestOrigin)
    {
        Assert.IsTrue(IsTrusted(requestOrigin), $"'{requestOrigin}' should be trusted by the built-in local-origin rule.");
    }

    /// <summary>
    /// The built-in regex's wildcarded alternatives must be bounded by a dot, so that a host which merely <em>ends</em>
    /// with the trusted text is refused. Without that boundary <c>evildevtunnels.ms</c> and <c>attacker-github.dev</c>
    /// are registrable domains that the server trusts - and a trusted origin is admitted by CORS
    /// (<c>SetIsOriginAllowed</c>, including the credentialed policy), accepted as an external-sign-in and
    /// email-confirmation return target, and accepted as a WebAuthn origin.
    /// <para>
    /// The regex is matched against the whole url rather than the authority (<see cref="ServerSharedSettings.IsTrustedOrigin"/>
    /// passes <c>origin.ToString()</c>), so the wildcard also has to be confined to host-label characters: otherwise it
    /// runs past the authority and a <em>path</em> is enough to be trusted (<c>https://evil.com/.devtunnels.ms</c>).
    /// </para>
    /// <para>
    /// The devtunnels/github.dev alternatives exist only in the <c>Development</c> arm, and <c>Development</c> is a
    /// real compile constant (<c>src/Directory.Build.props</c> derives it from the Configuration and pushes it into
    /// <c>DefineConstants</c>). Only the rows that expect those hosts to be <em>trusted</em> depend on that arm and are
    /// guarded; every row that expects a refusal holds in both arms and stays unconditional, which is the half that
    /// matters. The guard has to sit inside a <c>-:cnd:noEmit</c> region: the template engine reads an unwrapped C#
    /// preprocessor conditional as one of its own, evaluates the undeclared symbol as false, and silently deletes the
    /// block from every generated project.
    /// </para>
    /// </summary>
    [TestMethod]
    // The genuine local origins stay trusted...
    [DataRow("http://localhost", true)]
    //-:cnd:noEmit
#if Development
    [DataRow("https://mytunnel.devtunnels.ms", true)]
    [DataRow("https://myspace.github.dev", true)]
#endif
    //+:cnd:noEmit
    // ...but a domain that merely ends with the trusted text must not be...
    [DataRow("https://evildevtunnels.ms", false)]
    [DataRow("https://attacker-github.dev", false)]
    // ...nor may a path be able to carry the trusted text out of the authority...
    [DataRow("https://evil.com/.devtunnels.ms", false)]
    [DataRow("https://evil.com/x/.github.dev", false)]
    // ...nor may the loopback names be used as a prefix of someone else's domain.
    [DataRow("https://localhost.evil.com", false)]
    [DataRow("http://127.0.0.1.evil.com", false)]
    public void IsTrustedOrigin_Should_BoundTheBuiltInWildcardsAtADot(string requestOrigin, bool expected)
    {
        Assert.AreEqual(expected, IsTrusted(requestOrigin),
            $"'{requestOrigin}' should{(expected ? "" : " not")} be trusted by the built-in origin rule.");
    }

    /// <summary>
    /// With an empty allow-list and no built-in rule to fall back on, an arbitrary external origin is not trusted.
    /// </summary>
    [TestMethod]
    public void IsTrustedOrigin_Should_RejectUnknownOrigin_WhenNoneConfigured()
    {
        Assert.IsFalse(IsTrusted("https://evil.com"), "An unconfigured, non-local origin should not be trusted.");
    }

    /// <summary>
    /// Every configured entry is considered, so mixing exact and wildcard entries trusts each of them independently.
    /// </summary>
    [TestMethod]
    public void IsTrustedOrigin_Should_ConsiderEveryConfiguredEntry()
    {
        string[] trustedOrigins = ["https://adminpanel.bitplatform.dev", "https://*.myapp.com"];

        Assert.IsTrue(IsTrusted("https://adminpanel.bitplatform.dev", trustedOrigins), "The exact entry should match.");
        Assert.IsTrue(IsTrusted("https://tenant1.myapp.com", trustedOrigins), "The wildcard entry should match.");
        Assert.IsFalse(IsTrusted("https://unlisted.example.com", trustedOrigins), "An unlisted origin should not match.");
    }

    /// <summary>
    /// <see cref="ServerSharedSettings.GetTrustedOriginHost(string)"/> reduces an entry to just its host (keeping any
    /// <c>*</c>) so it can be handed to <c>ForwardedHeadersOptions.AllowedHosts</c>, which <see cref="Uri"/> cannot do
    /// because it rejects <c>*</c> in the host.
    /// </summary>
    [TestMethod]
    [DataRow("https://*.myapp.com", "*.myapp.com")]
    [DataRow("https://real.myapp.com", "real.myapp.com")]
    [DataRow("https://real.myapp.com:5000", "real.myapp.com")]
    [DataRow("https://real.myapp.com/some/path", "real.myapp.com")]
    // Also copes with a scheme-less entry.
    [DataRow("*.myapp.com", "*.myapp.com")]
    public void GetTrustedOriginHost_Should_ExtractHost(string trustedOrigin, string expectedHost)
    {
        Assert.AreEqual(expectedHost, ServerSharedSettings.GetTrustedOriginHost(trustedOrigin));
    }
}
