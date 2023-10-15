﻿using Bit.Core.AdminConsole.OrganizationAuth;
using Bit.Core.AdminConsole.OrganizationAuth.Interfaces;
using Bit.Core.Models.Business.Tokenables;
using Bit.Core.OrganizationFeatures.Groups;
using Bit.Core.OrganizationFeatures.Groups.Interfaces;
using Bit.Core.OrganizationFeatures.OrganizationApiKeys;
using Bit.Core.OrganizationFeatures.OrganizationApiKeys.Interfaces;
using Bit.Core.OrganizationFeatures.OrganizationCollections;
using Bit.Core.OrganizationFeatures.OrganizationCollections.Interfaces;
using Bit.Core.OrganizationFeatures.OrganizationConnections;
using Bit.Core.OrganizationFeatures.OrganizationConnections.Interfaces;
using Bit.Core.OrganizationFeatures.OrganizationDomains;
using Bit.Core.OrganizationFeatures.OrganizationDomains.Interfaces;
using Bit.Core.OrganizationFeatures.OrganizationLicenses;
using Bit.Core.OrganizationFeatures.OrganizationLicenses.Interfaces;
using Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise;
using Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.Cloud;
using Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.Interfaces;
using Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.SelfHosted;
using Bit.Core.OrganizationFeatures.OrganizationSubscriptions;
using Bit.Core.OrganizationFeatures.OrganizationSubscriptions.Interface;
using Bit.Core.OrganizationFeatures.OrganizationUsers;
using Bit.Core.OrganizationFeatures.OrganizationUsers.Interfaces;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Core.OrganizationFeatures;

public static class OrganizationServiceCollectionExtensions
{
    public static void AddOrganizationServices(this IServiceCollection services, IGlobalSettings globalSettings)
    {
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddTokenizers();
        services.AddOrganizationGroupCommands();
        services.AddOrganizationConnectionCommands();
        services.AddOrganizationSponsorshipCommands(globalSettings);
        services.AddOrganizationApiKeyCommandsQueries();
        services.AddOrganizationCollectionCommands();
        services.AddOrganizationGroupCommands();
        services.AddOrganizationLicenseCommandsQueries();
        services.AddOrganizationDomainCommandsQueries();
        services.AddOrganizationAuthCommands();
        services.AddOrganizationUserCommands();
        services.AddOrganizationUserCommandsQueries();
        services.AddBaseOrganizationSubscriptionCommandsQueries();
    }

    private static void AddOrganizationConnectionCommands(this IServiceCollection services)
    {
        services.AddScoped<ICreateOrganizationConnectionCommand, CreateOrganizationConnectionCommand>();
        services.AddScoped<IDeleteOrganizationConnectionCommand, DeleteOrganizationConnectionCommand>();
        services.AddScoped<IUpdateOrganizationConnectionCommand, UpdateOrganizationConnectionCommand>();
    }

    private static void AddOrganizationSponsorshipCommands(this IServiceCollection services, IGlobalSettings globalSettings)
    {
        services.AddScoped<ICreateSponsorshipCommand, CreateSponsorshipCommand>();
        services.AddScoped<IRemoveSponsorshipCommand, RemoveSponsorshipCommand>();
        services.AddScoped<ISendSponsorshipOfferCommand, SendSponsorshipOfferCommand>();
        services.AddScoped<ISetUpSponsorshipCommand, SetUpSponsorshipCommand>();
        services.AddScoped<IValidateRedemptionTokenCommand, ValidateRedemptionTokenCommand>();
        services.AddScoped<IValidateSponsorshipCommand, ValidateSponsorshipCommand>();
        services.AddScoped<IValidateBillingSyncKeyCommand, ValidateBillingSyncKeyCommand>();
        services.AddScoped<IOrganizationSponsorshipRenewCommand, OrganizationSponsorshipRenewCommand>();
        services.AddScoped<ICloudSyncSponsorshipsCommand, CloudSyncSponsorshipsCommand>();
        services.AddScoped<ISelfHostedSyncSponsorshipsCommand, SelfHostedSyncSponsorshipsCommand>();
        services.AddScoped<ISelfHostedSyncSponsorshipsCommand, SelfHostedSyncSponsorshipsCommand>();
        services.AddScoped<ICloudSyncSponsorshipsCommand, CloudSyncSponsorshipsCommand>();
        services.AddScoped<IValidateBillingSyncKeyCommand, ValidateBillingSyncKeyCommand>();
        if (globalSettings.SelfHosted)
        {
            services.AddScoped<IRevokeSponsorshipCommand, SelfHostedRevokeSponsorshipCommand>();
        }
        else
        {
            services.AddScoped<IRevokeSponsorshipCommand, CloudRevokeSponsorshipCommand>();
        }
    }

