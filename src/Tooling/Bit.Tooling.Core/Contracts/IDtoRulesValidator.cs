using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Bit.Tooling.Core.Contracts
{
    public interface IDtoRulesValidator
    {
        void Validate(DtoRules dtoRules, IList<Project>? allSourceProjects = null);
    }
}
