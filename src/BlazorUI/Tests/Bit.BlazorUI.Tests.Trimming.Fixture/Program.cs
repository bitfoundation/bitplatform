using Bit.BlazorUI;
using Bit.BlazorUI.Tests.Trimming.Fixture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();
builder.Services.AddBitBlazorUIServices();

var app = builder.Build();

app.MapStaticAssets();
app.MapRazorComponents<App>();

app.Run();
