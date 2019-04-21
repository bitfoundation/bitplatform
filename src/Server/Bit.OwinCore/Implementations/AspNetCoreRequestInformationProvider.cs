using Bit.Core.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using System;
using System.Globalization;
using System.Security.Claims;

namespace Bit.OwinCore.Implementations
{
    public class AspNetCoreRequestInformationProvider : IRequestInformationProvider
    {
        public virtual HttpContext Context { get; set; }

        protected virtual HttpContext GetHttpContext()
        {
            return Context ?? throw new InvalidOperationException("HttpContextIsNull");
        }

        protected virtual string GetHeaderValue(string key)
        {
            HttpContext httpContext = GetHttpContext();
            if (httpContext.Request?.Headers == null)
                return null;
            if (httpContext.Request.Headers.TryGetValue(key, out StringValues values) == true)
                return string.Join(",", values);
            return null;
        }

        public virtual string UserAgent
        {
            get => GetHeaderValue("user-agent");
            protected set => throw new InvalidOperationException();
        }

        public virtual string HttpMethod
        {
            get => GetHttpContext().Request?.Method;
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientIp
        {
            get => GetHttpContext().Connection?.RemoteIpAddress?.ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientAppVersion
        {
            get => GetHeaderValue("Client-App-Version");
            protected set => throw new InvalidOperationException();
        }

        public virtual string SystemLanguage
        {
            get => GetHeaderValue("System-Language");
            protected set => throw new InvalidOperationException();
        }

        public virtual DateTimeOffset? ClientDateTime
        {
            get
            {
                string clientDateTime = GetHeaderValue("Client-Date-Time");
                if (clientDateTime == null)
                    return null;
                return DateTimeOffset.Parse(clientDateTime, CultureInfo.InvariantCulture);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientType
        {
            get => GetHeaderValue("Client-Type");
            protected set => throw new InvalidOperationException();
        }

        public virtual string BitClientType
        {
            get => GetHeaderValue("Bit-Client-Type");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientCulture
        {
            get => GetHeaderValue("Client-Culture");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientScreenSize
        {
            get => GetHeaderValue("Client-Screen-Size");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientPlatform
        {
            get => GetHeaderValue("Client-Platform");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientRoute
        {
            get => GetHeaderValue("Client-Route");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientSysLanguage
        {
            get => GetHeaderValue("Client-Sys-Language");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientTheme
        {
            get => GetHeaderValue("Client-Theme");
            protected set => throw new InvalidOperationException();
        }

        public virtual bool? ClientDebugMode
        {
            get
            {
                string clientDebugMode = GetHeaderValue("Client-Debug-Mode");
                if (clientDebugMode == null)
                    return null;
                return bool.Parse(clientDebugMode);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string DisplayUrl
        {
            get => GetHttpContext().Request.GetDisplayUrl()?.AsUnescaped();
            protected set => throw new InvalidOperationException();
        }

        public virtual ClaimsIdentity Identity
        {
            get => GetHttpContext().User?.Identity as ClaimsIdentity;
            protected set => throw new InvalidOperationException();
        }

        public virtual string CurrentTimeZone
        {
            get => GetHeaderValue("Current-Time-Zone");
            protected set => throw new InvalidOperationException();
        }

        public virtual string DesiredTimeZone
        {
            get => GetHeaderValue("Desired-Time-Zone");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ContentType
        {
            get => GetHeaderValue("Content-Type");
            protected set => throw new InvalidOperationException();
        }

        public virtual string Origin
        {
            get => GetHeaderValue("Origin");
            protected set => throw new InvalidOperationException();
        }

        public virtual string Referer
        {
            get => GetHeaderValue("Referer");
            protected set => throw new InvalidOperationException();
        }

        public virtual Guid? CorrelationId
        {
            get
            {
                string correlationId = GetHeaderValue("X-CorrelationId");
                if (correlationId != null)
                    return Guid.Parse(correlationId);
                else
                    return null;
            }
            protected set => throw new InvalidOperationException();
        }
    }
}