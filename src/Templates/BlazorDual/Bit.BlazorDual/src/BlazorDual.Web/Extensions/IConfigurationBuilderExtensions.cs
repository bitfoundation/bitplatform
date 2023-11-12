﻿using System.Reflection;

namespace Microsoft.Extensions.Configuration;

public static class IConfigurationBuilderExtensions
{
    public static void AddClientConfigurations(this IConfigurationBuilder builder)
    {
        var assembly = Assembly.Load("BlazorDual.Web");
        builder.AddJsonStream(assembly.GetManifestResourceStream("BlazorDual.Web.appsettings.json")!);
    }
}
