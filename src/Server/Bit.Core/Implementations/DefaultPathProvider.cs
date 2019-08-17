using System;
using System.IO;
using Bit.Core.Contracts;
using Bit.Core.Models;

namespace Bit.Core.Implementations
{
    public class DefaultPathProvider : IPathProvider
    {
        private static IPathProvider _current;

        public static IPathProvider Current
        {
            get
            {
                if (_current == null)
                    _current = new DefaultPathProvider();
                return _current;
            }
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
            return MapPath(DefaultAppEnvironmentsProvider.Current.GetActiveAppEnvironment().GetConfig(AppEnvironment.KeyValues.StaticFilesRelativePath, AppEnvironment.KeyValues.StaticFilesRelativePathDefaultValue));
        }

        public string MapStaticFilePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return Path.Combine(GetStaticFilesFolderPath(), path);
        }
    }
}