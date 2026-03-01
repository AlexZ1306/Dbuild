# MiniCurrency / Notepads Technical Inventory and Refactor Plan

Date: 2026-02-25
Status: Non-destructive inventory completed (no feature removals, no behavior changes)

## Primary Workspace
- `C:\Users\zinal\Desktop\MiniCurrency_Final`

## Current Working Project
- Main app: `app\MiniCurrency-NotepadsBase\src\Notepads.sln`
- Reference source: `reference\Notepads-master`

## Safety Rules (active)
- Do not delete inherited Notepads features yet (they may be reused).
- Refactor in small slices only, preserving build/run behavior.
- Keep menu/settings support (theme/Acrylic/Mica, Full Screen, CompactOverlay/always-on-top mode).

## What Is Confirmed Working (already tested + code verified)
- MiniCurrency overlay mode is always enabled in main page (`IsMiniCurrencyMode => true`).
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:130`
- API exchange rates via `open.er-api.com`.
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:92`
- Local cache/settings via `ApplicationData.Current.LocalSettings`.
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:1295`
- Custom calculator-like keyboard input (replace-on-first-input behavior).
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:879`
- Custom drag/reorder rows and drag overlay UI.
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml:44`
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml:513`
- Flags loaded from `Assets/Flags` (`BitmapImage` + SVG fallback).
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:1166`
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:1189`
- Notepads menu stripped down in MiniCurrency mode, but settings/fullscreen/compact overlay remain.
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:1238`
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:1260`
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:1261`
- Full Screen mode retained.
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.ViewModes.cs:74`
- CompactOverlay mode retained (used as always-on-top style mode).
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.ViewModes.cs:46`
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.MainMenu.cs:35`
- Acrylic/theme infrastructure still present.
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Services\ThemeSettingsService.cs:280`
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Brushes\HostBackdropAcrylicBrush.cs:31`

## Current MainPage Code Layout (important for refactor)
Main page is split into partial classes, but MiniCurrency logic is still concentrated in the large file:
- `NotepadsMainPage.xaml.cs` (2119 lines) -> mixed core page lifecycle + most MiniCurrency logic + remaining Notepads flow
- `NotepadsMainPage.StatusBar.cs` (672 lines) -> status bar behavior (mostly legacy Notepads, with MiniCurrency hooks)
- `NotepadsMainPage.IO.cs` (301 lines) -> file I/O / print flows (legacy Notepads)
- `NotepadsMainPage.MainMenu.cs` (186 lines) -> menu wiring (includes CompactOverlay/FullScreen hooks)
- `NotepadsMainPage.ViewModes.cs` (107 lines) -> FullScreen + CompactOverlay mode transitions
- `NotepadsMainPage.Theme.cs`, `NotepadsMainPage.Notification.cs` -> smaller focused partials

## Refactor Seams Already Identified in `NotepadsMainPage.xaml.cs`
These are strong candidates to extract into one or more new partial files WITHOUT changing behavior:

### MiniCurrency state/config block
- Fields and constants start here:
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:91`
- Includes rates, visible/order/cache keys, active currency, drag state, status text.

### MiniCurrency initialization and overlay wiring
- `InitializeMiniCurrencyOverlay()`
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:175`
- Constructor calls MiniCurrency initialization before most Notepads setup.
  - `app\MiniCurrency-NotepadsBase\src\Notepads\Views\MainPage\NotepadsMainPage.xaml.cs:133`

### MiniCurrency input + row interactions (UI event handlers)
- Currency row/input handlers begin around:
  - `...NotepadsMainPage.xaml.cs:218` (focus)
  - `...NotepadsMainPage.xaml.cs:235` (tap)
  - `...NotepadsMainPage.xaml.cs:252` (drag start)
  - `...NotepadsMainPage.xaml.cs:879` (keyboard input mode)
  - `...NotepadsMainPage.xaml.cs:922` (text changed)

### MiniCurrency calculations / persistence / flags / shell adaptation
- Conversion logic: `...NotepadsMainPage.xaml.cs:939`
- Save values: `...NotepadsMainPage.xaml.cs:1065`
- Save row order: `...NotepadsMainPage.xaml.cs:1128`
- Flags init: `...NotepadsMainPage.xaml.cs:1166`
- Shell customizations (status bar/menu): `...NotepadsMainPage.xaml.cs:1204`
- MiniCurrency status bar mode: `...NotepadsMainPage.xaml.cs:1215`
- MiniCurrency main menu mode: `...NotepadsMainPage.xaml.cs:1238`

### MiniCurrency networking + cache
- Rates load / API + fallback cache:
  - `...NotepadsMainPage.xaml.cs:1447`

## Inherited Notepads Areas To Preserve For Now (do not remove)
These are still coupled to app shell and may be reused later:
- `Core\NotepadsCore.cs`, session management, activation services
- `Views\Settings\*` (settings UI pages)
- `Services\ThemeSettingsService.cs`, `AppSettingsService.cs`
- Main menu / status bar / view mode partials
- Text editor controls and dialogs (even if mostly hidden in MiniCurrency mode)

## Observed Coupling Risks (why we refactor gradually)
- `NotepadsMainPage.xaml.cs` mixes MiniCurrency logic with app lifecycle/session handling.
- Status bar partial still contains heavy TextEditor-specific logic, but MiniCurrency also writes into status indicators.
- Main menu customization for MiniCurrency depends on legacy menu item names/instances created by existing XAML.
- CompactOverlay/FullScreen behavior is shared infrastructure and must remain intact.

## Recommended Refactor Sequence (safe, behavior-preserving)
### Phase 1: Structural extraction only (no logic changes)
Goal: make code readable without changing runtime behavior.
- Add new partial file(s) under `Views\MainPage`, for example:
  - `NotepadsMainPage.MiniCurrency.State.cs`
  - `NotepadsMainPage.MiniCurrency.UI.cs`
  - `NotepadsMainPage.MiniCurrency.Rates.cs`
- Move MiniCurrency fields/constants and MiniCurrency methods out of `NotepadsMainPage.xaml.cs` into those partials.
- Keep method names and call sites unchanged.
- Keep `IsMiniCurrencyMode => true` unchanged for now.

### Phase 2: Encapsulation (still no deletions)
Goal: reduce cross-contamination between MiniCurrency and legacy Notepads code.
- Introduce a small internal helper/service class (e.g. `MiniCurrencyRatesService`) for:
  - API fetch
  - JSON parse/apply
  - cache read/write keys
- Introduce a small state model for row order/visible currencies if needed.
- Keep UI event handlers in page partials; move only pure logic first.

### Phase 3: Feature gates and isolation (no hard deletes yet)
Goal: make later cleanup safe and reversible.
- Group legacy Notepads-only initialization behind explicit checks/helpers.
- Add comments and boundaries for preserved-but-currently-unused paths.
- Optionally centralize all MiniCurrency setup in one `InitializeMiniCurrencyMode()` orchestration method.

### Phase 4: Deferred cleanup (only after repeated testing)
- Remove truly unused legacy paths only after proof they are not needed.
- Do this in separate focused steps with build/run verification after each step.

## First Low-Risk Refactor Slice (recommended next action)
1. Extract MiniCurrency fields/constants + `IsMiniCurrencyMode` into a new partial file.
2. Extract MiniCurrency rates/cache methods (`LoadMiniCurrencyRatesAsync`, cache helpers, JSON parsing).
3. Rebuild/test in VS 2022.

This gives immediate readability gains while keeping UI and event wiring intact.

## Notes
- `bin`/`obj` directories are present in the workspace because the project was built/tested. They are build artifacts, not source.
- No deletions were performed during this inventory stage.
