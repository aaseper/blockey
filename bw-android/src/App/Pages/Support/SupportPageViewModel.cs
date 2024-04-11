using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Bit.App.Abstractions;
using Bit.App.Resources;
using Bit.App.Utilities;
using Bit.Core;
using Bit.Core.Abstractions;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Domain;
using Bit.Core.Services;
using Bit.Core.Utilities;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public class SupportPageViewModel : BaseViewModel
    {
        private readonly IPlatformUtilsService _platformUtilsService;
        private readonly IMessagingService _messagingService;
        private readonly ILogger _loggerService;

        public SupportPageViewModel()
        {
            _platformUtilsService = ServiceContainer.Resolve<IPlatformUtilsService>("platformUtilsService");
            _messagingService = ServiceContainer.Resolve<IMessagingService>("messagingService");
            _loggerService = ServiceContainer.Resolve<ILogger>("logger");

            GroupedItems = new ObservableRangeCollection<ISettingsPageListItem>();
            PageTitle = AppResources.SupportPageTitle;

            ExecuteSettingItemCommand = new AsyncCommand<SettingsPageListItem>(item => item.ExecuteAsync(), onException: _loggerService.Exception, allowsMultipleExecutions: false);
        }

        public ObservableRangeCollection<ISettingsPageListItem> GroupedItems { get; set; }

        public IAsyncCommand<SettingsPageListItem> ExecuteSettingItemCommand { get; }

        public void BuildList()
        {
            var faqItems = new List<SettingsPageListItem>();

            faqItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportHowToLogOut,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportHowToLogOut)
            });

            var supportListGroupItems = new List<SettingsPageListGroup>()
            {
                new SettingsPageListGroup(faqItems, AppResources.SupportFAQ, false, true)
            };

            var items = new List<ISettingsPageListItem>();
            foreach (var itemGroup in supportListGroupItems)
            {
                items.Add(new SettingsPageHeaderListItem(itemGroup.Name));
                items.AddRange(itemGroup);
            }

            GroupedItems.ReplaceRange(items);
        }

        public async Task OpenExplanationPopUp(string title)
        {
            var text = string.Format("{0}\n\n{1}", "Para cerrar sesión, abajo en \"Ajustes\", en la sección \"Cuenta\" hay un opción para cerrar sesión.", "También, en los tres puntos arriba a la derecha, se puede \"Bloquear\" la sesión, y después volver a pulsar los tres puntos y \"Cerrar sesión.\"");
            var acceptTriggered = await _platformUtilsService.ShowDialogAsync(text, title,
                AppResources.ThankYou);
            // if (acceptTriggered) { _platformUtilsService.LaunchUri("https://bitwarden.com/es-la/help/getting-started-mobile/"); }
        }

        public async Task InitAsync()
        {
            try
            {
                BuildList();
            }
            catch (Exception ex)
            {
                _loggerService.Exception(ex);
            }
        }

        public void Exit()
        {
            _messagingService.Send("exit");
        }
    }
}
