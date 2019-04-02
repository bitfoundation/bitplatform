using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Bit.Core.Contracts;
using Microsoft.Owin;

namespace Bit.Owin.Implementations
{
    public class OwinRequestInformationProvider : IRequestInformationProvider
    {
        public virtual IOwinContext Context { get; set; }

        protected virtual IOwinContext GetOwinContext()
        {
            return Context ?? throw new InvalidOperationException("OwinContextIsNull");
        }

        protected virtual string GetHeaderValue(string key)
        {
            IOwinContext owinContext = GetOwinContext();
            if (owinContext.Request?.Headers == null)
                return null;
            if (owinContext.Request.Headers.TryGetValue(key, out string[] values) == true)
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
            get => GetOwinContext().Request?.Method;
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientIp
        {
            get => GetOwinContext().Request?.RemoteIpAddress;
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
            get => GetOwinContext().Request?.Uri?.ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual ClaimsIdentity Identity
        {
            get => GetOwinContext().Authentication?.User?.Identity as ClaimsIdentity;
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