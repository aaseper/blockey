﻿using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Business;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Braintree;
using NSubstitute;
using Xunit;
using Customer = Braintree.Customer;
using PaymentMethod = Braintree.PaymentMethod;
using PaymentMethodType = Bit.Core.Enums.PaymentMethodType;
using TaxRate = Bit.Core.Entities.TaxRate;

namespace Bit.Core.Test.Services;

[SutProviderCustomize]
public class StripePaymentServiceTests
{
    [Theory]
    [BitAutoData(PaymentMethodType.BitPay)]
    [BitAutoData(PaymentMethodType.BitPay)]
    [BitAutoData(PaymentMethodType.Credit)]
    [BitAutoData(PaymentMethodType.WireTransfer)]
    [BitAutoData(PaymentMethodType.AppleInApp)]
    [BitAutoData(PaymentMethodType.GoogleInApp)]
    [BitAutoData(PaymentMethodType.Check)]
    public async void PurchaseOrganizationAsync_Invalid(PaymentMethodType paymentMethodType, SutProvider<StripePaymentService> sutProvider)
    {
        var exception = await Assert.ThrowsAsync<GatewayException>(
            () => sutProvider.Sut.PurchaseOrganizationAsync(null, paymentMethodType, null, null, 0, 0, false, null, false, -1, -1));

        Assert.Equal("Payment method is not supported at this time.", exception.Message);
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_Stripe_ProviderOrg_Coupon_Add(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo, bool provider = true)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
        });
        sutProvider.GetDependency<IGlobalSettings>()
            .BaseServiceUri.CloudRegion
            .Returns("US");

        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plans, 0, 0, false, taxInfo, provider);

        Assert.Null(result);
        Assert.Equal(GatewayType.Stripe, organization.Gateway);
        Assert.Equal("C-1", organization.GatewayCustomerId);
        Assert.Equal("S-1", organization.GatewaySubscriptionId);
        Assert.True(organization.Enabled);
        Assert.Equal(DateTime.Today.AddDays(10), organization.ExpirationDate);

        await stripeAdapter.Received().CustomerCreateAsync(Arg.Is<Stripe.CustomerCreateOptions>(c =>
            c.Description == organization.BusinessName &&
            c.Email == organization.BillingEmail &&
            c.Source == paymentToken &&
            c.PaymentMethod == null &&
            c.Coupon == "msp-discount-35" &&
            c.Metadata.Count == 1 &&
            c.Metadata["region"] == "US" &&
            c.InvoiceSettings.DefaultPaymentMethod == null &&
            c.Address.Country == taxInfo.BillingAddressCountry &&
            c.Address.PostalCode == taxInfo.BillingAddressPostalCode &&
            c.Address.Line1 == taxInfo.BillingAddressLine1 &&
            c.Address.Line2 == taxInfo.BillingAddressLine2 &&
            c.Address.City == taxInfo.BillingAddressCity &&
            c.Address.State == taxInfo.BillingAddressState &&
            c.TaxIdData == null
        ));

        await stripeAdapter.Received().SubscriptionCreateAsync(Arg.Is<Stripe.SubscriptionCreateOptions>(s =>
            s.Customer == "C-1" &&
            s.Expand[0] == "latest_invoice.payment_intent" &&
            s.Metadata[organization.GatewayIdField()] == organization.Id.ToString() &&
            s.Items.Count == 0
        ));
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_SM_Stripe_ProviderOrg_Coupon_Add(SutProvider<StripePaymentService> sutProvider, Organization organization,
        string paymentToken, TaxInfo taxInfo, bool provider = true)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),

        });
        sutProvider.GetDependency<IGlobalSettings>()
            .BaseServiceUri.CloudRegion
            .Returns("US");

        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plans, 1, 1,
            false, taxInfo, provider, 1, 1);

        Assert.Null(result);
        Assert.Equal(GatewayType.Stripe, organization.Gateway);
        Assert.Equal("C-1", organization.GatewayCustomerId);
        Assert.Equal("S-1", organization.GatewaySubscriptionId);
        Assert.True(organization.Enabled);
        Assert.Equal(DateTime.Today.AddDays(10), organization.ExpirationDate);

        await stripeAdapter.Received().CustomerCreateAsync(Arg.Is<Stripe.CustomerCreateOptions>(c =>
            c.Description == organization.BusinessName &&
            c.Email == organization.BillingEmail &&
            c.Source == paymentToken &&
            c.PaymentMethod == null &&
            c.Coupon == "msp-discount-35" &&
            c.Metadata.Count == 1 &&
            c.Metadata["region"] == "US" &&
            c.InvoiceSettings.DefaultPaymentMethod == null &&
            c.Address.Country == taxInfo.BillingAddressCountry &&
            c.Address.PostalCode == taxInfo.BillingAddressPostalCode &&
            c.Address.Line1 == taxInfo.BillingAddressLine1 &&
            c.Address.Line2 == taxInfo.BillingAddressLine2 &&
            c.Address.City == taxInfo.BillingAddressCity &&
            c.Address.State == taxInfo.BillingAddressState &&
            c.TaxIdData == null
        ));

        await stripeAdapter.Received().SubscriptionCreateAsync(Arg.Is<Stripe.SubscriptionCreateOptions>(s =>
            s.Customer == "C-1" &&
            s.Expand[0] == "latest_invoice.payment_intent" &&
            s.Metadata[organization.GatewayIdField()] == organization.Id.ToString() &&
            s.Items.Count == 4
        ));
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_Stripe(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
        });
        sutProvider.GetDependency<IGlobalSettings>()
            .BaseServiceUri.CloudRegion
            .Returns("US");

        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plans, 0, 0
            , false, taxInfo, false, 8, 10);

        Assert.Null(result);
        Assert.Equal(GatewayType.Stripe, organization.Gateway);
        Assert.Equal("C-1", organization.GatewayCustomerId);
        Assert.Equal("S-1", organization.GatewaySubscriptionId);
        Assert.True(organization.Enabled);
        Assert.Equal(DateTime.Today.AddDays(10), organization.ExpirationDate);
        await stripeAdapter.Received().CustomerCreateAsync(Arg.Is<Stripe.CustomerCreateOptions>(c =>
            c.Description == organization.BusinessName &&
            c.Email == organization.BillingEmail &&
            c.Source == paymentToken &&
            c.PaymentMethod == null &&
            c.Metadata.Count == 1 &&
            c.Metadata["region"] == "US" &&
            c.InvoiceSettings.DefaultPaymentMethod == null &&
            c.InvoiceSettings.CustomFields != null &&
            c.InvoiceSettings.CustomFields[0].Name == "Organization" &&
            c.InvoiceSettings.CustomFields[0].Value == organization.SubscriberName().Substring(0, 30) &&
            c.Address.Country == taxInfo.BillingAddressCountry &&
            c.Address.PostalCode == taxInfo.BillingAddressPostalCode &&
            c.Address.Line1 == taxInfo.BillingAddressLine1 &&
            c.Address.Line2 == taxInfo.BillingAddressLine2 &&
            c.Address.City == taxInfo.BillingAddressCity &&
            c.Address.State == taxInfo.BillingAddressState &&
            c.TaxIdData == null
        ));

        await stripeAdapter.Received().SubscriptionCreateAsync(Arg.Is<Stripe.SubscriptionCreateOptions>(s =>
            s.Customer == "C-1" &&
            s.Expand[0] == "latest_invoice.payment_intent" &&
            s.Metadata[organization.GatewayIdField()] == organization.Id.ToString() &&
            s.Items.Count == 2
        ));
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_Stripe_PM(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.PasswordManagerPlans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();
        paymentToken = "pm_" + paymentToken;

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
        });
        sutProvider.GetDependency<IGlobalSettings>()
            .BaseServiceUri.CloudRegion
            .Returns("US");

        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plans, 0, 0, false, taxInfo);

        Assert.Null(result);
        Assert.Equal(GatewayType.Stripe, organization.Gateway);
        Assert.Equal("C-1", organization.GatewayCustomerId);
        Assert.Equal("S-1", organization.GatewaySubscriptionId);
        Assert.True(organization.Enabled);
        Assert.Equal(DateTime.Today.AddDays(10), organization.ExpirationDate);

        await stripeAdapter.Received().CustomerCreateAsync(Arg.Is<Stripe.CustomerCreateOptions>(c =>
            c.Description == organization.BusinessName &&
            c.Email == organization.BillingEmail &&
            c.Source == null &&
            c.PaymentMethod == paymentToken &&
            c.Metadata.Count == 1 &&
            c.Metadata["region"] == "US" &&
            c.InvoiceSettings.DefaultPaymentMethod == paymentToken &&
            c.InvoiceSettings.CustomFields != null &&
            c.InvoiceSettings.CustomFields[0].Name == "Organization" &&
            c.InvoiceSettings.CustomFields[0].Value == organization.SubscriberName().Substring(0, 30) &&
            c.Address.Country == taxInfo.BillingAddressCountry &&
            c.Address.PostalCode == taxInfo.BillingAddressPostalCode &&
            c.Address.Line1 == taxInfo.BillingAddressLine1 &&
            c.Address.Line2 == taxInfo.BillingAddressLine2 &&
            c.Address.City == taxInfo.BillingAddressCity &&
            c.Address.State == taxInfo.BillingAddressState &&
            c.TaxIdData == null
        ));

        await stripeAdapter.Received().SubscriptionCreateAsync(Arg.Is<Stripe.SubscriptionCreateOptions>(s =>
            s.Customer == "C-1" &&
            s.Expand[0] == "latest_invoice.payment_intent" &&
            s.Metadata[organization.GatewayIdField()] == organization.Id.ToString() &&
            s.Items.Count == 0
        ));
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_Stripe_TaxRate(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
        });
        sutProvider.GetDependency<ITaxRateRepository>().GetByLocationAsync(Arg.Is<TaxRate>(t =>
                t.Country == taxInfo.BillingAddressCountry && t.PostalCode == taxInfo.BillingAddressPostalCode))
            .Returns(new List<TaxRate> { new() { Id = "T-1" } });

        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plans, 0, 0, false, taxInfo);

        Assert.Null(result);

        await stripeAdapter.Received().SubscriptionCreateAsync(Arg.Is<Stripe.SubscriptionCreateOptions>(s =>
            s.DefaultTaxRates.Count == 1 &&
            s.DefaultTaxRates[0] == "T-1"
        ));
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_Stripe_TaxRate_SM(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
        });
        sutProvider.GetDependency<ITaxRateRepository>().GetByLocationAsync(Arg.Is<TaxRate>(t =>
                t.Country == taxInfo.BillingAddressCountry && t.PostalCode == taxInfo.BillingAddressPostalCode))
            .Returns(new List<TaxRate> { new() { Id = "T-1" } });

        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plans, 2, 2,
            false, taxInfo, false, 2, 2);

        Assert.Null(result);

        await stripeAdapter.Received().SubscriptionCreateAsync(Arg.Is<Stripe.SubscriptionCreateOptions>(s =>
            s.DefaultTaxRates.Count == 1 &&
            s.DefaultTaxRates[0] == "T-1"
        ));
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_Stripe_Declined(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plan = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();
        paymentToken = "pm_" + paymentToken;

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
            Status = "incomplete",
            LatestInvoice = new Stripe.Invoice
            {
                PaymentIntent = new Stripe.PaymentIntent
                {
                    Status = "requires_payment_method",
                },
            },
        });

        var exception = await Assert.ThrowsAsync<GatewayException>(
            () => sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plan, 0, 0, false, taxInfo));

        Assert.Equal("Payment method was declined.", exception.Message);

        await stripeAdapter.Received(1).CustomerDeleteAsync("C-1");
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_SM_Stripe_Declined(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plan = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();
        paymentToken = "pm_" + paymentToken;

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
            Status = "incomplete",
            LatestInvoice = new Stripe.Invoice
            {
                PaymentIntent = new Stripe.PaymentIntent
                {
                    Status = "requires_payment_method",
                },
            },
        });

        var exception = await Assert.ThrowsAsync<GatewayException>(
            () => sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plan,
                1, 12, false, taxInfo, false, 10, 10));

        Assert.Equal("Payment method was declined.", exception.Message);

        await stripeAdapter.Received(1).CustomerDeleteAsync("C-1");
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_Stripe_RequiresAction(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
            Status = "incomplete",
            LatestInvoice = new Stripe.Invoice
            {
                PaymentIntent = new Stripe.PaymentIntent
                {
                    Status = "requires_action",
                    ClientSecret = "clientSecret",
                },
            },
        });

        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plans, 0, 0, false, taxInfo);

        Assert.Equal("clientSecret", result);
        Assert.False(organization.Enabled);
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_SM_Stripe_RequiresAction(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
            Status = "incomplete",
            LatestInvoice = new Stripe.Invoice
            {
                PaymentIntent = new Stripe.PaymentIntent
                {
                    Status = "requires_action",
                    ClientSecret = "clientSecret",
                },
            },
        });

        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.Card, paymentToken, plans,
            10, 10, false, taxInfo, false, 10, 10);

        Assert.Equal("clientSecret", result);
        Assert.False(organization.Enabled);
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_Paypal(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
        });

        sutProvider.GetDependency<IGlobalSettings>()
            .BaseServiceUri.CloudRegion
            .Returns("US");

        var customer = Substitute.For<Customer>();
        customer.Id.ReturnsForAnyArgs("Braintree-Id");
        customer.PaymentMethods.ReturnsForAnyArgs(new[] { Substitute.For<PaymentMethod>() });
        var customerResult = Substitute.For<Result<Customer>>();
        customerResult.IsSuccess().Returns(true);
        customerResult.Target.ReturnsForAnyArgs(customer);

        var braintreeGateway = sutProvider.GetDependency<IBraintreeGateway>();
        braintreeGateway.Customer.CreateAsync(default).ReturnsForAnyArgs(customerResult);

        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.PayPal, paymentToken, plans, 0, 0, false, taxInfo);

        Assert.Null(result);
        Assert.Equal(GatewayType.Stripe, organization.Gateway);
        Assert.Equal("C-1", organization.GatewayCustomerId);
        Assert.Equal("S-1", organization.GatewaySubscriptionId);
        Assert.True(organization.Enabled);
        Assert.Equal(DateTime.Today.AddDays(10), organization.ExpirationDate);

        await stripeAdapter.Received().CustomerCreateAsync(Arg.Is<Stripe.CustomerCreateOptions>(c =>
            c.Description == organization.BusinessName &&
            c.Email == organization.BillingEmail &&
            c.PaymentMethod == null &&
            c.Metadata.Count == 2 &&
            c.Metadata["btCustomerId"] == "Braintree-Id" &&
            c.Metadata["region"] == "US" &&
            c.InvoiceSettings.DefaultPaymentMethod == null &&
            c.Address.Country == taxInfo.BillingAddressCountry &&
            c.Address.PostalCode == taxInfo.BillingAddressPostalCode &&
            c.Address.Line1 == taxInfo.BillingAddressLine1 &&
            c.Address.Line2 == taxInfo.BillingAddressLine2 &&
            c.Address.City == taxInfo.BillingAddressCity &&
            c.Address.State == taxInfo.BillingAddressState &&
            c.TaxIdData == null
        ));

        await stripeAdapter.Received().SubscriptionCreateAsync(Arg.Is<Stripe.SubscriptionCreateOptions>(s =>
            s.Customer == "C-1" &&
            s.Expand[0] == "latest_invoice.payment_intent" &&
            s.Metadata[organization.GatewayIdField()] == organization.Id.ToString() &&
            s.Items.Count == 0
        ));
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_SM_Paypal(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();
        var passwordManagerPlan = plans.Single(p => p.BitwardenProduct == BitwardenProductType.PasswordManager);
        var secretsManagerPlan = plans.Single(p => p.BitwardenProduct == BitwardenProductType.SecretsManager);

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
        });

        var customer = Substitute.For<Customer>();
        customer.Id.ReturnsForAnyArgs("Braintree-Id");
        customer.PaymentMethods.ReturnsForAnyArgs(new[] { Substitute.For<PaymentMethod>() });
        var customerResult = Substitute.For<Result<Customer>>();
        customerResult.IsSuccess().Returns(true);
        customerResult.Target.ReturnsForAnyArgs(customer);

        var braintreeGateway = sutProvider.GetDependency<IBraintreeGateway>();
        braintreeGateway.Customer.CreateAsync(default).ReturnsForAnyArgs(customerResult);

        sutProvider.GetDependency<IGlobalSettings>()
            .BaseServiceUri.CloudRegion
            .Returns("US");

        var additionalStorage = (short)2;
        var additionalSeats = 10;
        var additionalSmSeats = 5;
        var additionalServiceAccounts = 20;
        var result = await sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.PayPal, paymentToken, plans,
            additionalStorage, additionalSeats, false, taxInfo, false, additionalSmSeats, additionalServiceAccounts);

        Assert.Null(result);
        Assert.Equal(GatewayType.Stripe, organization.Gateway);
        Assert.Equal("C-1", organization.GatewayCustomerId);
        Assert.Equal("S-1", organization.GatewaySubscriptionId);
        Assert.True(organization.Enabled);
        Assert.Equal(DateTime.Today.AddDays(10), organization.ExpirationDate);

        await stripeAdapter.Received().CustomerCreateAsync(Arg.Is<Stripe.CustomerCreateOptions>(c =>
            c.Description == organization.BusinessName &&
            c.Email == organization.BillingEmail &&
            c.PaymentMethod == null &&
            c.Metadata.Count == 2 &&
            c.Metadata["region"] == "US" &&
            c.Metadata["btCustomerId"] == "Braintree-Id" &&
            c.InvoiceSettings.DefaultPaymentMethod == null &&
            c.Address.Country == taxInfo.BillingAddressCountry &&
            c.Address.PostalCode == taxInfo.BillingAddressPostalCode &&
            c.Address.Line1 == taxInfo.BillingAddressLine1 &&
            c.Address.Line2 == taxInfo.BillingAddressLine2 &&
            c.Address.City == taxInfo.BillingAddressCity &&
            c.Address.State == taxInfo.BillingAddressState &&
            c.TaxIdData == null
        ));

        await stripeAdapter.Received().SubscriptionCreateAsync(Arg.Is<Stripe.SubscriptionCreateOptions>(s =>
            s.Customer == "C-1" &&
            s.Expand[0] == "latest_invoice.payment_intent" &&
            s.Metadata[organization.GatewayIdField()] == organization.Id.ToString() &&
            s.Items.Count == 4 &&
            s.Items.Count(i => i.Plan == passwordManagerPlan.StripeSeatPlanId && i.Quantity == additionalSeats) == 1 &&
            s.Items.Count(i => i.Plan == passwordManagerPlan.StripeStoragePlanId && i.Quantity == additionalStorage) == 1 &&
            s.Items.Count(i => i.Plan == secretsManagerPlan.StripeSeatPlanId && i.Quantity == additionalSmSeats) == 1 &&
            s.Items.Count(i => i.Plan == secretsManagerPlan.StripeServiceAccountPlanId && i.Quantity == additionalServiceAccounts) == 1
        ));
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_Paypal_FailedCreate(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var customerResult = Substitute.For<Result<Customer>>();
        customerResult.IsSuccess().Returns(false);

        var braintreeGateway = sutProvider.GetDependency<IBraintreeGateway>();
        braintreeGateway.Customer.CreateAsync(default).ReturnsForAnyArgs(customerResult);

        var exception = await Assert.ThrowsAsync<GatewayException>(
            () => sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.PayPal, paymentToken, plans, 0, 0, false, taxInfo));

        Assert.Equal("Failed to create PayPal customer record.", exception.Message);
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_SM_Paypal_FailedCreate(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var customerResult = Substitute.For<Result<Customer>>();
        customerResult.IsSuccess().Returns(false);

        var braintreeGateway = sutProvider.GetDependency<IBraintreeGateway>();
        braintreeGateway.Customer.CreateAsync(default).ReturnsForAnyArgs(customerResult);

        var exception = await Assert.ThrowsAsync<GatewayException>(
            () => sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.PayPal, paymentToken, plans,
                1, 1, false, taxInfo, false, 8, 8));

        Assert.Equal("Failed to create PayPal customer record.", exception.Message);
    }

    [Theory, BitAutoData]
    public async void PurchaseOrganizationAsync_PayPal_Declined(SutProvider<StripePaymentService> sutProvider, Organization organization, string paymentToken, TaxInfo taxInfo)
    {
        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();
        paymentToken = "pm_" + paymentToken;

        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerCreateAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription
        {
            Id = "S-1",
            CurrentPeriodEnd = DateTime.Today.AddDays(10),
            Status = "incomplete",
            LatestInvoice = new Stripe.Invoice
            {
                PaymentIntent = new Stripe.PaymentIntent
                {
                    Status = "requires_payment_method",
                },
            },
        });

        var customer = Substitute.For<Customer>();
        customer.Id.ReturnsForAnyArgs("Braintree-Id");
        customer.PaymentMethods.ReturnsForAnyArgs(new[] { Substitute.For<PaymentMethod>() });
        var customerResult = Substitute.For<Result<Customer>>();
        customerResult.IsSuccess().Returns(true);
        customerResult.Target.ReturnsForAnyArgs(customer);

        var braintreeGateway = sutProvider.GetDependency<IBraintreeGateway>();
        braintreeGateway.Customer.CreateAsync(default).ReturnsForAnyArgs(customerResult);

        var exception = await Assert.ThrowsAsync<GatewayException>(
            () => sutProvider.Sut.PurchaseOrganizationAsync(organization, PaymentMethodType.PayPal, paymentToken, plans, 0, 0, false, taxInfo));

        Assert.Equal("Payment method was declined.", exception.Message);

        await stripeAdapter.Received(1).CustomerDeleteAsync("C-1");
        await braintreeGateway.Customer.Received(1).DeleteAsync("Braintree-Id");
    }

    [Theory, BitAutoData]
    public async void UpgradeFreeOrganizationAsync_Success(SutProvider<StripePaymentService> sutProvider,
        Organization organization, TaxInfo taxInfo)
    {
        organization.GatewaySubscriptionId = null;
        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerGetAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
            Metadata = new Dictionary<string, string>
            {
                { "btCustomerId", "B-123" },
            }
        });
        stripeAdapter.InvoiceUpcomingAsync(default).ReturnsForAnyArgs(new Stripe.Invoice
        {
            PaymentIntent = new Stripe.PaymentIntent { Status = "requires_payment_method", },
            AmountDue = 0
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription { });

        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();

        var upgrade = new OrganizationUpgrade()
        {
            AdditionalStorageGb = 0,
            AdditionalSeats = 0,
            PremiumAccessAddon = false,
            TaxInfo = taxInfo,
            AdditionalSmSeats = 0,
            AdditionalServiceAccounts = 0
        };
        var result = await sutProvider.Sut.UpgradeFreeOrganizationAsync(organization, plans, upgrade);

        Assert.Null(result);
    }

    [Theory, BitAutoData]
    public async void UpgradeFreeOrganizationAsync_SM_Success(SutProvider<StripePaymentService> sutProvider,
        Organization organization, TaxInfo taxInfo)
    {
        organization.GatewaySubscriptionId = null;
        var stripeAdapter = sutProvider.GetDependency<IStripeAdapter>();
        stripeAdapter.CustomerGetAsync(default).ReturnsForAnyArgs(new Stripe.Customer
        {
            Id = "C-1",
            Metadata = new Dictionary<string, string>
            {
                { "btCustomerId", "B-123" },
            }
        });
        stripeAdapter.InvoiceUpcomingAsync(default).ReturnsForAnyArgs(new Stripe.Invoice
        {
            PaymentIntent = new Stripe.PaymentIntent { Status = "requires_payment_method", },
            AmountDue = 0
        });
        stripeAdapter.SubscriptionCreateAsync(default).ReturnsForAnyArgs(new Stripe.Subscription { });

        var upgrade = new OrganizationUpgrade()
        {
            AdditionalStorageGb = 1,
            AdditionalSeats = 10,
            PremiumAccessAddon = false,
            TaxInfo = taxInfo,
            AdditionalSmSeats = 5,
            AdditionalServiceAccounts = 50
        };

        var plans = StaticStore.Plans.Where(p => p.Type == PlanType.EnterpriseAnnually).ToList();
        var result = await sutProvider.Sut.UpgradeFreeOrganizationAsync(organization, plans, upgrade);

        Assert.Null(result);
    }
}
