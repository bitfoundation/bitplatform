using System;
using System.Threading.Tasks;

namespace Bit.Core.Contracts
{
    public static class IDependencyManagerExtensions
    {
        public static IDependencyManager RegisterAppEvents<TAppEvents>(this IDependencyManager dependencyManager)
            where TAppEvents : class, IAppEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IAppEvents, TAppEvents>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExisting: false);

            return dependencyManager;
        }

        public static async Task<bool> TransactionAction(this IDependencyManager dependencyManager, string operationName, Func<IDependencyResolver, Task> task)
        {
            await using var resolver = dependencyManager.CreateChildDependencyResolver();

            try
            {
                await task.Invoke(resolver);

                resolver.Resolve<IScopeStatusManager>().MarkAsSucceeded();

                return true;
            }
            catch (Exception exp)
            {
                await resolver.Resolve<ILogger>().LogExceptionAsync(exp, $"{operationName} failed.");
                resolver.Resolve<IScopeStatusManager>().MarkAsFailed(exp.Message);

                return false;
            }
        }

        public static async Task<TResult> TransactionFunc<TResult>(this IDependencyManager dependencyManager, string operationName, Func<IDependencyResolver, Task<TResult>> task)
        {
            await using var resolver = dependencyManager.CreateChildDependencyResolver();

            try
            {
                TResult result = await task.Invoke(resolver);

                resolver.Resolve<IScopeStatusManager>().MarkAsSucceeded();

                return result;
            }
            catch (Exception exp)
            {
                await resolver.Resolve<ILogger>().LogExceptionAsync(exp, $"{operationName} failed.");
                resolver.Resolve<IScopeStatusManager>().MarkAsFailed(exp.Message);
                throw;
            }
        }
    }
}
