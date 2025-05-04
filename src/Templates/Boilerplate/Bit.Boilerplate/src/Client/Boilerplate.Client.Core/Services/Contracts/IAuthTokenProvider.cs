using System.Text;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface IAuthTokenProvider
{
    Task<string?> GetAccessToken();

    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    public static ClaimsPrincipal ParseAccessToken(string? accessToken, bool validateExpiry)
    {
        if (string.IsNullOrEmpty(accessToken) is true)
            return Anonymous();

        var claims = ReadClaims(accessToken, validateExpiry);

        if (claims is null)
            return Anonymous();

        var identity = new ClaimsIdentity(claims: claims, authenticationType: "Bearer", nameType: "name", roleType: "role");

        var claimPrinciple = new ClaimsPrincipal(identity);

        return claimPrinciple;
    }

    private static IEnumerable<Claim>? ReadClaims(string accessToken, bool validateExpiry)
    {
        var parsedClaims = DeserializeAccessToken(accessToken);

        if (validateExpiry && long.TryParse(parsedClaims["exp"].ToString(), out var expSeconds))
        {
            var expirationDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            if (expirationDate <= DateTimeOffset.UtcNow)
                return null;
        }

        var claims = new List<Claim>();
        foreach (var keyValue in parsedClaims)
        {
            if (keyValue.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in keyValue.Value.EnumerateArray())
                {
                    claims.Add(new Claim(keyValue.Key, element.ToString() ?? string.Empty));
                }
            }
            else
            {
                claims.Add(new Claim(keyValue.Key, keyValue.Value.ToString() ?? string.Empty));
            }
        }

        return claims;
    }

    private static Dictionary<string, JsonElement> DeserializeAccessToken(string accessToken)
    {
        // Split the token to get the payload
        string base64UrlPayload = accessToken.Split('.')[1];

        // Convert the payload from Base64Url format to Base64
        string base64Payload = ConvertBase64UrlToBase64(base64UrlPayload);

        // Decode the Base64 string to get a JSON string
        string jsonPayload = Encoding.UTF8.GetString(Convert.FromBase64String(base64Payload));

        // Deserialize the JSON string to a dictionary
        var claims = JsonSerializer.Deserialize(jsonPayload, AppJsonContext.Default.Options.GetTypeInfo<Dictionary<string, JsonElement>>())!;

        return claims;
    }

    private static string ConvertBase64UrlToBase64(string base64Url)
    {
        base64Url = base64Url.Replace('-', '+').Replace('_', '/');

        // Adjust base64Url string length for padding
        switch (base64Url.Length % 4)
        {
            case 2:
                base64Url += "==";
                break;
            case 3:
                base64Url += "=";
                break;
        }

        return base64Url;
    }
}
