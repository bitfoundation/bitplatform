var builder = WebApplication.CreateBuilder(args);

#if BlazorWebAssembly
builder.Configuration.AddClientConfigurations();
#endif

Bit.Websites.Sales.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

Bit.Websites.Sales.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