    private static void AddOrganizationUserCommands(this IServiceCollection services)
    {
        services.AddScoped<IDeleteOrganizationUserCommand, DeleteOrganizationUserCommand>();
        services.AddScoped<IUpdateOrganizationUserGroupsCommand, UpdateOrganizationUserGroupsCommand>();
    }

    private static void AddOrganizationApiKeyCommandsQueries(this IServiceCollection services)
    {
        services.AddScoped<IGetOrganizationApiKeyQuery, GetOrganizationApiKeyQuery>();
        services.AddScoped<IRotateOrganizationApiKeyCommand, RotateOrganizationApiKeyCommand>();
        services.AddScoped<ICreateOrganizationApiKeyCommand, CreateOrganizationApiKeyCommand>();
    }

    public static void AddOrganizationCollectionCommands(this IServiceCollection services)
    {
        services.AddScoped<IDeleteCollectionCommand, DeleteCollectionCommand>();
    }

    private static void AddOrganizationGroupCommands(this IServiceCollection services)
    {
        services.AddScoped<ICreateGroupCommand, CreateGroupCommand>();
        services.AddScoped<IDeleteGroupCommand, DeleteGroupCommand>();
        services.AddScoped<IUpdateGroupCommand, UpdateGroupCommand>();
    }

    private static void AddOrganizationLicenseCommandsQueries(this IServiceCollection services)
    {
        services.AddScoped<ICloudGetOrganizationLicenseQuery, CloudGetOrganizationLicenseQuery>();
        services.AddScoped<ISelfHostedGetOrganizationLicenseQuery, SelfHostedGetOrganizationLicenseQuery>();
        services.AddScoped<IUpdateOrganizationLicenseCommand, UpdateOrganizationLicenseCommand>();
    }

    private static void AddOrganizationDomainCommandsQueries(this IServiceCollection services)
    {
        services.AddScoped<ICreateOrganizationDomainCommand, CreateOrganizationDomainCommand>();
        services.AddScoped<IVerifyOrganizationDomainCommand, VerifyOrganizationDomainCommand>();
        services.AddScoped<IGetOrganizationDomainByIdQuery, GetOrganizationDomainByIdQuery>();
        services.AddScoped<IGetOrganizationDomainByOrganizationIdQuery, GetOrganizationDomainByOrganizationIdQuery>();
        services.AddScoped<IDeleteOrganizationDomainCommand, DeleteOrganizationDomainCommand>();
    }

    private static void AddOrganizationAuthCommands(this IServiceCollection services)
    {
        services.AddScoped<IUpdateOrganizationAuthRequestCommand, UpdateOrganizationAuthRequestCommand>();
    }

    private static void AddOrganizationUserCommandsQueries(this IServiceCollection services)
    {
        services.AddScoped<ICountNewSmSeatsRequiredQuery, CountNewSmSeatsRequiredQuery>();
    }

    // TODO: move to OrganizationSubscriptionServiceCollectionExtensions when OrganizationUser methods are moved out of
    // TODO: OrganizationService - see PM-1880
    private static void AddBaseOrganizationSubscriptionCommandsQueries(this IServiceCollection services)
    {
        services.AddScoped<IUpdateSecretsManagerSubscriptionCommand, UpdateSecretsManagerSubscriptionCommand>();
    }

    private static void AddTokenizers(this IServiceCollection services)
    {
        services.AddSingleton<IDataProtectorTokenFactory<OrganizationSponsorshipOfferTokenable>>(serviceProvider =>
            new DataProtectorTokenFactory<OrganizationSponsorshipOfferTokenable>(
                OrganizationSponsorshipOfferTokenable.ClearTextPrefix,
                OrganizationSponsorshipOfferTokenable.DataProtectorPurpose,
                serviceProvider.GetDataProtectionProvider(),
                serviceProvider.GetRequiredService<ILogger<DataProtectorTokenFactory<OrganizationSponsorshipOfferTokenable>>>())
        );
    }
}

