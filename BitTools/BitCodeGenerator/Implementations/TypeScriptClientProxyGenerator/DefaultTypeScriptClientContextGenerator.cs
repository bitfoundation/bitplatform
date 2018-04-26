using System;
using System.Collections.Generic;
using BitTools.Core.Contracts.TypeScriptClientProxyGenerator;
using BitCodeGenerator.Implementations.TypeScriptClientProxyGenerator.Templates;
using BitTools.Core.Model;

namespace BitCodeGenerator.Implementations.TypeScriptClientProxyGenerator
{
    public class DefaultTypeScriptClientContextGenerator : ITypeScriptClientContextGenerator
    {
        public virtual string GenerateTypeScriptContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping)
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

        public virtual string GenerateJavaScriptContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping)
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
