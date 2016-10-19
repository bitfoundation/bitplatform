using System;
using System.Collections.Generic;
using Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator;
using Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator.Templates;
using Foundation.CodeGenerators.Model;

namespace Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator
{
    public class DefaultHtmlClientContainerGenerator : IHtmlClientContainerGenerator
    {
        public virtual string GenerateTypeScriptContainer(IList<DtoController> controllers, HtmlClientProxyGeneratorMapping mapping)
        {
            if (controllers == null)
                throw new ArgumentNullException(nameof(controllers));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            TypeScriptContainerGeneratorTemplate template = new TypeScriptContainerGeneratorTemplate
            {
                Session = new Dictionary<string, object>
                {
                    { "Controllers", controllers },
                    { "Mapping", mapping }
                }
            };

            template.Initialize();

            return template.TransformText();
        }

        public virtual string GenerateJavaScriptContainer(IList<DtoController> controllers, HtmlClientProxyGeneratorMapping mapping)
        {
            if (controllers == null)
                throw new ArgumentNullException(nameof(controllers));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            JavaScriptContainerGeneratorTemplate template = new JavaScriptContainerGeneratorTemplate
            {
                Session = new Dictionary<string, object>
                {
                    { "Controllers", controllers },
                    { "Mapping", mapping }
                }
            };

            template.Initialize();

            return template.TransformText();
        }
    }
}
