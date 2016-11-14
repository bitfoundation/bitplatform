using System;
using System.Collections.Generic;
using Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator;
using Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator.Templates;
using Foundation.CodeGenerators.Model;

namespace Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator
{
    public class DefaultHtmlClientContextGenerator : IHtmlClientContextGenerator
    {
        public virtual string GenerateTypeScriptContext(IList<DtoController> controllers, HtmlClientProxyGeneratorMapping mapping)
        {
            if (controllers == null)
                throw new ArgumentNullException(nameof(controllers));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            TypeScriptContextGeneratorTemplate template = new TypeScriptContextGeneratorTemplate
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

        public virtual string GenerateJavaScriptContext(IList<DtoController> controllers, HtmlClientProxyGeneratorMapping mapping)
        {
            if (controllers == null)
                throw new ArgumentNullException(nameof(controllers));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            JavaScriptContextGeneratorTemplate template = new JavaScriptContextGeneratorTemplate
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
