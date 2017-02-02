using System;
using System.Security.Claims;

namespace Foundation.Core.Contracts
{
    public interface IRequestInformationProvider
    {
        string UserAgent { get; set; }

        string HttpMethod { get; set; }

        string ClientIp { get; set; }

        string ClientAppVersion { get; set; }

        string SystemLanguage { get; set; }

        DateTimeOffset? ClientDateTime { get; set; }

        string ClientType { get; set; }

        string ClientCulture { get; set; }

        string ClientScreenSize { get; set; }

        string ClientPlatform { get; set; }

        string ClientRoute { get; set; }

        string ClientSysLanguage { get; set; }

        string ClientTheme { get; set; }

        bool? ClientDebugMode { get; set; }

        string RequestUri { get; set; }

        ClaimsIdentity Identity { get; set; }

        string CurrentTimeZone { get; set; }

        string DesiredTimeZone { get; set; }

        string ContentType { get; set; }

        string Origin { get; set; }

        string Referer { get; set; }

        Guid? CorrelationId { get; set; }
    }
}