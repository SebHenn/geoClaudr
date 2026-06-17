# geoClaudr

A cross-platform **GeoGuessr-style** game built with **.NET MAUI** (Windows, Android, iOS, macOS).
You get dropped at a random real-world street location, can look/move around, then drop a pin on
a world map to guess where you are — and get scored on how close you were.

## Features

- 🎯 **Random street-level drop** anywhere coverage exists. Locations are found from Mapillary's **CDN-cached coverage vector tiles** (with a bbox search as fallback), so drops are near-instant — and the next round is **prefetched** while you guess, so "Next round" is instant too.
- 🚶 **Move around** the panorama — look in every direction, zoom, and **click in any direction to move that way** (Street-View style, or **WASD** on desktop), with a cursor arrow that rotates to show where you'll go. A live **compass** shows where north is.
- 🗺️ **Guess map** (Leaflet + OpenStreetMap/CARTO) — drop & drag a pin on an expandable mini-map you can also **minimize to a floating button** (handy on phones), with an optional **no-labels hard mode** so you can't read place names off it.
- 🛟 **Bad-spot handling** — if a panorama fails to load (dead/black image) it auto-rerolls to a new one, and there's a manual **"New spot"** button too.
- 🏆 **GeoGuessr-style scoring** — up to 5000 points/round with exponential distance decay, **scaled to the chosen region** (a near miss matters more in "Europe only" than in "World"); per-round result map with the line between your guess and the truth.
- 🌍 **Reverse-geocoded answers** — the result screen names the real place (e.g. *"Paris, Île-de-France, France 🇫🇷"*).
- ⚙️ **Game modes**: World / Europe / N. America / Asia, 3·5·10 rounds, one-tap **difficulty presets** (Easy / Normal / Hard) or fine-tune **Move / No-Move / NMPZ** + **time limit**, plus **km/mi** units. A built-in **how-to-play** card greets first-time players.
- 📊 **Final scoreboard** with per-round breakdown — **tap any round to replay its result** — plus a **lifetime stats** screen (games & rounds played, average score, best game, best round, closest guess ever).
- 💾 **Persistent** token, best score, lifetime stats, and chosen settings — saved natively via MAUI `Preferences` (reliable on Windows too, not just `localStorage`). Enter your Mapillary token once and it's remembered.
- 📲 **Native touches**: share your result to the OS share sheet, **haptic feedback** on guesses, and the screen stays awake while you play.
- 📱 Identical experience on **desktop and mobile** (the game is a self-contained web app hosted in a MAUI `WebView`).

## Free APIs used (no billing, no credit card)

| Purpose | Service | Notes |
|---|---|---|
| Street-level imagery + navigation | **[Mapillary](https://www.mapillary.com)** (`mapillary-js`) | Free; needs a free client token (see below). The free, keyless alternative to Google Street View. |
| Finding covered locations | **Mapillary coverage vector tiles** | CDN-cached; parsed in-browser with `@mapbox/vector-tile` + `pbf`. |
| Guessing / result map | **Leaflet** + **OpenStreetMap / CARTO** tiles | No key required. |
| Reverse geocoding (place names) | **[BigDataCloud](https://www.bigdatacloud.com)** client API | Free, keyless, CORS-friendly. |
| Distance & scoring | Haversine + exponential decay | Computed locally. |

## Setup — get your free Mapillary token (~2 min)

Google Street View is **not** free/keyless, so this uses Mapillary instead. You need a free token:

1. Create an account at **https://www.mapillary.com**
2. Open the **[Developers dashboard](https://www.mapillary.com/dashboard/developers)**
3. Click **Register Application**, give it any name, and create it
4. Copy the **Client token** (it starts with `MLY|`)

Then either:

- **Easiest:** run the app and paste the token into the box on the start screen (it's saved on the device), **or**
- **In code:** set it once in [`Constants.cs`](geoClaudr/Constants.cs) → `DefaultMapillaryToken`.

## Run it

```powershell
# Windows desktop
dotnet build geoClaudr/geoClaudr.csproj -f net9.0-windows10.0.19041.0
dotnet run   --project geoClaudr/geoClaudr.csproj -f net9.0-windows10.0.19041.0

# Android (device/emulator attached)
dotnet build geoClaudr/geoClaudr.csproj -f net9.0-android -t:Run
```

Or just open `geoClaudr.sln` in Visual Studio and pick a target.

## How it's built

- The whole game (street view, maps, scoring, all screens) lives in one self-contained file:
  [`geoClaudr/Resources/Raw/game.html`](geoClaudr/Resources/Raw/game.html) — HTML/CSS/JS using `mapillary-js` + Leaflet.
- [`MainPage.xaml`](geoClaudr/MainPage.xaml) is a full-screen `WebView`; [`MainPage.xaml.cs`](geoClaudr/MainPage.xaml.cs)
  loads the HTML from packaged assets, injects the Mapillary token, and gives it a real `https` base URL.

**Finding a location (fast path).** `findRandomLocation()` fetches a few z14 Mapillary coverage tiles in
parallel, parses them in-browser (the MVT parser is loaded on demand via dynamic `import()`), and takes the
first tile with imagery (`Promise.any`). If the parser can't load or tiles come back empty, it falls back to
the `/images` bbox search — so it's never worse than before.

**Native bridge.** Persistence is split for reliability:
- **Reads (load):** C# injects every saved value (token, best score, settings, stats) straight into the HTML
  as a base64 blob via string replacement — so a fresh launch never depends on a runtime JS round-trip.
- **Writes (in-game):** the page sends `save`/`share`/`keepawake` commands to C# as cancelled
  `geoclaudr://…` navigations (intercepted in `WebView.Navigating`), which write to MAUI `Preferences`, open
  the OS share sheet, or toggle keep-screen-awake.

In a plain browser the bridge is a no-op and the game falls back to `localStorage`, which is why you can also
iterate on the UI without building MAUI:

> `python -m http.server 8777 --directory geoClaudr/Resources/Raw` then open `http://localhost:8777/game.html`

This keeps behaviour pixel-identical across PC and mobile and avoids per-platform map SDKs.

## Ideas / not yet done

- **Self-host the JS libraries** (Leaflet, mapillary-js, MVT parser) as packaged assets to remove the runtime
  CDN dependency — needs an asset-loading change since the HTML is currently loaded as a string.
- **Daily challenge / shareable seeds** — deterministic rounds so friends can compare the same locations.
- **Map-size-aware scoring** — score tighter in region-limited modes (e.g. Europe-only).
