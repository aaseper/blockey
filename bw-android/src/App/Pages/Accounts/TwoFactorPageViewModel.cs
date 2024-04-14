﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using Bit.App.Abstractions;
using Bit.App.Resources;
using Bit.App.Utilities;
using Bit.Core.Abstractions;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Request;
using Bit.Core.Services;
using Bit.Core.Utilities;
using Newtonsoft.Json;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public class TwoFactorPageViewModel : CaptchaProtectedViewModel
    {
        private readonly IDeviceActionService _deviceActionService;
        private readonly IAuthService _authService;
        private readonly ISyncService _syncService;
        private readonly IApiService _apiService;
        private readonly IPlatformUtilsService _platformUtilsService;
        private readonly IEnvironmentService _environmentService;
        private readonly IMessagingService _messagingService;
        private readonly IBroadcasterService _broadcasterService;
        private readonly IStateService _stateService;
        private readonly II18nService _i18nService;
        private readonly IAppIdService _appIdService;
        private readonly IVaultTimeoutService _vaultTimeoutService;
        private readonly ILogger _logger;
        private readonly IDeviceTrustCryptoService _deviceTrustCryptoService;
        private TwoFactorProviderType? _selectedProviderType;
        private string _totpInstruction;
        private string _webVaultUrl = "https://vault.bitwarden.com";
        private bool _enableContinue = false;
        private bool _showContinue = true;

        public TwoFactorPageViewModel()
        {
            _deviceActionService = ServiceContainer.Resolve<IDeviceActionService>("deviceActionService");
            _authService = ServiceContainer.Resolve<IAuthService>("authService");
            _syncService = ServiceContainer.Resolve<ISyncService>("syncService");
            _apiService = ServiceContainer.Resolve<IApiService>("apiService");
            _platformUtilsService = ServiceContainer.Resolve<IPlatformUtilsService>("platformUtilsService");
            _environmentService = ServiceContainer.Resolve<IEnvironmentService>("environmentService");
            _messagingService = ServiceContainer.Resolve<IMessagingService>("messagingService");
            _broadcasterService = ServiceContainer.Resolve<IBroadcasterService>("broadcasterService");
            _stateService = ServiceContainer.Resolve<IStateService>("stateService");
            _i18nService = ServiceContainer.Resolve<II18nService>("i18nService");
            _appIdService = ServiceContainer.Resolve<IAppIdService>("appIdService");
            _vaultTimeoutService = ServiceContainer.Resolve<IVaultTimeoutService>();
            _logger = ServiceContainer.Resolve<ILogger>();
            _deviceTrustCryptoService = ServiceContainer.Resolve<IDeviceTrustCryptoService>();

            PageTitle = AppResources.TwoStepLogin;
            SubmitCommand = new Command(async () => await SubmitAsync());
            MoreCommand = new AsyncCommand(MoreAsync, onException: _logger.Exception, allowsMultipleExecutions: false);
        }

        public string TotpInstruction
        {
            get => _totpInstruction;
            set => SetProperty(ref _totpInstruction, value);
        }

        public bool Remember { get; set; }

        public bool AuthingWithSso { get; set; }

        public string Token { get; set; }

        public bool DuoMethod => SelectedProviderType == TwoFactorProviderType.Duo ||
            SelectedProviderType == TwoFactorProviderType.OrganizationDuo;

        public bool Fido2Method => SelectedProviderType == TwoFactorProviderType.Fido2WebAuthn;

        public bool YubikeyMethod => SelectedProviderType == TwoFactorProviderType.YubiKey;

        public bool AuthenticatorMethod => SelectedProviderType == TwoFactorProviderType.Authenticator;

        public bool EmailMethod => SelectedProviderType == TwoFactorProviderType.Email;

        public bool TotpMethod => AuthenticatorMethod || EmailMethod;

        public bool ShowTryAgain => (YubikeyMethod && Device.RuntimePlatform == Device.iOS) || Fido2Method;

        public bool ShowContinue
        {
            get => _showContinue;
            set => SetProperty(ref _showContinue, value);
        }

        public bool EnableContinue
        {
            get => _enableContinue;
            set => SetProperty(ref _enableContinue, value);
        }

        public string YubikeyInstruction => Device.RuntimePlatform == Device.iOS ? AppResources.YubiKeyInstructionIos :
            AppResources.YubiKeyInstruction;

        public TwoFactorProviderType? SelectedProviderType
        {
            get => _selectedProviderType;
            set => SetProperty(ref _selectedProviderType, value, additionalPropertyNames: new string[]
            {
                nameof(EmailMethod),
                nameof(DuoMethod),
                nameof(Fido2Method),
                nameof(YubikeyMethod),
                nameof(AuthenticatorMethod),
                nameof(TotpMethod),
                nameof(ShowTryAgain),
            });
        }
        public Command SubmitCommand { get; }
        public ICommand MoreCommand { get; }
        public Action TwoFactorAuthSuccessAction { get; set; }
        public Action LockAction { get; set; }
        public Action StartDeviceApprovalOptionsAction { get; set; }
        public Action StartSetPasswordAction { get; set; }
        public Action CloseAction { get; set; }
        public Action UpdateTempPasswordAction { get; set; }

        protected override II18nService i18nService => _i18nService;
        protected override IEnvironmentService environmentService => _environmentService;
        protected override IDeviceActionService deviceActionService => _deviceActionService;
        protected override IPlatformUtilsService platformUtilsService => _platformUtilsService;

        public void Init()
        {
            if ((!_authService.AuthingWithSso() && !_authService.AuthingWithPassword()) ||
                _authService.TwoFactorProvidersData == null)
            {
                // TODO: dismiss modal?
                return;
            }

            if (!string.IsNullOrWhiteSpace(_environmentService.BaseUrl))
            {
                _webVaultUrl = _environmentService.BaseUrl;
            }
            else if (!string.IsNullOrWhiteSpace(_environmentService.WebVaultUrl))
            {
                _webVaultUrl = _environmentService.WebVaultUrl;
            }

            SelectedProviderType = _authService.GetDefaultTwoFactorProvider(_platformUtilsService.SupportsFido2());
            Load();
        }

        public void Load()
        {
            if (SelectedProviderType == null)
            {
                PageTitle = AppResources.LoginUnavailable;
                return;
            }
            var page = Page as TwoFactorPage;
            PageTitle = _authService.TwoFactorProviders[SelectedProviderType.Value].Name;
            var providerData = _authService.TwoFactorProvidersData[SelectedProviderType.Value];
            switch (SelectedProviderType.Value)
            {
                case TwoFactorProviderType.Fido2WebAuthn:
                    Fido2AuthenticateAsync(providerData);
                    break;
                case TwoFactorProviderType.YubiKey:
                    _messagingService.Send("listenYubiKeyOTP", true);
                    break;
                case TwoFactorProviderType.Duo:
                case TwoFactorProviderType.OrganizationDuo:
                    var host = WebUtility.UrlEncode(providerData["Host"] as string);
                    var req = WebUtility.UrlEncode(providerData["Signature"] as string);
                    page.DuoWebView.Uri = $"{_webVaultUrl}/duo-connector.html?host={host}&request={req}";
                    page.DuoWebView.RegisterAction(sig =>
                    {
                        Token = sig;
                        Device.BeginInvokeOnMainThread(async () => await SubmitAsync());
                    });
                    break;
                case TwoFactorProviderType.Email:
                    TotpInstruction = string.Format(AppResources.EnterVerificationCodeEmail,
                        providerData["Email"] as string);
                    if (_authService.TwoFactorProvidersData.Count > 1)
                    {
                        var emailTask = Task.Run(() => SendEmailAsync(false, false));
                    }
                    break;
                case TwoFactorProviderType.Authenticator:
                    TotpInstruction = AppResources.EnterVerificationCodeApp;
                    break;
                default:
                    break;
            }

            if (!YubikeyMethod)
            {
                _messagingService.Send("listenYubiKeyOTP", false);
            }
            ShowContinue = !(SelectedProviderType == null || DuoMethod || Fido2Method);
        }

        public async Task Fido2AuthenticateAsync(Dictionary<string, object> providerData = null)
        {
            await _deviceActionService.ShowLoadingAsync(AppResources.Validating);

            if (providerData == null)
            {
                providerData = _authService.TwoFactorProvidersData[TwoFactorProviderType.Fido2WebAuthn];
            }

            var callbackUri = "bitwarden://webauthn-callback";
            var data = AppHelpers.EncodeDataParameter(new
            {
                callbackUri = callbackUri,
                data = JsonConvert.SerializeObject(providerData),
                headerText = AppResources.Fido2Title,
                btnText = AppResources.Fido2AuthenticateWebAuthn,
                btnReturnText = AppResources.Fido2ReturnToApp,
            });

            var url = _webVaultUrl + "/webauthn-mobile-connector.html?" + "data=" + data +
                      "&parent=" + Uri.EscapeDataString(callbackUri) + "&v=2";

            WebAuthenticatorResult authResult = null;
            try
            {
                var options = new WebAuthenticatorOptions
                {
                    Url = new Uri(url),
                    CallbackUrl = new Uri(callbackUri),
                    PrefersEphemeralWebBrowserSession = true,
                };
                authResult = await WebAuthenticator.AuthenticateAsync(options);
            }
            catch (TaskCanceledException)
            {
                // user canceled
                await _deviceActionService.HideLoadingAsync();
                return;
            }

            string response = null;
            if (authResult != null && authResult.Properties.TryGetValue("data", out var resultData))
            {
                response = Uri.UnescapeDataString(resultData);
            }
            if (!string.IsNullOrWhiteSpace(response))
            {
                Token = response;
                await SubmitAsync(false);
            }
            else
            {
                await _deviceActionService.HideLoadingAsync();
                if (authResult != null && authResult.Properties.TryGetValue("error", out var resultError))
                {
                    var message = AppResources.Fido2CheckBrowser + "\n\n" + resultError;
                    await _platformUtilsService.ShowDialogAsync(message, AppResources.AnErrorHasOccurred,
                        AppResources.Ok);
                }
                else
                {
                    await _platformUtilsService.ShowDialogAsync(AppResources.Fido2CheckBrowser,
                        AppResources.AnErrorHasOccurred, AppResources.Ok);
                }
            }
        }

        public async Task SubmitAsync(bool showLoading = true)
        {
            if (SelectedProviderType == null)
            {
                return;
            }
            if (Xamarin.Essentials.Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.None)
            {
                await _platformUtilsService.ShowDialogAsync(AppResources.InternetConnectionRequiredMessage,
                    AppResources.InternetConnectionRequiredTitle, AppResources.Ok);
                return;
            }
            if (string.IsNullOrWhiteSpace(Token))
            {
                await _platformUtilsService.ShowDialogAsync(
                    string.Format(AppResources.ValidationFieldRequired, AppResources.VerificationCode),
                    AppResources.AnErrorHasOccurred, AppResources.Ok);
                return;
            }
            if (SelectedProviderType == TwoFactorProviderType.Email ||
                SelectedProviderType == TwoFactorProviderType.Authenticator)
            {
                Token = Token.Replace(" ", string.Empty).Trim();
            }

            try
            {
                if (showLoading)
                {
                    await _deviceActionService.ShowLoadingAsync(AppResources.Validating);
                }
                var result = await _authService.LogInTwoFactorAsync(SelectedProviderType.Value, Token, _captchaToken, Remember);

                if (result.CaptchaNeeded)
                {
                    if (await HandleCaptchaAsync(result.CaptchaSiteKey))
                    {
                        await SubmitAsync(false);
                        _captchaToken = null;
                    }
                    return;
                }
                _captchaToken = null;

                var task = Task.Run(() => _syncService.FullSyncAsync(true));
                await _deviceActionService.HideLoadingAsync();
                var decryptOptions = await _stateService.GetAccountDecryptionOptions();
                _messagingService.Send("listenYubiKeyOTP", false);
                _broadcasterService.Unsubscribe(nameof(TwoFactorPage));

                if (decryptOptions?.TrustedDeviceOption != null)
                {
                    if (await _deviceTrustCryptoService.IsDeviceTrustedAsync())
                    {
                        // If we have a device key but no keys on server, we need to remove the device key
                        if (decryptOptions.TrustedDeviceOption.EncryptedPrivateKey == null && decryptOptions.TrustedDeviceOption.EncryptedUserKey == null)
                        {
                            await _deviceTrustCryptoService.RemoveTrustedDeviceAsync();
                            StartDeviceApprovalOptionsAction?.Invoke();
                            return;
                        }
                        // If user doesn't have a MP, but has reset password permission, they must set a MP
                        if (!decryptOptions.HasMasterPassword &&
                            decryptOptions.TrustedDeviceOption.HasManageResetPasswordPermission)
                        {
                            StartSetPasswordAction?.Invoke();
                            return;
                        }
                        // Update temp password only if the device is trusted and therefore has a decrypted User Key set
                        if (result.ForcePasswordReset)
                        {
                            UpdateTempPasswordAction?.Invoke();
                            return;
                        }

                        // Device is trusted and has keys, so we can decrypt
                        _syncService.FullSyncAsync(true).FireAndForget();
                        await TwoFactorAuthSuccessAsync();
                        return;
                    }

                    // Check for pending Admin Auth requests before navigating to device approval options
                    var pendingRequest = await _stateService.GetPendingAdminAuthRequestAsync();
                    if (pendingRequest != null)
                    {
                        var authRequest = await _authService.GetPasswordlessLoginRequestByIdAsync(pendingRequest.Id);
                        if (authRequest?.RequestApproved == true)
                        {
                            var authResult = await _authService.LogInPasswordlessAsync(true, await _stateService.GetActiveUserEmailAsync(), authRequest.RequestAccessCode, pendingRequest.Id, pendingRequest.PrivateKey, authRequest.Key, authRequest.MasterPasswordHash);
                            if (authResult == null && await _stateService.IsAuthenticatedAsync())
                            {
                                await Xamarin.Essentials.MainThread.InvokeOnMainThreadAsync(
                                 () => _platformUtilsService.ShowToast("info", null, AppResources.LoginApproved));
                                await _stateService.SetPendingAdminAuthRequestAsync(null);
                                _syncService.FullSyncAsync(true).FireAndForget();
                                await TwoFactorAuthSuccessAsync();
                            }
                        }
                        else
                        {
                            await _stateService.SetPendingAdminAuthRequestAsync(null);
                            StartDeviceApprovalOptionsAction?.Invoke();
                        }
                    }
                    else
                    {
                        StartDeviceApprovalOptionsAction?.Invoke();
                    }
                    return;
                }

                // In the standard, non TDE case, a user must set password if they don't
                // have one and they aren't using key connector.
                // Note: TDE & Key connector are mutually exclusive org config options.
                if (result.ResetMasterPassword || (decryptOptions?.RequireSetPassword ?? false))
                {
                    // TODO: We need to look into how to handle this when Org removes TDE
                    // Will we have the User Key by now to set a new password?
                    StartSetPasswordAction?.Invoke();
                    return;
                }

                _syncService.FullSyncAsync(true).FireAndForget();
                await TwoFactorAuthSuccessAsync();
            }
            catch (ApiException e)
            {
                _captchaToken = null;
                await _deviceActionService.HideLoadingAsync();
                if (e?.Error != null)
                {
                    await _platformUtilsService.ShowDialogAsync(e.Error.GetSingleMessage(),
                        AppResources.AnErrorHasOccurred, AppResources.Ok);
                }
            }
        }

        private async Task MoreAsync()
        {
            var selection = await _deviceActionService.DisplayActionSheetAsync(AppResources.Options, AppResources.Cancel, null, AppResources.UseAnotherTwoStepMethod);
            if (selection == AppResources.UseAnotherTwoStepMethod)
            {
                await AnotherMethodAsync();
            }
        }

        public async Task AnotherMethodAsync()
        {
            var supportedProviders = _authService.GetSupportedTwoFactorProviders();
            var options = supportedProviders.Select(p => p.Name).ToList();
            options.Add(AppResources.RecoveryCodeTitle);
            var method = await _deviceActionService.DisplayActionSheetAsync(AppResources.TwoStepLoginOptions,
                AppResources.Cancel, null, options.ToArray());
            if (method == AppResources.RecoveryCodeTitle)
            {
                _platformUtilsService.LaunchUri("https://bitwarden.com/es-la/help/lost-two-step-device/");
            }
            else if (method != AppResources.Cancel && method != null)
            {
                var selected = supportedProviders.FirstOrDefault(p => p.Name == method)?.Type;
                if (selected == SelectedProviderType)
                {
                    // Nothing changed
                    return;
                }
                SelectedProviderType = selected;
                Load();
            }
        }

        public async Task<bool> SendEmailAsync(bool showLoading, bool doToast)
        {
            if (!EmailMethod)
            {
                return false;
            }
            if (Xamarin.Essentials.Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.None)
            {
                await _platformUtilsService.ShowDialogAsync(AppResources.InternetConnectionRequiredMessage,
                    AppResources.InternetConnectionRequiredTitle, AppResources.Ok);
                return false;
            }
            try
            {
                if (showLoading)
                {
                    await _deviceActionService.ShowLoadingAsync(AppResources.Submitting);
                }
                var request = new TwoFactorEmailRequest
                {
                    Email = _authService.Email,
                    MasterPasswordHash = _authService.MasterPasswordHash,
                    DeviceIdentifier = await _appIdService.GetAppIdAsync(),
                    SsoEmail2FaSessionToken = _authService.SsoEmail2FaSessionToken
                };
                await _apiService.PostTwoFactorEmailAsync(request);
                if (showLoading)
                {
                    await _deviceActionService.HideLoadingAsync();
                }
                if (doToast)
                {
                    _platformUtilsService.ShowToast("success", null, AppResources.VerificationEmailSent);
                }
                return true;
            }
            catch (ApiException)
            {
                if (showLoading)
                {
                    await _deviceActionService.HideLoadingAsync();
                }
                await _platformUtilsService.ShowDialogAsync(AppResources.VerificationEmailNotSent,
                    AppResources.AnErrorHasOccurred, AppResources.Ok);
                return false;
            }
        }

        public async Task TwoFactorAuthSuccessAsync()
        {
            if (AuthingWithSso && await _vaultTimeoutService.IsLockedAsync())
            {
                LockAction?.Invoke();
            }
            else
            {
                TwoFactorAuthSuccessAction?.Invoke();
            }
        }
    }
}
