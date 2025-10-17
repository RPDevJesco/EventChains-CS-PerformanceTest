using PhoneNumbers;

namespace EventChains_CS.Validation_Events
{
    public static class PhoneValidation
    {
        private static readonly PhoneNumberUtil _phoneUtil = PhoneNumberUtil.GetInstance();

        /// <summary>
        /// Tries to parse and normalize any plausible phone number.
        /// If successful, returns E.164 (e.g., +15551234567) and the detected region (e.g., "US").
        /// </summary>
        public static bool TryNormalizePhone(string? raw, string? countryIso2, out string? e164, out string? region, out string? error)
        {
            e164 = null;
            region = null;
            error = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Empty phone number.";
                return false;
            }

            // Normalize country input (expect ISO 3166-1 alpha-2 like "US", "GB", etc.)
            string? regionHint = null;
            if (!string.IsNullOrWhiteSpace(countryIso2))
            {
                var c = countryIso2.Trim().ToUpperInvariant();
                // libphonenumber expects valid ISO-2; guard against placeholders like "XX"
                if (_phoneUtil.GetSupportedRegions().Contains(c))
                    regionHint = c;
            }

            PhoneNumber? parsed = null;
            try
            {
                // First attempt: use region hint if available; otherwise null lets leading '+' drive parsing.
                parsed = _phoneUtil.Parse(raw, regionHint);
            }
            catch (NumberParseException)
            {
                // Fallback: if we had no hint and the input lacked '+', you can optionally assume a default.
                // Example: default to US if your dataset is mostly US.
                if (regionHint == null)
                {
                    try
                    {
                        parsed = _phoneUtil.Parse(raw, "US");
                    }
                    catch (NumberParseException ex2)
                    {
                        error = $"Could not parse phone number: {ex2.Message}";
                        return false;
                    }
                }
                else
                {
                    error = "Could not parse phone number.";
                    return false;
                }
            }

            if (!_phoneUtil.IsValidNumber(parsed))
            {
                error = "Invalid phone number.";
                return false;
            }

            e164 = _phoneUtil.Format(parsed, PhoneNumberFormat.E164);
            region = _phoneUtil.GetRegionCodeForNumber(parsed);
            return true;
        }
    }
}