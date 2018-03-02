using Bit.Core.Contracts;
using IdentityServer3.Core.Events;
using IdentityServer3.Core.Services;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultEventService : IEventService
    {
        public virtual IDependencyManager DependencyManager { get; set; }

        public virtual async Task RaiseAsync<T>(Event<T> @event)
        {
            if (@event.EventType == EventTypes.Error || @event.EventType == EventTypes.Failure)
            {
                using (Core.Contracts.IDependencyResolver resolver = DependencyManager.CreateChildDependencyResolver())
                {
                    ILogger logger = resolver.Resolve<ILogger>();

                    logger.AddLogData(nameof(EventContext.ActivityId), @event.Context.ActivityId);
                    logger.AddLogData(nameof(EventContext.RemoteIpAddress), @event.Context.RemoteIpAddress);
                    logger.AddLogData(nameof(EventContext.SubjectId), @event.Context.SubjectId);
                    logger.AddLogData(nameof(Event<object>.Category), @event.Category);
                    logger.AddLogData("IdentityServerEventId", @event.Id);
                    logger.AddLogData(nameof(Event<object>.Name), @event.Name);

                    await logger.LogFatalAsync(@event.Message).ConfigureAwait(false);
                }
            }
        }
    }
}
