namespace Bit.Core.Contracts
{
    /// <summary>
    /// To get absolute path of your files
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>
        /// Combines path with <see cref="GetCurrentAppPath"/>
        /// </summary>
        string MapPath(string path);

        /// <summary>
        /// Returns current app path
        /// </summary>
        string GetCurrentAppPath();

        /// <summary>
        /// Returns current app path for static files (wwwroot for example)
        /// </summary>
        string GetCurrentStaticFilesPath();

        /// <summary>
        /// Combines path with <see cref="GetCurrentStaticFilesPath"/>
        /// </summary>
        string StaticFileMapPath(string path);
    }
}