﻿using Bit.Core.Auth.Enums;
using Bit.Core.Auth.Models;
using Bit.Core.Auth.Utilities.Duo;
using Bit.Core.Entities;
using Bit.Core.Settings;

namespace Bit.Core.Auth.Identity;

public interface IOrganizationDuoWebTokenProvider : IOrganizationTwoFactorTokenProvider { }

public class OrganizationDuoWebTokenProvider : IOrganizationDuoWebTokenProvider
{
    private readonly GlobalSettings _globalSettings;

    public OrganizationDuoWebTokenProvider(GlobalSettings globalSettings)
    {
        _globalSettings = globalSettings;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(Organization organization)
    {
        if (organization == null || !organization.Enabled || !organization.Use2fa)
        {
            return Task.FromResult(false);
        }

        var provider = organization.GetTwoFactorProvider(TwoFactorProviderType.OrganizationDuo);
        var canGenerate = organization.TwoFactorProviderIsEnabled(TwoFactorProviderType.OrganizationDuo)
            && HasProperMetaData(provider);
        return Task.FromResult(canGenerate);
    }

    public Task<string> GenerateAsync(Organization organization, User user)
    {
        if (organization == null || !organization.Enabled || !organization.Use2fa)
        {
            return Task.FromResult<string>(null);
        }

        var provider = organization.GetTwoFactorProvider(TwoFactorProviderType.OrganizationDuo);
        if (!HasProperMetaData(provider))
        {
            return Task.FromResult<string>(null);
        }

        var signatureRequest = DuoWeb.SignRequest(provider.MetaData["IKey"].ToString(),
            provider.MetaData["SKey"].ToString(), _globalSettings.Duo.AKey, user.Email);
        return Task.FromResult(signatureRequest);
    }

    public Task<bool> ValidateAsync(string token, Organization organization, User user)
    {
        if (organization == null || !organization.Enabled || !organization.Use2fa)
        {
            return Task.FromResult(false);
        }

        var provider = organization.GetTwoFactorProvider(TwoFactorProviderType.OrganizationDuo);
        if (!HasProperMetaData(provider))
        {
            return Task.FromResult(false);
        }

        var response = DuoWeb.VerifyResponse(provider.MetaData["IKey"].ToString(),
            provider.MetaData["SKey"].ToString(), _globalSettings.Duo.AKey, token);

        return Task.FromResult(response == user.Email);
    }

    private bool HasProperMetaData(TwoFactorProvider provider)
    {
        return provider?.MetaData != null && provider.MetaData.ContainsKey("IKey") &&
            provider.MetaData.ContainsKey("SKey") && provider.MetaData.ContainsKey("Host");
    }
}
