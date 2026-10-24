var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped(typeof(Bit.Butil.Geolocation));

var app = builder.Build();

#if NET9_0_OR_GREATER
app.MapStaticAssets();
#else
app.UseStaticFiles();
#endif
app.MapFallbackToFile("index.html");

app.Run();
