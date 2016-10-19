namespace Foundation.Core.Contracts
{
    public interface IPathProvider
    {
        string MapPath(string path);

        string GetCurrentAppPath();
    }
}