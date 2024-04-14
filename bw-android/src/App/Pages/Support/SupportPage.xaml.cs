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
    public partial class SupportPage : BaseContentPage, IThemeDirtablePage
    {
        private readonly IVaultTimeoutService _vaultTimeoutService;
        private readonly IBroadcasterService _broadcasterService;
        private readonly TabsPage _tabsPage;
        private SupportPageViewModel _vm;
        

        public SupportPage(bool fromTabPage, Action<string> selectAction = null, TabsPage tabsPage = null, bool isUsernameGenerator = false, string emailWebsite = null, bool editMode = false, AppOptions appOptions = null)
        {
            _tabsPage = tabsPage;
            InitializeComponent();
            _vm = BindingContext as SupportPageViewModel;
            _vaultTimeoutService = ServiceContainer.Resolve<IVaultTimeoutService>("vaultTimeoutService");
            _vm.StartRegisterAction = () => Device.BeginInvokeOnMainThread(async () => await StartRegisterAsync());
            ToolbarItems.Add(_lockItem);
            ToolbarItems.Add(_exitItem);
        }

        public async Task InitAsync()
        {
            await _vm.InitAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            if (Device.RuntimePlatform == Device.Android && _tabsPage != null)
            {
                _tabsPage.ResetToVaultPage();
                return true;
            }
            return base.OnBackButtonPressed();
        }

        private async Task StartRegisterAsync()
        {
            await Navigation.PushModalAsync(new NavigationPage(new SecurityAnalysisPage()));

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
    }
}
