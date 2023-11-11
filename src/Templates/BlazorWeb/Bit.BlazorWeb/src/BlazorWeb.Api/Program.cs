//-:cnd:noEmit
var builder = WebApplication.CreateBuilder(args);

// We need to load the client app configurations to prerender the app on server side.
builder.Configuration.AddClientAppConfigurations();

BlazorWeb.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

BlazorWeb.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
