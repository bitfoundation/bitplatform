using System.Buffers.Text;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.Keys;

public sealed record AppleApiKey(string IssuerId, string KeyId, string PrivateKey);

public sealed record AppleSigningResult(byte[] Pkcs12, string Pkcs12Password, string ProfileName, byte[] Profile, string? TeamId, DateTimeOffset CertificateExpiry);

public sealed class AppleSigningException(string message) : Exception(message);

public sealed partial class AppleSigning(HttpMessageHandler? handler = null)
{
    public const string ApiAddress = "https://api.appstoreconnect.apple.com/";

    public const string CertificatesPage = "https://developer.apple.com/account/resources/certificates/list";

    public const string CertificateType = "DISTRIBUTION";

    public const string ProfileType = "IOS_APP_STORE";

    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);

    private const string PasswordCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public static string DefaultProfileName(string bundleId) => $"{bundleId} App Store";

    public static string Attempt(AppleApiKey key, string bundleId, string profileName)
        => $"bit was creating the App Store signing of {bundleId} with the App Store Connect API ({ApiAddress}v1) and the API key {key.KeyId}: "
            + $"register the bundle id if Apple doesn't have it, create an Apple Distribution certificate ({CertificateType}) for a new RSA 2048 key, "
            + $"create the App Store provisioning profile ({ProfileType}) \"{profileName}\" for the two, and put the certificate and its key in a .p12. "
            + "The same can be done by hand at https://developer.apple.com/account/resources.";

    public async Task<AppleSigningResult> CreateAppStoreSigningAsync(AppleApiKey key, string bundleId, string profileName, CancellationToken cancellationToken)
    {
        ImportKey(key.PrivateKey).Dispose();

        using var ownHandler = handler is null ? new HttpClientHandler() : null;
        using var http = new HttpClient(handler ?? ownHandler!, disposeHandler: false) { BaseAddress = new Uri(ApiAddress), Timeout = TimeSpan.FromMinutes(1) };

        var bundle = await FindOrRegisterBundleIdAsync(http, key, bundleId, cancellationToken);

        using var privateKey = RSA.Create(2048);
        var (certificateId, certificate) = await CreateCertificateAsync(http, key, CreateSigningRequest(privateKey), cancellationToken);

        using (certificate)
        {
            try
            {
                var (name, profile) = await ReplaceProfileAsync(http, key, profileName, bundle.Id, certificateId, cancellationToken);
                var password = CreatePassword();

                return new AppleSigningResult(ExportPkcs12(certificate, privateKey, password), password, name, profile, TeamIdOf(certificate) ?? bundle.SeedId, new DateTimeOffset(certificate.NotAfter.ToUniversalTime()));
            }
            catch
            {
                await RevokeAsync(http, key, certificateId);
                throw;
            }
        }
    }

    internal static string CreateToken(AppleApiKey key, DateTimeOffset now)
    {
        using var signer = ImportKey(key.PrivateKey);

        var header = new JsonObject { ["alg"] = "ES256", ["kid"] = key.KeyId, ["typ"] = "JWT" };
        var payload = new JsonObject { ["iss"] = key.IssuerId, ["iat"] = now.ToUnixTimeSeconds(), ["exp"] = (now + TokenLifetime).ToUnixTimeSeconds(), ["aud"] = "appstoreconnect-v1" };
        var signingInput = $"{Base64Url.EncodeToString(Encoding.UTF8.GetBytes(header.ToJsonString()))}.{Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payload.ToJsonString()))}";

        return $"{signingInput}.{Base64Url.EncodeToString(signer.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256))}";
    }

    internal static string CreateSigningRequest(RSA key)
    {
        return new CertificateRequest(new X500DistinguishedName("CN=bit CLI"), key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).CreateSigningRequestPem();
    }

    internal static string CreatePassword() => RandomNumberGenerator.GetString(PasswordCharacters, 24);

    internal static byte[] ExportPkcs12(X509Certificate2 certificate, RSA privateKey, string password)
    {
        using var withKey = certificate.CopyWithPrivateKey(privateKey);
        return withKey.ExportPkcs12(Pkcs12ExportPbeParameters.Pkcs12TripleDesSha1, password);
    }

    internal static string? TeamIdOf(X509Certificate2 certificate)
    {
        foreach (var name in certificate.SubjectName.EnumerateRelativeDistinguishedNames())
        {
            if (name.HasMultipleElements is false && name.GetSingleElementType().Value is "2.5.4.11")
                return name.GetSingleElementValue();
        }

        return null;
    }

    internal static string BundleIdName(string bundleId)
    {
        var name = NonAlphanumericRegex().Replace(bundleId, " ").Trim();
        return name.Length is 0 ? "App" : name;
    }

    private static ECDsa ImportKey(string pem)
    {
        var key = ECDsa.Create();

        try
        {
            key.ImportFromPem(pem);
            key.ExportParameters(includePrivateParameters: true);

            if (key.KeySize is not 256)
                throw new CryptographicException();

            return key;
        }
        catch (Exception exp) when (exp is CryptographicException or ArgumentException)
        {
            key.Dispose();
            throw new AppleSigningException("The private key isn't an App Store Connect API key. Pass the AuthKey_<key id>.p8 file App Store Connect gave when the key was created.");
        }
    }

    private static async Task<(string Id, string? SeedId)> FindOrRegisterBundleIdAsync(HttpClient http, AppleApiKey key, string bundleId, CancellationToken cancellationToken)
    {
        var found = await SendAsync(http, key, HttpMethod.Get, $"v1/bundleIds?filter[identifier]={Uri.EscapeDataString(bundleId)}&limit=200", null, cancellationToken);

        if (found.Succeeded is false)
            throw Failure(found, $"look up the bundle id {bundleId}");

        foreach (var item in Items(found.Body))
        {
            if (string.Equals(Text(Attributes(item), "identifier"), bundleId, StringComparison.OrdinalIgnoreCase))
                return (Id(item), Text(Attributes(item), "seedId"));
        }

        var created = await SendAsync(http, key, HttpMethod.Post, "v1/bundleIds", new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "bundleIds",
                ["attributes"] = new JsonObject { ["identifier"] = bundleId, ["name"] = BundleIdName(bundleId), ["platform"] = "IOS" }
            }
        }, cancellationToken);

        if (created.Succeeded is false)
            throw Failure(created, $"register the bundle id {bundleId}");

        var data = Data(created.Body);
        return (Id(data), Text(Attributes(data), "seedId"));
    }

    private static async Task<(string Id, X509Certificate2 Certificate)> CreateCertificateAsync(HttpClient http, AppleApiKey key, string signingRequest, CancellationToken cancellationToken)
    {
        var created = await SendAsync(http, key, HttpMethod.Post, "v1/certificates", new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "certificates",
                ["attributes"] = new JsonObject { ["certificateType"] = CertificateType, ["csrContent"] = signingRequest }
            }
        }, cancellationToken);

        if (created.Status is HttpStatusCode.Conflict && await LimitReachedAsync(http, key, created, cancellationToken) is { } limitReached)
            throw limitReached;

        if (created.Succeeded is false)
            throw Failure(created, "create a distribution certificate");

        var data = Data(created.Body);
        var content = Text(Attributes(data), "certificateContent") ?? throw new AppleSigningException("App Store Connect created the certificate but didn't send it back. Revoke it at " + CertificatesPage + " and run this again.");

        return (Id(data), X509CertificateLoader.LoadCertificate(Convert.FromBase64String(content)));
    }

    private static async Task<AppleSigningException?> LimitReachedAsync(HttpClient http, AppleApiKey key, Response conflict, CancellationToken cancellationToken)
    {
        var existing = await SendAsync(http, key, HttpMethod.Get, "v1/certificates?filter[certificateType]=DISTRIBUTION,IOS_DISTRIBUTION&limit=200", null, cancellationToken);

        var certificates = existing.Succeeded
            ? Items(existing.Body).Select(item => Attributes(item)).Select(attributes => $"  - {Text(attributes, "name") ?? Text(attributes, "displayName") ?? "a certificate"}{Expires(Text(attributes, "expirationDate"))}{(Text(attributes, "serialNumber") is { } serial ? $", serial {serial}" : "")}").ToList()
            : [];

        if (certificates.Count is 0)
            return null;

        return new AppleSigningException(
            $"The team already has as many distribution certificates as Apple allows, so it refused a new one ({FirstError(conflict.Body).Detail ?? "409 Conflict"}). The team's distribution certificates:\n"
            + string.Join('\n', certificates)
            + $"\nRevoke one that no build signs with anymore at {CertificatesPage}, then run this again. Apps already in the App Store keep working when their certificate is revoked.");
    }

    private static string Expires(string? date)
    {
        return DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expires) ? $", expires {expires.UtcDateTime:yyyy-MM-dd}" : "";
    }

    private static async Task<(string Name, byte[] Content)> ReplaceProfileAsync(HttpClient http, AppleApiKey key, string profileName, string bundleId, string certificateId, CancellationToken cancellationToken)
    {
        var existing = await SendAsync(http, key, HttpMethod.Get, $"v1/profiles?filter[name]={Uri.EscapeDataString(profileName)}&limit=200", null, cancellationToken);

        if (existing.Succeeded is false)
            throw Failure(existing, $"look up the profile {profileName}");

        foreach (var item in Items(existing.Body))
        {
            if (string.Equals(Text(Attributes(item), "name"), profileName, StringComparison.OrdinalIgnoreCase) is false)
                continue;

            var deleted = await SendAsync(http, key, HttpMethod.Delete, $"v1/profiles/{Uri.EscapeDataString(Id(item))}", null, cancellationToken);

            if (deleted.Succeeded is false && deleted.Status is not HttpStatusCode.NotFound)
                throw Failure(deleted, $"replace the profile {profileName}");
        }

        var created = await SendAsync(http, key, HttpMethod.Post, "v1/profiles", new JsonObject
        {
            ["data"] = new JsonObject
            {
                ["type"] = "profiles",
                ["attributes"] = new JsonObject { ["name"] = profileName, ["profileType"] = ProfileType },
                ["relationships"] = new JsonObject
                {
                    ["bundleId"] = new JsonObject { ["data"] = new JsonObject { ["type"] = "bundleIds", ["id"] = bundleId } },
                    ["certificates"] = new JsonObject { ["data"] = new JsonArray(new JsonObject { ["type"] = "certificates", ["id"] = certificateId }) }
                }
            }
        }, cancellationToken);

        if (created.Succeeded is false)
            throw Failure(created, $"create the profile {profileName}");

        var attributes = Attributes(Data(created.Body));
        var content = Text(attributes, "profileContent") ?? throw new AppleSigningException($"App Store Connect created the profile {profileName} but didn't send it back.");

        return (Text(attributes, "name") ?? profileName, Convert.FromBase64String(content));
    }

    private static async Task RevokeAsync(HttpClient http, AppleApiKey key, string certificateId)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            await SendAsync(http, key, HttpMethod.Delete, $"v1/certificates/{Uri.EscapeDataString(certificateId)}", null, timeout.Token);
        }
        catch (Exception exp) when (exp is AppleSigningException or OperationCanceledException)
        {
        }
    }

    private static async Task<Response> SendAsync(HttpClient http, AppleApiKey key, HttpMethod method, string path, JsonObject? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(key, DateTimeOffset.UtcNow));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("bit-cli", BuildInfo.Version));
        request.Headers.Accept.ParseAdd("application/json");

        if (body is not null)
        {
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            return new Response(response.StatusCode, Parse(await response.Content.ReadAsStringAsync(cancellationToken)));
        }
        catch (HttpRequestException exp)
        {
            throw new AppleSigningException($"Couldn't reach App Store Connect: {exp.Message}");
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            throw new AppleSigningException($"App Store Connect didn't answer within {http.Timeout.TotalSeconds:0} seconds.");
        }
    }

    private static JsonElement Parse(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static AppleSigningException Failure(Response response, string action)
    {
        var (code, detail) = FirstError(response.Body);
        var answer = detail is null ? "" : $" App Store Connect says: {detail}";

        return new AppleSigningException(response.Status switch
        {
            HttpStatusCode.Unauthorized => "App Store Connect didn't accept the API key. Check the issuer id, the key id and the .p8 file, and that the key is still active in App Store Connect, under Users and Access, Integrations.",
            HttpStatusCode.Forbidden when code?.Contains("REQUIRED_AGREEMENTS", StringComparison.Ordinal) is true => $"Couldn't {action}, since the team hasn't accepted Apple's latest agreement. The team's Account Holder accepts it at https://developer.apple.com/account.{answer}",
            HttpStatusCode.Forbidden => $"The API key isn't allowed to {action}. Certificates and profiles need a team key with the Admin role: create one in App Store Connect, under Users and Access, Integrations.{answer}",
            _ => $"Couldn't {action}. App Store Connect answered {(int)response.Status}{(detail is null ? "." : $": {detail}")}"
        });
    }

    private static (string? Code, string? Detail) FirstError(JsonElement body)
    {
        if (body.ValueKind is JsonValueKind.Object && body.TryGetProperty("errors", out var errors) && errors.ValueKind is JsonValueKind.Array && errors.GetArrayLength() > 0)
            return (Text(errors[0], "code"), Text(errors[0], "detail") ?? Text(errors[0], "title"));

        return (null, null);
    }

    private static IEnumerable<JsonElement> Items(JsonElement body)
    {
        return body.ValueKind is JsonValueKind.Object && body.TryGetProperty("data", out var data) && data.ValueKind is JsonValueKind.Array ? data.EnumerateArray() : [];
    }

    private static JsonElement Data(JsonElement body)
    {
        return body.ValueKind is JsonValueKind.Object && body.TryGetProperty("data", out var data) ? data : default;
    }

    private static JsonElement Attributes(JsonElement resource)
    {
        return resource.ValueKind is JsonValueKind.Object && resource.TryGetProperty("attributes", out var attributes) ? attributes : default;
    }

    private static string Id(JsonElement resource)
    {
        return Text(resource, "id") ?? throw new AppleSigningException("App Store Connect sent back a resource without an id.");
    }

    private static string? Text(JsonElement element, string name)
    {
        return element.ValueKind is JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String ? value.GetString() : null;
    }

    [GeneratedRegex("[^A-Za-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();

    private sealed record Response(HttpStatusCode Status, JsonElement Body)
    {
        public bool Succeeded => (int)Status is >= 200 and < 300;
    }
}
