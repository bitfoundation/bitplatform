using System;
using System.Reflection;

namespace Bit.SourceGenerators;

public static class BitSourceGeneratorUtil
{
    public static string GetPackageVersion()
    {
        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        return version.ToString();
    }
}
