using System;

namespace Bit.Owin.Models
{
    [Serializable]
    public class DefaultPageModel
    {
        public virtual string AppTitle { get; set; }

        public virtual string Theme { get; set; }

        public virtual string Culture { get; set; }

        public virtual string AppVersion { get; set; }

        public virtual bool DebugMode { get; set; }

        public virtual string AppName { get; set; }

        public virtual string DesiredTimeZoneValue { get; set; }

        public virtual string EnvironmentConfigsJson { get; set; }

        public virtual string BaseHref { get; set; }

        public override string ToString()
        {
            return $"{nameof(AppName)}: {AppName}, {nameof(DebugMode)}: {DebugMode}";
        }
    }
}
