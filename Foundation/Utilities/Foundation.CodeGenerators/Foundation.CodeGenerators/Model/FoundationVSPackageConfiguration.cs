namespace Foundation.CodeGenerators.Model
{
    public class FoundationVSPackageConfiguration
    {
        public virtual HtmlClientProxyGeneratorConfiguration HtmlClientProxyGeneratorConfiguration { get; set; }
    }

    public class HtmlClientProxyGeneratorConfiguration
    {
        public virtual HtmlClientProxyGeneratorMapping[] HtmlClientProxyGeneratorMappings { get; set; }
    }

    public class HtmlClientProxyGeneratorMapping
    {
        public virtual string EdmName { get; set; }

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
}
