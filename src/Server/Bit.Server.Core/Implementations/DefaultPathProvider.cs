using Bit.Core.Contracts;
using Bit.Core.Models;
using System;
using System.IO;

namespace Bit.Core.Implementations
{
    public class DefaultPathProvider : IPathProvider // Move to Bit.Server.Owin
    {
        private static IPathProvider _current = default!;

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
            return MapPath(DefaultAppEnvironmentsProvider.Current.GetActiveAppEnvironment().GetConfig(AppEnvironment.KeyValues.StaticFilesRelativePath, AppEnvironment.KeyValues.StaticFilesRelativePathDefaultValue)!);
        }

        public string MapStaticFilePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return Path.Combine(GetStaticFilesFolderPath(), path);
        }
    }
}