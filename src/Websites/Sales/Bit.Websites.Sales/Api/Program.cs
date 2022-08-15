var builder = WebApplication.CreateBuilder(args);

#if DEBUG
// The following line (using the * in the URL), allows the emulators and mobile devices to access the app using the host IP address.
builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000", "https://*:5001", "http://*:5000");
#endif

Bit.Websites.Sales.Api.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

Bit.Websites.Sales.Api.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
