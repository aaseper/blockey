﻿using Bit.Core.Vault.Enums;

namespace Bit.Core.Vault.Models.Data;

public class CipherSecureNoteData : CipherData
{
    public CipherSecureNoteData() { }

    public SecureNoteType Type { get; set; }
}
