using Bit.Tooling.CodeGenerator.Implementations.TypeScriptJayDataClientProxyGenerator.Templates;
using Bit.Tooling.Core.Contracts.TypeScriptClientProxyGenerator;
using Bit.Tooling.Core.Model;
using System;
using System.Collections.Generic;

namespace Bit.Tooling.CodeGenerator.Implementations.TypeScriptClientProxyGenerator
{
    public class TypeScriptJayDataClientContextGenerator : ITypeScriptClientContextGenerator
    {
        public virtual string GenerateTypeScriptContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping)
        {
            if (controllers == null)
                throw new ArgumentNullException(nameof(controllers));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            TypeScriptJayDataContextGeneratorTemplate template = new TypeScriptJayDataContextGeneratorTemplate
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

            JavaScriptJayDataContextGeneratorTemplate template = new JavaScriptJayDataContextGeneratorTemplate
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
