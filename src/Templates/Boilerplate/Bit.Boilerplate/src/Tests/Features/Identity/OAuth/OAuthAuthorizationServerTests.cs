//+:cnd:noEmit
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.WebUtilities;
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;
//#if (advancedTests == true)
using Boilerplate.Tests.Features.DevMcp;
//#endif

namespace Boilerplate.Tests.Features.Identity.OAuth;

/// <summary>
/// The authorization code flow, covering the parts where nearly right is indistinguishable from right on the happy
/// path - a wrong audience, a code accepted twice, a PKCE challenge never verified - and only an attacker exercises.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class OAuthAuthorizationServerTests
{
    public TestContext TestContext { get; set; } = default!;

    private const string TestClientId = "test-mcp-client";
    private const string TestRedirectUri = "http://127.0.0.1:33418/callback";

    /// <summary>
    /// A pre-registered client: the other way in, a Client ID Metadata Document, is fetched over https from a host the
    /// client owns, which a loopback test server cannot be.
    /// </summary>
    private static void ConfigureTestClient(ConfigurationManager configuration)
    {
        configuration["OAuth:Clients:0:ClientId"] = TestClientId;
        configuration["OAuth:Clients:0:ClientName"] = "Test MCP Client";
        configuration["OAuth:Clients:0:RedirectUris:0"] = TestRedirectUri;
    }

    /// <summary>
    /// The whole flow, then the shape of what came out: an OAuth token has to be narrower than the session that granted
    /// it, in audience and in what it can do.
    /// </summary>
    [TestMethod]
    public async Task TheFullFlow_Should_IssueATokenScopedToTheResourceAndToTheGrantedScope()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (verifier, challenge) = GeneratePkcePair();
        var request = BuildRequest(server, challenge);

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(request, TestContext.CancellationToken);

        var code = QueryValueOf(approval.RedirectUrl, "code");

        Assert.IsNotNull(code, "Approving the request should have produced an authorization code.");
        Assert.AreEqual(Issuer(server), QueryValueOf(approval.RedirectUrl, "iss"),
            "RFC 9207: the response has to say which authorization server answered, or a client cannot tell ours from anyone else's.");

        var token = await ExchangeCode(server, code!, verifier);

        var accessToken = new JwtSecurityToken(token.GetProperty("access_token").GetString());

        Assert.AreEqual($"{Issuer(server)}/dev-mcp", accessToken.Audiences.Single(),
            "The token's audience is the one thing keeping it out of the rest of the api. If it carries the app's own " +
            "audience instead, every token handed to a third party is a token for the whole backend.");

        Assert.AreEqual(Issuer(server), accessToken.Issuer,
            "And its issuer has to be the url the client discovered us at, or no consumer can validate it.");

        var features = accessToken.Claims.Where(claim => claim.Type is AppClaimTypes.FEATURES).Select(claim => claim.Value).ToArray();

        Assert.AreSequenceEqual([AppFeatures.System.DevMcp], features, SequenceOrder.InAnyOrder,
            "Only the features the granted scope names. A global admin consenting to `dev-mcp` must not hand out a " +
            "token that can manage users - the scope is the ceiling, not the user's own permissions.");

        Assert.IsEmpty(accessToken.Claims.Where(claim => claim.Type is ClaimTypes.Role).ToArray(),
            "No role claims: AppJwtSecureDataFormat expands GlobalAdmin into every feature there is, so a token " +
            "carrying that role would quietly be a token carrying everything.");

        Assert.Contains("mfa", accessToken.Claims.Where(claim => claim.Type is AppClaimTypes.AMR).Select(claim => claim.Value).ToArray(),
            "/dev-mcp demands TFA_ENABLED as well as the feature, so a token that drops amr could hold the right " +
            "feature and still never be let in to the one resource this whole flow exists for.");
    }

    /// <summary>
    /// The open redirect: an unregistered redirect uri must never be honoured, not even to report the error, because
    /// reporting it means sending the browser there.
    /// </summary>
    [TestMethod]
    public async Task AnUnregisteredRedirectUri_Should_NeverBeRedirectedTo()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        const string attackerUri = "https://attacker.example/steal";

        var (_, challenge) = GeneratePkcePair();

        var authorizeUrl = QueryHelpers.AddQueryString("oauth/authorize", new Dictionary<string, string?>
        {
            ["client_id"] = TestClientId,
            ["redirect_uri"] = attackerUri,
            ["response_type"] = "code",
            ["scope"] = OAuthScopes.DevMcp,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["resource"] = $"{Issuer(server)}/dev-mcp"
        });

        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = server.WebAppServerAddress
        };

        using var response = await httpClient.GetAsync(authorizeUrl, TestContext.CancellationToken);

        var location = response.Headers.Location?.ToString() ?? "";

        Assert.DoesNotContain("attacker.example", location,
            "The authorize endpoint sent the browser to an unregistered redirect uri. That is an open redirect, and " +
            "with an OAuth error attached it is one that looks legitimate to whoever is watching.");

        Assert.Contains(PageUrls.OAuthConsent, location,
            "The user should land on our own consent page, which explains that the request could not be identified.");
    }

    /// <summary>
    /// A code travels once, so a second exchange means two parties hold it and only one is the real client; OAuth 2.1
    /// requires revoking what it already issued, which here is the session it created.
    /// </summary>
    [TestMethod]
    public async Task AReplayedCode_Should_BeRefused_AndRevokeWhatTheFirstExchangeIssued()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (verifier, challenge) = GeneratePkcePair();

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(BuildRequest(server, challenge), TestContext.CancellationToken);

        var code = QueryValueOf(approval.RedirectUrl, "code")!;

        var token = await ExchangeCode(server, code, verifier);
        var sessionId = SessionIdOf(token.GetProperty("access_token").GetString()!);

        var (status, body) = await PostToken(server, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = TestClientId,
            ["redirect_uri"] = TestRedirectUri,
            ["code_verifier"] = verifier
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, status, "The second exchange of one code must fail.");
        Assert.Contains("invalid_grant", body, "And it must fail with the error code a client knows how to read.");

        await using var verificationScope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.IsFalse(await dbContext.UserSessions.AnyAsync(session => session.Id == sessionId, TestContext.CancellationToken),
            "Refusing the replay is not enough: whoever replayed the code proves the first token pair is compromised, " +
            "so the session it minted has to go with it.");
    }

    /// <summary>
    /// PKCE is all that stands between an intercepted code and a token, and a challenge stored but never verified
    /// passes every normal test.
    /// </summary>
    [TestMethod]
    public async Task ACodeVerifierThatDoesNotMatchTheChallenge_Should_BeRefused()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (_, challenge) = GeneratePkcePair();
        var (wrongVerifier, _) = GeneratePkcePair();

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(BuildRequest(server, challenge), TestContext.CancellationToken);

        var (status, body) = await PostToken(server, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = QueryValueOf(approval.RedirectUrl, "code")!,
            ["client_id"] = TestClientId,
            ["redirect_uri"] = TestRedirectUri,
            ["code_verifier"] = wrongVerifier
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, status,
            "A code presented with the wrong verifier is a code somebody else intercepted. Accepting it makes PKCE decorative.");
        Assert.Contains("invalid_grant", body);
    }

    /// <summary>The metadata a client reads first; getting it wrong breaks every client silently, at discovery time.</summary>
    [TestMethod]
    public async Task TheAuthorizationServerMetadata_Should_DescribeWhatThisServerActuallyDoes()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
        using var document = JsonDocument.Parse(
            await httpClient.GetStringAsync(".well-known/oauth-authorization-server", TestContext.CancellationToken));

        var metadata = document.RootElement;

        Assert.AreEqual(Issuer(server), metadata.GetProperty("issuer").GetString(),
            "The issuer must be the url the client reached us at, or its RFC 9207 check on the authorization response fails.");

        Assert.AreEqual($"{Issuer(server)}/oauth/authorize", metadata.GetProperty("authorization_endpoint").GetString());
        Assert.AreEqual($"{Issuer(server)}/oauth/token", metadata.GetProperty("token_endpoint").GetString());

        Assert.AreSequenceEqual(["S256"],
            metadata.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(item => item.GetString()).ToArray(),
            "`plain` must not be advertised: a client that sees it may use it, and then PKCE protects nothing.");

        Assert.IsTrue(metadata.GetProperty("client_id_metadata_document_supported").GetBoolean(),
            "This is how a client with no prior relationship registers since MCP 2026-07-28. Without it they fall " +
            "back to dynamic registration, which this server does not implement.");
    }

    /// <summary>
    /// The load-bearing assertion: all that separates "an app can read diagnostics" from "an app is the user" is an
    /// audience of one resource. A token that opens <c>/dev-mcp</c> and the api too is not a narrower credential.
    /// </summary>
    [TestMethod]
    public async Task AnOAuthToken_Should_ReachDevMcp_AndNothingElse()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (verifier, challenge) = GeneratePkcePair();

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(BuildRequest(server, challenge), TestContext.CancellationToken);

        var token = await ExchangeCode(server, QueryValueOf(approval.RedirectUrl, "code")!, verifier);
        var oauthAccessToken = token.GetProperty("access_token").GetString()!;

        Assert.AreNotEqual(HttpStatusCode.Unauthorized,
            await DevMcpTestUtils.ProbeInitialize(server.WebAppServerAddress, "dev-mcp", oauthAccessToken, TestContext.CancellationToken),
            "The token was issued for /dev-mcp, so /dev-mcp has to accept it - otherwise the whole flow issues " +
            "credentials that nothing in this app will honour.");

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
        using var apiRequest = new HttpRequestMessage(HttpMethod.Get, "api/v1/User/GetCurrentUser");
        apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oauthAccessToken);

        using var apiResponse = await httpClient.SendAsync(apiRequest, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, apiResponse.StatusCode,
            "And the rest of the api must refuse it. If this ever returns 200, every token handed to a third-party " +
            "app is a token for the user's whole account, and nothing else in this suite would notice.");
    }

    /// <summary>
    /// A client arriving with no token has to be told where to go; without this header it just fails, which looks like
    /// a broken server rather than a sign-in prompt.
    /// </summary>
    [TestMethod]
    public async Task AnUnauthenticatedDevMcpCall_Should_PointAtTheProtectedResourceMetadata()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
        using var request = new HttpRequestMessage(HttpMethod.Post, "dev-mcp");
        request.Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""", Encoding.UTF8, "application/json");
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        var challenge = string.Join(' ', response.Headers.WwwAuthenticate.Select(header => $"{header.Scheme} {header.Parameter}"));

        Assert.Contains($"{Issuer(server)}/.well-known/oauth-protected-resource/dev-mcp", challenge,
            "RFC 9728: the 401 has to carry resource_metadata pointing at this resource's document.");

        Assert.Contains(OAuthScopes.DevMcp, challenge,
            "And the scope, so the client asks for what this resource needs instead of guessing or asking for everything.");
    }

    /// <summary>The document that challenge points at.</summary>
    [TestMethod]
    public async Task TheProtectedResourceMetadata_Should_NameThisServerAsItsOwnAuthorizationServer()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
        using var document = JsonDocument.Parse(
            await httpClient.GetStringAsync(".well-known/oauth-protected-resource/dev-mcp", TestContext.CancellationToken));

        Assert.AreEqual($"{Issuer(server)}/dev-mcp", document.RootElement.GetProperty("resource").GetString());

        Assert.AreSequenceEqual([Issuer(server)],
            document.RootElement.GetProperty("authorization_servers").EnumerateArray().Select(item => item.GetString()).ToArray(),
            "The resource and its authorization server are the same deployment here, and both have to be the origin " +
            "the caller reached - a client that discovers a different host cannot get a token this server accepts.");
    }

    /// <summary>
    /// RFC 8252 §7.3: a desktop client listens on whatever ephemeral port the OS gave it - Visual Studio registers
    /// :33419 and arrives on 62435 - so comparing the port turns every one of them away.
    /// </summary>
    [TestMethod]
    public async Task ALoopbackRedirectUri_Should_BeAcceptedOnAnyPort()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (_, challenge) = GeneratePkcePair();

        var request = BuildRequest(server, challenge);
        request.RedirectUri = "http://127.0.0.1:62435/callback"; // ConfigureTestClient registers the same path on :33418.

        var consent = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                 .Review(request, TestContext.CancellationToken);

        Assert.AreEqual(request.RedirectUri, consent.RedirectUri,
            "A loopback redirect uri must be matched on everything except its port.");

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(request, TestContext.CancellationToken);

        Assert.StartsWith(request.RedirectUri, approval.RedirectUrl,
            "And the code has to come back on the port the client is actually listening on, not the registered one.");

        // The port is the only thing relaxed: relaxing the path too would let any loopback listener collect this
        // user's codes.
        var wrongPath = BuildRequest(server, challenge);
        wrongPath.RedirectUri = "http://127.0.0.1:62435/somewhere-else";

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => scope.ServiceProvider.GetRequiredService<IOAuthController>().Review(wrongPath, TestContext.CancellationToken),
            "Only the port may differ - a different path on the same loopback host is still an unregistered uri.");
    }

    /// <summary>
    /// The other half of a real Visual Studio request: no <c>scope</c> at all, which RFC 6749 §3.3 allows and MCP's
    /// scope selection produces, so <c>invalid_scope</c> would turn away a conforming client.
    /// </summary>
    [TestMethod]
    public async Task AnOmittedScope_Should_DefaultToWhatTheResourceIsFor()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (verifier, challenge) = GeneratePkcePair();

        var request = BuildRequest(server, challenge);
        request.Scope = null;

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(request, TestContext.CancellationToken);

        var token = await ExchangeCode(server, QueryValueOf(approval.RedirectUrl, "code")!, verifier);

        Assert.AreEqual(OAuthScopes.DevMcp, token.GetProperty("scope").GetString(),
            "A request with no scope, for the dev-mcp resource, should end up with the dev-mcp scope - and the token " +
            "response has to say so, because that is how the client learns what it was actually granted.");
    }

    /// <summary>
    /// The OAuth scheme is a way <b>in</b> for other people's apps, not a way to sign in - and it sits on the same
    /// builder as Google and GitHub, where a display name would make it a sign-in button that cannot work.
    /// </summary>
    [TestMethod]
    public async Task TheOAuthScheme_Should_NotAppearAsAnExternalSignInProvider()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var providers = await scope.ServiceProvider.GetRequiredService<IIdentityController>()
                                                   .GetSupportedExternalAuthSchemes(TestContext.CancellationToken);

        Assert.DoesNotContain(AppAuthSchemes.OAUTH_BEARER, providers,
            "This app being an authorization server must not turn into a sign-in button on its own sign-in page.");
    }

    /// <summary>
    /// A grant is a row in the user's sessions list naming the app - the whole revocation story. If the id stops
    /// reaching the dto, an app looks like one of the user's devices.
    /// </summary>
    [TestMethod]
    public async Task AnAuthorizedApplication_Should_AppearInTheUsersSessionsWithItsClientId()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (verifier, challenge) = GeneratePkcePair();

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(BuildRequest(server, challenge), TestContext.CancellationToken);

        await ExchangeCode(server, QueryValueOf(approval.RedirectUrl, "code")!, verifier);

        var sessions = await scope.ServiceProvider.GetRequiredService<IUserController>()
                                                  .GetUserSessions(TestContext.CancellationToken);

        var grantedSession = sessions.SingleOrDefault(session => session.OAuthClientId is not null);

        Assert.IsNotNull(grantedSession, "The grant should show up as a session of this user, marked as an application.");
        Assert.AreEqual(TestClientId, grantedSession.OAuthClientId);

        Assert.AreEqual("Test MCP Client", grantedSession.OAuthClientName,
            "And carry the app's own name, so the row reads as something recognisable.");

        Assert.AreEqual(OAuthScopes.DevMcp, grantedSession.OAuthScope,
            "And what it may do: the token is the user's to revoke, and they can only judge that from the scope.");

        Assert.IsNull(grantedSession.DeviceInfo,
            "DeviceInfo is for devices. Putting the app's name there is what made the sessions list guess an " +
            "operating system from it and show Visual Studio under an Apple logo.");
    }

    /// <summary>
    /// Refresh rotates, and a token presented after its replacement revokes the grant immediately, with no tolerance
    /// window - <c>IdentityController.Refresh</c>'s ten-second allowance let the same token be redeemed twice, which is
    /// the replay OAuth 2.1 §4.3.1 says must revoke the grant.
    /// </summary>
    [TestMethod]
    public async Task AReusedRefreshToken_Should_BeRefusedImmediately_AndRevokeTheGrant()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (verifier, challenge) = GeneratePkcePair();

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(BuildRequest(server, challenge), TestContext.CancellationToken);

        var granted = await ExchangeCode(server, QueryValueOf(approval.RedirectUrl, "code")!, verifier);
        var refreshToken = granted.GetProperty("refresh_token").GetString()!;
        var sessionId = SessionIdOf(granted.GetProperty("access_token").GetString()!);

        var refreshed = await PostToken(server, new()
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = TestClientId
        });

        Assert.AreEqual(HttpStatusCode.OK, refreshed.Status, $"The refresh grant should work at all: {refreshed.Body}");

        // No waiting: the replay lands in the same second as the rotation, where a tolerance window would hide it.
        var replayed = await PostToken(server, new()
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = TestClientId
        });

        Assert.AreEqual(HttpStatusCode.BadRequest, replayed.Status,
            "A refresh token redeemed twice must be refused the second time, however quickly the second attempt comes.");
        Assert.Contains("invalid_grant", replayed.Body);

        await using var verificationScope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.IsFalse(await dbContext.UserSessions.AnyAsync(session => session.Id == sessionId, TestContext.CancellationToken),
            "And the grant itself has to go: two parties hold that token and the server cannot tell which one is the client.");
    }

    /// <summary>
    /// Nobody delegates what they do not hold: an ordinary account asking for <c>dev-mcp</c> is refused at consent,
    /// not handed a token that turns out to be worthless - and certainly not one that works.
    /// </summary>
    [TestMethod]
    public async Task AUserWithoutTheFeature_Should_BeRefusedAtConsent()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var (_, challenge) = GeneratePkcePair();

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => scope.ServiceProvider.GetRequiredService<IOAuthController>()
                       .Review(BuildRequest(server, challenge), TestContext.CancellationToken),
            "An account without System.DevMcp has nothing to grant for this scope, so consent must refuse rather than " +
            "present a screen whose only outcome is an empty grant.");
    }

    /// <summary>
    /// A global admin without 2FA holds <c>System.DevMcp</c>, so scope narrowing alone lets the grant through and the
    /// token comes back valid - then <c>/dev-mcp</c> refuses it on <see cref="AuthPolicies.TFA_ENABLED"/>. Review and
    /// Approve both refuse instead: a token whose only outcome is a 403 is worse than an explanation.
    /// </summary>
    [TestMethod]
    public async Task AnAdminWithoutTwoFactor_Should_BeRefusedAtConsent()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        await using var grant = await TestAccountUtils.MakeGlobalAdmin(server, scope, userId, TestContext.CancellationToken);

        var (_, challenge) = GeneratePkcePair();
        var oauthController = scope.ServiceProvider.GetRequiredService<IOAuthController>();

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => oauthController.Review(BuildRequest(server, challenge), TestContext.CancellationToken),
            "The consent screen must say why rather than offer a button whose grant cannot work.");

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => oauthController.Approve(BuildRequest(server, challenge), TestContext.CancellationToken),
            "And Approve enforces it too - the request reaches it from a query string the browser can edit.");
    }

    //#if (signalR == true)
    /// <summary>
    /// The chatbot's scope maps to no feature, which NarrowScopes grants vacuously. Says that path works, and that the
    /// two-factor gate is per resource rather than blanket: it must not fire for <c>/mcp</c>.
    /// </summary>
    [TestMethod]
    public async Task AnOrdinaryUser_Should_BeAbleToGrantTheChatScope()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var (verifier, challenge) = GeneratePkcePair();
        var request = BuildRequest(server, challenge, OAuthScopes.Chat, "/mcp");

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(request, TestContext.CancellationToken);

        var accessToken = (await ExchangeCode(server, QueryValueOf(approval.RedirectUrl, "code")!, verifier))
            .GetProperty("access_token").GetString()!;

        Assert.AreEqual($"{Issuer(server)}/mcp", new JwtSecurityToken(accessToken).Audiences.Single(),
            "The audience is the resource asked for, which is what keeps this token away from every other one.");

        var status = await DevMcpTestUtils.ProbeInitialize(server.WebAppServerAddress, "mcp", accessToken, TestContext.CancellationToken);

        Assert.IsFalse(status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            $"/mcp must accept a token granted for it, but answered {status}.");
    }

    /// <summary>
    /// With two resources a token can be replayed at the wrong one, and these are the worst pair to confuse: anybody
    /// may grant <c>chat</c>, while <c>dev-mcp</c> is a global admin with 2FA.
    /// </summary>
    [TestMethod]
    public async Task AChatToken_Should_NotReachDevMcp()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        await using var grant = await TestAccountUtils.MakeGlobalAdmin(server, scope, userId, TestContext.CancellationToken);

        var (verifier, challenge) = GeneratePkcePair();

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
            .Approve(BuildRequest(server, challenge, OAuthScopes.Chat, "/mcp"), TestContext.CancellationToken);

        var accessToken = (await ExchangeCode(server, QueryValueOf(approval.RedirectUrl, "code")!, verifier))
            .GetProperty("access_token").GetString()!;

        Assert.AreEqual(HttpStatusCode.Unauthorized,
            await DevMcpTestUtils.ProbeInitialize(server.WebAppServerAddress, "dev-mcp", accessToken, TestContext.CancellationToken),
            "Even held by a global admin, a token whose audience is /mcp must not authenticate at /dev-mcp.");
    }

    /// <summary>
    /// The dangerous direction: <c>/mcp</c> asks only for a signed-in user, so an audience checked against "some
    /// resource we serve" rather than the one requested lets a dev-mcp token walk in unopposed.
    /// </summary>
    [TestMethod]
    public async Task ADevMcpToken_Should_NotReachTheChatbot()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (verifier, challenge) = GeneratePkcePair();

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(BuildRequest(server, challenge), TestContext.CancellationToken);

        var accessToken = (await ExchangeCode(server, QueryValueOf(approval.RedirectUrl, "code")!, verifier))
            .GetProperty("access_token").GetString()!;

        Assert.AreEqual(HttpStatusCode.Unauthorized,
            await DevMcpTestUtils.ProbeInitialize(server.WebAppServerAddress, "mcp", accessToken, TestContext.CancellationToken),
            "/mcp would otherwise accept it: the token authenticates and /mcp asks for nothing more.");
    }
    //#endif

    /// <summary>
    /// The authorize request normalises the resource, and the token request must apply the same rule - or one spelling
    /// passes the first leg and fails the second, after the user has consented.
    /// </summary>
    [TestMethod]
    public async Task TheTokenEndpoint_Should_NormalizeTheResourceTheWayTheAuthorizationEndpointDid()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;

        var (verifier, challenge) = GeneratePkcePair();

        var approval = await scope.ServiceProvider.GetRequiredService<IOAuthController>()
                                                  .Approve(BuildRequest(server, challenge), TestContext.CancellationToken);

        var (status, body) = await PostToken(server, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = QueryValueOf(approval.RedirectUrl, "code")!,
            ["client_id"] = TestClientId,
            ["redirect_uri"] = TestRedirectUri,
            ["code_verifier"] = verifier,
            ["resource"] = $"{Issuer(server).ToUpperInvariant()}/dev-mcp/"
        });

        Assert.AreEqual(HttpStatusCode.OK, status,
            $"The same resource, differently cased and with a trailing slash, was refused at the token endpoint: {body}");
    }

    /// <summary>
    /// The consent url carries the client's query on. Rebuilding it through <see cref="Uri"/> unescapes what the client
    /// encoded, and a non-ascii <c>state</c> then fails Kestrel's header validation - a 500 instead of consent.
    /// </summary>
    [TestMethod]
    public async Task TheAuthorizeEndpoint_Should_ForwardTheQueryStringAsItArrived()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        var (_, challenge) = GeneratePkcePair();

        var authorizeUrl = QueryHelpers.AddQueryString("oauth/authorize", new Dictionary<string, string?>
        {
            ["client_id"] = TestClientId,
            ["redirect_uri"] = TestRedirectUri,
            ["response_type"] = "code",
            ["scope"] = OAuthScopes.DevMcp,
            ["state"] = "✓ a+b",
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["resource"] = $"{Issuer(server)}/dev-mcp"
        });

        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = server.WebAppServerAddress
        };

        using var response = await httpClient.GetAsync(authorizeUrl, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode,
            "A valid request goes to the consent page whatever the state looks like.");

        var location = response.Headers.Location?.OriginalString ?? "";

        Assert.Contains(PageUrls.OAuthConsent, location);
        Assert.Contains("state=%E2%9C%93%20a%2Bb", location,
            "The state must reach the consent page encoded exactly as the client sent it, or it hands something else back.");
    }

    /// <summary>
    /// The refresh grant picks its protector by the presented token's own audience, and the app's first-party refresh
    /// token has one too - so it has to be turned away before that choice, by not naming a resource of ours.
    /// </summary>
    [TestMethod]
    public async Task AFirstPartyRefreshToken_Should_BeRefusedAtTheTokenEndpoint()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(), ConfigureTestClient)
                    .Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var firstPartyRefreshToken = await scope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("refresh_token");
        Assert.IsNotNull(firstPartyRefreshToken, "Sign-in should have stored a refresh token.");

        foreach (var clientId in new[] { "", TestClientId })
        {
            var (status, body) = await PostToken(server, new()
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = firstPartyRefreshToken,
                ["client_id"] = clientId
            });

            Assert.AreEqual(HttpStatusCode.BadRequest, status, $"The app's own refresh token was accepted by the OAuth token endpoint: {body}");
            Assert.AreEqual("invalid_grant", JsonDocument.Parse(body).RootElement.GetProperty("error").GetString());
        }
    }

    private static string Issuer(AppTestServer server) => server.WebAppServerAddress.ToString().TrimEnd('/');

    private static OAuthAuthorizeRequestDto BuildRequest(AppTestServer server, string codeChallenge, string scope = OAuthScopes.DevMcp, string resourcePath = "/dev-mcp") => new()
    {
        ClientId = TestClientId,
        RedirectUri = TestRedirectUri,
        ResponseType = "code",
        Scope = scope,
        State = "test-state",
        CodeChallenge = codeChallenge,
        CodeChallengeMethod = "S256",
        Resource = $"{Issuer(server)}{resourcePath}"
    };

    private async Task<JsonElement> ExchangeCode(AppTestServer server, string code, string codeVerifier, string? clientId = null, string? redirectUri = null)
    {
        var (status, body) = await PostToken(server, new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = clientId ?? TestClientId,
            ["redirect_uri"] = redirectUri ?? TestRedirectUri,
            ["code_verifier"] = codeVerifier
        });

        Assert.AreEqual(HttpStatusCode.OK, status, $"The token exchange failed: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private async Task<(HttpStatusCode Status, string Body)> PostToken(AppTestServer server, Dictionary<string, string> form)
    {
        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        // Form encoded with no Authorization header - what AutoCsrfProtectionFilter rejects, which is why the token
        // endpoint is a minimal api.
        using var content = new FormUrlEncodedContent(form);
        using var response = await httpClient.PostAsync("oauth/token", content, TestContext.CancellationToken);

        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    private static Guid SessionIdOf(string accessToken)
    {
        return Guid.Parse(new JwtSecurityToken(accessToken).Claims.Single(claim => claim.Type is AppClaimTypes.SESSION_ID).Value);
    }

    private static string? QueryValueOf(string url, string key)
    {
        return QueryHelpers.ParseQuery(new Uri(url).Query).TryGetValue(key, out var value) ? value.ToString() : null;
    }

    private static (string Verifier, string Challenge) GeneratePkcePair()
    {
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return (verifier, challenge);
    }
}
