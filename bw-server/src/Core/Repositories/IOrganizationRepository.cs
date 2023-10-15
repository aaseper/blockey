﻿using Bit.Core.Entities;
using Bit.Core.Models.Data.Organizations;

namespace Bit.Core.Repositories;

public interface IOrganizationRepository : IRepository<Organization, Guid>
{
    Task<Organization> GetByIdentifierAsync(string identifier);
    Task<ICollection<Organization>> GetManyByEnabledAsync();
    Task<ICollection<Organization>> GetManyByUserIdAsync(Guid userId);
    Task<ICollection<Organization>> SearchAsync(string name, string userEmail, bool? paid, int skip, int take);
    Task UpdateStorageAsync(Guid id);
    Task<ICollection<OrganizationAbility>> GetManyAbilitiesAsync();
    Task<Organization> GetByLicenseKeyAsync(string licenseKey);
    Task<SelfHostedOrganizationDetails> GetSelfHostedOrganizationDetailsById(Guid id);
    Task<ICollection<Organization>> SearchUnassignedToProviderAsync(string name, string ownerEmail, int skip, int take);
}
