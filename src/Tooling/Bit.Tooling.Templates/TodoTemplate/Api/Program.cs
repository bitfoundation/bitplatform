var builder = WebApplication.CreateBuilder(args);

//-:cnd:noEmit
#if DEBUG
builder.WebHost.UseUrls("https://*:5001", "http://*:5000");
#endif
//+:cnd:noEmit

TodoTemplate.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

TodoTemplate.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
