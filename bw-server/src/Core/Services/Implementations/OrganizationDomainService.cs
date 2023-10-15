﻿using Bit.Core.Enums;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Microsoft.Extensions.Logging;

namespace Bit.Core.Services;

public class OrganizationDomainService : IOrganizationDomainService
{
    private readonly IOrganizationDomainRepository _domainRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IDnsResolverService _dnsResolverService;
    private readonly IEventService _eventService;
    private readonly IMailService _mailService;
    private readonly ILogger<OrganizationDomainService> _logger;
    private readonly IGlobalSettings _globalSettings;

    public OrganizationDomainService(
        IOrganizationDomainRepository domainRepository,
        IOrganizationUserRepository organizationUserRepository,
        IDnsResolverService dnsResolverService,
        IEventService eventService,
        IMailService mailService,
        ILogger<OrganizationDomainService> logger,
        IGlobalSettings globalSettings)
    {
        _domainRepository = domainRepository;
        _organizationUserRepository = organizationUserRepository;
        _dnsResolverService = dnsResolverService;
        _eventService = eventService;
        _mailService = mailService;
        _logger = logger;
        _globalSettings = globalSettings;
    }

    public async Task ValidateOrganizationsDomainAsync()
    {
        //Date should be set 1 hour behind to ensure it selects all domains that should be verified
        var runDate = DateTime.UtcNow.AddHours(-1);

        var verifiableDomains = await _domainRepository.GetManyByNextRunDateAsync(runDate);

        _logger.LogInformation(Constants.BypassFiltersEventId, "Validating {verifiableDomainsCount} domains.", verifiableDomains.Count);

        foreach (var domain in verifiableDomains)
        {
            try
            {
                _logger.LogInformation(Constants.BypassFiltersEventId, "Attempting verification for organization {OrgId} with domain {Domain}", domain.OrganizationId, domain.DomainName);

                var status = await _dnsResolverService.ResolveAsync(domain.DomainName, domain.Txt);
                if (status)
                {
                    _logger.LogInformation(Constants.BypassFiltersEventId, "Successfully validated domain");

                    //update entry on OrganizationDomain table 
                    domain.SetLastCheckedDate();
                    domain.SetVerifiedDate();
                    domain.SetJobRunCount();
                    await _domainRepository.ReplaceAsync(domain);

                    await _eventService.LogOrganizationDomainEventAsync(domain, EventType.OrganizationDomain_Verified,
                        EventSystemUser.DomainVerification);
                }
                else
                {
                    //update entry on OrganizationDomain table 
                    domain.SetLastCheckedDate();
                    domain.SetJobRunCount();
                    domain.SetNextRunDate(_globalSettings.DomainVerification.VerificationInterval);
                    await _domainRepository.ReplaceAsync(domain);

                    await _eventService.LogOrganizationDomainEventAsync(domain, EventType.OrganizationDomain_NotVerified,
                        EventSystemUser.DomainVerification);
                    _logger.LogInformation(Constants.BypassFiltersEventId, "Verification for organization {OrgId} with domain {Domain} failed",
                        domain.OrganizationId, domain.DomainName);
                }
            }
            catch (Exception ex)
            {
                //update entry on OrganizationDomain table 
                domain.SetLastCheckedDate();
                domain.SetJobRunCount();
                domain.SetNextRunDate(_globalSettings.DomainVerification.VerificationInterval);
                await _domainRepository.ReplaceAsync(domain);

                await _eventService.LogOrganizationDomainEventAsync(domain, EventType.OrganizationDomain_NotVerified,
                    EventSystemUser.DomainVerification);

                _logger.LogError(ex, "Verification for organization {OrgId} with domain {Domain} threw an exception: {errorMessage}",
                    domain.OrganizationId, domain.DomainName, ex.Message);
            }
        }
    }

    public async Task OrganizationDomainMaintenanceAsync()
    {
        try
        {
            //Get domains that have not been verified within 72 hours
            var expiredDomains = await _domainRepository.GetExpiredOrganizationDomainsAsync();

            _logger.LogInformation(Constants.BypassFiltersEventId,
                "Attempting email reminder for {expiredDomainCount} expired domains.", expiredDomains.Count);

            foreach (var domain in expiredDomains)
            {
                //get admin emails of organization
                var adminEmails = await GetAdminEmailsAsync(domain.OrganizationId);

                //Send email to administrators
                if (adminEmails.Count > 0)
                {
                    await _mailService.SendUnverifiedOrganizationDomainEmailAsync(adminEmails,
                        domain.OrganizationId.ToString(), domain.DomainName);
                }

                _logger.LogInformation(Constants.BypassFiltersEventId, "Expired domain: {domainName}", domain.DomainName);
            }
            //delete domains that have not been verified within 7 days 
            var status = await _domainRepository.DeleteExpiredAsync(_globalSettings.DomainVerification.ExpirationPeriod);
            _logger.LogInformation(Constants.BypassFiltersEventId, "Delete status {status}", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Organization domain maintenance failed");
        }
    }

    private async Task<List<string>> GetAdminEmailsAsync(Guid organizationId)
    {
        var orgUsers = await _organizationUserRepository.GetManyDetailsByOrganizationAsync(organizationId);
        var emailList = orgUsers.Where(o => o.Type <= OrganizationUserType.Admin
                                        || o.GetPermissions()?.ManageSso == true)
            .Select(a => a.Email).Distinct().ToList();

        return emailList;
    }
}
