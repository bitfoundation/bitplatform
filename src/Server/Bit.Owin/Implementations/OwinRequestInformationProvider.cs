using System;
using System.Linq;
using System.Security.Claims;
using Bit.Core.Contracts;
using Microsoft.Owin;

namespace Bit.Owin.Implementations
{
    public class OwinRequestInformationProvider : IRequestInformationProvider
    {
        public virtual IOwinContext Context { get; set; }

        public virtual string UserAgent
        {
            get => Context.Request?.Headers?.Get("user-agent");
            protected set => throw new InvalidOperationException();
        }

        public virtual string HttpMethod
        {
            get => Context.Request?.Method;
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientIp
        {
            get => Context.Request?.RemoteIpAddress;
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientAppVersion
        {
            get => Context.Request?.Headers?.Get("Client-App-Version");
            protected set => throw new InvalidOperationException();
        }

        public virtual string SystemLanguage
        {
            get => Context.Request?.Headers?.Get("System-Language");
            protected set => throw new InvalidOperationException();
        }

        public virtual DateTimeOffset? ClientDateTime
        {
            get
            {
                string clientDateTime = Context.Request?.Headers?.Get("Client-Date-Time");
                if (clientDateTime == null)
                    return null;
                return DateTimeOffset.Parse(clientDateTime);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientType
        {
            get => Context.Request?.Headers?.Get("Client-Type");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientCulture
        {
            get => Context.Request?.Headers?.Get("Client-Culture");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientScreenSize
        {
            get => Context.Request?.Headers?.Get("Client-Screen-Size");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientPlatform
        {
            get => Context.Request?.Headers?.Get("Client-Platform");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientRoute
        {
            get => Context.Request?.Headers?.Get("Client-Route");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientSysLanguage
        {
            get => Context.Request?.Headers?.Get("Client-Sys-Language");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientTheme
        {
            get => Context.Request?.Headers?.Get("Client-Theme");
            protected set => throw new InvalidOperationException();
        }

        public virtual bool? ClientDebugMode
        {
            get
            {
                string clientDebugMode = Context.Request?.Headers?.Get("Client-Debug-Mode");
                if (clientDebugMode == null)
                    return null;
                return bool.Parse(clientDebugMode);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string RequestUri
        {
            get => Context.Request?.Uri?.ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual ClaimsIdentity Identity
        {
            get => Context.Authentication?.User?.Identity as ClaimsIdentity;
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

                if (Context.Request == null)
                    throw new InvalidOperationException();

                if (Context.Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = Context.Request.Headers;

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
            get => Context.Request?.Headers?.Get("Content-Type");
            protected set => throw new InvalidOperationException();
        }

        public virtual string Origin
        {
            get => Context.Request?.Headers?.Get("Origin");
            protected set => throw new InvalidOperationException();
        }

        public virtual string Referer
        {
            get => Context.Request?.Headers?.Get("Referer");
            protected set => throw new InvalidOperationException();
        }

        public virtual Guid? CorrelationId
        {
            get
            {
                string correlationId = Context.Request?.Headers?.Get("X-CorrelationId");
                if (correlationId != null)
                    return Guid.Parse(correlationId);
                else
                    return null;
            }
            protected set => throw new InvalidOperationException();
        }
    }
}