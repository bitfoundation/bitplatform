using Bit.Tooling.Core.Model;
using System.Collections.Generic;

namespace Bit.Tooling.Core.Contracts.TypeScriptClientProxyGenerator
{
    public interface ITypeScriptClientProxyDtosGenerator
    {
        string GenerateTypeScriptDtos(IList<Dto> dtos, IList<EnumType> enumTypes, string typingsPath);

        string GenerateJavaScriptDtos(IList<Dto> dtos, IList<EnumType> enumTypes);
    }
}