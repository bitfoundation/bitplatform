using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitTools.Core.Contracts
{
    public interface IProjectEnumTypesProvider
    {
        Task<IList<EnumType>> GetProjectEnumTypes(Project project, IList<Project> allSourceProjects = null);
    }
}
