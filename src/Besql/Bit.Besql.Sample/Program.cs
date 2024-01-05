using Bit.Besql.Sample.Client.Data;
using Bit.Besql.Sample.Client.Pages;
using Bit.Besql.Sample.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Counter).Assembly);

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

app.Run();
