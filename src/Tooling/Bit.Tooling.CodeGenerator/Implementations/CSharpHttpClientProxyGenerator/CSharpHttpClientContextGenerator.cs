using Bit.Tooling.CodeGenerator.Implementations.CSharpHttpClientProxyGenerator.Templates;
using Bit.Tooling.Core.Contracts.CSharpClientProxyGenerator;
using Bit.Tooling.Core.Model;
using System;
using System.Collections.Generic;

namespace Bit.Tooling.CodeGenerator.Implementations.CSharpHttpClientProxyGenerator
{
    public class CSharpHttpClientContextGenerator : ICSharpClientContextGenerator
    {
        public virtual string GenerateCSharpContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping)
        {
            if (controllers == null)
                throw new ArgumentNullException(nameof(controllers));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            CSharpHttpClientContextGeneratorTemplate template = new CSharpHttpClientContextGeneratorTemplate
            {
                Session = new Dictionary<string, object>
                {
                    { "Controllers", controllers },
                    { "Mapping", mapping },
                    { "BitToolingVersion", typeof(CSharpHttpClientContextGenerator).Assembly.GetName().Version!.ToString() }
                }
            };

            template.Initialize();

            return template.TransformText();
        }
    }
}
