using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;

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
        private IList<CipherView> _loginCiphers;

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
            
            IList<CipherView> veryWeakCiphers = new List<CipherView>();
            IList<CipherView> weakCiphers = new List<CipherView>();
            IList<CipherView> reusedCiphers = new List<CipherView>();
            ISet<string> uniquePasswords = new HashSet<string>();
            
            foreach (var cipher in _loginCiphers)
            {
                var passwordStrengthResult = _passwordGenerationService.PasswordStrength(cipher.Login.Password, new List<string>{ cipher.Login.Username ?? "" });
                string passwordStrengthLevel = PasswordStrengthProjection(passwordStrengthResult);
                bool isReusedPassword = !uniquePasswords.Add(cipher.Login.Password);
                
                if (passwordStrengthLevel.Equals("VeryWeak")) veryWeakCiphers.Add(cipher);
                else if (passwordStrengthLevel.Equals("Weak")) weakCiphers.Add(cipher);

                if (isReusedPassword) reusedCiphers.Add(cipher);
            }

            if (!HasCiphers || 
                (veryWeakCiphers.Count == 0 && weakCiphers.Count == 0 && reusedCiphers.Count == 0))
            { 
                securityAnalysisItems.Add(new SettingsPageListItem
                {
                    Name = AppResources.SupportSecurityAnalysisCongratulation
                });
            }

            foreach (var cipher in veryWeakCiphers)
            {
                securityAnalysisItems.Add(new SettingsPageListItem
                {
                    Name = string.Format(AppResources.SupportSecurityAnalysisVeryWeakPassword ,cipher.Name),
                    ExecuteAsync = () => OpenExplanationPopUp(Regex.Replace(AppResources.SupportSecurityAnalysisVeryWeakPassword, @": \{0}", "", RegexOptions.CultureInvariant), 
                        string.Format(AppResources.SupportSecurityAnalysisVeryWeakPasswordText, cipher.Name)),
                    SubLabel = AppResources.SupportSecurityAnalysisAlert
                });
            }
            
            foreach (var cipher in weakCiphers)
            {
                securityAnalysisItems.Add(new SettingsPageListItem
                {
                    Name = string.Format(AppResources.SupportSecurityAnalysisWeakPassword ,cipher.Name),
                    ExecuteAsync = () => OpenExplanationPopUp(Regex.Replace(AppResources.SupportSecurityAnalysisWeakPassword, @": \{0}", "", RegexOptions.CultureInvariant), 
                        string.Format(AppResources.SupportSecurityAnalysisWeakPasswordText, cipher.Name)),
                    SubLabel = AppResources.SupportSecurityAnalysisAlert
                });
            }

            foreach (var cipher in reusedCiphers)
            {
                securityAnalysisItems.Add(new SettingsPageListItem
                {
                    Name = string.Format(AppResources.SupportSecurityAnalysisReusedPassword, cipher.Name),
                    ExecuteAsync = () => OpenExplanationPopUp(Regex.Replace(AppResources.SupportSecurityAnalysisReusedPassword, @": \{0}", "", RegexOptions.CultureInvariant), 
                        string.Format(AppResources.SupportSecurityAnalysisReusedPasswordText, cipher.Name)),
                    SubLabel = AppResources.SupportSecurityAnalysisAlert
                });
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
                Name = AppResources.SupportFAQGenerateRandom,
                ExecuteAsync = () => OpenExplanationPopUp(AppResources.SupportFAQGenerateRandom, AppResources.SupportFAQGenerateRandomText)
            });
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
            _loginCiphers = (await _cipherService.GetAllDecryptedAsync()).Where(c => c.OrganizationId == null && c.Type == CipherType.Login && !c.IsDeleted && !string.IsNullOrWhiteSpace(c.Login.Password)).ToList();
            HasCiphers = _loginCiphers.Any();
        }

        public async Task OpenExplanationPopUp(string title, string body)
        {
            var text = Regex.Replace(body, @"\\e0A", "\n", RegexOptions.CultureInvariant);
            var acceptTriggered = await _platformUtilsService.ShowDialogAsync(text, title,
                AppResources.ThankYou);
        }

        public async Task InitAsync()
        {
            try
            {
                await Device.InvokeOnMainThreadAsync(() => _loginCiphers = new List<CipherView>());
                await Device.InvokeOnMainThreadAsync(() => GroupedItems.ReplaceRange(new List<ISettingsPageListItem>()));
                
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

        public void Icon_LongPressed(object sender, EventArgs e)
        {
            // Execute async delegates
        }
    }
}
