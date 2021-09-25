using Bit.Core.Contracts;
using IdentityServer3.Core.Events;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    internal class FakeEventService : IEventService
    {
        public Task RaiseAsync<T>(Event<T> evt)
        {
            throw new InvalidOperationException("This method wasn't expected to be called.");
        }
    }

    public class DefaultEventService : IEventService
    {
        public virtual ILogger Logger { get; set; } = default!;

        public virtual async Task RaiseAsync<T>(Event<T> @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            if (@event.EventType == EventTypes.Error || @event.EventType == EventTypes.Failure)
            {
                Logger.AddLogData("IdentityServerEventId", @event.Id);
                Logger.AddLogData(nameof(Event<object>.Name), @event.Name);
                Logger.AddLogData(nameof(Event<object>.Category), @event.Category);
                Logger.AddLogData(nameof(Event<object>.EventType), @event.EventType);
                Logger.AddLogData("IdentityServerMessage", @event.Message);

                Logger.AddLogData(nameof(EventContext.ActivityId), @event.Context.ActivityId);
                Logger.AddLogData(nameof(EventContext.RemoteIpAddress), @event.Context.RemoteIpAddress);
                Logger.AddLogData(nameof(EventContext.SubjectId), @event.Context.SubjectId);

                if (@event is Event<LocalLoginDetails> localLoginEvent)
                {
                    Logger.AddLogData("UserName", localLoginEvent.Details.LoginUserName);

                    LogSignInMessage(localLoginEvent.Details.SignInMessage);
                }

                if (@event is Event<ExternalLoginDetails> externalLoginEvent)
                {
                    Logger.AddLogData("Provider", externalLoginEvent.Details.Provider);
                    Logger.AddLogData("ProviderId", externalLoginEvent.Details.ProviderId);

                    LogSignInMessage(externalLoginEvent.Details.SignInMessage);
                }
            }
        }

        protected virtual void LogSignInMessage(SignInMessage signInMessage)
        {
            if (signInMessage == null)
                throw new ArgumentNullException(nameof(signInMessage));

            Logger.AddLogData("ClientId", signInMessage.ClientId);

            if (signInMessage.AcrValues != null && signInMessage.AcrValues.Any())
                Logger.AddLogData("AcrValues", string.Join(" ", signInMessage.AcrValues));

            if (signInMessage.ReturnUrl != null)
                Logger.AddLogData("ReturnUrl", signInMessage.ReturnUrl);

            if (signInMessage.IdP != null)
                Logger.AddLogData("IdP", signInMessage.IdP);

            if (signInMessage.Tenant != null)
                Logger.AddLogData("Tenant", signInMessage.Tenant);
        }
    }
}
