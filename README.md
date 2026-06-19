# geoClaudr

A cross-platform **GeoGuessr-style** game built with **.NET MAUI** (Windows, Android, iOS, macOS).
You get dropped at a random real-world street location, can look/move around, then drop a pin on
a world map to guess where you are — and get scored on how close you were.

## Features

- 🎯 **Random street-level drop** anywhere coverage exists. Locations are found from Mapillary's **CDN-cached coverage vector tiles** (with a bbox search as fallback), so drops are near-instant — and the next round is **prefetched** while you guess, so "Next round" is instant too.
- 🚶 **Move around** the panorama — look in every direction, zoom, and **click in any direction to move that way** (Street-View style, or **WASD** on desktop), with a cursor arrow that rotates to show where you'll go. A live **compass** shows where north is.
- 🗺️ **Guess map** (Leaflet) — drop & drag a pin on an expandable mini-map you can also **minimize to a floating button** (handy on phones). Choose your basemap: **Map**, **No-labels** hard mode (can't read place names), or **Satellite** (Esri imagery).
- 🛟 **Bad-spot handling** — if a panorama fails to load (dead/black image) it auto-rerolls to a new one, and there's a manual **"New spot"** button too.
- 🏆 **GeoGuessr-style scoring** — up to 5000 points/round with exponential distance decay, **scaled to the chosen region** (a near miss matters more in "Europe only" than in "World"); per-round result map with the line between your guess and the truth.
- 🌍 **Reverse-geocoded answers** — the result screen names the real place (e.g. *"Paris, Île-de-France, France 🇫🇷"*).
- ⚙️ **Game modes**: World / Europe / N. America / Asia, 3·5·10 rounds (or **∞ endless** — play with no fixed length and a live running average, ending the session whenever you like), one-tap **difficulty presets** (Easy / Normal / Hard) or fine-tune **Move / No-Move / NMPZ** + **time limit**, plus **km/mi** units and optional **synthesized sound effects**. A built-in **how-to-play** card greets first-time players, and the loading screen shows rotating **pro tips**.
- 🗓️ **Daily Challenge & shareable seeds** — a deterministic seed (today's date, or any code you type) reproduces the **same 5 locations** for everyone, so you can compare scores with friends. Daily play builds a **🔥 streak**. Settings are fixed (World · 5 rounds · Move) and the seed is shown/shared on the results screen.
- 📊 **Final scoreboard** with per-round breakdown — **tap any round to replay its result** — plus a **lifetime stats** screen (games/rounds, averages, bests, closest guess, **per-region average/best breakdown**), **6 unlockable achievements** with pop-up toasts, and a **recent-games** list.
- 🎉 **Juicy results** — the round score counts up and a **confetti burst** celebrates great guesses and new best totals. **Enter** submits a guess / advances; **W A S D** moves.
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

**Self-hosted libraries.** On native, `MainPage.xaml.cs` swaps the CDN `<link>`/`<script>` tags for inline
copies of **Leaflet** (JS + CSS), the **Mapillary CSS**, and the **MVT parser** (a self-contained
`lib/mvt.esm.js` bundle, base64-injected and imported from a blob URL) — packaged under
[`geoClaudr/Resources/Raw/lib`](geoClaudr/Resources/Raw/lib). Each swap is best-effort: if an asset is missing
the original CDN tag survives. Only `mapillary-js` itself stays on the CDN — it's too large for the Windows
WebView2 ~2 MB cap on string-loaded HTML. In a plain browser the tags are left untouched, so the dev preview
still loads everything from the CDN.

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

> **Seed reproducibility note:** seeded games line up because same-day Mapillary coverage tiles are stable.
> A seed replayed much later (after Mapillary updates coverage) may drift, and a client that can't load the
> MVT parser falls back to the bbox search, which yields a different set — so daily challenges match best
> when everyone plays the same day with the tile parser available.

## Ideas / not yet done

- **Fully self-host `mapillary-js`** — Leaflet, the Mapillary CSS and the MVT parser are now packaged locally
  (see "Self-hosted libraries" above), but the large `mapillary-js` bundle still loads from the CDN because it
  would blow the Windows WebView2 ~2 MB cap on string-loaded HTML. Removing that last dependency needs a
  different asset-loading approach (e.g. serving `Resources/Raw` from a `file://`/virtual-host origin).
