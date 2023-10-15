﻿using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Models.Business;
using Bit.Core.Models.StaticStore;

namespace Bit.Core.Services;

public interface IPaymentService
{
    Task CancelAndRecoverChargesAsync(ISubscriber subscriber);
    Task<string> PurchaseOrganizationAsync(Organization org, PaymentMethodType paymentMethodType,
        string paymentToken, List<Plan> plans, short additionalStorageGb, int additionalSeats,
        bool premiumAccessAddon, TaxInfo taxInfo, bool provider = false, int additionalSmSeats = 0,
        int additionalServiceAccount = 0);
    Task SponsorOrganizationAsync(Organization org, OrganizationSponsorship sponsorship);
    Task RemoveOrganizationSponsorshipAsync(Organization org, OrganizationSponsorship sponsorship);
    Task<string> UpgradeFreeOrganizationAsync(Organization org, List<Plan> plans, OrganizationUpgrade upgrade);
    Task<string> PurchasePremiumAsync(User user, PaymentMethodType paymentMethodType, string paymentToken,
        short additionalStorageGb, TaxInfo taxInfo);
    Task<string> AdjustSeatsAsync(Organization organization, Plan plan, int additionalSeats, DateTime? prorationDate = null);
    Task<string> AdjustStorageAsync(IStorableSubscriber storableSubscriber, int additionalStorage, string storagePlanId, DateTime? prorationDate = null);

    Task<string> AdjustServiceAccountsAsync(Organization organization, Plan plan, int additionalServiceAccounts,
        DateTime? prorationDate = null);
    Task CancelSubscriptionAsync(ISubscriber subscriber, bool endOfPeriod = false,
        bool skipInAppPurchaseCheck = false);
    Task ReinstateSubscriptionAsync(ISubscriber subscriber);
    Task<bool> UpdatePaymentMethodAsync(ISubscriber subscriber, PaymentMethodType paymentMethodType,
        string paymentToken, bool allowInAppPurchases = false, TaxInfo taxInfo = null);
    Task<bool> CreditAccountAsync(ISubscriber subscriber, decimal creditAmount);
    Task<BillingInfo> GetBillingAsync(ISubscriber subscriber);
    Task<BillingInfo> GetBillingHistoryAsync(ISubscriber subscriber);
    Task<BillingInfo> GetBillingBalanceAndSourceAsync(ISubscriber subscriber);
    Task<SubscriptionInfo> GetSubscriptionAsync(ISubscriber subscriber);
    Task<TaxInfo> GetTaxInfoAsync(ISubscriber subscriber);
    Task SaveTaxInfoAsync(ISubscriber subscriber, TaxInfo taxInfo);
    Task<TaxRate> CreateTaxRateAsync(TaxRate taxRate);
    Task UpdateTaxRateAsync(TaxRate taxRate);
    Task ArchiveTaxRateAsync(TaxRate taxRate);
    Task<string> AddSecretsManagerToSubscription(Organization org, Plan plan, int additionalSmSeats,
        int additionalServiceAccount, DateTime? prorationDate = null);
}
