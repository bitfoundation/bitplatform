using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Foundation.CodeGenerators.Contracts
{
    public interface IProjectDtoRulesProvider
    {
        IList<DtoRules> GetProjectAllDtoRules(Project project);
    }
}
