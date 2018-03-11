namespace BitTools.Core.Model
{
    public class BitConfig
    {
        public virtual BitCodeGeneratorConfig BitCodeGeneratorConfigs { get; set; }
    }

    public class BitCodeGeneratorConfig
    {
        public virtual BitCodeGeneratorMapping[] BitCodeGeneratorMappings { get; set; }
    }

    public class BitCodeGeneratorMapping
    {
        public virtual string Route { get; set; }

        public virtual string Namespace { get; set; }

        public virtual ProjectInfo[] SourceProjects { get; set; }

        public virtual ProjectInfo DestinationProject { get; set; }

        public virtual NamespaceAlias[] NamespaceAliases { get; set; } = new NamespaceAlias[] { };

        public virtual string DestinationFileName { get; set; }

        public virtual string TypingsPath { get; set; }
    }

    public class ProjectInfo
    {
        public virtual string Name { get; set; }
    }

    public class NamespaceAlias
    {
        public virtual string Namespace { get; set; }

        public virtual string Alias { get; set; }
    }
}
