﻿using System.ComponentModel.DataAnnotations;

namespace Bit.Api.Models.Request;

public class DeviceVerificationRequestModel
{
    [Obsolete("Leaving this for backwards compatibility on clients")]
    [Required]
    public bool UnknownDeviceVerificationEnabled { get; set; }
}
