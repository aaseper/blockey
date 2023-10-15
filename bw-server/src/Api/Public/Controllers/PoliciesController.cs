﻿using System.Net;
using Bit.Api.Models.Public.Request;
using Bit.Api.Models.Public.Response;
using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.Public.Controllers;

[Route("public/policies")]
[Authorize("Organization")]
public class PoliciesController : Controller
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IPolicyService _policyService;
    private readonly IUserService _userService;
    private readonly IOrganizationService _organizationService;
    private readonly ICurrentContext _currentContext;

    public PoliciesController(
        IPolicyRepository policyRepository,
        IPolicyService policyService,
        IUserService userService,
        IOrganizationService organizationService,
        ICurrentContext currentContext)
    {
        _policyRepository = policyRepository;
        _policyService = policyService;
        _userService = userService;
        _organizationService = organizationService;
        _currentContext = currentContext;
    }

    /// <summary>
    /// Retrieve a policy.
    /// </summary>
    /// <remarks>
    /// Retrieves the details of a policy.
    /// </remarks>
    /// <param name="type">The type of policy to be retrieved.</param>
    [HttpGet("{type}")]
    [ProducesResponseType(typeof(GroupResponseModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Get(PolicyType type)
    {
        var policy = await _policyRepository.GetByOrganizationIdTypeAsync(
            _currentContext.OrganizationId.Value, type);
        if (policy == null)
        {
            return new NotFoundResult();
        }
        var response = new PolicyResponseModel(policy);
        return new JsonResult(response);
    }

    /// <summary>
    /// List all policies.
    /// </summary>
    /// <remarks>
    /// Returns a list of your organization's policies.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ListResponseModel<PolicyResponseModel>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> List()
    {
        var policies = await _policyRepository.GetManyByOrganizationIdAsync(_currentContext.OrganizationId.Value);
        var policyResponses = policies.Select(p => new PolicyResponseModel(p));
        var response = new ListResponseModel<PolicyResponseModel>(policyResponses);
        return new JsonResult(response);
    }

    /// <summary>
    /// Update a policy.
    /// </summary>
    /// <remarks>
    /// Updates the specified policy. If a property is not provided,
    /// the value of the existing property will be reset.
    /// </remarks>
    /// <param name="type">The type of policy to be updated.</param>
    /// <param name="model">The request model.</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PolicyResponseModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponseModel), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> Put(PolicyType type, [FromBody] PolicyUpdateRequestModel model)
    {
        var policy = await _policyRepository.GetByOrganizationIdTypeAsync(
            _currentContext.OrganizationId.Value, type);
        if (policy == null)
        {
            policy = model.ToPolicy(_currentContext.OrganizationId.Value);
        }
        else
        {
            policy = model.ToPolicy(policy);
        }
        await _policyService.SaveAsync(policy, _userService, _organizationService, null);
        var response = new PolicyResponseModel(policy);
        return new JsonResult(response);
    }
}
