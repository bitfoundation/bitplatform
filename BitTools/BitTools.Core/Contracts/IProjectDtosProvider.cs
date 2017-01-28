using System.Collections.Generic;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;

namespace BitTools.Core.Contracts
{
    public interface IProjectDtosProvider
    {
        IList<Dto> GetProjectDtos(Project project, IList<Project> sourceProjects = null);
    }
}
