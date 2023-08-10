var builder = WebApplication.CreateBuilder(args);

Bit.Websites.Sales.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

Bit.Websites.Sales.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
