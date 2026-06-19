using System.Text;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace geoClaudr;

public partial class MainPage : ContentPage
{
    // Keys the game persists (best score, settings, lifetime stats, token).
    private static readonly string[] PersistedKeys =
    {
        "mapillary_token", "best_score",
        "opt_region", "opt_rounds", "opt_move", "opt_time",
        "st_games", "st_rounds", "st_points", "st_bestround", "st_closest", "st_regions"
    };

    public MainPage()
    {
        InitializeComponent();
        GameView.Navigating += OnNavigating;   // JS -> C# message channel
        _ = LoadGameAsync();
    }

    /// <summary>
    /// Loads the self-contained HTML game from packaged assets and injects, via simple
    /// string replacement: the Mapillary token, a "native" flag, and a base64 blob of
    /// every saved value. Doing it at load time means persistence never depends on a
    /// runtime JS round-trip, which is the most reliable option across Windows/Android/iOS.
    /// </summary>
    private async Task LoadGameAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("game.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            var data = new Dictionary<string, string>();
            foreach (var key in PersistedKeys)
            {
                var v = Preferences.Default.Get(key, (string?)null);
                if (!string.IsNullOrEmpty(v)) data[key] = v!;
            }
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data)));

            html = html
                .Replace("__MAPILLARY_TOKEN__", Constants.MapillaryToken)
                .Replace("__NATIVE_FLAG__", "1")
                .Replace("__NATIVE_DATA_B64__", b64);

            // Self-host the small JS libraries: swap their CDN tags for inline copies
            // packaged under Resources/Raw/lib. Each replacement is best-effort — if an
            // asset is missing the original CDN tag survives, so it's never worse than before.
            // (mapillary-js itself stays on the CDN: it's too large for the WebView2
            // NavigateToString ~2 MB cap on string-loaded HTML.)
            html = await InlineLibraryAsync(html,
                "<link rel=\"stylesheet\" href=\"https://unpkg.com/leaflet@1.9.4/dist/leaflet.css\" />",
                "lib/leaflet.css", css => $"<style>{css}</style>");
            html = await InlineLibraryAsync(html,
                "<link rel=\"stylesheet\" href=\"https://unpkg.com/mapillary-js@4.1.2/dist/mapillary.css\" />",
                "lib/mapillary.css", css => $"<style>{css}</style>");
            html = await InlineLibraryAsync(html,
                "<script src=\"https://unpkg.com/leaflet@1.9.4/dist/leaflet.js\"></script>",
                "lib/leaflet.js", js => $"<script>{js}</script>");
            html = await InlineLibraryAsync(html,
                "__MVT_SRC_B64__", "lib/mvt.esm.js",
                js => Convert.ToBase64String(Encoding.UTF8.GetBytes(js)));

            GameView.Source = new HtmlWebViewSource
            {
                Html = html,
                BaseUrl = "https://localhost/"
            };
        }
        catch (Exception ex)
        {
            GameView.Source = new HtmlWebViewSource
            {
                Html = $"<html><body style='font-family:sans-serif;padding:24px;background:#0e1116;color:#fff'>" +
                       $"<h2>Failed to load game</h2><pre>{System.Net.WebUtility.HtmlEncode(ex.ToString())}</pre></body></html>"
            };
        }
    }

    /// <summary>
    /// Replaces <paramref name="marker"/> in the HTML with an inline copy of a packaged
    /// asset (transformed by <paramref name="wrap"/>). If the marker is absent or the
    /// asset can't be read, the HTML is returned unchanged so the original CDN reference
    /// remains in place.
    /// </summary>
    private static async Task<string> InlineLibraryAsync(
        string html, string marker, string assetPath, Func<string, string> wrap)
    {
        if (!html.Contains(marker)) return html;
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
            using var reader = new StreamReader(stream);
            var contents = await reader.ReadToEndAsync();
            return html.Replace(marker, wrap(contents));
        }
        catch
        {
            return html;   // asset missing -> leave the CDN tag/placeholder untouched
        }
    }

    // The page talks back to us via cancelled "geoclaudr://<action>?<query>" navigations.
    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Url) ||
            !e.Url.StartsWith("geoclaudr://", StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;   // never actually navigate; this is just a message channel

        try
        {
            var uri = new Uri(e.Url);
            var action = uri.Host;
            var p = ParseQuery(uri.Query);

            switch (action)
            {
                case "save":
                    if (p.TryGetValue("k", out var k) && p.TryGetValue("v", out var v))
                        Preferences.Default.Set(k, v);
                    break;

                case "keepawake":
                    DeviceDisplay.Current.KeepScreenOn =
                        p.TryGetValue("on", out var on) && on == "1";
                    break;

                case "share":
                    if (p.TryGetValue("text", out var text))
                        _ = Share.Default.RequestAsync(new ShareTextRequest { Text = text, Title = "geoClaudr" });
                    break;

                case "haptic":
                    try
                    {
                        var strong = p.TryGetValue("type", out var ht) && ht == "success";
                        HapticFeedback.Default.Perform(strong ? HapticFeedbackType.LongPress : HapticFeedbackType.Click);
                    }
                    catch { /* unsupported on this platform (e.g. desktop) */ }
                    break;
            }
        }
        catch { /* ignore malformed bridge messages */ }
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var d = new Dictionary<string, string>();
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = pair.IndexOf('=');
            if (i < 0) d[Uri.UnescapeDataString(pair)] = "";
            else d[Uri.UnescapeDataString(pair[..i])] = Uri.UnescapeDataString(pair[(i + 1)..]);
        }
        return d;
    }
}
