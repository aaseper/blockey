﻿using Bit.Core.AdminConsole.Models.OrganizationConnectionConfigs;
using Xunit;

namespace Bit.Core.Test.Models.OrganizationConnectionConfigs;

public class ScimConfigTests
{
    [Fact]
    public void ScimConfig_CanUse_Success()
    {
        var config = new ScimConfig() { Enabled = true };
        Assert.True(config.Validate(out var exception));
        Assert.True(string.IsNullOrEmpty(exception));
    }

    [Fact]
    public void ScimConfig_CanUse_WhenDisabled_ReturnsFalse()
    {
        var config = new ScimConfig() { Enabled = false };
        Assert.False(config.Validate(out var exception));
        Assert.Contains("Config is disabled", exception);
    }
}
