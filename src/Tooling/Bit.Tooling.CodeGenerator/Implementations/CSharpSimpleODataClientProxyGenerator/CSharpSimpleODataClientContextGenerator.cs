using Bit.Tooling.CodeGenerator.Implementations.CSharpSimpleODataClientProxyGenerator.Templates;
using Bit.Tooling.Core.Contracts.CSharpClientProxyGenerator;
using Bit.Tooling.Core.Model;
using System;
using System.Collections.Generic;

namespace Bit.Tooling.CodeGenerator.Implementations.CSharpSimpleODataClientProxyGenerator
{
    public class CSharpSimpleODataClientContextGenerator : ICSharpClientContextGenerator
    {
        public virtual string GenerateCSharpContext(IList<DtoController> controllers, BitCodeGeneratorMapping mapping)
        {
            if (controllers == null)
                throw new ArgumentNullException(nameof(controllers));

            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            CSharpSimpleODataClientContextGeneratorTemplate template = new CSharpSimpleODataClientContextGeneratorTemplate
            {
                Session = new Dictionary<string, object>
                {
                    { "Controllers", controllers },
                    { "Mapping", mapping },
                    { "BitToolingVersion", typeof(CSharpSimpleODataClientContextGenerator).Assembly.GetName().Version!.ToString() }
                }
            };

            template.Initialize();

            return template.TransformText();
        }
    }
}
