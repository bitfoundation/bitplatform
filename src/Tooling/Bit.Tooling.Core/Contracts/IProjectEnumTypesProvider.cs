using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bit.Tooling.Core.Contracts
{
    public interface IProjectEnumTypesProvider
    {
        Task<IList<EnumType>> GetProjectEnumTypes(Project project, IList<Project>? allSourceProjects = null);
    }
}
