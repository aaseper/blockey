﻿namespace Bit.Core.Tools.Entities;

public interface IReferenceable
{
    Guid Id { get; set; }
    string ReferenceData { get; set; }
    bool IsUser();
}
