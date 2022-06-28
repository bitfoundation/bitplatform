﻿var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.WebHost.UseUrls("https://*:5001", "http://*:5000");
#endif

Bit.BlazorUI.Playground.Api.Startup.Services.Add(builder.Services);

var app = builder.Build();

Bit.BlazorUI.Playground.Api.Startup.Middlewares.Use(app, builder.Environment);

app.Run();
