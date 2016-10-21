namespace IdentityServer.Api.Model
{
    public class SSOPageModel
    {
        public virtual string AppTitle { get; set; }

        public virtual string Theme { get; set; }

        public virtual string Culture { get; set; }

        public virtual string AppVersion { get; set; }

        public virtual bool DebugMode { get; set; }

        public virtual string AppName { get; set; }

        public virtual string DesiredTimeZoneValue { get; set; }

        public virtual string EnvironmentConfigsJSON { get; set; }
    }
}
