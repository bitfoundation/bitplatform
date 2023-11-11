//-:cnd:noEmit
var builder = WebApplication.CreateBuilder(args);

#if BlazorWebAssembly
builder.Configuration.AddClientAppConfigurations();
#endif

BlazorDual.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

BlazorDual.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
