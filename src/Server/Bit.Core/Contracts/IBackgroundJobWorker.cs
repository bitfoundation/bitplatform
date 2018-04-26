using Bit.Core.Models;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Core.Contracts
{
    public interface IBackgroundJobWorker
    {
        Task<string> PerformBackgroundJobAsync<TService>(Expression<Action<TService>> methodCall);

        string PerformBackgroundJob<TService>(Expression<Action<TService>> methodCall);

        Task<string> PerformBackgroundJobWhenAnotherJobFinishedAsync<TService>(string anotherJobId, Expression<Action<TService>> methodCall);

        string PerformBackgroundJobWhenAnotherJobFinished<TService>(string anotherJobId, Expression<Action<TService>> methodCall);

        Task<string> PerformBackgroundJobWhenAnotherJobSucceededAsync<TService>(string anotherJobId, Expression<Action<TService>> methodCall);

        string PerformBackgroundJobWhenAnotherJobSucceeded<TService>(string anotherJobId, Expression<Action<TService>> methodCall);

        Task<string> PerformBackgroundJobAsync<TService>(Expression<Action<TService>> methodCall, TimeSpan when);

        string PerformBackgroundJob<TService>(Expression<Action<TService>> methodCall, TimeSpan when);

        Task<JobInfo> GetJobInfoAsync(string key, CancellationToken cancellationToken);

        JobInfo GetJobInfo(string key, CancellationToken cancellationToken);

        Task PerformRecurringBackgroundJobAsync<TService>(string jobId, Expression<Action<TService>> methodCall, string cronExpression);

        void PerformRecurringBackgroundJob<TService>(string jobId, Expression<Action<TService>> methodCall, string cronExpression);

        Task StopRecurringBackgroundJobAsync(string jobId);

        void StopRecurringBackgroundJob(string jobId);

        Task TriggerRecurringBackgroundJobAsync(string jobId);

        void TriggerRecurringBackgroundJob(string jobId);
    }
}
