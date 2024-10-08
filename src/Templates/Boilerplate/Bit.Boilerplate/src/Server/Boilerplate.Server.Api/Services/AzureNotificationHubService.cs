using System.Web;
using System.Text;
using System.Security.Cryptography;
using Boilerplate.Shared.Dtos.PushNotification;

namespace Boilerplate.Server.Api.Services;

public class AzureNotificationHubService(HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    AppSettings appSettings)
{
    public async Task CreateOrUpdateInstallation([Required] DeviceInstallationDto deviceInstallation, CancellationToken cancellationToken)
    {
        List<string> tags = [CultureInfo.CurrentUICulture.Name /* To send push notification to all users with specific culture */];

        using HttpRequestMessage request = BuildRequest(HttpMethod.Put, $"installations/{deviceInstallation.InstallationId}");

        var userId = httpContextAccessor.HttpContext!.User.IsAuthenticated() ? httpContextAccessor.HttpContext.User.GetUserId().ToString() : null;

        if (userId is not null)
        {
            tags.Add(userId);
        }

        request.Content = new StringContent($@"{{
            ""installationId"": ""{deviceInstallation.InstallationId}"",
            ""userId"": {(userId is null ? "null" : $"\"{userId}\"")},
            ""pushChannel"": {(deviceInstallation.Platform is "browser" ? deviceInstallation.PushChannel : $"\"{deviceInstallation.PushChannel}\"")},
            ""pushChannelExpired"": null,
            ""platform"": ""{deviceInstallation.Platform}"",
            ""expirationTime"": ""{DateTimeOffset.UtcNow.AddMonths(1).DateTime:yyyy-MM-ddTHH:mm:sszzz}"",
            ""tags"": [{string.Join(',', tags.Select(t => $"\"{t}\""))}] }}", Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exp)
        {
            throw new UnknownException(await response.Content.ReadAsStringAsync(cancellationToken), exp);
        }
    }

    public async Task RequestPush(string? title = null, string? message = null, string? action = null, string[]? tags = null, CancellationToken cancellationToken = default)
    {
        tags ??= [];
        var tagsHeaderValue = string.Join("||", tags);

        using var apnsRequest = BuildRequest(HttpMethod.Post, "messages");
        apnsRequest.Headers.Add("ServiceBusNotification-Format", "apple");
        if (tags.Any())
        {
            apnsRequest.Headers.Add("ServiceBusNotification-Tags", tagsHeaderValue);
        }
        // https://learn.microsoft.com/en-us/rest/api/notificationhubs/send-apns-native-notification#request-body
        apnsRequest.Content = new StringContent($@"{{
                ""message"": ""{message}"",
                ""aps"":
                {{
                    ""content-available"": 1
                }},
                ""action"": ""{action}""
            }}", Encoding.UTF8, "application/json");

        using var fcm1Request = BuildRequest(HttpMethod.Post, "messages");
        fcm1Request.Headers.Add("ServiceBusNotification-Format", "fcmv1");
        if (tags.Any())
        {
            fcm1Request.Headers.Add("ServiceBusNotification-Tags", tagsHeaderValue);
        }
        // https://learn.microsoft.com/en-us/azure/notification-hubs/firebase-migration-rest#option-3-fcmv1-native-notification-audience-send
        fcm1Request.Content = new StringContent($@"{{
        ""message"":
        {{
            ""notification"":
            {{
                ""title"": ""{title}"",
                ""body"": ""{message}""
            }},
            ""data"": {{ ""action"": ""{action}"" }}
        }} }}", Encoding.UTF8, "application/json");

        using var browserRequest = BuildRequest(HttpMethod.Post, "messages");
        browserRequest.Headers.Add("ServiceBusNotification-Format", "browser");
        if (tags.Any())
        {
            browserRequest.Headers.Add("ServiceBusNotification-Tags", tagsHeaderValue);
        }
        browserRequest.Content = new StringContent($@"{{
            ""title"": ""{title}"",
            ""body"": ""{message}""
        }}", Encoding.UTF8, "application/json");

        var responses = await Task.WhenAll(httpClient.SendAsync(apnsRequest, cancellationToken),
                     httpClient.SendAsync(fcm1Request, cancellationToken),
                     httpClient.SendAsync(browserRequest, cancellationToken));

        foreach (var response in responses)
        {
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception exp)
            {
                throw new UnknownException(await response.Content.ReadAsStringAsync(cancellationToken), exp);
            }
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod httpMethod, string requestUrl)
    {
        if (appSettings.NotificationHub.Configured is false)
            throw new InvalidOperationException("Notification hub is not configured.");

        Dictionary<string, string> connectionStringKeyValues = appSettings.NotificationHub.ConnectionString!
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(split => split[0].Trim(), split => split[1].Trim());

        var resourceUri = $"{connectionStringKeyValues["Endpoint"].Replace("sb://", "https://")}{appSettings.NotificationHub.Name}/{requestUrl}?api-version=2020-06";
        HttpRequestMessage request = new HttpRequestMessage(httpMethod, new Uri(resourceUri));
        request.Headers.TryAddWithoutValidation("User-Agent", "NHub/2020-06 (api-origin=DotNetSdk;os=Win32NT;os-version=10.0.22631.0)");
        var normalizedResourceUri = request.RequestUri!.ToString().Replace("https://", "http://").Replace("?api-version=2020-06", "/");
        var sasToken = CreateToken(normalizedResourceUri, connectionStringKeyValues["SharedAccessKeyName"], connectionStringKeyValues["SharedAccessKey"]);

        request.Headers.Add("Authorization", sasToken);

        return request;
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/rest/api/eventhub/generate-sas-token#c
    /// </summary>
    private static string CreateToken(string resourceUri, string keyName, string key)
    {
        TimeSpan sinceEpoch = DateTimeOffset.UtcNow - new DateTime(1970, 1, 1);
        var week = 60 * 60 * 24 * 7;
        var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + week);
        string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
        using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        var sasToken = FormattableString.Invariant($"SharedAccessSignature sr={HttpUtility.UrlEncode(resourceUri)}&sig={HttpUtility.UrlEncode(signature)}&se={expiry}&skn={keyName}");
        return sasToken;
    }
}
