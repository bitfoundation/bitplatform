using System.Collections.Generic;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Contracts
{
    public interface IFoundationVSPackageConfigurationProvider
    {
        FoundationVSPackageConfiguration GetConfiguration(Workspace workspace, Solution solution, IList<Project> projects);
    }
}
