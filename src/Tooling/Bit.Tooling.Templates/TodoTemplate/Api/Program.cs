var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.WebHost.UseUrls("https://*:5001", "http://*:5000");
#endif

TodoTemplate.Api.Startup.Services.Add(builder);

TodoTemplate.Api.Startup.Middlewares.Use(builder);
