using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Bit.App.Controls;
using Bit.Core.Abstractions;
using Bit.Core.Utilities;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public partial class SettingsPage : BaseContentPage
    {
        private readonly TabsPage _tabsPage;
        private readonly IVaultTimeoutService _vaultTimeoutService;
        private SettingsPageViewModel _vm;

        public SettingsPage(TabsPage tabsPage)
        {
            _tabsPage = tabsPage;
            InitializeComponent();
            _vm = BindingContext as SettingsPageViewModel;
            _vm.Page = this;
            _vaultTimeoutService = ServiceContainer.Resolve<IVaultTimeoutService>("vaultTimeoutService");
            ToolbarItems.Add(_lockItem);
            ToolbarItems.Add(_exitItem);
        }

        public async Task InitAsync()
        {
            await _vm.InitAsync();
        }

        public void BuildList()
        {
            _vm.BuildList();
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

        void ActivateTimePicker(object sender, EventArgs args)
        {
            var stackLayout = (ExtendedStackLayout)sender;
            SettingsPageListItem item = (SettingsPageListItem)stackLayout.BindingContext;
            if (item.ShowTimeInput)
            {
                var timePicker = stackLayout.Children.Where(x => x is TimePicker).FirstOrDefault();
                ((TimePicker)timePicker)?.Focus();
            }
        }

        async void OnTimePickerPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            var s = (TimePicker)sender;
            var time = s.Time.TotalMinutes;
            if (s.IsFocused && args.PropertyName == "Time")
            {
                await _vm.VaultTimeoutAsync(false, (int)time);
            }
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
