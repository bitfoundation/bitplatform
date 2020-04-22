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
        string GetStaticFilesFolderPath();

        /// <summary>
        /// Combines path with <see cref="GetStaticFilesFolderPath"/>
        /// </summary>
        string MapStaticFilePath(string path);
    }
}