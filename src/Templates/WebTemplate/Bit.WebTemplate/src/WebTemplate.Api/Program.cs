//-:cnd:noEmit
using WebTemplate.Web.Shared;

var builder = WebApplication.CreateBuilder(args);

WebTemplate.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

WebTemplate.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
