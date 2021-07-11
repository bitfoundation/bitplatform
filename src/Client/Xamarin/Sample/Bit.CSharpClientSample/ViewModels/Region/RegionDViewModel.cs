﻿using Bit.ViewModel;
using System.Threading.Tasks;

namespace Bit.CSharpClientSample.ViewModels
{
    public class RegionDViewModel : BitViewModelBase
    {
        public BitDelegateCommand GoBackCommand { get; set; }

        public RegionDViewModel()
        {
            GoBackCommand = new BitDelegateCommand(GoBack);
        }

        async Task GoBack()
        {
            RegionManager.Regions["ContentRegion2"].NavigationService.Journal.GoBack();
        }

        public string Text { get; set; } = "D";
    }
}
