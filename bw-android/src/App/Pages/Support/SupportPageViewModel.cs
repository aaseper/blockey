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
            var yourAccountItems = new List<SettingsPageListItem>();
            var yourPasswordsItems = new List<SettingsPageListItem>();
            var faqItems = new List<SettingsPageListItem>();
            var aboutUsItems = new List<SettingsPageListItem>();

            IList<SecurityAnalysisStatistics> securityAnalysisStatistics = new List<SecurityAnalysisStatistics>();
            ISet<string> uniquePasswords = new HashSet<string>();

            
            foreach (var cipher in _loginCiphers)
            {
                var passwordStrengthResult = _passwordGenerationService.PasswordStrength(cipher.Login.Password, new List<string>{ cipher.Login.Username });

                string passwordStrengthLevel = PasswordStrengthProjection(passwordStrengthResult);

                securityAnalysisStatistics.Add(new SecurityAnalysisStatistics(cipher, passwordStrengthLevel, new List<string>()));
            }

            /* Security Analysis */
            securityAnalysisItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportSecurityAnalysisProblemSolveAndSupport,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportSecurityAnalysisProblemSolveAndSupport, AppResources.SupportSecurityAnalysisProblemSolveAndSupportText)
            });

            /* Your Account */
            yourAccountItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourAccountCreateAccount,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourAccountCreateAccount, AppResources.SupportYourAccountCreateAccountText)
            });
            yourAccountItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourAccountManageAccount,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourAccountManageAccount, AppResources.SupportYourAccountManageAccountText)
            });
            yourAccountItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourAccountLogIn,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourAccountLogIn, AppResources.SupportYourAccountLogInText)
            });
            yourAccountItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourAccountDeleteAccount,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourAccountDeleteAccount, AppResources.SupportYourAccountDeleteAccountText)
            });

            /* Your Passwords */
            yourPasswordsItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourSavedPasswordsWhatIs,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourSavedPasswordsWhatIs, AppResources.SupportYourSavedPasswordsWhatIsText)
            });
            yourPasswordsItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourSavedPasswordsSavePassword,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourSavedPasswordsSavePassword, AppResources.SupportYourSavedPasswordsSavePasswordText)
            });
            yourPasswordsItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourSavedPasswordsEditPassword,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourSavedPasswordsEditPassword, AppResources.SupportYourSavedPasswordsEditPasswordText)
            });
            yourPasswordsItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourSavedPasswordsGenerateSecurePassword,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourSavedPasswordsGenerateSecurePassword, AppResources.SupportYourSavedPasswordsGenerateSecurePasswordText)
            });
            yourPasswordsItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourSavedPasswordsDeletePassword,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourSavedPasswordsDeletePassword, AppResources.SupportYourSavedPasswordsDeletePasswordText)
            });
            yourPasswordsItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportYourSavedPasswordsPasswordProblemSolve,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportYourSavedPasswordsPasswordProblemSolve, AppResources.SupportYourSavedPasswordsPasswordProblemSolveText)
            });

            /* FAQ */
            faqItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportFAQForget,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportFAQForget, AppResources.SupportFAQForgetText)
            });
            faqItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportFAQMissingPassword,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportFAQMissingPassword, AppResources.SupportFAQMissingPasswordText)
            });

            /* About Us */
            aboutUsItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportAboutBlocKeyWhy,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportAboutBlocKeyWhy, AppResources.SupportAboutBlocKeyWhyText)
            });
            aboutUsItems.Add(new SettingsPageListItem
            {
                Name = AppResources.SupportAboutBlocKeyContactUs,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportAboutBlocKeyContactUs, AppResources.SupportAboutBlocKeyContactUsText)
            });

            var supportListGroupItems = new List<SettingsPageListGroup>()
            {
                new SettingsPageListGroup(securityAnalysisItems, AppResources.SupportSecurityAnalysis, false, true),
                new SettingsPageListGroup(yourAccountItems, AppResources.SupportYourAccount, false, false),
                new SettingsPageListGroup(yourPasswordsItems, AppResources.SupportYourSavedPasswords, false, false),
                new SettingsPageListGroup(faqItems, AppResources.SupportFAQ, false, false),
                new SettingsPageListGroup(aboutUsItems, AppResources.SupportAboutBlocKey, false, false)
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
            var text = Regex.Replace(body, @"\\e0A", "\n", RegexOptions.CultureInvariant);
            var acceptTriggered = await _platformUtilsService.ShowDialogAsync(text, title,
                AppResources.ThankYou);
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

    public class SecurityAnalysisStatistics
    {
        CipherView Cipher { get; set; }
        string PasswordStrengthLevel { get; set; }
        IList<string> ReusedPasswords { get; set; }
        public SecurityAnalysisStatistics(CipherView cipher, string passwordStrengthLevel, IList<string> reusedPasswords)
        {
            Cipher = cipher;
            PasswordStrengthLevel = passwordStrengthLevel;
            ReusedPasswords = reusedPasswords;
        }
    }
}
