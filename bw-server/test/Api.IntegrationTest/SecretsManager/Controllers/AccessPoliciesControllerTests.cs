﻿using System.Net;
using System.Net.Http.Headers;
using Bit.Api.IntegrationTest.Factories;
using Bit.Api.IntegrationTest.SecretsManager.Enums;
using Bit.Api.Models.Response;
using Bit.Api.SecretsManager.Models.Request;
using Bit.Api.SecretsManager.Models.Response;
using Bit.Core.Enums;
using Bit.Core.SecretsManager.Entities;
using Bit.Core.SecretsManager.Repositories;
using Bit.Test.Common.Helpers;
using Xunit;

namespace Bit.Api.IntegrationTest.SecretsManager.Controllers;

public class AccessPoliciesControllerTests : IClassFixture<ApiApplicationFactory>, IAsyncLifetime
{
    private const string _mockEncryptedString =
        "2.3Uk+WNBIoU5xzmVFNcoWzz==|1MsPIYuRfdOHfu/0uY6H2Q==|/98sp4wb6pHP1VTZ9JcNCYgQjEUMFPlqJgCwRk1YXKg=";

    private readonly IAccessPolicyRepository _accessPolicyRepository;

    private readonly HttpClient _client;
    private readonly ApiApplicationFactory _factory;
    private readonly IProjectRepository _projectRepository;
    private readonly IServiceAccountRepository _serviceAccountRepository;
    private string _email = null!;
    private SecretsManagerOrganizationHelper _organizationHelper = null!;

    public AccessPoliciesControllerTests(ApiApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _accessPolicyRepository = _factory.GetService<IAccessPolicyRepository>();
        _serviceAccountRepository = _factory.GetService<IServiceAccountRepository>();
        _projectRepository = _factory.GetService<IProjectRepository>();
    }

