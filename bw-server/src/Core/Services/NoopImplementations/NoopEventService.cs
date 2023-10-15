﻿using Bit.Core.Entities;
using Bit.Core.Entities.Provider;
using Bit.Core.Enums;
using Bit.Core.SecretsManager.Entities;
using Bit.Core.Vault.Entities;

namespace Bit.Core.Services;

public class NoopEventService : IEventService
{
    public Task LogCipherEventAsync(Cipher cipher, EventType type, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogCipherEventsAsync(IEnumerable<Tuple<Cipher, EventType, DateTime?>> events)
    {
        return Task.FromResult(0);
    }

    public Task LogCollectionEventAsync(Collection collection, EventType type, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    Task IEventService.LogCollectionEventsAsync(IEnumerable<(Collection collection, EventType type, DateTime? date)> events)
    {
        return Task.FromResult(0);
    }

    public Task LogGroupEventsAsync(
        IEnumerable<(Group group, EventType type, EventSystemUser? systemUser, DateTime? date)> events)
    {
        return Task.FromResult(0);
    }

    public Task LogPolicyEventAsync(Policy policy, EventType type, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogGroupEventAsync(Group group, EventType type, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogGroupEventAsync(Group group, EventType type, EventSystemUser systemUser, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogOrganizationEventAsync(Organization organization, EventType type, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogProviderUserEventAsync(ProviderUser providerUser, EventType type, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogProviderUsersEventAsync(IEnumerable<(ProviderUser, EventType, DateTime?)> events)
    {
        return Task.FromResult(0);
    }

    public Task LogProviderOrganizationEventAsync(ProviderOrganization providerOrganization, EventType type,
        DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogProviderOrganizationEventsAsync(IEnumerable<(ProviderOrganization, EventType, DateTime?)> events)
    {
        return Task.FromResult(0);
    }

    public Task LogOrganizationDomainEventAsync(OrganizationDomain organizationDomain, EventType type,
            DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogOrganizationDomainEventAsync(OrganizationDomain organizationDomain, EventType type,
        EventSystemUser systemUser,
        DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogOrganizationUserEventAsync(OrganizationUser organizationUser, EventType type, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogOrganizationUserEventAsync(OrganizationUser organizationUser, EventType type,
        EventSystemUser systemUser, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogOrganizationUserEventsAsync(IEnumerable<(OrganizationUser, EventType, DateTime?)> events)
    {
        return Task.FromResult(0);
    }

    public Task LogOrganizationUserEventsAsync(IEnumerable<(OrganizationUser, EventType, EventSystemUser, DateTime?)> events)
    {
        return Task.FromResult(0);
    }

    public Task LogUserEventAsync(Guid userId, EventType type, DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogServiceAccountSecretEventAsync(Guid serviceAccountId, Secret secret, EventType type,
        DateTime? date = null)
    {
        return Task.FromResult(0);
    }

    public Task LogServiceAccountSecretsEventAsync(Guid serviceAccountId, IEnumerable<Secret> secrets, EventType type,
        DateTime? date = null)
    {
        return Task.FromResult(0);
    }
}
