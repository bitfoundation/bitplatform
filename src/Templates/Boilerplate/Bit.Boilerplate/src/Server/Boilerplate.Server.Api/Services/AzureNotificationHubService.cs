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

        var installation = new
        {
            deviceInstallation.InstallationId,
            PushChannel = deviceInstallation.Platform is "browser" ? new
            {
                deviceInstallation.Endpoint,
                deviceInstallation.P256dh,
                deviceInstallation.Auth
            } as object : deviceInstallation.PushChannel,
            ExpirationTime = DateTimeOffset.UtcNow.AddMonths(1).DateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"), // 2024-10-06T20:20:43+02:00 1997-07-16T19:20+01:00
            Tags = tags,
            UserId = httpContextAccessor.HttpContext!.User.IsAuthenticated() ? httpContextAccessor.HttpContext.User.GetUserId().ToString() : null,
            platform = deviceInstallation.Platform
        };

        using HttpRequestMessage request = BuildRequest(HttpMethod.Post, $"installations/{deviceInstallation.InstallationId}?api-version=2020-06");
        (await httpClient.SendAsync(request, cancellationToken)).EnsureSuccessStatusCode();
    }

    public async Task RequestPush(string? title = null, string? message = null, string? action = null, string[]? tags = null, bool silent = false, CancellationToken cancellationToken = default)
    {
        tags ??= [];
        var tagsHeaderValue = string.Join(',', tags);

        using var apnsRequest = BuildRequest(HttpMethod.Post, "messages?api-version=2015-04");
        apnsRequest.Headers.Add("ServiceBusNotification-Format", "apple");
        apnsRequest.Headers.Add("ServiceBusNotification-Tags", tagsHeaderValue);
        apnsRequest.Content = new StringContent(JsonSerializer.Serialize(new
        {
            // https://learn.microsoft.com/en-us/rest/api/notificationhubs/send-apns-native-notification#request-body
            aps = new
            {
                alert = message,
                sound = silent ? "" : "default",
            },
            action = action
        }), Encoding.UTF8, "application/json");

        using var fcm1Request = BuildRequest(HttpMethod.Post, "messages?api-version=2015-04");
        fcm1Request.Headers.Add("ServiceBusNotification-Format", "fcmV1");
        apnsRequest.Headers.Add("ServiceBusNotification-Tags", tagsHeaderValue);
        fcm1Request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            // https://learn.microsoft.com/en-us/azure/notification-hubs/firebase-migration-rest#option-3-fcmv1-native-notification-audience-send
            message = new
            {
                notification = new
                {
                    title = title,
                    body = message,
                    sound = silent ? "" : "default"
                },
                data = new
                {
                    action
                }
            }
        }), Encoding.UTF8, "application/json");

        using var browserRequest = BuildRequest(HttpMethod.Post, "messages?api-version=2015-04");
        browserRequest.Headers.Add("ServiceBusNotification-Format", "browser");
        apnsRequest.Headers.Add("ServiceBusNotification-Tags", tagsHeaderValue);
        browserRequest.Content = new StringContent(JsonSerializer.Serialize(new
        {
            title = title,
            body = message
        }), Encoding.UTF8, "application/json");

        await Task.WhenAll(httpClient.SendAsync(apnsRequest, cancellationToken).ContinueWith(_ => _.Result.EnsureSuccessStatusCode()),
            httpClient.SendAsync(fcm1Request, cancellationToken).ContinueWith(_ => _.Result.EnsureSuccessStatusCode()),
            httpClient.SendAsync(browserRequest, cancellationToken).ContinueWith(_ => _.Result.EnsureSuccessStatusCode()));
    }

    private HttpRequestMessage BuildRequest(HttpMethod httpMethod, string requestUrl)
    {
        if (appSettings.NotificationHub.Configured is false)
            throw new InvalidOperationException("Notification hub is not configured.");

        Dictionary<string, string> connectionStringKeyValues = appSettings.NotificationHub.ConnectionString!
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(split => split[0].Trim(), split => split[1].Trim());

        var resourceUri = $"{connectionStringKeyValues["Endpoint"]}{appSettings.NotificationHub.Name}";

        HttpRequestMessage request = new HttpRequestMessage(httpMethod, new Uri(new Uri(resourceUri.Replace("sb://", "https://")), requestUrl));

        TimeSpan sinceEpoch = DateTimeOffset.UtcNow - new DateTime(1970, 1, 1);
        const int week = 60 * 60 * 24 * 7;
        var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + week);
        string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
        using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(connectionStringKeyValues["SharedAccessKey"]));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        var sasToken = FormattableString.Invariant($"SharedAccessSignature sr={HttpUtility.UrlEncode(resourceUri)}&sig={HttpUtility.UrlEncode(signature)}&se={expiry}&skn={connectionStringKeyValues["SharedAccessKeyName"]}");

        request.Headers.Add("Authorization", sasToken);

        return request;
    }
}
