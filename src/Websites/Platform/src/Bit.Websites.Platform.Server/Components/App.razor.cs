﻿using Microsoft.AspNetCore.Components;

namespace Bit.Websites.Platform.Server.Components;

[StreamRendering(enabled: true)]
public partial class App
{
    [CascadingParameter] HttpContext HttpContext { get; set; } = default!;

    protected override void OnInitialized()
    {
        HttpContext.Response.OnStarting(async _ =>
        {
            HttpContext.Response.GetTypedHeaders().CacheControl = new()
            {
                MaxAge = TimeSpan.FromDays(7),
                Public = true
            };
        }, null!);

        base.OnInitialized();
    }
}
