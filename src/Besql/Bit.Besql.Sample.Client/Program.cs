using Bit.Besql.Sample.Client.Data;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAppServices();

var app = builder.Build();

// To Create database and apply migrations
await using (var scope = app.Services.CreateAsyncScope())
{
    // Create db context
    await using var dbContext = await scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<OfflineDbContext>>()
        .CreateDbContextAsync();

    // migrate database
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
