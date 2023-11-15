﻿var builder = WebApplication.CreateBuilder(args);

Bit.Websites.Careers.Server.Startup.Services.Add(builder.Services, builder.Environment, builder.Configuration);

var app = builder.Build();

Bit.Websites.Careers.Server.Startup.Middlewares.Use(app, builder.Environment, builder.Configuration);

app.Run();
