using Bit.Core.Contracts;
using Bit.Owin.Implementations;
using IdentityServer3.Core.Logging;
using System;
using System.Globalization;
using System.Linq;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultIdentityServerLogProvider : ILogProvider, IDisposable
    {
        static bool IdentityServerLog(LogLevel logLevel, Func<string>? messageFunc, Exception? exception = null, params object[] formatParameters)
        {
            if (logLevel == LogLevel.Error || logLevel == LogLevel.Fatal || logLevel == LogLevel.Warn || exception != null)
            {
                if (messageFunc != null)
                {
                    using (IDependencyResolver scope = DefaultDependencyManager.Current.CreateChildDependencyResolver())
                    {
                        ILogger logger = scope.Resolve<ILogger>();

                        string message = messageFunc();

                        try
                        {
                            if (formatParameters.Any())
                                message = string.Format(CultureInfo.InvariantCulture, message, formatParameters);
                        }
                        catch (FormatException) { }

                        if (exception != null)
                            logger.LogException(exception, message);
                        else if (logLevel == LogLevel.Warn)
                            logger.LogWarning(message);
                        else
                            logger.LogFatal(message);
                    }
                }

                return true;
            }

            return false;
        }

        private readonly Logger _logger = IdentityServerLog;

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
