using System;
using System.Reflection;

namespace Bit.Tooling.SourceGenerators;

internal static class AutoInjectConstantInformation
{
    public static readonly string AttributeName = typeof(AutoInjectAttribute).FullName;

    public static string GetPackageVersion()
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        return version.ToString();
    }
}