    public async Task InitializeAsync()
    {
        _email = $"integration-test{Guid.NewGuid()}@bitwarden.com";
        await _factory.LoginWithNewAccount(_email);
        _organizationHelper = new SecretsManagerOrganizationHelper(_factory, _email);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private async Task LoginAsync(string email)
    {
        var tokens = await _factory.LoginAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.Token);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CreateProjectAccessPolicies_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var (projectId, serviceAccountId) = await CreateProjectAndServiceAccountAsync(org.Id);

        var request = new AccessPoliciesCreateRequest
        {
            ServiceAccountAccessPolicyRequests = new List<AccessPolicyRequest>
            {
                new() { GranteeId = serviceAccountId, Read = true, Write = true },
            },
        };

        var response = await _client.PostAsJsonAsync($"/projects/{projectId}/access-policies", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProjectAccessPolicies_NoPermission()
    {
        // Create a new account as a user
        var (org, _) = await _organizationHelper.Initialize(true, true);
        var (email, _) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        var (projectId, serviceAccountId) = await CreateProjectAndServiceAccountAsync(org.Id);
        var request = new AccessPoliciesCreateRequest
        {
            ServiceAccountAccessPolicyRequests = new List<AccessPolicyRequest>
            {
                new() { GranteeId = serviceAccountId, Read = true, Write = true },
            },
        };

        var response = await _client.PostAsJsonAsync($"/projects/{projectId}/access-policies", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task CreateProjectAccessPolicies_MismatchedOrgIds_NotFound(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var (projectId, serviceAccountId) = await CreateProjectAndServiceAccountAsync(org.Id, true);
        await SetupProjectAndServiceAccountPermissionAsync(permissionType, projectId, serviceAccountId);

        var request = new AccessPoliciesCreateRequest
        {
            ServiceAccountAccessPolicyRequests = new List<AccessPolicyRequest>
            {
                new() { GranteeId = serviceAccountId, Read = true, Write = true },
            },
        };


        var response = await _client.PostAsJsonAsync($"/projects/{projectId}/access-policies", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task CreateProjectAccessPolicies_Success(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var (projectId, serviceAccountId) = await CreateProjectAndServiceAccountAsync(org.Id);
        await SetupProjectAndServiceAccountPermissionAsync(permissionType, projectId, serviceAccountId);

        var request = new AccessPoliciesCreateRequest
        {
            ServiceAccountAccessPolicyRequests = new List<AccessPolicyRequest>
            {
                new() { GranteeId = serviceAccountId, Read = true, Write = true },
            },
        };

        var response = await _client.PostAsJsonAsync($"/projects/{projectId}/access-policies", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProjectAccessPoliciesResponseModel>();

        Assert.NotNull(result);
        Assert.Equal(serviceAccountId, result!.ServiceAccountAccessPolicies.First().ServiceAccountId);
        Assert.True(result.ServiceAccountAccessPolicies.First().Read);
        Assert.True(result.ServiceAccountAccessPolicies.First().Write);
        AssertHelper.AssertRecent(result.ServiceAccountAccessPolicies.First().RevisionDate);
        AssertHelper.AssertRecent(result.ServiceAccountAccessPolicies.First().CreationDate);

        var createdAccessPolicy =
            await _accessPolicyRepository.GetByIdAsync(result.ServiceAccountAccessPolicies.First().Id);
        Assert.NotNull(createdAccessPolicy);
        Assert.Equal(result.ServiceAccountAccessPolicies.First().Read, createdAccessPolicy!.Read);
        Assert.Equal(result.ServiceAccountAccessPolicies.First().Write, createdAccessPolicy.Write);
        Assert.Equal(result.ServiceAccountAccessPolicies.First().Id, createdAccessPolicy.Id);
        AssertHelper.AssertRecent(createdAccessPolicy.CreationDate);
        AssertHelper.AssertRecent(createdAccessPolicy.RevisionDate);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task UpdateAccessPolicy_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);
        var initData = await SetupAccessPolicyRequest(org.Id);

        const bool expectedRead = true;
        const bool expectedWrite = false;
        var request = new AccessPolicyUpdateRequest { Read = expectedRead, Write = expectedWrite };

        var response = await _client.PutAsJsonAsync($"/access-policies/{initData.AccessPolicyId}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAccessPolicy_NoPermission()
    {
        // Create a new account as a user
        await _organizationHelper.Initialize(true, true);
        var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        var initData = await SetupAccessPolicyRequest(orgUser.OrganizationId);

        const bool expectedRead = true;
        const bool expectedWrite = false;
        var request = new AccessPolicyUpdateRequest { Read = expectedRead, Write = expectedWrite };

        var response = await _client.PutAsJsonAsync($"/access-policies/{initData.AccessPolicyId}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task UpdateAccessPolicy_Success(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);
        var initData = await SetupAccessPolicyRequest(org.Id);

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);
            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserProjectAccessPolicy
                {
                    GrantedProjectId = initData.ProjectId, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }

        const bool expectedRead = true;
        const bool expectedWrite = false;
        var request = new AccessPolicyUpdateRequest { Read = expectedRead, Write = expectedWrite };

        var response = await _client.PutAsJsonAsync($"/access-policies/{initData.AccessPolicyId}", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ServiceAccountProjectAccessPolicyResponseModel>();

        Assert.NotNull(result);
        Assert.Equal(expectedRead, result!.Read);
        Assert.Equal(expectedWrite, result.Write);
        AssertHelper.AssertRecent(result.RevisionDate);

        var updatedAccessPolicy = await _accessPolicyRepository.GetByIdAsync(result.Id);
        Assert.NotNull(updatedAccessPolicy);
        Assert.Equal(expectedRead, updatedAccessPolicy!.Read);
        Assert.Equal(expectedWrite, updatedAccessPolicy.Write);
        AssertHelper.AssertRecent(updatedAccessPolicy.RevisionDate);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DeleteAccessPolicy_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);
        var initData = await SetupAccessPolicyRequest(org.Id);

        var response = await _client.DeleteAsync($"/access-policies/{initData.AccessPolicyId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccessPolicy_NoPermission()
    {
        // Create a new account as a user
        await _organizationHelper.Initialize(true, true);
        var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        var initData = await SetupAccessPolicyRequest(orgUser.OrganizationId);

        var response = await _client.DeleteAsync($"/access-policies/{initData.AccessPolicyId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task DeleteAccessPolicy_Success(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);
        var initData = await SetupAccessPolicyRequest(org.Id);

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);
            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserProjectAccessPolicy
                {
                    GrantedProjectId = initData.ProjectId, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }

        var response = await _client.DeleteAsync($"/access-policies/{initData.AccessPolicyId}");
        response.EnsureSuccessStatusCode();

        var test = await _accessPolicyRepository.GetByIdAsync(initData.AccessPolicyId);
        Assert.Null(test);
    }

    [Fact]
    public async Task GetProjectAccessPolicies_ReturnsEmpty()
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var project = await _projectRepository.CreateAsync(new Project
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        var response = await _client.GetAsync($"/projects/{project.Id}/access-policies");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProjectAccessPoliciesResponseModel>();

        Assert.NotNull(result);
        Assert.Empty(result!.UserAccessPolicies);
        Assert.Empty(result.GroupAccessPolicies);
        Assert.Empty(result.ServiceAccountAccessPolicies);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetProjectAccessPolicies_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var initData = await SetupAccessPolicyRequest(org.Id);

        var response = await _client.GetAsync($"/projects/{initData.ProjectId}/access-policies");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectAccessPolicies_NoPermission()
    {
        // Create a new account as a user
        await _organizationHelper.Initialize(true, true);
        var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        var initData = await SetupAccessPolicyRequest(orgUser.OrganizationId);

        var response = await _client.GetAsync($"/projects/{initData.ProjectId}/access-policies");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task GetProjectAccessPolicies(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);
        var initData = await SetupAccessPolicyRequest(org.Id);

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);
            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserProjectAccessPolicy
                {
                    GrantedProjectId = initData.ProjectId, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }

        var response = await _client.GetAsync($"/projects/{initData.ProjectId}/access-policies");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProjectAccessPoliciesResponseModel>();

        Assert.NotNull(result?.ServiceAccountAccessPolicies);
        Assert.Single(result!.ServiceAccountAccessPolicies);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetPeoplePotentialGrantees_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var response =
            await _client.GetAsync(
                $"/organizations/{org.Id}/access-policies/people/potential-grantees");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task GetPeoplePotentialGrantees_Success(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, _) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);
        }

        var response =
            await _client.GetAsync(
                $"/organizations/{org.Id}/access-policies/people/potential-grantees");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResponseModel<PotentialGranteeResponseModel>>();

        Assert.NotNull(result?.Data);
        Assert.NotEmpty(result!.Data);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetServiceAccountPotentialGrantees_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var response =
            await _client.GetAsync(
                $"/organizations/{org.Id}/access-policies/service-accounts/potential-grantees");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetServiceAccountPotentialGrantees_OnlyReturnsServiceAccountsWithWriteAccess()
    {
        // Create a new account as a user
        var (org, _) = await _organizationHelper.Initialize(true, true);
        var (email, _) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });


        var response =
            await _client.GetAsync(
                $"/organizations/{org.Id}/access-policies/service-accounts/potential-grantees");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResponseModel<PotentialGranteeResponseModel>>();

        Assert.NotNull(result?.Data);
        Assert.Empty(result!.Data);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task GetServiceAccountsPotentialGrantees_Success(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);

            await _accessPolicyRepository.CreateManyAsync(
                new List<BaseAccessPolicy>
                {
                    new UserServiceAccountAccessPolicy
                    {
                        GrantedServiceAccountId = serviceAccount.Id,
                        OrganizationUserId = orgUser.Id,
                        Read = true,
                        Write = true,
                    },
                });
        }

        var response =
            await _client.GetAsync(
                $"/organizations/{org.Id}/access-policies/service-accounts/potential-grantees");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResponseModel<PotentialGranteeResponseModel>>();

        Assert.NotNull(result?.Data);
        Assert.NotEmpty(result!.Data);
        Assert.Equal(serviceAccount.Id, result.Data.First(x => x.Id == serviceAccount.Id).Id);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetProjectPotentialGrantees_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var response =
            await _client.GetAsync(
                $"/organizations/{org.Id}/access-policies/projects/potential-grantees");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectPotentialGrantees_OnlyReturnsProjectsWithWriteAccess()
    {
        // Create a new account as a user
        var (org, _) = await _organizationHelper.Initialize(true, true);
        var (email, _) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        await _projectRepository.CreateAsync(new Project { OrganizationId = org.Id, Name = _mockEncryptedString });


        var response =
            await _client.GetAsync(
                $"/organizations/{org.Id}/access-policies/projects/potential-grantees");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResponseModel<PotentialGranteeResponseModel>>();

        Assert.NotNull(result?.Data);
        Assert.Empty(result!.Data);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task GetProjectPotentialGrantees_Success(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var project = await _projectRepository.CreateAsync(new Project
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);

            await _accessPolicyRepository.CreateManyAsync(
                new List<BaseAccessPolicy>
                {
                    new UserProjectAccessPolicy
                    {
                        GrantedProjectId = project.Id, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                    },
                });
        }

        var response =
            await _client.GetAsync(
                $"/organizations/{org.Id}/access-policies/projects/potential-grantees");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListResponseModel<PotentialGranteeResponseModel>>();

        Assert.NotNull(result?.Data);
        Assert.NotEmpty(result!.Data);
        Assert.Equal(project.Id, result.Data.First(x => x.Id == project.Id).Id);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CreateServiceAccountAccessPolicies_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, orgUser) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        var request = new AccessPoliciesCreateRequest
        {
            UserAccessPolicyRequests = new List<AccessPolicyRequest>
            {
                new() { GranteeId = orgUser.Id, Read = true, Write = true },
            },
        };

        var response =
            await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-policies", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task CreateServiceAccountAccessPolicies_MismatchOrgId_NotFound(PermissionType permissionType)
    {
        var (org, orgUser) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);
        var ownerOrgUserId = orgUser.Id;

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = Guid.NewGuid(),
            Name = _mockEncryptedString,
        });
        var request =
            await SetupUserServiceAccountAccessPolicyRequestAsync(permissionType, org.Id, orgUser.Id,
                serviceAccount.Id);

        var response =
            await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-policies", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task CreateServiceAccountAccessPolicies_Success(PermissionType permissionType)
    {
        var (org, orgUser) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);
        var ownerOrgUserId = orgUser.Id;

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });
        var request =
            await SetupUserServiceAccountAccessPolicyRequestAsync(permissionType, org.Id, orgUser.Id,
                serviceAccount.Id);

        var response =
            await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-policies", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ServiceAccountAccessPoliciesResponseModel>();

        Assert.NotNull(result);
        Assert.Equal(ownerOrgUserId,
            result!.UserAccessPolicies.First(ap => ap.OrganizationUserId == ownerOrgUserId).OrganizationUserId);
        Assert.True(result.UserAccessPolicies.First().Read);
        Assert.True(result.UserAccessPolicies.First().Write);

        var createdAccessPolicy =
            await _accessPolicyRepository.GetByIdAsync(result.UserAccessPolicies.First().Id);
        Assert.NotNull(createdAccessPolicy);
        Assert.Equal(result.UserAccessPolicies.First().Read, createdAccessPolicy!.Read);
        Assert.Equal(result.UserAccessPolicies.First().Write, createdAccessPolicy.Write);
        Assert.Equal(result.UserAccessPolicies.First().Id, createdAccessPolicy.Id);
    }

    [Fact]
    public async Task CreateServiceAccountAccessPolicies_NoPermission()
    {
        // Create a new account as a user
        var (org, _) = await _organizationHelper.Initialize(true, true);
        var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        var request = new AccessPoliciesCreateRequest
        {
            UserAccessPolicyRequests = new List<AccessPolicyRequest>
            {
                new() { GranteeId = orgUser.Id, Read = true, Write = true },
            },
        };

        var response =
            await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/access-policies", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetServiceAccountAccessPolicies_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);
        var initData = await SetupAccessPolicyRequest(org.Id);

        var response = await _client.GetAsync($"/service-accounts/{initData.ServiceAccountId}/access-policies");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetServiceAccountAccessPolicies_ReturnsEmpty()
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        var response = await _client.GetAsync($"/service-accounts/{serviceAccount.Id}/access-policies");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ServiceAccountAccessPoliciesResponseModel>();

        Assert.NotNull(result);
        Assert.Empty(result!.UserAccessPolicies);
        Assert.Empty(result.GroupAccessPolicies);
    }

    [Fact]
    public async Task GetServiceAccountAccessPolicies_NoPermission()
    {
        // Create a new account as a user
        await _organizationHelper.Initialize(true, true);
        var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        var initData = await SetupAccessPolicyRequest(orgUser.OrganizationId);

        var response = await _client.GetAsync($"/service-accounts/{initData.ServiceAccountId}/access-policies");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task GetServiceAccountAccessPolicies(PermissionType permissionType)
    {
        var (org, ownerOrgUser) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);
        var initData = await SetupAccessPolicyRequest(org.Id);

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);
            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserServiceAccountAccessPolicy
                {
                    GrantedServiceAccountId = initData.ServiceAccountId,
                    OrganizationUserId = orgUser.Id,
                    Read = true,
                    Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }

        var policies = new List<BaseAccessPolicy>
        {
            new UserServiceAccountAccessPolicy
            {
                GrantedServiceAccountId = initData.ServiceAccountId,
                OrganizationUserId = ownerOrgUser.Id,
                Read = true,
                Write = true,
            },
        };
        await _accessPolicyRepository.CreateManyAsync(policies);

        var response = await _client.GetAsync($"/service-accounts/{initData.ServiceAccountId}/access-policies");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ServiceAccountAccessPoliciesResponseModel>();

        Assert.NotNull(result?.UserAccessPolicies);
        Assert.NotEmpty(result!.UserAccessPolicies);
        Assert.Equal(ownerOrgUser.Id,
            result.UserAccessPolicies.First(x => x.OrganizationUserId == ownerOrgUser.Id).OrganizationUserId);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CreateServiceAccountGrantedPolicies_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        var request = new List<GrantedAccessPolicyRequest> { new() { GrantedId = new Guid() } };

        var response =
            await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/granted-policies", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceAccountGrantedPolicies_NoPermission()
    {
        // Create a new account as a user
        var (org, _) = await _organizationHelper.Initialize(true, true);
        var (email, _) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        var project = await _projectRepository.CreateAsync(new Project
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        var request =
            new List<GrantedAccessPolicyRequest> { new() { GrantedId = project.Id, Read = true, Write = true } };

        var response =
            await _client.PostAsJsonAsync($"/service-accounts/{serviceAccount.Id}/granted-policies", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task CreateServiceAccountGrantedPolicies_MismatchedOrgId_NotFound(PermissionType permissionType)
    {
        var (org, orgUser) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);
        var ownerOrgUserId = orgUser.Id;

        var (projectId, serviceAccountId) = await CreateProjectAndServiceAccountAsync(org.Id, true);
        await SetupProjectAndServiceAccountPermissionAsync(permissionType, projectId, serviceAccountId);

        var request =
            new List<GrantedAccessPolicyRequest> { new() { GrantedId = projectId, Read = true, Write = true } };

        var response =
            await _client.PostAsJsonAsync($"/service-accounts/{serviceAccountId}/granted-policies", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task CreateServiceAccountGrantedPolicies_Success(PermissionType permissionType)
    {
        var (org, orgUser) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);
        var ownerOrgUserId = orgUser.Id;

        var (projectId, serviceAccountId) = await CreateProjectAndServiceAccountAsync(org.Id);
        await SetupProjectAndServiceAccountPermissionAsync(permissionType, projectId, serviceAccountId);

        var request =
            new List<GrantedAccessPolicyRequest> { new() { GrantedId = projectId, Read = true, Write = true } };

        var response =
            await _client.PostAsJsonAsync($"/service-accounts/{serviceAccountId}/granted-policies", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ListResponseModel<ServiceAccountProjectAccessPolicyResponseModel>>();

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Data);
        Assert.Equal(projectId, result.Data.First().GrantedProjectId);

        var createdAccessPolicy =
            await _accessPolicyRepository.GetByIdAsync(result.Data.First().Id);
        Assert.NotNull(createdAccessPolicy);
        Assert.Equal(result.Data.First().Read, createdAccessPolicy!.Read);
        Assert.Equal(result.Data.First().Write, createdAccessPolicy.Write);
        Assert.Equal(result.Data.First().Id, createdAccessPolicy.Id);
        AssertHelper.AssertRecent(createdAccessPolicy.CreationDate);
        AssertHelper.AssertRecent(createdAccessPolicy.RevisionDate);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetServiceAccountGrantedPolicies_SmNotEnabled_NotFound(bool useSecrets, bool accessSecrets)
    {
        var (org, _) = await _organizationHelper.Initialize(useSecrets, accessSecrets);
        await LoginAsync(_email);
        var initData = await SetupAccessPolicyRequest(org.Id);

        var response = await _client.GetAsync($"/service-accounts/{initData.ServiceAccountId}/granted-policies");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetServiceAccountGrantedPolicies_ReturnsEmpty()
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = org.Id,
            Name = _mockEncryptedString,
        });

        var response = await _client.GetAsync($"/service-accounts/{serviceAccount.Id}/granted-policies");
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ListResponseModel<ServiceAccountProjectAccessPolicyResponseModel>>();

        Assert.NotNull(result);
        Assert.Empty(result!.Data);
    }

    [Fact]
    public async Task GetServiceAccountGrantedPolicies_NoPermission_ReturnsEmpty()
    {
        // Create a new account as a user
        await _organizationHelper.Initialize(true, true);
        var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
        await LoginAsync(email);

        var initData = await SetupAccessPolicyRequest(orgUser.OrganizationId);

        var response = await _client.GetAsync($"/service-accounts/{initData.ServiceAccountId}/granted-policies");

        var result = await response.Content
            .ReadFromJsonAsync<ListResponseModel<ServiceAccountProjectAccessPolicyResponseModel>>();

        Assert.NotNull(result);
        Assert.Empty(result!.Data);
    }

    [Theory]
    [InlineData(PermissionType.RunAsAdmin)]
    [InlineData(PermissionType.RunAsUserWithPermission)]
    public async Task GetServiceAccountGrantedPolicies(PermissionType permissionType)
    {
        var (org, _) = await _organizationHelper.Initialize(true, true);
        await LoginAsync(_email);
        var initData = await SetupAccessPolicyRequest(org.Id);

        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);
            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserProjectAccessPolicy
                {
                    GrantedProjectId = initData.ProjectId, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }

        var response = await _client.GetAsync($"/service-accounts/{initData.ServiceAccountId}/granted-policies");
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<ListResponseModel<ServiceAccountProjectAccessPolicyResponseModel>>();

        Assert.NotNull(result?.Data);
        Assert.NotEmpty(result!.Data);
        Assert.Equal(initData.ServiceAccountId, result.Data.First().ServiceAccountId);
        Assert.NotNull(result.Data.First().ServiceAccountName);
        Assert.NotNull(result.Data.First().GrantedProjectName);
    }

    private async Task<RequestSetupData> SetupAccessPolicyRequest(Guid organizationId)
    {
        var project = await _projectRepository.CreateAsync(new Project
        {
            OrganizationId = organizationId,
            Name = _mockEncryptedString,
        });

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = organizationId,
            Name = _mockEncryptedString,
        });

        var accessPolicy = await _accessPolicyRepository.CreateManyAsync(
            new List<BaseAccessPolicy>
            {
                new ServiceAccountProjectAccessPolicy
                {
                    Read = true, Write = true, ServiceAccountId = serviceAccount.Id, GrantedProjectId = project.Id,
                },
            });

        return new RequestSetupData
        {
            ProjectId = project.Id,
            ServiceAccountId = serviceAccount.Id,
            AccessPolicyId = accessPolicy.First().Id,
        };
    }

    private async Task<(Guid ProjectId, Guid ServiceAccountId)> CreateProjectAndServiceAccountAsync(Guid organizationId,
        bool misMatchOrganization = false)
    {
        var project = await _projectRepository.CreateAsync(new Project
        {
            OrganizationId = misMatchOrganization ? Guid.NewGuid() : organizationId,
            Name = _mockEncryptedString,
        });

        var serviceAccount = await _serviceAccountRepository.CreateAsync(new ServiceAccount
        {
            OrganizationId = organizationId,
            Name = _mockEncryptedString,
        });

        return (project.Id, serviceAccount.Id);
    }

    private async Task SetupProjectAndServiceAccountPermissionAsync(PermissionType permissionType, Guid projectId,
        Guid serviceAccountId)
    {
        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, orgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);
            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserProjectAccessPolicy
                {
                    GrantedProjectId = projectId, OrganizationUserId = orgUser.Id, Read = true, Write = true,
                },
                new UserServiceAccountAccessPolicy
                {
                    GrantedServiceAccountId = serviceAccountId,
                    OrganizationUserId = orgUser.Id,
                    Read = true,
                    Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }
    }

    private async Task<AccessPoliciesCreateRequest> SetupUserServiceAccountAccessPolicyRequestAsync(
        PermissionType permissionType, Guid organizationId, Guid userId, Guid serviceAccountId)
    {
        if (permissionType == PermissionType.RunAsUserWithPermission)
        {
            var (email, newOrgUser) = await _organizationHelper.CreateNewUser(OrganizationUserType.User, true);
            await LoginAsync(email);
            var accessPolicies = new List<BaseAccessPolicy>
            {
                new UserServiceAccountAccessPolicy
                {
                    GrantedServiceAccountId = serviceAccountId,
                    OrganizationUserId = newOrgUser.Id,
                    Read = true,
                    Write = true,
                },
            };
            await _accessPolicyRepository.CreateManyAsync(accessPolicies);
        }

        return new AccessPoliciesCreateRequest
        {
            UserAccessPolicyRequests = new List<AccessPolicyRequest>
            {
                new() { GranteeId = userId, Read = true, Write = true },
            },
        };
    }

    private class RequestSetupData
    {
        public Guid ProjectId { get; set; }
        public Guid AccessPolicyId { get; set; }
        public Guid ServiceAccountId { get; set; }
    }
}
