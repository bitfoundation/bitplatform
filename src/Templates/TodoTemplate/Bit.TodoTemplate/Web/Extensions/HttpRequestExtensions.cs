using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

public static class HttpRequestExtensions
{
    public static bool IsSearchEngineBot(this HttpRequest request)
    {
        var agent = GetUserAgent(request);
        
        var isGoogleBot = IsGoogleBot(agent);
        var isBingBot = IsBingBot(agent);
        var isLightHouse = IsLightHouse(agent);
        
        return isGoogleBot || isBingBot || isLightHouse;
    }

    private static bool IsGoogleBot(string agent)
    {
        if (string.IsNullOrEmpty(agent))
            return false;

        return agent.Contains("google");
    }

    private static bool IsBingBot(string agent)
    {
        if (string.IsNullOrEmpty(agent))
            return false;

        return agent.Contains("bing");
    }

    private static bool IsLightHouse(string agent)
    {
        if (string.IsNullOrEmpty(agent))
            return false;

        return agent.Contains("lighthouse");
    }

    private static string GetUserAgent(HttpRequest request)
    {
        var userAgent = request.Headers[HeaderNames.UserAgent].ToString();

        if (string.IsNullOrEmpty(userAgent))
            return string.Empty;

        var formattedUserAgent = userAgent.ToLower();
        return formattedUserAgent;
    }
}
