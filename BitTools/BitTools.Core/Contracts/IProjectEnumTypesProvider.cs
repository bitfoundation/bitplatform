using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace BitTools.Core.Contracts
{
    public interface IProjectEnumTypesProvider
    {
        IList<EnumType> GetProjectEnumTypes(Project project, IList<Project> allSourceProjects = null);
    }
}
