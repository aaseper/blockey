﻿using System.ComponentModel.DataAnnotations;
using Bit.Core.Entities;
using Bit.Core.Entities.Provider;
using Bit.Core.Enums;
using Bit.Core.Enums.Provider;
using Bit.Core.Models.Business;
using Bit.Core.Models.Data.Organizations.OrganizationUsers;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Bit.Core.Vault.Entities;
using Bit.SharedWeb.Utilities;

namespace Bit.Admin.Models;

public class OrganizationEditModel : OrganizationViewModel
{
    public OrganizationEditModel() { }

    public OrganizationEditModel(Provider provider)
    {
        Provider = provider;
        BillingEmail = provider.Type == ProviderType.Reseller ? provider.BillingEmail : string.Empty;
        PlanType = Core.Enums.PlanType.TeamsMonthly;
        Plan = Core.Enums.PlanType.TeamsMonthly.GetDisplayAttribute()?.GetName();
        LicenseKey = RandomLicenseKey;
    }

    public OrganizationEditModel(Organization org, Provider provider, IEnumerable<OrganizationUserUserDetails> orgUsers,
        IEnumerable<Cipher> ciphers, IEnumerable<Collection> collections, IEnumerable<Group> groups,
        IEnumerable<Policy> policies, BillingInfo billingInfo, IEnumerable<OrganizationConnection> connections,
        GlobalSettings globalSettings, int secrets, int projects, int serviceAccounts, int occupiedSmSeats)
        : base(org, provider, connections, orgUsers, ciphers, collections, groups, policies, secrets, projects,
            serviceAccounts, occupiedSmSeats)
    {
        BillingInfo = billingInfo;
        BraintreeMerchantId = globalSettings.Braintree.MerchantId;

        Name = org.Name;
        BusinessName = org.BusinessName;
        BillingEmail = provider?.Type == ProviderType.Reseller ? provider.BillingEmail : org.BillingEmail;
        PlanType = org.PlanType;
        Plan = org.Plan;
        Seats = org.Seats;
        MaxAutoscaleSeats = org.MaxAutoscaleSeats;
        MaxCollections = org.MaxCollections;
        UsePolicies = org.UsePolicies;
        UseSso = org.UseSso;
        UseKeyConnector = org.UseKeyConnector;
        UseScim = org.UseScim;
        UseGroups = org.UseGroups;
        UseDirectory = org.UseDirectory;
        UseEvents = org.UseEvents;
        UseTotp = org.UseTotp;
        Use2fa = org.Use2fa;
        UseApi = org.UseApi;
        UseSecretsManager = org.UseSecretsManager;
        UseResetPassword = org.UseResetPassword;
        SelfHost = org.SelfHost;
        UsersGetPremium = org.UsersGetPremium;
        UseCustomPermissions = org.UseCustomPermissions;
        MaxStorageGb = org.MaxStorageGb;
        Gateway = org.Gateway;
        GatewayCustomerId = org.GatewayCustomerId;
        GatewaySubscriptionId = org.GatewaySubscriptionId;
        Enabled = org.Enabled;
        LicenseKey = org.LicenseKey;
        ExpirationDate = org.ExpirationDate;
        SmSeats = org.SmSeats;
        MaxAutoscaleSmSeats = org.MaxAutoscaleSmSeats;
        SmServiceAccounts = org.SmServiceAccounts;
        MaxAutoscaleSmServiceAccounts = org.MaxAutoscaleSmServiceAccounts;
        SecretsManagerBeta = org.SecretsManagerBeta;
    }

    public BillingInfo BillingInfo { get; set; }
    public string RandomLicenseKey => CoreHelpers.SecureRandomString(20);
    public string FourteenDayExpirationDate => DateTime.Now.AddDays(14).ToString("yyyy-MM-ddTHH:mm");
    public string BraintreeMerchantId { get; set; }

