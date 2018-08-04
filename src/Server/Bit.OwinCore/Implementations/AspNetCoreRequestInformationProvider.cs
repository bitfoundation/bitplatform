using Bit.Core.Contracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace Bit.Owin.Implementations
{
    public class AspNetCoreRequestInformationProvider : IRequestInformationProvider
    {
        public virtual HttpContext Context { get; set; }

        protected virtual HttpContext GetHttpContext()
        {
            return Context ?? throw new InvalidOperationException("HttpContextIsNull");
        }

        public virtual string UserAgent
        {
            get => GetHttpContext().Request?.Headers["user-agent"];
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
            get => GetHttpContext().Request?.Headers["Client-App-Version"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string SystemLanguage
        {
            get => GetHttpContext().Request?.Headers["System-Language"];
            protected set => throw new InvalidOperationException();
        }

        public virtual DateTimeOffset? ClientDateTime
        {
            get
            {
                string clientDateTime = GetHttpContext().Request?.Headers["Client-Date-Time"];
                if (clientDateTime == null)
                    return null;
                return DateTimeOffset.Parse(clientDateTime, CultureInfo.InvariantCulture);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientType
        {
            get => GetHttpContext().Request?.Headers["Client-Type"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientCulture
        {
            get => GetHttpContext().Request?.Headers["Client-Culture"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientScreenSize
        {
            get => GetHttpContext().Request?.Headers["Client-Screen-Size"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientPlatform
        {
            get => GetHttpContext().Request?.Headers["Client-Platform"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientRoute
        {
            get => GetHttpContext().Request?.Headers["Client-Route"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientSysLanguage
        {
            get => GetHttpContext().Request?.Headers["Client-Sys-Language"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientTheme
        {
            get => GetHttpContext().Request?.Headers["Client-Theme"];
            protected set => throw new InvalidOperationException();
        }

        public virtual bool? ClientDebugMode
        {
            get
            {
                string clientDebugMode = GetHttpContext().Request?.Headers["Client-Debug-Mode"];
                if (clientDebugMode == null)
                    return null;
                return bool.Parse(clientDebugMode);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string RequestUri
        {
            get => new Uri($"{GetHttpContext().Request.Scheme}://{GetHttpContext().Request.Host}{GetHttpContext().Request.Path}{GetHttpContext().Request.QueryString}").ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual ClaimsIdentity Identity
        {
            get => GetHttpContext().User?.Identity as ClaimsIdentity;
            protected set => throw new InvalidOperationException();
        }

        private string _currentTimeZone;

        public virtual string CurrentTimeZone
        {
            get
            {
                if (_currentTimeZone != null)
                    return _currentTimeZone;

                if (GetHttpContext().Request == null)
                    throw new InvalidOperationException();

                if (GetHttpContext().Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = GetHttpContext().Request.Headers;

                if (headers.ContainsKey("Current-Time-Zone"))
                {
                    _currentTimeZone = headers["Current-Time-Zone"].ExtendedSingle("Finding Current-Time-Zone header");

                    return _currentTimeZone;
                }
                else
                {
                    return null;
                }
            }
            protected set => throw new InvalidOperationException();
        }

        private string _desiredTimeZone;

        public virtual string DesiredTimeZone
        {
            get
            {
                if (_desiredTimeZone != null)
                    return _desiredTimeZone;

                if (GetHttpContext().Request == null)
                    throw new InvalidOperationException();

                if (GetHttpContext().Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = GetHttpContext().Request.Headers;

                if (headers.ContainsKey("Desired-Time-Zone"))
                {
                    _desiredTimeZone =
                        headers["Desired-Time-Zone"].ExtendedSingle("Finding Desired-Time-Zone header");

                    return _desiredTimeZone;
                }
                else
                {
                    return null;
                }
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string ContentType
        {
            get => GetHttpContext().Request?.Headers["Content-Type"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string Origin
        {
            get => GetHttpContext().Request?.Headers["Origin"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string Referer
        {
            get => GetHttpContext().Request?.Headers["Referer"];
            protected set => throw new InvalidOperationException();
        }

        public virtual Guid? CorrelationId
        {
            get
            {
                string correlationId = GetHttpContext().Request?.Headers["X-CorrelationId"];
                if (correlationId != null)
                    return Guid.Parse(correlationId);
                else
                    return null;
            }
            protected set => throw new InvalidOperationException();
        }
    }
}
