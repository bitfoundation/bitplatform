//-:cnd:noEmit
var builder = WebApplication.CreateBuilder(args);

#if DEBUG
// ysm: Hataman IDE (Visual Studio) ro dar halat e Run as Admin ejra konid va ba komak e khat e zir, emaulator ha va device ha ye Android, iOS be server az tarigh e IP dastresi khahand dasht.
builder.WebHost.UseUrls("https://*:5001", "http://*:5000");
#endif

TodoTemplate.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

TodoTemplate.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