    [Required]
    [Display(Name = "Organization Name")]
    public string Name { get; set; }
    [Display(Name = "Business Name")]
    public string BusinessName { get; set; }
    [Display(Name = "Billing Email")]
    public string BillingEmail { get; set; }
    [Required]
    [Display(Name = "Plan")]
    public PlanType? PlanType { get; set; }
    [Required]
    [Display(Name = "Plan Name")]
    public string Plan { get; set; }
    [Display(Name = "Seats")]
    public int? Seats { get; set; }
    [Display(Name = "Max. Autoscale Seats")]
    public int? MaxAutoscaleSeats { get; set; }
    [Display(Name = "Max. Collections")]
    public short? MaxCollections { get; set; }
    [Display(Name = "Policies")]
    public bool UsePolicies { get; set; }
    [Display(Name = "SSO")]
    public bool UseSso { get; set; }
    [Display(Name = "Key Connector with Customer Encryption")]
    public bool UseKeyConnector { get; set; }
    [Display(Name = "Groups")]
    public bool UseGroups { get; set; }
    [Display(Name = "Directory")]
    public bool UseDirectory { get; set; }
    [Display(Name = "Events")]
    public bool UseEvents { get; set; }
    [Display(Name = "TOTP")]
    public bool UseTotp { get; set; }
    [Display(Name = "2FA")]
    public bool Use2fa { get; set; }
    [Display(Name = "API")]
    public bool UseApi { get; set; }
    [Display(Name = "Reset Password")]
    public bool UseResetPassword { get; set; }
    [Display(Name = "SCIM")]
    public bool UseScim { get; set; }
    [Display(Name = "Secrets Manager")]
    public bool UseSecretsManager { get; set; }
    [Display(Name = "Self Host")]
    public bool SelfHost { get; set; }
    [Display(Name = "Users Get Premium")]
    public bool UsersGetPremium { get; set; }
    [Display(Name = "Custom Permissions")]
    public bool UseCustomPermissions { get; set; }
    [Display(Name = "Max. Storage GB")]
    public short? MaxStorageGb { get; set; }
    [Display(Name = "Gateway")]
    public GatewayType? Gateway { get; set; }
    [Display(Name = "Gateway Customer Id")]
    public string GatewayCustomerId { get; set; }
    [Display(Name = "Gateway Subscription Id")]
    public string GatewaySubscriptionId { get; set; }
    [Display(Name = "Enabled")]
    public bool Enabled { get; set; }
    [Display(Name = "License Key")]
    public string LicenseKey { get; set; }
    [Display(Name = "Expiration Date")]
    public DateTime? ExpirationDate { get; set; }
    public bool SalesAssistedTrialStarted { get; set; }
    [Display(Name = "Seats")]
    public int? SmSeats { get; set; }
    [Display(Name = "Max Autoscale Seats")]
    public int? MaxAutoscaleSmSeats { get; set; }
    [Display(Name = "Service Accounts")]
    public int? SmServiceAccounts { get; set; }
    [Display(Name = "Max Autoscale Service Accounts")]
    public int? MaxAutoscaleSmServiceAccounts { get; set; }
    [Display(Name = "Secrets Manager Beta")]
    public bool SecretsManagerBeta { get; set; }

    /**
     * Creates a Plan[] object for use in Javascript
     * This is mapped manually below to provide some type safety in case the plan objects change
     * Add mappings for individual properties as you need them
     */
    public IEnumerable<Dictionary<string, object>> GetPlansHelper() =>
        StaticStore.SecretManagerPlans.Select(p =>
            new Dictionary<string, object>
            {
                { "type", p.Type },
                { "baseServiceAccount", p.BaseServiceAccount }
            });

    public Organization CreateOrganization(Provider provider)
    {
        BillingEmail = provider.BillingEmail;

        return ToOrganization(new Organization());
    }

    public Organization ToOrganization(Organization existingOrganization)
    {
        existingOrganization.Name = Name;
        existingOrganization.BusinessName = BusinessName;
        existingOrganization.BillingEmail = BillingEmail?.ToLowerInvariant()?.Trim();
        existingOrganization.PlanType = PlanType.Value;
        existingOrganization.Plan = Plan;
        existingOrganization.Seats = Seats;
        existingOrganization.MaxCollections = MaxCollections;
        existingOrganization.UsePolicies = UsePolicies;
        existingOrganization.UseSso = UseSso;
        existingOrganization.UseKeyConnector = UseKeyConnector;
        existingOrganization.UseScim = UseScim;
        existingOrganization.UseGroups = UseGroups;
        existingOrganization.UseDirectory = UseDirectory;
        existingOrganization.UseEvents = UseEvents;
        existingOrganization.UseTotp = UseTotp;
        existingOrganization.Use2fa = Use2fa;
        existingOrganization.UseApi = UseApi;
        existingOrganization.UseSecretsManager = UseSecretsManager;
        existingOrganization.UseResetPassword = UseResetPassword;
        existingOrganization.SelfHost = SelfHost;
        existingOrganization.UsersGetPremium = UsersGetPremium;
        existingOrganization.UseCustomPermissions = UseCustomPermissions;
        existingOrganization.MaxStorageGb = MaxStorageGb;
        existingOrganization.Gateway = Gateway;
        existingOrganization.GatewayCustomerId = GatewayCustomerId;
        existingOrganization.GatewaySubscriptionId = GatewaySubscriptionId;
        existingOrganization.Enabled = Enabled;
        existingOrganization.LicenseKey = LicenseKey;
        existingOrganization.ExpirationDate = ExpirationDate;
        existingOrganization.MaxAutoscaleSeats = MaxAutoscaleSeats;
        existingOrganization.SmSeats = SmSeats;
        existingOrganization.MaxAutoscaleSmSeats = MaxAutoscaleSmSeats;
        existingOrganization.SmServiceAccounts = SmServiceAccounts;
        existingOrganization.MaxAutoscaleSmServiceAccounts = MaxAutoscaleSmServiceAccounts;
        existingOrganization.SecretsManagerBeta = SecretsManagerBeta;
        return existingOrganization;
    }
}
