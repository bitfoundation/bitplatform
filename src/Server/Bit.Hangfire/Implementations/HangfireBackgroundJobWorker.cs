using Bit.Core.Contracts;
using Bit.Core.Models;
using Bit.Owin.Exceptions;
using Bit.Owin.Metadata;
using Hangfire;
using Hangfire.Storage;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Hangfire.Implementations
{
    public class HangfireBackgroundJobWorker : IBackgroundJobWorker
    {
        public virtual Task<string> PerformBackgroundJobAsync<TService>(Expression<Action<TService>> methodCall)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            return Task.FromResult(BackgroundJob.Enqueue(methodCall));
        }

        public virtual Task<string> PerformBackgroundJobWhenAnotherJobFinishedAsync<TService>(string anotherJobId, Expression<Action<TService>> methodCall)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            if (anotherJobId == null)
                throw new ArgumentNullException(nameof(anotherJobId));

            return Task.FromResult(BackgroundJob.ContinueWith(anotherJobId, methodCall, JobContinuationOptions.OnAnyFinishedState));
        }

        public virtual Task<string> PerformBackgroundJobWhenAnotherJobSucceededAsync<TService>(string anotherJobId, Expression<Action<TService>> methodCall)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            if (anotherJobId == null)
                throw new ArgumentNullException(nameof(anotherJobId));

            return Task.FromResult(BackgroundJob.ContinueWith(anotherJobId, methodCall, JobContinuationOptions.OnlyOnSucceededState));
        }

        public virtual Task<string> PerformBackgroundJobAsync<TService>(Expression<Action<TService>> methodCall, TimeSpan when)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            return Task.FromResult(BackgroundJob.Schedule(methodCall, when));
        }

        public virtual Task PerformRecurringBackgroundJobAsync<TService>(string jobId, Expression<Action<TService>> methodCall, string cronExpression, TimeZoneInfo timeZoneInfo = null)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            if (cronExpression == null)
                throw new ArgumentNullException(nameof(cronExpression));

            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            RecurringJob.AddOrUpdate(recurringJobId: jobId, methodCall: methodCall, cronExpression: cronExpression, timeZone: timeZoneInfo);

            return Task.CompletedTask;
        }

        public virtual Task StopRecurringBackgroundJobAsync(string jobId)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            RecurringJob.RemoveIfExists(jobId);

            return Task.CompletedTask;
        }

        public virtual Task TriggerRecurringBackgroundJobAsync(string jobId)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            RecurringJob.Trigger(jobId);

            return Task.CompletedTask;
        }

        public virtual Task<JobInfo> GetJobInfoAsync(string jobId, CancellationToken cancellationToken)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            using (IStorageConnection connection = JobStorage.Current.GetConnection())
            {
                JobData jobData = connection.GetJobData(jobId);

                if (jobData == null)
                {
                    throw new AppException(BitMetadataBuilder.JobNotFound);
                }

                return Task.FromResult(new JobInfo
                {
                    Id = jobId,
                    CreatedAt = jobData.CreatedAt,
                    State = jobData.State
                });
            }
        }

        public virtual string PerformBackgroundJob<TService>(Expression<Action<TService>> methodCall)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            return BackgroundJob.Enqueue(methodCall);
        }

        public virtual string PerformBackgroundJobWhenAnotherJobFinished<TService>(string anotherJobId, Expression<Action<TService>> methodCall)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            if (anotherJobId == null)
                throw new ArgumentNullException(nameof(anotherJobId));

            return BackgroundJob.ContinueWith(anotherJobId, methodCall, JobContinuationOptions.OnAnyFinishedState);
        }

        public virtual string PerformBackgroundJobWhenAnotherJobSucceeded<TService>(string anotherJobId, Expression<Action<TService>> methodCall)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            if (anotherJobId == null)
                throw new ArgumentNullException(nameof(anotherJobId));

            return BackgroundJob.ContinueWith(anotherJobId, methodCall, JobContinuationOptions.OnlyOnSucceededState);
        }

        public virtual string PerformBackgroundJob<TService>(Expression<Action<TService>> methodCall, TimeSpan when)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            return BackgroundJob.Schedule(methodCall, when);
        }

        public virtual JobInfo GetJobInfo(string jobId, CancellationToken cancellationToken)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            using (IStorageConnection connection = JobStorage.Current.GetConnection())
            {
                JobData jobData = connection.GetJobData(jobId);

                if (jobData == null)
                {
                    throw new AppException(BitMetadataBuilder.JobNotFound);
                }

                return new JobInfo
                {
                    Id = jobId,
                    CreatedAt = jobData.CreatedAt,
                    State = jobData.State
                };
            }
        }

        public virtual void PerformRecurringBackgroundJob<TService>(string jobId, Expression<Action<TService>> methodCall, string cronExpression, TimeZoneInfo timeZoneInfo = null)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            if (cronExpression == null)
                throw new ArgumentNullException(nameof(cronExpression));

            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            RecurringJob.AddOrUpdate(recurringJobId: jobId, methodCall: methodCall, cronExpression: cronExpression, timeZone: timeZoneInfo);
        }

        public virtual void StopRecurringBackgroundJob(string jobId)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            RecurringJob.RemoveIfExists(jobId);
        }

        public virtual void TriggerRecurringBackgroundJob(string jobId)
        {
            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            RecurringJob.Trigger(jobId);
        }
    }
}
