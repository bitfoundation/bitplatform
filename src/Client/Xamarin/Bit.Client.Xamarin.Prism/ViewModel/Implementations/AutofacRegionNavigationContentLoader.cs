using Autofac;
using Autofac.Core;
using Prism.Ioc;
using Prism.Regions;
using Prism.Regions.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace Bit.Client.Xamarin.Prism.ViewModel.Implementations
{
    public class AutofacRegionNavigationContentLoader : RegionNavigationContentLoader
    {
        private readonly ILifetimeScope _lifetimeScope;

        public AutofacRegionNavigationContentLoader(ILifetimeScope lifetimeScope, IContainerExtension containerExtensions) 
            : base(containerExtensions)
        {
            _lifetimeScope = lifetimeScope;
        }

        protected override string GetContractFromNavigationContext(INavigationContext navigationContext)
        {
            var result = base.GetContractFromNavigationContext(navigationContext);

            return result;
        }

        /// <summary>
        /// https://github.com/PrismLibrary/Prism/issues/1120#issuecomment-316459286
        /// </summary>
        protected override IEnumerable<VisualElement> GetCandidatesFromRegion(IRegion region, string candidateNavigationContract)
        {
            if (candidateNavigationContract == null || candidateNavigationContract.Equals(string.Empty))
                throw new ArgumentNullException(nameof(candidateNavigationContract));

            IEnumerable<VisualElement> contractCandidates = base.GetCandidatesFromRegion(region, candidateNavigationContract);

            if (!contractCandidates.Any())
            {
                // First try friendly name registration.
                var matchingRegistration = _lifetimeScope.ComponentRegistry.Registrations.Where(r => r.Services.OfType<KeyedService>().Any(s => s.ServiceKey.Equals(candidateNavigationContract))).FirstOrDefault();

                // If not found, try type registration
                if (matchingRegistration == null)
                    matchingRegistration = _lifetimeScope.ComponentRegistry.Registrations.Where(r => candidateNavigationContract.Equals(r.Activator.LimitType.Name, StringComparison.Ordinal)).FirstOrDefault();

                if (matchingRegistration == null)
                    return Array.Empty<VisualElement>();

                string typeCandidateName = matchingRegistration.Activator.LimitType.FullName;

                contractCandidates = base.GetCandidatesFromRegion(region, typeCandidateName);
            }

            return contractCandidates;
        }

        protected override object CreateNewRegionItem(string candidateTargetContract)
        {
            var result = base.CreateNewRegionItem(candidateTargetContract);

            return result;
        }
    }
}
