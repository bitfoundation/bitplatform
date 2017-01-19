using System;
using System.Linq;
using System.Security.Claims;
using Foundation.Core.Contracts;
using Microsoft.Owin;

namespace Foundation.Api.Implementations
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
            get { return _context.Request?.Headers?.Get("user-agent"); }
            set { throw new InvalidOperationException(); }
        }

        public virtual string HttpMethod
        {
            get { return _context.Request?.Method; }
            set { throw new InvalidOperationException(); }
        }

        public virtual string ClientIp
        {
            get { return _context.Request?.RemoteIpAddress; }
            set { throw new InvalidOperationException(); }
        }

        public virtual string ClientAppVersion
        {
            get { return _context.Request?.Headers?.Get("Client-App-Version"); }
            set { throw new InvalidOperationException(); }
        }

        public virtual string SystemLanguage
        {
            get { return _context.Request?.Headers?.Get("System-Language"); }
            set { throw new InvalidOperationException(); }
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
            set { throw new InvalidOperationException(); }
        }

        public virtual string ClientType
        {
            get { return _context.Request?.Headers?.Get("Client-Type"); }
            set { throw new InvalidOperationException(); }
        }

        public virtual string ClientCulture
        {
            get { return _context.Request?.Headers?.Get("Client-Culture"); }
            set { throw new InvalidOperationException(); }
        }

        public virtual string ClientScreenSize
        {
            get { return _context.Request?.Headers?.Get("Client-Screen-Size"); }
            set { throw new InvalidOperationException(); }
        }

        public virtual string ClientPlatform
        {
            get { return _context.Request?.Headers?.Get("Client-Platform"); }
            set { throw new InvalidOperationException(); }
        }

        public virtual string ClientRoute
        {
            get { return _context.Request?.Headers?.Get("Client-Route"); }
            set { throw new InvalidOperationException(); }
        }

        public virtual string ClientSysLanguage
        {
            get { return _context.Request?.Headers?.Get("Client-Sys-Language"); }
            set { throw new InvalidOperationException(); }
        }

        public virtual string ClientTheme
        {
            get { return _context.Request?.Headers?.Get("Client-Theme"); }
            set { throw new InvalidOperationException(); }
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
            set { throw new InvalidOperationException(); }
        }

        public virtual string RequestUri
        {
            get { return _context.Request?.Uri?.ToString(); }
            set { throw new InvalidOperationException(); }
        }

        public virtual ClaimsIdentity Identity
        {
            get { return _context.Authentication?.User?.Identity as ClaimsIdentity; }
            set { throw new InvalidOperationException(); }
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
            set { throw new InvalidOperationException(); }
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
            set { throw new InvalidOperationException(); }
        }

        public virtual string ContentType
        {
            get
            {
                return _context.Request?.Headers?.Get("Content-Type");
            }
            set { throw new InvalidOperationException(); }
        }

        public virtual string Origin
        {
            get
            {
                return _context.Request?.Headers?.Get("Origin");
            }
            set { throw new InvalidOperationException(); }
        }

        public virtual string Referer
        {
            get
            {
                return _context.Request?.Headers?.Get("Referer");
            }
            set { throw new InvalidOperationException(); }
        }
    }
}