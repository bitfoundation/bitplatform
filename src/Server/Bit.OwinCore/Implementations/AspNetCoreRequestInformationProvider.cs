using Bit.Core.Contracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;

namespace Bit.Owin.Implementations
{
    public class AspNetCoreRequestInformationProvider : IRequestInformationProvider
    {
        public virtual HttpContext Context { get; set; }

        public virtual string UserAgent
        {
            get => Context.Request?.Headers["user-agent"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string HttpMethod
        {
            get => Context.Request?.Method;
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientIp
        {
            get => Context.Connection?.RemoteIpAddress?.ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientAppVersion
        {
            get => Context.Request?.Headers["Client-App-Version"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string SystemLanguage
        {
            get => Context.Request?.Headers["System-Language"];
            protected set => throw new InvalidOperationException();
        }

        public virtual DateTimeOffset? ClientDateTime
        {
            get
            {
                string clientDateTime = Context.Request?.Headers["Client-Date-Time"];
                if (clientDateTime == null)
                    return null;
                return DateTimeOffset.Parse(clientDateTime);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientType
        {
            get => Context.Request?.Headers["Client-Type"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientCulture
        {
            get => Context.Request?.Headers["Client-Culture"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientScreenSize
        {
            get => Context.Request?.Headers["Client-Screen-Size"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientPlatform
        {
            get => Context.Request?.Headers["Client-Platform"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientRoute
        {
            get => Context.Request?.Headers["Client-Route"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientSysLanguage
        {
            get => Context.Request?.Headers["Client-Sys-Language"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientTheme
        {
            get => Context.Request?.Headers["Client-Theme"];
            protected set => throw new InvalidOperationException();
        }

        public virtual bool? ClientDebugMode
        {
            get
            {
                string clientDebugMode = Context.Request?.Headers["Client-Debug-Mode"];
                if (clientDebugMode == null)
                    return null;
                return bool.Parse(clientDebugMode);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string RequestUri
        {
            get => new Uri($"{Context.Request.Scheme}://{Context.Request.Host}{Context.Request.Path}{Context.Request.QueryString}").ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual ClaimsIdentity Identity
        {
            get => Context.User?.Identity as ClaimsIdentity;
            protected set => throw new InvalidOperationException();
        }

        private string _currentTimeZone;

        public virtual string CurrentTimeZone
        {
            get
            {
                if (_currentTimeZone != null)
                    return _currentTimeZone;

                if (Context.Request == null)
                    throw new InvalidOperationException();

                if (Context.Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = Context.Request.Headers;

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

                if (Context.Request == null)
                    throw new InvalidOperationException();

                if (Context.Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = Context.Request.Headers;

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
            get => Context.Request?.Headers["Content-Type"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string Origin
        {
            get => Context.Request?.Headers["Origin"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string Referer
        {
            get => Context.Request?.Headers["Referer"];
            protected set => throw new InvalidOperationException();
        }

        public virtual Guid? CorrelationId
        {
            get
            {
                string correlationId = Context.Request?.Headers["X-CorrelationId"];
                if (correlationId != null)
                    return Guid.Parse(correlationId);
                else
                    return null;
            }
            protected set => throw new InvalidOperationException();
        }
    }
}