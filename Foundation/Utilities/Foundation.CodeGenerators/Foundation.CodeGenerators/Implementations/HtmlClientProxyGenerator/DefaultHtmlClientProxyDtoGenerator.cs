using System;
using System.Collections.Generic;
using Foundation.CodeGenerators.Contracts.HtmlClientProxyGenerator;
using Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator.Templates;
using Foundation.CodeGenerators.Model;

namespace Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator
{
    public class DefaultHtmlClientProxyDtoGenerator : IHtmlClientProxyDtosGenerator
    {
        public virtual string GenerateTypeScriptDtos(IList<Dto> dtos, IList<EnumType> enumTypes, string typingsPath)
        {
            if (dtos == null)
                throw new ArgumentNullException(nameof(dtos));

            if (typingsPath == null)
                throw new ArgumentNullException(nameof(typingsPath));

            TypeScriptDtosGeneratorTemplate template = new TypeScriptDtosGeneratorTemplate
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

            JavaScriptDtosGeneratorTemplate template = new JavaScriptDtosGeneratorTemplate
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