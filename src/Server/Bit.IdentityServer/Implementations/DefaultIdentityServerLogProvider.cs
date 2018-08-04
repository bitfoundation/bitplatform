using Bit.Core.Contracts;
using IdentityServer3.Core.Logging;
using System;
using System.Globalization;

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

                bool IdentityServerLog(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
                {
                    if (logLevel == LogLevel.Error || logLevel == LogLevel.Fatal || logLevel == LogLevel.Warn || exception != null)
                    {
                        if (messageFunc != null)
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
                                        message = string.Format(CultureInfo.InvariantCulture, messageFunc(), formatParameters);
                                    }
                                    catch
                                    {
                                        message = messageFunc();
                                    }

                                    if (exception != null)
                                        logger.LogException(exception, message);
                                    else if (logLevel == LogLevel.Warn)
                                        logger.LogWarning(message);
                                    else
                                        logger.LogFatal(message);
                                }
                            }
                        }

                        return true;
                    }

                    return false;
                }

                _logger = IdentityServerLog;
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
