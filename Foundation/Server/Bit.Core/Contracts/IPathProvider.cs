namespace Bit.Core.Contracts
{
    public interface IPathProvider
    {
        string MapPath(string path);

        string GetCurrentAppPath();

        string GetCurrentStaticFilesPath();

        string StaticFileMapPath(string path);
    }
}