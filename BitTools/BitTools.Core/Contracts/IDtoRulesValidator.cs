using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace BitTools.Core.Contracts
{
    public interface IDtoRulesValidator
    {
        void Validate(DtoRules dtoRules, IList<Project> allSourceProjects = null);
    }
}
