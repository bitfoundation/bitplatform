using System;
using System.Linq;
using System.Security.Claims;
using Bit.Core.Contracts;
using Microsoft.Owin;

namespace Bit.Owin.Implementations
{
    public class DefaultRequestInformationProvider : IRequestInformationProvider
    {
        private readonly IOwinContext _context;

        protected DefaultRequestInformationProvider()
        {
        }

        public DefaultRequestInformationProvider(IOwinContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _context = context;
        }

        public virtual string UserAgent
        {
            get => _context.Request?.Headers?.Get("user-agent");
            protected set => throw new InvalidOperationException();
        }

        public virtual string HttpMethod
        {
            get => _context.Request?.Method;
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientIp
        {
            get => _context.Request?.RemoteIpAddress;
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientAppVersion
        {
            get => _context.Request?.Headers?.Get("Client-App-Version");
            protected set => throw new InvalidOperationException();
        }

        public virtual string SystemLanguage
        {
            get => _context.Request?.Headers?.Get("System-Language");
            protected set => throw new InvalidOperationException();
        }

        public virtual DateTimeOffset? ClientDateTime
        {
            get
            {
                string clientDateTime = _context.Request?.Headers?.Get("Client-Date-Time");
                if (clientDateTime == null)
                    return null;
                return DateTimeOffset.Parse(clientDateTime);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientType
        {
            get => _context.Request?.Headers?.Get("Client-Type");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientCulture
        {
            get => _context.Request?.Headers?.Get("Client-Culture");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientScreenSize
        {
            get => _context.Request?.Headers?.Get("Client-Screen-Size");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientPlatform
        {
            get => _context.Request?.Headers?.Get("Client-Platform");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientRoute
        {
            get => _context.Request?.Headers?.Get("Client-Route");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientSysLanguage
        {
            get => _context.Request?.Headers?.Get("Client-Sys-Language");
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientTheme
        {
            get => _context.Request?.Headers?.Get("Client-Theme");
            protected set => throw new InvalidOperationException();
        }

        public virtual bool? ClientDebugMode
        {
            get
            {
                string clientDebugMode = _context.Request?.Headers?.Get("Client-Debug-Mode");
                if (clientDebugMode == null)
                    return null;
                return bool.Parse(clientDebugMode);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string RequestUri
        {
            get => _context.Request?.Uri?.ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual ClaimsIdentity Identity
        {
            get => _context.Authentication?.User?.Identity as ClaimsIdentity;
            protected set => throw new InvalidOperationException();
        }

        private string _currentTimeZone;

        public virtual string CurrentTimeZone
        {
            get
            {
                if (_currentTimeZone != null)
                    return _currentTimeZone;

                if (_context.Request == null)
                    throw new InvalidOperationException();

                if (_context.Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = _context.Request.Headers;

                if (headers.ContainsKey("Current-Time-Zone"))
                {
                    _currentTimeZone = headers.GetValues("Current-Time-Zone").Single();

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

                if (_context.Request == null)
                    throw new InvalidOperationException();

                if (_context.Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = _context.Request.Headers;

                if (headers.ContainsKey("Desired-Time-Zone"))
                {
                    _desiredTimeZone =
                        headers.GetValues("Desired-Time-Zone").Single();

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
            get => _context.Request?.Headers?.Get("Content-Type");
            protected set => throw new InvalidOperationException();
        }

        public virtual string Origin
        {
            get => _context.Request?.Headers?.Get("Origin");
            protected set => throw new InvalidOperationException();
        }

        public virtual string Referer
        {
            get => _context.Request?.Headers?.Get("Referer");
            protected set => throw new InvalidOperationException();
        }

        public virtual Guid? CorrelationId
        {
            get
            {
                string correlationId = _context.Request?.Headers?.Get("X-CorrelationId");
                if (correlationId != null)
                    return Guid.Parse(correlationId);
                else
                    return null;
            }
            protected set => throw new InvalidOperationException();
        }
    }
}