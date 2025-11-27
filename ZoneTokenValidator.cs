using System;

// Simple placeholder for now.
// Later this will do real HMAC + expiry checks for zone transfer tokens.
public static class ZoneTokenValidator
{
    // TEMP IMPLEMENTATION:
    // - returns true if the token is non-empty
    // - returns false if it's null/whitespace
    public static bool TryValidate(string token, string account, string characterName)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // TODO: replace with real validation:
        //  - parse token payload
        //  - verify HMAC/signature using a shared secret
        //  - check timestamp/expiry
        //  - confirm account/character match if you encode them into the token
        return true;
    }
}
