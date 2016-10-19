using Foundation.Core.Contracts;
using Foundation.Model.DomainModels;
using Foundation.Model.Dtos;
using Humanizer.Inflections;

namespace Foundation.Api.Implementations.Project
{
    public class FoundationInitialNameServiceConfiguration : IAppEvents
    {
        public virtual void OnAppStartup()
        {
            Vocabularies.Default.AddPlural(nameof(JobInfo), "JobsInfo");
            Vocabularies.Default.AddPlural(nameof(UserSetting), "UsersSettings");
            Vocabularies.Default.AddPlural(nameof(ClientLogDto), "ClientsLogs");
        }

        public virtual void OnAppEnd()
        {

        }
    }
}
