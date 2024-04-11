using System;
using System.Linq;
using System.Threading.Tasks;
using Bit.App.Controls;
using Bit.App.Models;
using Bit.App.Resources;
using Bit.App.Styles;
using Bit.Core.Abstractions;
using Bit.Core.Services;
using Bit.Core.Utilities;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public partial class SecurityAnalysisPage : BaseContentPage, IThemeDirtablePage
    {
        private readonly IVaultTimeoutService _vaultTimeoutService;
        private readonly IBroadcasterService _broadcasterService;
        private readonly TabsPage _tabsPage;
        private SecurityAnalysisPageViewModel _vm;


        public SecurityAnalysisPage()
        {
            InitializeComponent();
            _vm = BindingContext as SecurityAnalysisPageViewModel;
            _vaultTimeoutService = ServiceContainer.Resolve<IVaultTimeoutService>("vaultTimeoutService");
            
            ToolbarItems.Add(_lockItem);
            ToolbarItems.Add(_exitItem);
        }

        public async Task InitAsync()
        {
            await _vm.InitAsync();
        }

        private void RowSelected(object sender, SelectionChangedEventArgs e)
        {
            ((ExtendedCollectionView)sender).SelectedItem = null;
            if (e.CurrentSelection?.FirstOrDefault() is SettingsPageListItem item)
            {
                _vm?.ExecuteSettingItemCommand.Execute(item);
            }
        }

        private async void Lock_Clicked(object sender, EventArgs e)
        {
            await _vaultTimeoutService.LockAsync(true, true);
        }

        private void Exit_Clicked(object sender, EventArgs e)
        {
            _vm.Exit();
        }

        private void Close_Clicked(object sender, EventArgs e)
        {
            _vm.CloseAction();
        }
    }
}
