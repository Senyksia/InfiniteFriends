# Changelog

Based on [Keep a Changelog](https://keepachangelog.com)!

## [Unreleased]
### BREAKING!
- Downgraded from BepInHecks/BepInEx 6, to BepInEx 5 and [ModWeaver](https://github.com/modweaver/modweaver).
> [!IMPORTANT]
> When updating from 0.2.1, either BepInEx 5 or ModWeaver will need to be installed.
> To uninstall BepInHecks: remove the `BepInEx` folder found in your SpiderHeck install directory,
> and install either BepInEx 5 or ModWeaver, overwriting any files needed. Note that this will also remove any old mods
> or configuration files. See [README.md](https://github.com/Senyksia/InfiniteFriends/blob/main/README.md) for updated or more detailed information.

> [!NOTE]
> The [1.1.0 release](https://github.com/modweaver/modweaver/releases/tag/1.1.0) of **ModWeaver** is non-functional at time of writing; instead the [latest build](https://github.com/modweaver/modweaver/actions/runs/10702095842/artifacts/1891040653) should be installed,
> until a new ModWeaver release is officially published.
### Added
- Support for more players in the customization menu.
- Support for more players on the round scoreboard.

## [0.2.1] - 2023-04-21

### Added

-   Parkour, tiers, and the map editor, can now be joined with more than one player.

### Fixed

-   Crash when trying to load parkour or the map editor.
-   Crash when trying to load the map Skulls.
-   Zero-gravity levels now follow airborne spawning logic by default (same as the lobby).

## [0.2.0] - 2023-04-14

### Added

-   Improved the spawn point generation logic.
-   Spawns are now distributed among platforms, prioritising those with a greater area and fewer spawns.

## [0.1.1] - 2022-10-09

### Fixed

-   Upgraded BepInEx to [BepInHecks](https://github.com/cobwebsh/BepInEx)

## [0.1.0] - 2022-10-09

### Added

-   Patched the local multiplayer to remove the player limit of four. Online is currently untested, and may or may not work.
-   Extended the default spawn locations with some basic logic.

[Unreleased]: https://github.com/Senyksia/InfiniteFriends/compare/0.2.1...HEAD

[0.2.1]: https://github.com/Senyksia/InfiniteFriends/compare/0.2.0...0.2.1

[0.2.0]: https://github.com/Senyksia/InfiniteFriends/compare/0.1.1...0.2.0

[0.1.1]: https://github.com/Senyksia/InfiniteFriends/compare/0.1.0...0.1.1

[0.1.0]: https://github.com/Senyksia/InfiniteFriends/releases/tag/v0.1.0
