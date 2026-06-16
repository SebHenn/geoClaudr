# geoClaudr

A cross-platform **GeoGuessr-style** game built with **.NET MAUI** (Windows, Android, iOS, macOS).
You get dropped at a random real-world street location, can look/move around, then drop a pin on
a world map to guess where you are — and get scored on how close you were.

## Features

- 🎯 **Random street-level drop** anywhere coverage exists, with a curated set of well-covered cities so rounds load fast.
- 🚶 **Move around** the panorama, look in every direction, and zoom (just like Street View).
- 🗺️ **Guess map** (Leaflet + OpenStreetMap) — drop & drag a pin, expandable mini-map.
- 🏆 **GeoGuessr-style scoring** — up to 5000 points/round with exponential distance decay; per-round result map with the line between your guess and the truth.
- ⚙️ **Game modes**: World / Europe / N. America / Asia, 3·5·10 rounds, **Move / No-Move / NMPZ**, optional **time limit** (1 or 2 min, auto-submits).
- 📊 Final scoreboard with per-round breakdown and a persisted **best score**.
- 📱 Identical experience on **desktop and mobile** (the game is a self-contained web app hosted in a MAUI `WebView`).

## Free APIs used (no billing, no credit card)

| Purpose | Service | Notes |
|---|---|---|
| Street-level imagery + navigation | **[Mapillary](https://www.mapillary.com)** (`mapillary-js`) | Free; needs a free client token (see below). The free, keyless alternative to Google Street View. |
| Guessing / result map | **Leaflet** + **OpenStreetMap / CARTO** tiles | No key required. |
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

> Tip: you can preview/iterate on the game UI without building MAUI by serving the HTML directly —
> `python -m http.server 8777 --directory geoClaudr/Resources/Raw` then open `http://localhost:8777/game.html`.

## How it's built

- The whole game (street view, map, scoring, screens) lives in one self-contained file:
  [`geoClaudr/Resources/Raw/game.html`](geoClaudr/Resources/Raw/game.html) — HTML/CSS/JS using `mapillary-js` + Leaflet.
- [`MainPage.xaml`](geoClaudr/MainPage.xaml) is a full-screen `WebView`; [`MainPage.xaml.cs`](geoClaudr/MainPage.xaml.cs)
  loads the HTML from packaged assets, injects the Mapillary token, and gives it a real `https` base URL
  (so `localStorage`/CORS behave on Android & iOS).

This keeps behaviour pixel-identical across PC and mobile and avoids per-platform map SDKs.
