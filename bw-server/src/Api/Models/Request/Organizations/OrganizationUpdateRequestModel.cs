﻿using System.ComponentModel.DataAnnotations;
using Bit.Core.Entities;
using Bit.Core.Models.Data;
using Bit.Core.Settings;

namespace Bit.Api.Models.Request.Organizations;

public class OrganizationUpdateRequestModel
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; }
    [StringLength(50)]
    public string BusinessName { get; set; }
    [EmailAddress]
    [Required]
    [StringLength(256)]
    public string BillingEmail { get; set; }
    public Permissions Permissions { get; set; }
    public OrganizationKeysRequestModel Keys { get; set; }

    public virtual Organization ToOrganization(Organization existingOrganization, GlobalSettings globalSettings)
    {
        if (!globalSettings.SelfHosted)
        {
            // These items come from the license file
            existingOrganization.Name = Name;
            existingOrganization.BusinessName = BusinessName;
            existingOrganization.BillingEmail = BillingEmail?.ToLowerInvariant()?.Trim();
        }
        Keys?.ToOrganization(existingOrganization);
        return existingOrganization;
    }
}
