using System;
using System.Threading.Tasks;
using Bit.Signalr.Contracts;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;

namespace Bit.Signalr
{
    public class MessagesHub : Hub
    {
        public override async Task OnConnected()
        {
            try
            {
                IOwinContext context = new OwinContext(Context.Request.Environment);

                Core.Contracts.IDependencyResolver dependencyResolver = context.Get<Core.Contracts.IDependencyResolver>("DependencyResolver");

                await dependencyResolver.Resolve<IMessagesHubEvents>().OnConnected(this);
            }
            finally
            {
                await base.OnConnected();
            }
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            try
            {
                IOwinContext context = new OwinContext(Context.Request.Environment);

                Core.Contracts.IDependencyResolver dependencyResolver = context.Get<Core.Contracts.IDependencyResolver>("DependencyResolver");

                await dependencyResolver.Resolve<IMessagesHubEvents>().OnDisconnected(this, stopCalled);
            }
            catch (ObjectDisposedException) { /* https://github.com/SignalR/SignalR/issues/2972 */ }
            finally
            {
                await base.OnDisconnected(stopCalled);
            }
        }

        public override async Task OnReconnected()
        {
            try
            {
                IOwinContext context = new OwinContext(Context.Request.Environment);

                Core.Contracts.IDependencyResolver dependencyResolver = context.Get<Core.Contracts.IDependencyResolver>("DependencyResolver");

                await dependencyResolver.Resolve<IMessagesHubEvents>().OnReconnected(this);
            }
            finally
            {
                await base.OnReconnected();
            }
        }
    }
}