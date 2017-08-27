using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Tracing;
using Bit.Core.Contracts;
using Microsoft.Owin;

namespace Bit.WebApi.Implementations
{
    public class DefaultWebApiTraceWritter : ITraceWriter
    {
        public virtual void Trace(HttpRequestMessage request, string category, TraceLevel level, Action<TraceRecord> traceAction)
        {
            if (request?.GetOwinContext() != null && (level == TraceLevel.Fatal || level == TraceLevel.Warn || level == TraceLevel.Error))
            {
                TraceRecord traceRecord = new TraceRecord(request, category, level);
                traceAction(traceRecord);

                IDependencyResolver scopeDependencyResolver = request.GetOwinContext().GetDependencyResolver();

                ILogger logger = scopeDependencyResolver.Resolve<ILogger>();
                IScopeStatusManager scopeStatusManager = scopeDependencyResolver.Resolve<IScopeStatusManager>();

                if (scopeStatusManager.WasSucceeded())
                    scopeStatusManager.MarkAsFailed();

                if (traceRecord.Exception != null)
                {
                    Exception exception = traceRecord.Exception;

                    if (exception is TargetInvocationException && exception.InnerException != null)
                        exception = exception.InnerException;
                    if (!logger.LogData.Any(d => d.Key == "X-CorrelationId"))
                        logger.AddLogData("X-CorrelationId", request.GetCorrelationId());
                    logger.AddLogData("WebExceptionType", exception.GetType().FullName);
                    logger.AddLogData("WebException", exception);
                    logger.AddLogData("WebApiErrorMessage", traceRecord.Message);
                }
            }
        }
    }
}