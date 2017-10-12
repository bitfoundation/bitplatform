using Bit.Core.Contracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;

namespace Bit.Owin.Implementations
{
    public class AspNetCoreRequestInformationProvider : IRequestInformationProvider
    {
        public virtual IHttpContextAccessor HttpContextAccessor
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(HttpContextAccessor));

                _context = value.HttpContext;
            }
        }

        private HttpContext _context;

        public virtual string UserAgent
        {
            get => _context.Request?.Headers["user-agent"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string HttpMethod
        {
            get => _context.Request?.Method;
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientIp
        {
            get => _context.Connection?.RemoteIpAddress?.ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientAppVersion
        {
            get => _context.Request?.Headers["Client-App-Version"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string SystemLanguage
        {
            get => _context.Request?.Headers["System-Language"];
            protected set => throw new InvalidOperationException();
        }

        public virtual DateTimeOffset? ClientDateTime
        {
            get
            {
                string clientDateTime = _context.Request?.Headers["Client-Date-Time"];
                if (clientDateTime == null)
                    return null;
                return DateTimeOffset.Parse(clientDateTime);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientType
        {
            get => _context.Request?.Headers["Client-Type"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientCulture
        {
            get => _context.Request?.Headers["Client-Culture"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientScreenSize
        {
            get => _context.Request?.Headers["Client-Screen-Size"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientPlatform
        {
            get => _context.Request?.Headers["Client-Platform"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientRoute
        {
            get => _context.Request?.Headers["Client-Route"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientSysLanguage
        {
            get => _context.Request?.Headers["Client-Sys-Language"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string ClientTheme
        {
            get => _context.Request?.Headers["Client-Theme"];
            protected set => throw new InvalidOperationException();
        }

        public virtual bool? ClientDebugMode
        {
            get
            {
                string clientDebugMode = _context.Request?.Headers["Client-Debug-Mode"];
                if (clientDebugMode == null)
                    return null;
                return bool.Parse(clientDebugMode);
            }
            protected set => throw new InvalidOperationException();
        }

        public virtual string RequestUri
        {
            get => new Uri($"{_context.Request.Scheme}://{_context.Request.Host}{_context.Request.Path}{_context.Request.QueryString}").ToString();
            protected set => throw new InvalidOperationException();
        }

        public virtual ClaimsIdentity Identity
        {
            get => _context.User?.Identity as ClaimsIdentity;
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

                if (_context.Request == null)
                    throw new InvalidOperationException();

                if (_context.Request.Headers == null)
                    throw new InvalidOperationException();

                IHeaderDictionary headers = _context.Request.Headers;

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
            get => _context.Request?.Headers["Content-Type"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string Origin
        {
            get => _context.Request?.Headers["Origin"];
            protected set => throw new InvalidOperationException();
        }

        public virtual string Referer
        {
            get => _context.Request?.Headers["Referer"];
            protected set => throw new InvalidOperationException();
        }

        public virtual Guid? CorrelationId
        {
            get
            {
                string correlationId = _context.Request?.Headers["X-CorrelationId"];
                if (correlationId != null)
                    return Guid.Parse(correlationId);
                else
                    return null;
            }
            protected set => throw new InvalidOperationException();
        }
    }
}