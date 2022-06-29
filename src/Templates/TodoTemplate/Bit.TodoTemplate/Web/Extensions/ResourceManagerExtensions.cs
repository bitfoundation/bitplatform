namespace System.Resources;

public static class ResourceManagerExtensions
{
    public static string Translate(this ResourceManager resourceManager, string name, params string[] args)
    {
        return string.Format(resourceManager.GetString(name) ?? name, args);
    }
}
