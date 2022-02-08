var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.WebHost.UseUrls("https://*:5001", "http://*:5000");
#endif

Bit.Client.Web.BlazorUI.Playground.Api.Startup.Services.Add(builder);

Bit.Client.Web.BlazorUI.Playground.Api.Startup.Middlewares.Use(builder);
