# Changelog

All notable changes to this project will be documented in this file.

## [0.0.0.5] - 2026-06-27

### Added
- **Support & Community Tab**: Added a dedicated Support & Community tab in the configuration window featuring direct "Join Support Discord" and "Support on Ko-fi" buttons.
- **Local Dev Icon Packaging**: Configured project build settings to automatically copy `icon.png` to local output directories (`bin/x64/Debug` and `bin/x64/Release`).

---

## [0.0.0.4] - 2026-06-11

### Added
- **Turns Mode Tab**: Added a new manual "taking turns" mode log layout.
  - Left-to-right flow with list style display.
  - Dynamically stretches vertically to utilize all available layout height.
  - Support for custom manual turns logging with vicinity player list dropdowns.
  - Verification that default log choices are labeled as Truths if separate tracking is disabled.
- **ToD Database Tab**: Moved the Truth & Dare inspiration database manager out of the Settings menu and placed it next to the Balancing tab for fast CRUD and roll operations.
- **Dice Mode Integration**: Configured automated dice roll announcements to automatically register in the Turn Log.
- **Round Count Integration**: Every recorded turn now increments the `RoundsParticipated` metric for all players in the game (and decrements it upon undo), keeping stats fully synchronized.
- **Next-Turn Highlight**: The name of the receiver in the latest turn log entry is highlighted with a bright gold/yellow color, providing a clear indicator of who asks next.
- **Taller UI Elements**: Vertical heights for **Record Turn** and **Undo Last Turn** buttons increased to `35f`.

### Fixed
- **Right-Click Edit & Removal Modals**: Fixed an issue where right-clicking turn entries or player names did not open the edit and delete confirmation popup modals due to ImGui ID stack nested scoping issues. Popups now open reliably at the tab's root ID stack level.
- **vicinity Dropdown Stability**: Comboboxes for selecting Givers and Receivers now bind to stable string name references instead of array indexes to prevent choice fluctuation when vicinity players move out of range.
- **Separated Tracking Visibility**: Truth/Dare selectors and indicators are now dynamically hidden when "Separate Truth/Dare Tracking" is disabled.
