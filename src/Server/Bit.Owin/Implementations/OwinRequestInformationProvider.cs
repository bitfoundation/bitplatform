using Bit.Core.Contracts;
using Microsoft.Owin;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace Bit.Owin.Implementations
{
    public class OwinRequestInformationProvider : IRequestInformationProvider
    {
        public virtual IOwinContext Context { get; set; }

        protected virtual IOwinContext GetOwinContext()
        {
            return Context ?? throw new InvalidOperationException("OwinContextIsNull");
        }

        public virtual string UserAgent
        {
            get => GetOwinContext().Request?.Headers?.Get("user-agent");
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
            get => GetOwinContext().Request?.Headers?.Get("Client-App-Version");
            protected set => throw new InvalidOperationException();
        }

        public virtual string SystemLanguage
        {
            get => GetOwinContext().Request?.Headers?.Get("System-Language");
            protected set => throw new InvalidOperationException();
        }

        public virtual DateTimeOffset? ClientDateTime
        {
            get
            {
                string clientDateTime = GetOwinContext().Request?.Headers?.Get("Client-Date-Time");
                if (clientDateTime == null)
                    return null;
                return DateTimeOffset.Parse(clientDateTime, CultureInfo.InvariantCulture);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientType
        {
            get => GetOwinContext().Request?.Headers?.Get("Client-Type");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientCulture
        {
            get => GetOwinContext().Request?.Headers?.Get("Client-Culture");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientScreenSize
        {
            get => GetOwinContext().Request?.Headers?.Get("Client-Screen-Size");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientPlatform
        {
            get => GetOwinContext().Request?.Headers?.Get("Client-Platform");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientRoute
        {
            get => GetOwinContext().Request?.Headers?.Get("Client-Route");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientSysLanguage
        {
            get => GetOwinContext().Request?.Headers?.Get("Client-Sys-Language");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientTheme
        {
            get => GetOwinContext().Request?.Headers?.Get("Client-Theme");
            protected set => throw new InvalidOperationException();
        }

        public virtual bool? ClientDebugMode
        {
            get
            {
                string clientDebugMode = GetOwinContext().Request?.Headers?.Get("Client-Debug-Mode");
                if (clientDebugMode == null)
                    return null;
                return bool.Parse(clientDebugMode);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string RequestUri
        {
            get => GetOwinContext().Request?.Uri?.ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual ClaimsIdentity Identity
        {
            get => GetOwinContext().Authentication?.User?.Identity as ClaimsIdentity;
            protected set => throw new InvalidOperationException();
        }

        private string _currentTimeZone;

        public virtual string CurrentTimeZone
        {
            get
            {
                if (_currentTimeZone != null)
                    return _currentTimeZone;

                if (GetOwinContext().Request == null)
                    throw new InvalidOperationException();

                if (GetOwinContext().Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = GetOwinContext().Request.Headers;

                if (headers.ContainsKey("Current-Time-Zone"))
                {
                    _currentTimeZone = headers.GetValues("Current-Time-Zone").ExtendedSingle("Finding Current-Time-Zone header");

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

                if (GetOwinContext().Request == null)
                    throw new InvalidOperationException();

                if (GetOwinContext().Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = GetOwinContext().Request.Headers;

                if (headers.ContainsKey("Desired-Time-Zone"))
                {
                    _desiredTimeZone =
                        headers.GetValues("Desired-Time-Zone").ExtendedSingle("Finding Desired-Time-Zone header");

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
            get => GetOwinContext().Request?.Headers?.Get("Content-Type");
            protected set => throw new InvalidOperationException();
        }

        public virtual string Origin
        {
            get => GetOwinContext().Request?.Headers?.Get("Origin");
            protected set => throw new InvalidOperationException();
        }

        public virtual string Referer
        {
            get => GetOwinContext().Request?.Headers?.Get("Referer");
            protected set => throw new InvalidOperationException();
        }

        public virtual Guid? CorrelationId
        {
            get
            {
                string correlationId = GetOwinContext().Request?.Headers?.Get("X-CorrelationId");
                if (correlationId != null)
                    return Guid.Parse(correlationId);
                else
                    return null;
            }
            protected set => throw new InvalidOperationException();
        }
    }
}
