﻿using Bit.Admin.Enums;
using Bit.Admin.Models;
using Bit.Admin.Services;
using Bit.Admin.Utilities;
using Bit.Core.Entities;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Bit.Core.Vault.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Admin.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly ICipherRepository _cipherRepository;
    private readonly IPaymentService _paymentService;
    private readonly GlobalSettings _globalSettings;
    private readonly IAccessControlService _accessControlService;

    public UsersController(
        IUserRepository userRepository,
        ICipherRepository cipherRepository,
        IPaymentService paymentService,
        GlobalSettings globalSettings,
        IAccessControlService accessControlService)
    {
        _userRepository = userRepository;
        _cipherRepository = cipherRepository;
        _paymentService = paymentService;
        _globalSettings = globalSettings;
        _accessControlService = accessControlService;
    }

    [RequirePermission(Permission.User_List_View)]
    public async Task<IActionResult> Index(string email, int page = 1, int count = 25)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (count < 1)
        {
            count = 1;
        }

        var skip = (page - 1) * count;
        var users = await _userRepository.SearchAsync(email, skip, count);
        return View(new UsersModel
        {
            Items = users as List<User>,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            Page = page,
            Count = count,
            Action = _globalSettings.SelfHosted ? "View" : "Edit"
        });
    }

    public async Task<IActionResult> View(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return RedirectToAction("Index");
        }

        var ciphers = await _cipherRepository.GetManyByUserIdAsync(id);
        return View(new UserViewModel(user, ciphers));
    }

    [SelfHosted(NotSelfHostedOnly = true)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return RedirectToAction("Index");
        }

        var ciphers = await _cipherRepository.GetManyByUserIdAsync(id);
        var billingInfo = await _paymentService.GetBillingAsync(user);
        return View(new UserEditModel(user, ciphers, billingInfo, _globalSettings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [SelfHosted(NotSelfHostedOnly = true)]
    public async Task<IActionResult> Edit(Guid id, UserEditModel model)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return RedirectToAction("Index");
        }

        var canUpgradePremium = _accessControlService.UserHasPermission(Permission.User_UpgradePremium);

        if (_accessControlService.UserHasPermission(Permission.User_Premium_Edit) ||
            canUpgradePremium)
        {
            user.MaxStorageGb = model.MaxStorageGb;
            user.Premium = model.Premium;
        }

        if (_accessControlService.UserHasPermission(Permission.User_Billing_Edit))
        {
            user.Gateway = model.Gateway;
            user.GatewayCustomerId = model.GatewayCustomerId;
            user.GatewaySubscriptionId = model.GatewaySubscriptionId;
        }

        if (_accessControlService.UserHasPermission(Permission.User_Licensing_Edit) ||
            canUpgradePremium)
        {
            user.LicenseKey = model.LicenseKey;
            user.PremiumExpirationDate = model.PremiumExpirationDate;
        }

        await _userRepository.ReplaceAsync(user);
        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission(Permission.User_Delete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user != null)
        {
            await _userRepository.DeleteAsync(user);
        }

        return RedirectToAction("Index");
    }
}
