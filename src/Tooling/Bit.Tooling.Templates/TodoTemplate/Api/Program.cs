var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.WebHost.UseUrls("https://*:5001", "http://*:5000");
#endif

TodoTemplate.Api.Startup.Services.Add(builder.Services, builder.Configuration, builder.Environment);

var app = builder.Build();

TodoTemplate.Api.Startup.Middlewares.Use(app, builder.Environment);

app.Run();
