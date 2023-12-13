﻿using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.App.Assist;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views.Autofill;
using Bit.App.Resources;
using Bit.Core.Abstractions;
using Bit.Core.Enums;
using Bit.Core.Models.View;
using Bit.Core.Utilities;
using Bit.Droid.Autofill;
using Plugin.CurrentActivity;

namespace Bit.Droid.Services
{
    public class AutofillHandler : IAutofillHandler
    {
        private readonly IStateService _stateService;
        private readonly IMessagingService _messagingService;
        private readonly IClipboardService _clipboardService;
        private readonly IPlatformUtilsService _platformUtilsService;
        private readonly LazyResolve<IEventService> _eventService;

        public AutofillHandler(IStateService stateService,
            IMessagingService messagingService,
            IClipboardService clipboardService,
            IPlatformUtilsService platformUtilsService,
            LazyResolve<IEventService> eventService)
        {
            _stateService = stateService;
            _messagingService = messagingService;
            _clipboardService = clipboardService;
            _platformUtilsService = platformUtilsService;
            _eventService = eventService;
        }

        public bool AutofillServiceEnabled()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return false;
            }
            try
            {
                var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
                var afm = (AutofillManager)activity.GetSystemService(
                    Java.Lang.Class.FromType(typeof(AutofillManager)));
                return afm.IsEnabled && afm.HasEnabledAutofillServices;
            }
            catch
            {
                return false;
            }
        }

        public bool SupportsAutofillService()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return false;
            }
            try
            {
                var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
                var type = Java.Lang.Class.FromType(typeof(AutofillManager));
                var manager = activity.GetSystemService(type) as AutofillManager;
                return manager.IsAutofillSupported;
            }
            catch
            {
                return false;
            }
        }

        public void Autofill(CipherView cipher)
        {
            var activity = CrossCurrentActivity.Current.Activity as Xamarin.Forms.Platform.Android.FormsAppCompatActivity;
            if (activity == null)
            {
                return;
            }
            if (activity.Intent?.GetBooleanExtra(AutofillConstants.AutofillFramework, false) ?? false)
            {
                if (cipher == null)
                {
                    activity.SetResult(Result.Canceled);
                    activity.Finish();
                    return;
                }
                var structure = activity.Intent.GetParcelableExtra(
                    AutofillManager.ExtraAssistStructure) as AssistStructure;
                if (structure == null)
                {
                    activity.SetResult(Result.Canceled);
                    activity.Finish();
                    return;
                }
                var parser = new Parser(structure, activity.ApplicationContext);
                parser.Parse();
                if ((!parser.FieldCollection?.Fields?.Any() ?? true) || string.IsNullOrWhiteSpace(parser.Uri))
                {
                    activity.SetResult(Result.Canceled);
                    activity.Finish();
                    return;
                }
                var task = CopyTotpAsync(cipher);
                var dataset = AutofillHelpers.BuildDataset(activity, parser.FieldCollection, new FilledItem(cipher), false);
                var replyIntent = new Intent();
                replyIntent.PutExtra(AutofillManager.ExtraAuthenticationResult, dataset);
                activity.SetResult(Result.Ok, replyIntent);
                activity.Finish();
                var eventTask = _eventService.Value.CollectAsync(EventType.Cipher_ClientAutofilled, cipher.Id);
            }
            else
            {
                var data = new Intent();
                if (cipher?.Login == null)
                {
                    data.PutExtra("canceled", "true");
                }
                else
                {
                    var task = CopyTotpAsync(cipher);
                    data.PutExtra("uri", cipher.Login.Uri);
                    data.PutExtra("username", cipher.Login.Username);
                    data.PutExtra("password", cipher.Login.Password);
                }
                if (activity.Parent == null)
                {
                    activity.SetResult(Result.Ok, data);
                }
                else
                {
                    activity.Parent.SetResult(Result.Ok, data);
                }
                activity.Finish();
                _messagingService.Send("finishMainActivity");
                if (cipher != null)
                {
                    var eventTask = _eventService.Value.CollectAsync(EventType.Cipher_ClientAutofilled, cipher.Id);
                }
            }
        }

        public void CloseAutofill()
        {
            Autofill(null);
        }

        public bool AutofillAccessibilityServiceRunning()
        {
            var enabledServices = Settings.Secure.GetString(Application.Context.ContentResolver,
                Settings.Secure.EnabledAccessibilityServices);
            return Application.Context.PackageName != null &&
                   (enabledServices?.Contains(Application.Context.PackageName) ?? false);
        }

        public bool AutofillAccessibilityOverlayPermitted()
        {
            return Accessibility.AccessibilityHelpers.OverlayPermitted();
        }

        

        public void DisableAutofillService()
        {
            try
            {
                var activity = (MainActivity)CrossCurrentActivity.Current.Activity;
                var type = Java.Lang.Class.FromType(typeof(AutofillManager));
                var manager = activity.GetSystemService(type) as AutofillManager;
                manager.DisableAutofillServices();
            }
            catch { }
        }

        public bool AutofillServicesEnabled()
        {
            if (Build.VERSION.SdkInt <= BuildVersionCodes.M)
            {
                // Android 5-6: Both accessibility & overlay are required or nothing happens
                return AutofillAccessibilityServiceRunning() && AutofillAccessibilityOverlayPermitted();
            }
            if (Build.VERSION.SdkInt == BuildVersionCodes.N)
            {
                // Android 7: Only accessibility is required (overlay is optional when using quick-action tile)
                return AutofillAccessibilityServiceRunning();
            }
            // Android 8+: Either autofill or accessibility is required
            return AutofillServiceEnabled() || AutofillAccessibilityServiceRunning();
        }

        private async Task CopyTotpAsync(CipherView cipher)
        {
            if (!string.IsNullOrWhiteSpace(cipher?.Login?.Totp))
            {
                var autoCopyDisabled = await _stateService.GetDisableAutoTotpCopyAsync();
                var canAccessPremium = await _stateService.CanAccessPremiumAsync();
                if ((canAccessPremium || cipher.OrganizationUseTotp) && !autoCopyDisabled.GetValueOrDefault())
                {
                    var totpService = ServiceContainer.Resolve<ITotpService>("totpService");
                    var totp = await totpService.GetCodeAsync(cipher.Login.Totp);
                    if (totp != null)
                    {
                        await _clipboardService.CopyTextAsync(totp);
                        _platformUtilsService.ShowToastForCopiedValue(AppResources.VerificationCodeTotp);
                    }
                }
            }
        }
    }
}
