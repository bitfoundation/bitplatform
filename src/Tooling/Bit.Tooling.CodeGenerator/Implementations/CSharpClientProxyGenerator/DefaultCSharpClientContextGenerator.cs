using Bit.Tooling.CodeGenerator.Implementations.CSharpClientProxyGenerator.Templates;
using Bit.Tooling.Core.Contracts.CSharpClientProxyGenerator;
using Bit.Tooling.Core.Model;
using System;
using System.Collections.Generic;

namespace Bit.Tooling.CodeGenerator.Implementations.CSharpClientProxyGenerator
{
    public class DefaultCSharpClientContextGenerator : ICSharpClientContextGenerator
    {
        public virtual string GenerateCSharpContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping)
        {
            if (controllers == null)
                throw new ArgumentNullException(nameof(controllers));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            CSharpContextGeneratorTemplate template = new CSharpContextGeneratorTemplate
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
