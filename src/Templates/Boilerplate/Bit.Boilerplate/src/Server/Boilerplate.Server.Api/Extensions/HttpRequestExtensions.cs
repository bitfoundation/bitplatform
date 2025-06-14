﻿using Boilerplate.Server.Api;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public static partial class HttpRequestExtensions
{
    internal static Uri GetWebAppUrl(this HttpRequest req)
    {
        var settings = req.HttpContext.RequestServices.GetRequiredService<ServerApiSettings>();

        var serverUrl = req.GetBaseUrl();

        var origin = req.Query["origin"].Union(req.Headers["X-Origin"]).Select(o => new Uri(o)).FirstOrDefault();

        if (origin is null)
            return serverUrl; // Assume that web app and server are hosted in one place.

        if (origin == serverUrl || settings.IsTrustedOrigin(origin))
            return origin;

        throw new BadRequestException($"Invalid origin {origin}");
    }

    internal static Uri GetUri(this HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Scheme))
        {
            throw new ArgumentException("Http request Scheme is not specified");
        }

        return new Uri($"{request.Scheme}://{((!request.Host.HasValue) ? "UNKNOWN-HOST" : ((request.Host.Value.IndexOf(',') > 0) ? "MULTIPLE-HOST" : request.Host.Value))}{(request.PathBase.HasValue ? request.PathBase.Value : string.Empty)}{(request.Path.HasValue ? request.Path.Value : string.Empty)}{(request.QueryString.HasValue ? request.QueryString.Value : string.Empty)}");
    }

    /// <summary>
    /// https://blog.elmah.io/how-to-get-base-url-in-asp-net-core/
    /// </summary>
    internal static Uri GetBaseUrl(this HttpRequest req)
    {
        var uriBuilder = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? -1);
        if (uriBuilder.Uri.IsDefaultPort)
        {
            uriBuilder.Port = -1;
        }

        return uriBuilder.Uri;
    }

    internal static bool IsFromCDN(this HttpRequest request)
    {
        return request.Headers.ContainsKey("CDN-Loop");
    }

    internal static bool IsLightHouseRequest(this HttpRequest request)
    {
        var agent = GetLoweredUserAgent(request);

        if (agent.Contains("google")) return true;

        if (agent.Contains("lighthouse")) return true;

        return false;
    }

    private static string GetLoweredUserAgent(HttpRequest request)
    {
        var userAgent = request.Headers[HeaderNames.UserAgent].ToString();

        if (string.IsNullOrEmpty(userAgent)) return string.Empty;

        return userAgent.ToLowerInvariant();
    }
}
