﻿using System.ComponentModel.DataAnnotations;
using Bit.Core.Entities;

namespace Bit.Api.Models.Request;

public class GroupRequestModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    [Required]
    public bool? AccessAll { get; set; }
    public IEnumerable<SelectionReadOnlyRequestModel> Collections { get; set; }
    public IEnumerable<Guid> Users { get; set; }

    public Group ToGroup(Guid orgId)
    {
        return ToGroup(new Group
        {
            OrganizationId = orgId
        });
    }

    public Group ToGroup(Group existingGroup)
    {
        existingGroup.Name = Name;
        existingGroup.AccessAll = AccessAll.Value;
        return existingGroup;
    }
}

public class GroupBulkRequestModel
{
    [Required]
    public IEnumerable<Guid> Ids { get; set; }
}
