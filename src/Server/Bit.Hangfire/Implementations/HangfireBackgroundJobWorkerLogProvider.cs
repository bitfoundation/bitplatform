using Bit.Core.Contracts;
using Hangfire.Logging;
using System;

namespace Bit.Hangfire.Implementations
{
    public class HangfireBackgroundJobWorkerLogProvider : ILog, ILogProvider
    {
        private readonly IDependencyManager _dependencyManager;

#if DEBUG
        protected HangfireBackgroundJobWorkerLogProvider()
        {
        }
#endif

        public HangfireBackgroundJobWorkerLogProvider(IDependencyManager dependencyManager)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            _dependencyManager = dependencyManager;
        }

        public virtual ILog GetLogger(string name)
        {
            return this;
        }

        public virtual bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null)
        {
            string message = messageFunc?.Invoke() ?? "";

            if ((exception != null || !string.IsNullOrEmpty(message)) && (logLevel != LogLevel.Debug && logLevel != LogLevel.Trace && logLevel != LogLevel.Info))
            {
                using (IDependencyResolver childResolver = _dependencyManager.CreateChildDependencyResolver())
                {
                    ILogger logger = childResolver.Resolve<ILogger>();
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
    }
}
