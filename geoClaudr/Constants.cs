using Microsoft.Maui.Storage;

namespace geoClaudr;

/// <summary>
/// App-wide configuration. The only thing you (optionally) need to set is the
/// free Mapillary access token used for the street-level imagery.
/// </summary>
public static class Constants
{
    // ---------------------------------------------------------------------
    //  HOW TO GET A FREE MAPILLARY TOKEN (takes ~2 minutes, no credit card):
    //
    //   1. Create a free account at https://www.mapillary.com
    //   2. Open https://www.mapillary.com/dashboard/developers
    //   3. Click "Register Application" (any name, e.g. "geoClaudr"),
    //      tick the read scopes, and create it.
    //   4. Copy the "Client token"  (it starts with  MLY|...).
    //   5. Paste it below, replacing the placeholder.
    //
    //  You can ALSO just paste the token into the start screen inside the app
    //  the first time you run it - no code change required.
    // ---------------------------------------------------------------------
    public const string DefaultMapillaryToken = "MLY|YOUR_CLIENT_TOKEN_HERE";

    /// <summary>
    /// Returns the token saved at runtime (via the in-app start screen) if any,
    /// otherwise the compile-time default above.
    /// </summary>
    public static string MapillaryToken =>
        Preferences.Default.Get("mapillary_token", DefaultMapillaryToken);
}
