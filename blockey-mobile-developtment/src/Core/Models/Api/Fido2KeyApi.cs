﻿using Bit.Core.Models.Domain;
using Bit.Core.Models.Export;

namespace Bit.Core.Models.Api
{
    public class Fido2KeyApi
    {
        public Fido2KeyApi()
        {
        }

        public Fido2KeyApi(Fido2Key fido2Key)
        {
            NonDiscoverableId = fido2Key.NonDiscoverableId?.EncryptedString;
            KeyType = fido2Key.KeyType?.EncryptedString;
            KeyAlgorithm = fido2Key.KeyAlgorithm?.EncryptedString;
            KeyCurve = fido2Key.KeyCurve?.EncryptedString;
            KeyValue = fido2Key.KeyValue?.EncryptedString;
            RpId = fido2Key.RpId?.EncryptedString;
            RpName = fido2Key.RpName?.EncryptedString;
            UserHandle = fido2Key.UserHandle?.EncryptedString;
            UserName = fido2Key.UserName?.EncryptedString;
            Counter = fido2Key.Counter?.EncryptedString;
        }

        public string NonDiscoverableId { get; set; }
        public string KeyType { get; set; } = Constants.DefaultFido2KeyType;
        public string KeyAlgorithm { get; set; } = Constants.DefaultFido2KeyAlgorithm;
        public string KeyCurve { get; set; } = Constants.DefaultFido2KeyCurve;
        public string KeyValue { get; set; }
        public string RpId { get; set; }
        public string RpName { get; set; }
        public string UserHandle { get; set; }
        public string UserName { get; set; }
        public string Counter { get; set; }
    }
}
