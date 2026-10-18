var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped(typeof(Bit.Butil.Geolocation));

var app = builder.Build();

app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();
