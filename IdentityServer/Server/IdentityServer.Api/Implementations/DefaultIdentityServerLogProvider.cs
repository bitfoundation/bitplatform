using System;
using Foundation.Core.Contracts;
using IdentityServer3.Core.Logging;

namespace IdentityServer.Api.Implementations
{
    public class DefaultIdentityServerLogProvider : ILogProvider, IDisposable
    {
        private readonly IDependencyManager _dependencyManager;

        public DefaultIdentityServerLogProvider(IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            _dependencyManager = dependencyManager;

            _logger = (level, func, exception, parameters) =>
             {
                 if (level == LogLevel.Error || level == LogLevel.Fatal || level == LogLevel.Warn || exception != null)
                 {
                     if (func != null)
                     {
                         IDependencyResolver scope = null;

                         try
                         {
                             scope = _dependencyManager.CreateChildDependencyResolver();
                         }
                         catch (ObjectDisposedException)
                         { }

                         if (scope != null)
                         {
                             using (scope)
                             {
                                 ILogger logger = scope.Resolve<ILogger>();

                                 string message = string.Format(func(), parameters);
                                 if (exception != null)
                                     logger.LogException(exception, message);
                                 else if (level == LogLevel.Warn)
                                     logger.LogWarning(message);
                                 else
                                     logger.LogFatal(message);
                             }
                         }
                     }

                     return true;
                 }

                 return false;
             };
        }

        protected DefaultIdentityServerLogProvider()
        {

        }

        private readonly Logger _logger;

        public virtual Logger GetLogger(string name)
        {
            return _logger;
        }

        public virtual IDisposable OpenNestedContext(string message)
        {
            return this;
        }

        public virtual IDisposable OpenMappedContext(string key, string value)
        {
            return this;
        }

        public virtual void Dispose()
        {

        }
    }
}
