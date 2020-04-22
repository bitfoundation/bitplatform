using Bit.Core.Contracts;
using Bit.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Owin.Implementations
{
    public class SerilogLogStore : ILogStore, ILogEventEnricher
    {
        public virtual IDiagnosticContext DiagnosticContext { get; set; } = default!;

        public virtual IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

        public Serilog.ILogger SerilogLogger { get; set; } = default!;

        public virtual IContentFormatter ContentFormatter { get; set; } = default!;

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));

            LogEntryAppLevelConstantInfo logEntryAppLevelConstantInfo = LogEntryAppLevelConstantInfo.GetAppConstantInfo();

            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.ApplicationName), new ScalarValue(logEntryAppLevelConstantInfo.ApplicationName)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppVersion), new ScalarValue(logEntryAppLevelConstantInfo.AppVersion)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppEnvironmentName), new ScalarValue(logEntryAppLevelConstantInfo.AppEnvironmentName)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppWasInDebugMode), new ScalarValue(logEntryAppLevelConstantInfo.AppWasInDebugMode)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppServerName), new ScalarValue(logEntryAppLevelConstantInfo.AppServerName)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppServerOSVersion), new ScalarValue(logEntryAppLevelConstantInfo.AppServerOSVersion)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppServerAppDomainName), new ScalarValue(logEntryAppLevelConstantInfo.AppServerAppDomainName)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppServerProcessId), new ScalarValue(logEntryAppLevelConstantInfo.AppServerProcessId)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppServerUserAccountName), new ScalarValue(logEntryAppLevelConstantInfo.AppServerUserAccountName)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppServerWas64Bit), new ScalarValue(logEntryAppLevelConstantInfo.AppServerWas64Bit)));
            logEvent.AddOrUpdateProperty(new LogEventProperty(nameof(LogEntry.AppWas64Bit), new ScalarValue(logEntryAppLevelConstantInfo.AppWas64Bit)));
        }

        public virtual void SaveLog(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            PerformLog(logEntry);
        }

        public virtual Task SaveLogAsync(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            PerformLog(logEntry);

            return Task.CompletedTask;
        }

        private void PerformLog(LogEntry logEntry)
        {
            if (DiagnosticContext == null || SerilogLogger == null)
                throw new InvalidOperationException($"Configure asp.net core & serilog using https://github.com/serilog/serilog-aspnetcore");

            bool isRequestLogEntry = false;

            if (logEntry.LogData.Any(ld => ld.Key == nameof(IRequestInformationProvider.DisplayUrl)) && HttpContextAccessor.HttpContext != null)
            {
                isRequestLogEntry = true;

                HttpContext httpContext = HttpContextAccessor.HttpContext;

                IUserInformationProvider userInformationProvider = httpContext.RequestServices.GetRequiredService<IUserInformationProvider>();
            }

            var keyValues = logEntry.LogData.Select(ld =>
            {
                string k = ld.Key;

                if (k == nameof(IRequestInformationProvider.HttpMethod)
                || k == nameof(IRequestInformationProvider.DisplayUrl)
                || k == "ResponseStatusCode"
                || ld.Value == null)
                    return (Key: (string?)null, Value: (string?)null); // Already being logged by serilog!

                string? v = null;

                if (ld.Value is string valueAsStr)
                    v = valueAsStr;

                if (k == "ClientLogs" || k == "OperationArgs")
                {
                    v = ContentFormatter.Serialize(ld.Value);
                }
                else
                {
                    v = ld.Value.ToString();
                }

                return (Key: k, Value: v);
            })
            .Where(d => d.Key != null)
            .ToList();

            keyValues.Add((Key: nameof(LogEntry.MemoryUsage), Value: logEntry.MemoryUsage.ToString(CultureInfo.InvariantCulture)));

            if (logEntry.AppServerDateTime != null)
                keyValues.Add((Key: nameof(LogEntry.AppServerDateTime), Value: logEntry.AppServerDateTime.ToString()));

            keyValues.Add((Key: nameof(LogEntry.Severity), Value: logEntry.Severity));
            keyValues.Add((Key: nameof(LogEntry.Message), Value: logEntry.Message));

            if (logEntry.Id != null)
                keyValues.Add((Key: nameof(LogEntry.Id), Value: logEntry.Id.ToString()));

            if (logEntry.AppServerThreadId != null)
                keyValues.Add((Key: nameof(LogEntry.AppServerThreadId), Value: logEntry.AppServerThreadId.ToString()));

            foreach (var (Key, Value) in keyValues.OrderBy(kv => kv.Key))
            {
                DiagnosticContext.Set(Key, Value);
            }

            if (isRequestLogEntry == true)
            {
                LogData userAgent = logEntry.LogData.FirstOrDefault(ld => ld.Key == nameof(IRequestInformationProvider.UserAgent));

                if (userAgent != null)
                    DiagnosticContext.Set("UserAgent", userAgent.Value);
            }
            else
            {
                Exception? ex = null;

                try
                {
                    var (Key, Value) = keyValues.ExtendedSingleOrDefault("Finding ExceptionTypeAssemblyQualifiedName...", kv => kv.Key == "ExceptionTypeAssemblyQualifiedName");

                    if (!string.IsNullOrEmpty(Value))
                        ex = (Exception?)Activator.CreateInstance(Type.GetType(Value) ?? throw new InvalidOperationException($"{Value} could not be found"), args: new object[] { logEntry.Message });
                }
                catch { }

                LogEventLevel level = logEntry.Severity switch
                {
                    "Information" => LogEventLevel.Information,
                    "Warning" => LogEventLevel.Warning,
                    "Error" => LogEventLevel.Error,
                    "Fatal" => LogEventLevel.Fatal,
                    _ => LogEventLevel.Debug,
                };

                SerilogLogger.Write(level, ex?.Message ?? logEntry.Message);
            }
        }
    }
}
