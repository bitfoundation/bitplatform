//-:cnd:noEmit
var builder = WebApplication.CreateBuilder(args);

BlazorWeb.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

BlazorWeb.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
