//+:cnd:noEmit
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Distributed;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Server.Api.Controllers.Identity;

[ApiController, Route("api/[controller]/[action]")]
public partial class WebAuthnController : AppControllerBase, IWebAuthnController
{
    [AutoInject] private IFido2 fido2 = default!;
    [AutoInject] private IDistributedCache cache = default!;
    [AutoInject] private UserManager<User> userManager = default!;


    [HttpGet]
    public async Task<CredentialCreateOptions> GetCredentialOptions(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException("User");

        var existingCredentials = DbContext.WebAuthnCredential.Where(c => c.UserId == user.Id);
        var existingKeys = existingCredentials.Select(c => new PublicKeyCredentialDescriptor(PublicKeyCredentialType.PublicKey,
                                                                                             c.Id,
                                                                                             c.Transports));
        var fidoUser = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(user.Id.ToString()),
            Name = user.Email ?? user.PhoneNumber ?? user.UserName,
            DisplayName = user.DisplayName
        };

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = [.. existingKeys],
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = new AuthenticationExtensionsClientInputs
            {
                CredProps = true,
                Extensions = true,
                UserVerificationMethod = true,
            }
        });

        var key = $"WebAuthn_Options_{user.Id}";
        await cache.SetAsync(key, Encoding.UTF8.GetBytes(options.ToJson()), cancellationToken);

        return options;
    }

    [HttpPut()]
    public async Task CreateCredentialAsync([FromRoute] string username,
                                            [FromBody] AuthenticatorAttestationRawResponse attestationResponse,
                                            CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException("User");

        var key = $"WebAuthn_Options_{user.Id}";
        var cachedBytes = await cache.GetAsync(key, cancellationToken) ?? throw new InvalidOperationException("no create credential options found in the cache.");
        var jsonOptions = Encoding.UTF8.GetString(cachedBytes);
        var options = CredentialCreateOptions.FromJson(jsonOptions);

        var credential = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = attestationResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = CredentialIdUniqueToUserAsync
        }, cancellationToken: cancellationToken);

        await DbContext.WebAuthnCredential.AddAsync(new()
        {
            Id = credential.Id,
            PublicKey = credential.PublicKey,
            UserHandle = credential.User.Id,
            SignCount = credential.SignCount,
            RegDate = DateTimeOffset.UtcNow,
            AaGuid = credential.AaGuid,
            Transports = credential.Transports,
            AttestationFormat = credential.AttestationFormat,
            IsBackupEligible = credential.IsBackupEligible,
            IsBackedUp = credential.IsBackedUp,
            AttestationObject = credential.AttestationObject,
            AttestationClientDataJson = credential.AttestationClientDataJson,
        });

        await cache.RemoveAsync(key, cancellationToken);
    }

    private static async Task<bool> CredentialIdUniqueToUserAsync(IsCredentialIdUniqueToUserParams args, CancellationToken cancellationToken)
    {
        var users = await _demoStorage.GetUsersByCredentialIdAsync(args.CredentialId, cancellationToken);
        return users.Count <= 0;
    }

    [HttpGet("{username}/assertion-options")]
    [HttpGet("assertion-options")]
    public AssertionOptions MakeAssertionOptions([FromRoute] string? username, [FromQuery] UserVerificationRequirement? userVerification)
    {
        try
        {
            var existingKeys = new List<PublicKeyCredentialDescriptor>();
            if (!string.IsNullOrEmpty(username))
            {
                // 1. Get user and their credentials from DB
                var user = _demoStorage.GetUser(username);

                if (user != null)
                    existingKeys = _demoStorage.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();
            }

            var exts = new AuthenticationExtensionsClientInputs
            {
                UserVerificationMethod = true,
                Extensions = true
            };

            // 2. Create options (usernameless users will be prompted by their device to select a credential from their own list)
            var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
            {
                AllowedCredentials = existingKeys,
                UserVerification = userVerification ?? UserVerificationRequirement.Discouraged,
                Extensions = exts
            });

            // 4. Temporarily store options, session/in-memory cache/redis/db
            _pendingAssertions[new string(options.Challenge.Select(b => (char)b).ToArray())] = options;

            // 5. return options to client
            return options;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Verifies an assertion response from a client, generating a new JWT for the user.
    /// </summary>
    /// <param name="clientResponse">The client's authenticator's response to the challenge.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// Either a new JWT header or an error message.
    /// Example successful response:
    /// "Bearer eyyylmaooimtotallyatoken"
    /// Example error response:
    /// "Error: Invalid assertion"
    /// </returns>
    [HttpPost("assertion")]
    public async Task<string> MakeAssertionAsync([FromBody] AuthenticatorAssertionRawResponse clientResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get the assertion options we sent the client remove them from memory so they can't be used again
            var response = JsonSerializer.Deserialize<AuthenticatorResponse>(clientResponse.Response.ClientDataJson);
            if (response is null)
            {
                return "Error: Could not deserialize client data";
            }

            var key = new string(response.Challenge.Select(b => (char)b).ToArray());
            if (!_pendingAssertions.TryGetValue(key, out var options))
            {
                return "Error: Challenge not found, please get a new one via GET /{username?}/assertion-options";
            }
            _pendingAssertions.Remove(key);

            // 2. Get registered credential from database
            var creds = _demoStorage.GetCredentialById(clientResponse.Id) ?? throw new Exception("Unknown credentials");

            // 3. Make the assertion
            var res = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = clientResponse,
                OriginalOptions = options,
                StoredPublicKey = creds.PublicKey,
                StoredSignatureCounter = creds.SignCount,
                IsUserHandleOwnerOfCredentialIdCallback = UserHandleOwnerOfCredentialIdAsync
            }, cancellationToken: cancellationToken);

            // 4. Store the updated counter
            _demoStorage.UpdateCounter(res.CredentialId, res.SignCount);


            // 5. return result to client
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateEncodedJwt(
                HttpContext.Request.Host.Host,
                HttpContext.Request.Headers.Referer,
                new ClaimsIdentity(new Claim[] { new(ClaimTypes.Actor, Encoding.UTF8.GetString(creds.UserHandle)) }),
                DateTime.Now.Subtract(TimeSpan.FromMinutes(1)),
                DateTime.Now.AddDays(1),
                DateTime.Now,
                _signingCredentials,
                null);

            if (token is null)
            {
                return "Error: Token couldn't be created";
            }

            return $"Bearer {token}";
        }
        catch (Exception e)
        {
            return $"Error: {FormatException(e)}";
        }
    }

    private static async Task<bool> UserHandleOwnerOfCredentialIdAsync(IsUserHandleOwnerOfCredentialIdParams args, CancellationToken cancellationToken)
    {
        var storedCreds = await _demoStorage.GetCredentialsByUserHandleAsync(args.UserHandle, cancellationToken);
        return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
    }
}
