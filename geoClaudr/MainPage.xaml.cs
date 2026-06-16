namespace geoClaudr;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        _ = LoadGameAsync();
    }

    /// <summary>
    /// Loads the self-contained HTML game from the packaged raw assets, injects
    /// the Mapillary token, and hands it to the WebView. Using a BaseUrl gives the
    /// page a real https origin so localStorage / CORS behave on Android &amp; iOS.
    /// </summary>
    private async Task LoadGameAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("game.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            html = html.Replace("__MAPILLARY_TOKEN__", Constants.MapillaryToken);

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
}
