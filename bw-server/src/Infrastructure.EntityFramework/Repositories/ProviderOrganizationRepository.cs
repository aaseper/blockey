﻿using AutoMapper;
using Bit.Core.Entities.Provider;
using Bit.Core.Models.Data;
using Bit.Core.Repositories;
using Bit.Infrastructure.EntityFramework.Repositories.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Infrastructure.EntityFramework.Repositories;

public class ProviderOrganizationRepository :
    Repository<ProviderOrganization, Models.ProviderOrganization, Guid>, IProviderOrganizationRepository
{
    public ProviderOrganizationRepository(IServiceScopeFactory serviceScopeFactory, IMapper mapper)
        : base(serviceScopeFactory, mapper, context => context.ProviderOrganizations)
    { }

    public async Task<ICollection<ProviderOrganization>> CreateManyAsync(IEnumerable<ProviderOrganization> providerOrganizations)
    {
        var entities = providerOrganizations.ToList();

        if (!entities.Any())
        {
            return default;
        }

        foreach (var providerOrganization in entities)
        {
            providerOrganization.SetNewId();
        }

        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            await dbContext.AddRangeAsync(entities);
            await dbContext.SaveChangesAsync();
        }

        return entities;
    }

    public async Task<ICollection<ProviderOrganizationOrganizationDetails>> GetManyDetailsByProviderAsync(Guid providerId)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var query = new ProviderOrganizationOrganizationDetailsReadByProviderIdQuery(providerId);
            var data = await query.Run(dbContext).ToListAsync();
            return data;
        }
    }

    public async Task<ProviderOrganization> GetByOrganizationId(Guid organizationId)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var dbContext = GetDatabaseContext(scope);
        return await GetDbSet(dbContext).Where(po => po.OrganizationId == organizationId).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ProviderOrganizationProviderDetails>> GetManyByUserAsync(Guid userId)
    {
        using (var scope = ServiceScopeFactory.CreateScope())
        {
            var dbContext = GetDatabaseContext(scope);
            var query = new ProviderOrganizationReadByUserIdQuery(userId);
            var data = await query.Run(dbContext).ToListAsync();
            return data;
        }
    }

    public async Task<int> GetCountByOrganizationIdsAsync(IEnumerable<Guid> organizationIds)
    {
        var query = new ProviderOrganizationCountByOrganizationIdsQuery(organizationIds);
        return await GetCountFromQuery(query);
    }
}
