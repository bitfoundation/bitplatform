using Bit.Core.Contracts;
using Microsoft.Owin;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Tracing;

namespace Bit.WebApi.Implementations
{
    public class DefaultWebApiTraceWriter : ITraceWriter
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
                    scopeStatusManager.MarkAsFailed(traceRecord.Message ?? traceRecord.Exception?.Message);

                if (traceRecord.Exception != null)
                {
                    Exception exception = traceRecord.Exception;

                    if (exception is TargetInvocationException && exception.InnerException != null)
                        exception = exception.InnerException;
                    if (!logger.LogData.Any(d => d.Key == "X-Correlation-ID"))
                        logger.AddLogData("X-Correlation-ID", scopeDependencyResolver.Resolve<IRequestInformationProvider>().XCorrelationId);
                    if (!logger.LogData.Any(d => d.Key == "WebExceptionType"))
                        logger.AddLogData("WebExceptionType", exception.GetType().FullName);
                    if (!logger.LogData.Any(d => d.Key == "WebException"))
                        logger.AddLogData("WebException", exception.ToString());
                    if (!logger.LogData.Any(d => d.Key == "WebApiErrorMessage"))
                        logger.AddLogData("WebApiErrorMessage", traceRecord.Message);
                }
            }
        }
    }
}
