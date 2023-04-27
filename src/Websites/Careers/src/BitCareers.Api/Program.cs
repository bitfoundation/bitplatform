var builder = WebApplication.CreateBuilder(args);

BitCareers.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

BitCareers.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
