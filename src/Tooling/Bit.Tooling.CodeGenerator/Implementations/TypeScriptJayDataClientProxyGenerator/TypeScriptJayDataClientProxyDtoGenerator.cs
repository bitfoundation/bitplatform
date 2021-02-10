using Bit.Tooling.CodeGenerator.Implementations.TypeScriptJayDataClientProxyGenerator.Templates;
using Bit.Tooling.Core.Contracts.TypeScriptClientProxyGenerator;
using Bit.Tooling.Core.Model;
using System;
using System.Collections.Generic;

namespace Bit.Tooling.CodeGenerator.Implementations.TypeScriptClientProxyGenerator
{
    public class TypeScriptJayDataClientProxyDtoGenerator : ITypeScriptClientProxyDtosGenerator
    {
        public virtual string GenerateTypeScriptDtos(IList<Dto> dtos, IList<EnumType> enumTypes, string typingsPath)
        {
            if (dtos == null)
                throw new ArgumentNullException(nameof(dtos));

            if (typingsPath == null)
                throw new ArgumentNullException(nameof(typingsPath));

            TypeScriptJayDataDtosGeneratorTemplate template = new TypeScriptJayDataDtosGeneratorTemplate
            {
                Session = new Dictionary<string, object>
                {
                    { "Dtos", dtos },
                    { "EnumTypes" , enumTypes },
                    { "TypingsPath" , typingsPath },
                }
            };

            template.Initialize();

            return template.TransformText();
        }

        public virtual string GenerateJavaScriptDtos(IList<Dto> dtos, IList<EnumType> enumTypes)
        {
            if (dtos == null)
                throw new ArgumentNullException(nameof(dtos));

            JavaScriptJayDataDtosGeneratorTemplate template = new JavaScriptJayDataDtosGeneratorTemplate
            {
                Session = new Dictionary<string, object>
                {
                    { "Dtos", dtos },
                    { "EnumTypes" , enumTypes }
                }
            };

            template.Initialize();

            return template.TransformText();
        }
    }
}