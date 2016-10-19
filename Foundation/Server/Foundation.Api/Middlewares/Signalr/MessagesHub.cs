using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Foundation.Api.Middlewares.SignalR.Contracts;

namespace Foundation.Api.Middlewares.SignalR
{
    public class MessagesHub : Hub
    {
        public override async Task OnConnected()
        {
            IOwinContext context = new OwinContext(Context.Request.Environment);

            Core.Contracts.IDependencyResolver dependencyResolver = context.Get<Core.Contracts.IDependencyResolver>("DependencyResolver");

            await dependencyResolver.Resolve<IMessagesHubEvents>().OnConnected(this);

            await base.OnConnected();
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            IOwinContext context = new OwinContext(Context.Request.Environment);

            Core.Contracts.IDependencyResolver dependencyResolver = context.Get<Core.Contracts.IDependencyResolver>("DependencyResolver");

            await dependencyResolver.Resolve<IMessagesHubEvents>().OnDisconnected(this, stopCalled);

            await base.OnDisconnected(stopCalled);
        }

        public override async Task OnReconnected()
        {
            IOwinContext context = new OwinContext(Context.Request.Environment);

            Core.Contracts.IDependencyResolver dependencyResolver = context.Get<Core.Contracts.IDependencyResolver>("DependencyResolver");

            await dependencyResolver.Resolve<IMessagesHubEvents>().OnReconnected(this);

            await base.OnReconnected();
        }
    }
}