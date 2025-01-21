<div align="center">

# Infinite Friends

:tada::spider::spider::spider::spider::spider::tada:
<br />**Multiplayer beyond 4 people**
<br />A [ModWeaver](https://github.com/modweaver/modweaver) and [BepInEx 5](https://github.com/BepInEx/BepInEx) plugin for the game [SpiderHeck](https://store.steampowered.com/app/1329500/SpiderHeck)

### [Features](#features) • [Installation](#installation) • [Contributing](#contributing) • [Changelog](#changelog)

</div>


## Features

- [x] Allow more than 4 players to join a single lobby.
- [x] Removed player count limits on all game-modes.
- [x] Dynamic spawn point generation once the initial four are claimed.
- [x] (Partial) Improved UI scaling.
- [ ] Spawn a weapon next to each dynamic spawn.
- [ ] Online support?


## Installation

1. **Locate your SpiderHeck install directory**

   | `Steam library` -> `Right click SpiderHeck` -> `Manage` -> `Browse local files`. |
      |:--------------------------------------------------------------------------------:|
   | ![A visual guide to locating the SpiderHeck directory](../assets/locating_directory.png?raw=true) |

   On Windows, it will typically be located at `C:\Program Files (x86)\Steam\steamapps\common\SpiderHeck`.

> [!TIP]
> ModWeaver is the standard mod-loader for SpiderHeck, and is what you will probably need when using other mods.
> <br />If you're unsure which to install, go with ModWeaver. BepInEx 5 support is provided as an alternative/fallback.

> [!IMPORTANT]
> The [1.1.0 release](https://github.com/modweaver/modweaver/releases/tag/1.1.0) of **ModWeaver** is non-functional at time of writing; instead the [latest build](https://github.com/modweaver/modweaver/actions/runs/10702095842/artifacts/1891040653) should be installed,
> until a new ModWeaver release is officially published.
2. **Install ModWeaver or BepInEx**

    - **ModWeaver**

      Manually download the [latest release](https://github.com/modweaver/modweaver/releases/latest), and unzip it into your SpiderHeck directory.
      <br />Alternatively, follow the [instructions](https://docs.modweaver.org/users/installing-modweaver/) for using the ModWeaver installer.

    - **BepInEx**

      Follow the [instructions](https://docs.bepinex.dev/articles/user_guide/installation/index.html#installing-bepinex-1) for installing BepInEx 5.
      <br />When downloading BepInEx, choose the latest stable x64 release of BepInEx 5, which can be found [here](https://github.com/BepInEx/BepInEx/releases/latest).

3. **Install InfiniteFriends**

   Download the latest release for either [ModWeaver](https://github.com/Senyksia/InfiniteFriends/releases/latest/download/InfiniteFriends_ModWeaver.zip) or [BepInEx](https://github.com/Senyksia/InfiniteFriends/releases/latest/download/InfiniteFriends_BepInEx.zip), and unzip it into your SpiderHeck directory.


## Contributing

1. **Clone the latest source code**

   `git clone https://github.com/Senyksia/InfiniteFriends.git`

2. **Link to your SpiderHeck directory**

   Create a file called `InfiniteFriends.csproj.user` next to `InfiniteFriends.csproj`.
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <Project>
     <PropertyGroup>
       <GameDir>path\to\SpiderHeck</GameDir>                                                    <!-- User-defined absolute path to SpiderHeck -->
       <ReferencePath>$(ReferencePath);$(GameFolder)\SpiderHeckApp_Data\Managed</ReferencePath> <!-- Path to the SH game assemblies -->
       <AssemblySearchPaths>$(AssemblySearchPaths);$(ReferencePath)</AssemblySearchPaths>       <!-- Add that path to the assembly search list -->
     </PropertyGroup>
   </Project>
   ```
   Replace `path\to\SpiderHeck` with the absolute path to your SpiderHeck directory. E.g.
   ```xml
   <GameDir>C:\Program Files (x86)\Steam\steamapps\common\SpiderHeck</GameDir>
   ```

3. **Set up your SpiderHeck directory**

   Because we are building for multiple mod-loaders, your game directory will need to contain both of them (assuming you are building for both). This can mostly be done without conflict by simply installing both. Notably, however, each loader's `doorstop_config.ini` will need to be preserved. The Doorstop (`winhttp.dll`) version may also conflict; keeping the latest one should be sufficient.

   To launch with a particular loader, rename the dormant loader's `doorstop_config.ini` to something else, e.g. `doorstop_config.modweaver.ini`. This is a finicky solution, but as far as I can tell the `--doorstop-target-assembly` CLI parameter was broken in Doorstop 4, or I just can't get it to work.

5. **Compile**

   ModWeaver and BepInEx each have a build configuration.
   <br />Using Visual Studio or Rider, building will close the game and copy the compiled .dll directly into your mod folder.





## Changelog

See [CHANGELOG.md](https://github.com/Senyksia/InfiniteFriends/blob/main/CHANGELOG.md) for version changes.
