using Bit.Tooling.CodeGenerator.Implementations.TypeScriptClientProxyGenerator.Templates;
using Bit.Tooling.Core.Contracts.TypeScriptClientProxyGenerator;
using Bit.Tooling.Core.Model;
using System;
using System.Collections.Generic;

namespace Bit.Tooling.CodeGenerator.Implementations.TypeScriptClientProxyGenerator
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
