# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

geoClaudr is a GeoGuessr-style cross-platform game. The entire game (street view, maps, scoring, all screens) lives in one self-contained file: `geoClaudr/Resources/Raw/game.html`. The MAUI shell is intentionally thin — `MainPage.xaml` is just a full-screen `WebView` and `MainPage.xaml.cs` handles loading and the native bridge.

## Build & run

```powershell
# Windows desktop
dotnet build geoClaudr/geoClaudr.csproj -f net9.0-windows10.0.19041.0
dotnet run --project geoClaudr/geoClaudr.csproj -f net9.0-windows10.0.19041.0

# Android (device or emulator attached)
dotnet build geoClaudr/geoClaudr.csproj -f net9.0-android -t:Run
```

Or open `geoClaudr.sln` in Visual Studio and pick a target.

**Fastest iteration on the game UI** — no MAUI build needed:
```powershell
python -m http.server 8777 --directory geoClaudr/Resources/Raw
# then open http://localhost:8777/game.html
```
In this mode the native bridge is a no-op and the game falls back to `localStorage`.

## Architecture

### Native → web (startup injection)
`MainPage.LoadGameAsync()` reads `game.html` as a string and does several literal `String.Replace` substitutions before setting it as the `WebView` source:
- `__MAPILLARY_TOKEN__` → the saved/default token from `Constants.MapillaryToken`
- `__NATIVE_FLAG__` → `"1"` (tells JS it's running inside MAUI)
- `__NATIVE_DATA_B64__` → base64-encoded JSON of the persisted values listed in the `PersistedKeys` array
- `__MVT_SRC_B64__` and the library tag swaps (see below)

This means persistence never depends on a runtime JS round-trip; every value is available immediately when the page loads.

**Dev-preview parity rule (important):** every injected placeholder must be *browser-safe when left unreplaced*, because the same `game.html` is opened directly in a browser for UI iteration (no C# pass). Existing patterns: `'__NATIVE_FLAG__'==='1'` is `false`, `atob('__NATIVE_DATA_B64__')` is wrapped in try/catch, and the MVT check uses a deliberately split string (`"__MVT_"+"SRC_B64__"`) so C# never matches it. Preserve this when adding placeholders.

`LoadGameAsync` then **inlines the small JS libraries** (`InlineLibraryAsync`): it swaps the CDN `<link>`/`<script>` tags for inline copies of Leaflet (JS+CSS), the Mapillary CSS, and the MVT parser (`lib/mvt.esm.js`, base64-injected into `__MVT_SRC_B64__` and imported from a blob URL in `ensureMvt`). These live in `Resources/Raw/lib/`. Each swap is best-effort — a missing asset leaves the CDN tag in place. `mapillary-js` itself stays on the CDN (too big for WebView2's ~2 MB `NavigateToString` limit). When `game.html` is opened directly in a browser (dev preview), none of the tags are replaced, so everything loads from the CDN — preserve this when editing the tag/placeholder strings.

### Web → native (bridge)
JS sends commands to C# by navigating to cancelled `geoclaudr://<action>?<params>` URLs (queued in JS so a burst doesn't clobber itself), intercepted in `WebView.Navigating`. Supported actions in `OnNavigating`:
- `save?k=<key>&v=<value>` — writes to `Preferences`
- `keepawake?on=1|0` — toggles `DeviceDisplay.KeepScreenOn`
- `share?text=<text>` — opens the OS share sheet
- `haptic?type=success|click` — triggers haptic feedback

### Persistence keys (gotcha)
`store.set(key, …)` in JS persists *any* key to `Preferences` on write (via the `save` bridge command). But on the **next launch only the keys listed in the `PersistedKeys` array** (`MainPage.xaml.cs`) are re-injected into `window.__nativeData`. So **when you add a new persisted value that must survive a native restart, add its key to `PersistedKeys`** — otherwise it silently falls back to WebView `localStorage`, which is unreliable on native. Per-region stats are stored as a single JSON blob under `st_regions` for exactly this reason (one key to register). Note: several older keys (`ach_*`, `recent_games`, `daily_*`, `opt_units/basemap/sound`, `seen_help`) are *not* in `PersistedKeys` yet — a known gap.

### Seed / Daily determinism
Daily Challenge and shared seeds reproduce the same locations via a seeded PRNG (`hashSeed` + `makeRng`) that probes locations **sequentially** (no racing) with a constant number of `rng()` draws per attempt, so two clients stay in lockstep. This only lines up because same-day Mapillary coverage tiles are stable and both clients have the MVT parser available — keep both invariants if you touch `findSeededLocation`.

### Mapillary token
`Constants.cs` holds `DefaultMapillaryToken` (compile-time placeholder). `Constants.MapillaryToken` (runtime property) checks `Preferences` first, falling back to the compile-time value. The app prompts the user for a token on first launch; it's then stored in `Preferences` under `"mapillary_token"`.

### Free APIs (no billing)
- **Mapillary** (`mapillary-js`) — street-level imagery and navigation
- **Mapillary coverage vector tiles** — finding covered locations (MVT parsed in-browser with `@mapbox/vector-tile` + `pbf`)
- **Leaflet + OpenStreetMap/CARTO** — guess and result maps
- **BigDataCloud** — reverse geocoding (keyless, CORS-friendly)

## Workflow: commit & push after important changes

After completing any important change (a feature, a bug fix, or any change the user would want preserved), **commit and push it** without waiting to be asked again — this instruction is the standing authorization to do so for this repo:

```powershell
git add -A
git commit -m "<concise description of the change>"
git push
```

- Group related edits into one logical commit with a clear message; don't commit half-finished or broken work (build/verify first).
- If on the default branch and the change is substantial, prefer creating a feature branch and pushing that.
- End commit messages with the `Co-Authored-By: Claude ...` trailer.
