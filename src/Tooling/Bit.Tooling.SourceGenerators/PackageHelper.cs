using System;
using System.Reflection;

namespace Bit.Tooling.SourceGenerators;

internal static class PackageHelper
{
    public static string GetPackageVersion()
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        return version.ToString();
    }
}
