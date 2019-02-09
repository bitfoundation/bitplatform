using Bit.Core.Models;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Core.Contracts
{
    /// <summary>
    /// A contract which allow you to manage background jobs
    /// </summary>
    public interface IBackgroundJobWorker
    {
        /// <summary>
        /// Starts a job in background immediately and returns its Job Id. Storing job info in database will be performed async.
        /// </summary>
        /// <returns>Job Id</returns>
        Task<string> PerformBackgroundJobAsync<TService>(Expression<Action<TService>> methodCall);

        /// <summary>
        /// Starts a job in background immediately and returns its Job Id.
        /// </summary>
        /// <returns>Job Id</returns>
        string PerformBackgroundJob<TService>(Expression<Action<TService>> methodCall);

        /// <summary>
        /// Starts a job when another job is finished, no matter that job is succeeded or not. Storing job info in database will be performed async.
        /// </summary>
        /// <returns>Job Id</returns>
        Task<string> PerformBackgroundJobWhenAnotherJobFinishedAsync<TService>(string anotherJobId, Expression<Action<TService>> methodCall);

        /// <summary>
        /// Starts a job when another job is finished, no matter that job is succeeded or not.
        /// </summary>
        /// <returns>Job Id</returns>
        string PerformBackgroundJobWhenAnotherJobFinished<TService>(string anotherJobId, Expression<Action<TService>> methodCall);

        /// <summary>
        /// Starts a job only when another job is succeeded. Storing job info in database will be performed async.
        /// </summary>
        /// <returns>Job Id</returns>
        Task<string> PerformBackgroundJobWhenAnotherJobSucceededAsync<TService>(string anotherJobId, Expression<Action<TService>> methodCall);

        /// <summary>
        /// Starts a job only when another job is succeeded.
        /// </summary>
        /// <returns>Job Id</returns>
        string PerformBackgroundJobWhenAnotherJobSucceeded<TService>(string anotherJobId, Expression<Action<TService>> methodCall);

        /// <summary>
        /// Starts a job in background at specified time and returns its Job Id. Storing job info in database will be performed async.
        /// </summary>
        /// <returns>Job Id</returns>
        Task<string> PerformBackgroundJobAsync<TService>(Expression<Action<TService>> methodCall, TimeSpan when);

        /// <summary>
        /// Starts a job in background at specified time and returns its Job Id.
        /// </summary>
        /// <returns>Job Id</returns>
        string PerformBackgroundJob<TService>(Expression<Action<TService>> methodCall, TimeSpan when);

        /// <summary>
        /// By providing job id (key), you can get access to its info.
        /// </summary>
        Task<JobInfo> GetJobInfoAsync(string key, CancellationToken cancellationToken);

        /// <summary>
        /// By providing job id (key), you can get access to its info.
        /// </summary>
        JobInfo GetJobInfo(string key, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules a job using provided cronExpression. Storing job info in database will be performed async.
        /// </summary>
        /// <param name="jobId">You've to provide your own meaningful job id, so you can use that in <see cref="StopRecurringBackgroundJob(string)"/> and <see cref="TriggerRecurringBackgroundJob(string)"/> </param>
        Task PerformRecurringBackgroundJobAsync<TService>(string jobId, Expression<Action<TService>> methodCall, string cronExpression, TimeZoneInfo timeZoneInfo = null);

        /// <summary>
        /// Schedules a job using provided cronExpression.
        /// </summary>
        /// <param name="jobId">You've to provide your own meaningful job id, so you can use that in <see cref="StopRecurringBackgroundJob(string)"/> and <see cref="TriggerRecurringBackgroundJob(string)"/> </param>
        void PerformRecurringBackgroundJob<TService>(string jobId, Expression<Action<TService>> methodCall, string cronExpression, TimeZoneInfo timeZoneInfo = null);

        /// <summary>
        /// You can cancel job schedule you have defined using <see cref="PerformRecurringBackgroundJob{TService}(string, Expression{Action{TService}}, string, TimeZoneInfo)"/>. Storing job cancellation info in database will be performed async.
        /// </summary>
        Task StopRecurringBackgroundJobAsync(string jobId);

        /// <summary>
        /// You can cancel job schedule you have defined using <see cref="PerformRecurringBackgroundJob{TService}(string, Expression{Action{TService}}, string, TimeZoneInfo)"/>.
        /// </summary>
        void StopRecurringBackgroundJob(string jobId);

        /// <summary>
        /// Runs the job you have defined using <see cref="PerformRecurringBackgroundJob{TService}(string, Expression{Action{TService}}, string, TimeZoneInfo)"/> immediately. Storing job info in database will be performed async.
        /// </summary>
        Task TriggerRecurringBackgroundJobAsync(string jobId);

        /// <summary>
        /// Runs the job you have defined using <see cref="PerformRecurringBackgroundJob{TService}(string, Expression{Action{TService}}, string, TimeZoneInfo)"/> immediately.
        /// </summary>
        void TriggerRecurringBackgroundJob(string jobId);
    }
}
