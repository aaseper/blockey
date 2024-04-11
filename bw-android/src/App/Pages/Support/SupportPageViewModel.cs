using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Bit.App.Abstractions;
using Bit.App.Controls;
using Bit.App.Resources;
using Bit.App.Utilities;
using Bit.Core;
using Bit.Core.Abstractions;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Domain;
using Bit.Core.Models.Export;
using Bit.Core.Models.View;
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
        private readonly IPasswordGenerationService _passwordGenerationService;
        protected ICipherService _cipherService { get; }
        public IPasswordStrengthable _passwordStrengthable { get; set; }
        private List<CipherView> _loginCiphers;

        public SupportPageViewModel()
        {
            _platformUtilsService = ServiceContainer.Resolve<IPlatformUtilsService>("platformUtilsService");
            _messagingService = ServiceContainer.Resolve<IMessagingService>("messagingService");
            _loggerService = ServiceContainer.Resolve<ILogger>("logger");
            _cipherService = ServiceContainer.Resolve<ICipherService>("cipherService");
            _passwordGenerationService = ServiceContainer.Resolve<IPasswordGenerationService>("passwordGenerationService");

            GroupedItems = new ObservableRangeCollection<ISettingsPageListItem>();
            PageTitle = AppResources.SupportPageTitle;

            PasswordStrengthViewModel = new PasswordStrengthViewModel(_passwordStrengthable);
            ExecuteSettingItemCommand = new AsyncCommand<SettingsPageListItem>(item => item.ExecuteAsync(), onException: _loggerService.Exception, allowsMultipleExecutions: false);
        }

        public ObservableRangeCollection<ISettingsPageListItem> GroupedItems { get; set; }
        public IAsyncCommand<SettingsPageListItem> ExecuteSettingItemCommand { get; }
        public Action StartRegisterAction { get; set; }
        public bool HasCiphers { get; set; }
        public PasswordStrengthViewModel PasswordStrengthViewModel { get; }

        private string PasswordStrengthProjection(Zxcvbn.Result passwordStrengthResult)
        {
            double PasswordStrengthLevel = (passwordStrengthResult.Score + 1f) / 5f;
            if (PasswordStrengthLevel <= 0.4f) return "VeryWeak";
            else if (PasswordStrengthLevel <= 0.6f) return "Weak";
            else if (PasswordStrengthLevel <= 0.8f) return "Good";
            else return "Strong";
        }
        
        public void BuildList()
        {
            var securityAnalysisItems = new List<SettingsPageListItem>();
            var faqItems = new List<SettingsPageListItem>();

            Dictionary<string, string> reusedPasswords = new Dictionary<string, string>();  // pair id-password
            List<string> weakPasswords = new List<string>();  // id list
            
            foreach (var cipher in _loginCiphers)
            {
                var passwordStrengthResult = _passwordGenerationService.PasswordStrength(cipher.Login.Password, new List<string>{ cipher.Login.Username });

                string passwordStrengthLevel = PasswordStrengthProjection(passwordStrengthResult);
                
                Dictionary<string, string[]> cipherKeyValues = new Dictionary<string, string[]>();

                cipherKeyValues.Add(cipher.Id, new string[3]{ cipher.Name, cipher.Login.Password, passwordStrengthLevel });
                
                securityAnalysisItems.Add(new SettingsPageListItem
                {
                    Name = cipher.Name,
                    ExecuteAsync = () => OpenExplanationPopUp(cipher.Name, passwordStrengthLevel.ToString())
                });
            }

            securityAnalysisItems.Add(new SettingsPageListItem
            {
                Name = "Constraseñas débiles",
                ExecuteAsync = () => OpenExplanationPopUp("Constraseñas débiles", "Body")
            });

            faqItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportHowToLogOut,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportHowToLogOut, "Body")
            });
            faqItems.Add(new SettingsPageListItem
            {
                Name = "Subpágina de Análisis de Seguridad (WIP)",
                ExecuteAsync = async () => await Device.InvokeOnMainThreadAsync(StartRegisterAction)
            });  

            var supportListGroupItems = new List<SettingsPageListGroup>()
            {
                new SettingsPageListGroup(securityAnalysisItems, "Análisis de Seguridad", false, true),
                new SettingsPageListGroup(faqItems, AppResources.SupportFAQ, false, false)
            };

            var items = new List<ISettingsPageListItem>();
            foreach (var itemGroup in supportListGroupItems)
            {
                items.Add(new SettingsPageHeaderListItem(itemGroup.Name));
                items.AddRange(itemGroup);
            }

            GroupedItems.ReplaceRange(items);
        }

        private async Task LoadDataAsync()
        {
            _loginCiphers = (await _cipherService.GetAllDecryptedAsync()).Where(c => c.OrganizationId == null && c.Type == CipherType.Login).ToList();
            HasCiphers = _loginCiphers.Any();
        }

        public async Task OpenExplanationPopUp(string title, string body)
        {
            var text = body;
            var acceptTriggered = await _platformUtilsService.ShowDialogAsync(text, title,
                AppResources.ThankYou);
            // if (acceptTriggered) { _platformUtilsService.LaunchUri("https://bitwarden.com/es-la/help/getting-started-mobile/"); }
        }

        public async Task SelectCipherAsync(CipherView cipher)
        {
            var page = new CipherDetailsPage(cipher.Id);
            await Page.Navigation.PushModalAsync(new NavigationPage(page));
        }

        public async Task InitAsync()
        {
            try
            {
                await LoadDataAsync();
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
