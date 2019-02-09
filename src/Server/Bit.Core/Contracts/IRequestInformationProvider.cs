using System;
using System.Security.Claims;

namespace Bit.Core.Contracts
{
    public interface IRequestInformationProvider
    {
        string UserAgent { get; }

        string HttpMethod { get; }

        string ClientIp { get; }

        string ClientAppVersion { get; }

        string SystemLanguage { get; }

        DateTimeOffset? ClientDateTime { get; }

        string ClientType { get; }

        string BitClientType { get; }

        string ClientCulture { get; }

        string ClientScreenSize { get; }

        string ClientPlatform { get; }

        string ClientRoute { get; }

        string ClientSysLanguage { get; }

        string ClientTheme { get; }

        bool? ClientDebugMode { get; }

        string DisplayUrl { get; }

        ClaimsIdentity Identity { get; }

        string CurrentTimeZone { get; }

        string DesiredTimeZone { get; }

        string ContentType { get; }

        string Origin { get; }

        string Referer { get; }

        Guid? CorrelationId { get; }
    }
}