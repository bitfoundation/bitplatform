using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Foundation.CodeGenerators.Contracts
{
    public interface IProjectEnumTypesProvider
    {
        IList<EnumType> GetProjectEnumTypes(Project project, IList<Project> allSourceProjects = null);
    }
}
