using System;
using System.Globalization;
using Bit.Core.Contracts;
using IdentityServer3.Core.Logging;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultIdentityServerLogProvider : ILogProvider, IDisposable
    {
        public virtual IDependencyManager DependencyManager
        {
            set
            {
                IDependencyManager dependencyManager = value;

                if (dependencyManager == null)
                    throw new ArgumentNullException(nameof(dependencyManager));

                _logger = (level, func, exception, parameters) =>
                {
                    if (level == LogLevel.Error || level == LogLevel.Fatal || level == LogLevel.Warn || exception != null)
                    {
                        if (func != null)
                        {
                            IDependencyResolver scope = null;

                            try
                            {
                                scope = dependencyManager.CreateChildDependencyResolver();
                            }
                            catch (ObjectDisposedException)
                            { }

                            if (scope != null)
                            {
                                using (scope)
                                {
                                    ILogger logger = scope.Resolve<ILogger>();

                                    string message = null;

                                    try
                                    {
                                        message = string.Format(CultureInfo.InvariantCulture, func(), parameters);
                                    }
                                    catch
                                    {
                                        message = func();
                                    }

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
        }

        private Logger _logger;

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
            GC.SuppressFinalize(this);
        }
    }
}
