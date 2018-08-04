using Bit.Core.Contracts;
using System;
using System.IO;

namespace Bit.Core.Implementations
{
    public class DefaultPathProvider : IPathProvider
    {
        private static IPathProvider _current;

        public static IPathProvider Current
        {
            get => _current ?? (_current = new DefaultPathProvider());
            set => _current = value;
        }

        public virtual string GetCurrentAppPath()
        {
            return AppContext.BaseDirectory;
        }

        public virtual string MapPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return Path.Combine(GetCurrentAppPath(), path);
        }

        public string GetStaticFilesFolderPath()
        {
            return MapPath(DefaultAppEnvironmentsProvider.Current.GetActiveAppEnvironment().GetConfig("StaticFilesRelativePath", "./wwwroot/"));
        }

        public string MapStaticFilePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return Path.Combine(GetStaticFilesFolderPath(), path);
        }
    }
}
