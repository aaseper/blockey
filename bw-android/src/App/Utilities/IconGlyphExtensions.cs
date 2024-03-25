﻿using Bit.Core;
using Bit.Core.Enums;
using Bit.Core.Models.View;

namespace Bit.App.Utilities
{
    public static class IconGlyphExtensions
    {
        public static string GetIcon(this CipherView cipher)
        {
            switch (cipher.Type)
            {
                case CipherType.Login:
                    return GetLoginIconGlyph(cipher);
                case CipherType.SecureNote:
                    return BitwardenIcons.StickyNote;
                case CipherType.Card:
                    return BitwardenIcons.CreditCard;
                case CipherType.Identity:
                    return BitwardenIcons.IdCard;
                case CipherType.Fido2Key:
                    return BitwardenIcons.Passkey;
            }
            return null;
        }

        static string GetLoginIconGlyph(CipherView cipher)
        {
            var icon = BitwardenIcons.Globe;
            if (cipher.Login.Uri != null)
            {
                var hostnameUri = cipher.Login.Uri;
                if (hostnameUri.StartsWith(Constants.AndroidAppProtocol))
                {
                    icon = BitwardenIcons.Android;
                }
                else if (hostnameUri.StartsWith(Constants.iOSAppProtocol))
                {
                    icon = BitwardenIcons.Apple;
                }
            }
            return icon;
        }

        public static string GetBooleanIconGlyph(bool value, BooleanGlyphType type)
        {
            switch (type)
            {
                case BooleanGlyphType.Checkbox:
                    return value ? BitwardenIcons.CheckSquare : BitwardenIcons.Square;
                case BooleanGlyphType.Eye:
                    return value ? "eye_slash.png" : "eye_noslash.png";
                default:
                    return "";
            }
        }
    }

    public enum BooleanGlyphType
    {
        Checkbox,
        Eye
    }
}
