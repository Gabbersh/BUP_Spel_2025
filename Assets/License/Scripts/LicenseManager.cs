using System;
using System.Globalization;
using UnityEngine;

public static class LicenseManager
{
    private const string DateFormat = "yyyy-MM-dd";

    public static bool IsActivated()
    {
        if (!LicenseStorage.TryLoad(out var license)) return false;
        if (license == null) return false;

        if (!DateTime.TryParseExact(
                license.expiryUtc,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var expiry))
            return false;

        return DateTime.UtcNow.Date <= expiry.Date;
    }

    public static string GetTier()
    {
        if (!LicenseStorage.TryLoad(out var license) || license == null) return "NONE";
        return string.IsNullOrEmpty(license.tier) ? "NONE" : license.tier;
    }

    public static DateTime? GetExpiryUtc()
    {
        if (!LicenseStorage.TryLoad(out var license) || license == null) return null;

        if (DateTime.TryParseExact(
                license.expiryUtc,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var expiry))
            return expiry;

        return null;
    }

    // Mock-format: TEST-<TIER>-<YYYY-MM-DD>
    // Ex: TEST-FULL-2027-01-01
    public static bool TryActivateMock(string inputCode, out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(inputCode))
        {
            error = "Enter a code.";
            return false;
        }

        var code = inputCode.Trim().ToUpperInvariant()
            .Replace('–', '-')
            .Replace('—', '-');

        var parts = code.Split('-');
        if (parts.Length != 5 || parts[0] != "TEST")
        {
            error = "Invalid format. Example: TEST-FULL-2027-01-01";
            return false;
        }

        var tier = parts[1];
        var expiryStr = $"{parts[2]}-{parts[3]}-{parts[4]}";

        if (!DateTime.TryParseExact(
                expiryStr,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var expiry))
        {
            error = "Could not read expiry date.";
            return false;
        }

        if (DateTime.UtcNow.Date > expiry.Date)
        {
            error = "Code expired.";
            return false;
        }

        var local = new LocalLicense
        {
            tier = tier,
            expiryUtc = expiry.ToString(DateFormat),
        };

        if (!LicenseStorage.Save(local))
        {
            error = "Failed to save license locally.";
            return false;
        }

        return true;
    }

    public static void ClearLicense()
    {
        LicenseStorage.Delete();
    }
}