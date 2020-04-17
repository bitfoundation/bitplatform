using Bit.Core.Contracts;
using Microsoft.Owin.Logging;
using System;
using System.Diagnostics;

namespace Bit.Owin.Implementations
{
    public class DefaultOwinLoggerFactory : ILoggerFactory, Microsoft.Owin.Logging.ILogger
    {
        public virtual IDependencyManager DependencyManager { get; set; } = default!;

        public Microsoft.Owin.Logging.ILogger Create(string name)
        {
            return this;
        }

        public virtual bool WriteCore(TraceEventType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (eventType == TraceEventType.Error || eventType == TraceEventType.Critical || eventType == TraceEventType.Warning || exception != null)
            {
                if (formatter != null)
                {
                    IDependencyResolver? scope = null;

                    try
                    {
                        scope = DependencyManager.CreateChildDependencyResolver();
                    }
                    catch (ObjectDisposedException)
                    { }

                    if (scope != null)
                    {
                        using (scope)
                        {
                            Core.Contracts.ILogger logger = scope.Resolve<Core.Contracts.ILogger>();

                            string message = formatter(state, exception);

                            if (exception != null)
                                logger.LogException(exception, message);
                            else if (eventType == TraceEventType.Warning)
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
    }
}
