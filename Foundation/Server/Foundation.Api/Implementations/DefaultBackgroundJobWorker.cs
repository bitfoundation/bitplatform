using Hangfire;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Foundation.Api.Contracts;
using Foundation.Api.Exceptions;
using Foundation.Api.Metadata;
using Foundation.Model.DomainModels;
using Hangfire.Storage;

namespace Foundation.Api.Implementations
{
    public class DefaultBackgroundJobWorker : IBackgroundJobWorker
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

        public virtual Task PerformRecurringBackgroundJobAsync<TService>(string jobId, Expression<Action<TService>> methodCall, string cronExpression)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            if (cronExpression == null)
                throw new ArgumentNullException(nameof(cronExpression));

            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);

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
                    throw new AppException(FoundationMetadataBuilder.JobNotFound);
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
                    throw new AppException(FoundationMetadataBuilder.JobNotFound);
                }

                return new JobInfo
                {
                    Id = jobId,
                    CreatedAt = jobData.CreatedAt,
                    State = jobData.State
                };
            }
        }

        public virtual void PerformRecurringBackgroundJob<TService>(string jobId, Expression<Action<TService>> methodCall, string cronExpression)
        {
            if (methodCall == null)
                throw new ArgumentNullException(nameof(methodCall));

            if (cronExpression == null)
                throw new ArgumentNullException(nameof(cronExpression));

            if (jobId == null)
                throw new ArgumentNullException(nameof(jobId));

            RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
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
