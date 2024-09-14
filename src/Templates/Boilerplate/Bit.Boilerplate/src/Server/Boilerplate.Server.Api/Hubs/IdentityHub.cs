using Microsoft.AspNetCore.SignalR;

namespace Boilerplate.Server.Api.Hubs;

[Authorize]
public partial class IdentityHub : Hub
{
}
