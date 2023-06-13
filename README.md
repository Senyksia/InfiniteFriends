<div align="center">

# Infinite Friends

:tada::spider::spider::spider::spider::spider::tada:
<br />**Multiplayer beyond 4 people**
<br />A [BepInEx](https://github.com/BepInEx/BepInEx) plugin for the game [SpiderHeck](https://store.steampowered.com/app/1329500/SpiderHeck)

### [Installation](#installation) • [Contributing](#contributing) • [TODO](#todo) • [Changelog](#changelog)

</div>


## Installation

1. **Locate your SpiderHeck install directory**

   | `Steam library` -> `Right click SpiderHeck` -> `Manage` -> `Browse local files`. |
   |:--------------------------------------------------------------------------------:|
   | ![A visual guide to locating the SpiderHeck directory](../assets/locating_directory.png?raw=true) |

   On Windows, it will typically be located at `C:\Program Files (x86)\Steam\steamapps\common\SpiderHeck`.

2. **Install BepInEx**

   Follow the [instructions](https://docs.bepinex.dev/articles/user_guide/installation/index.html#installing-bepinex-1) for installing BepInEx 5.
   <br />When downloading BepInEx, choose the latest stable x64 release of BepInEx 5, which can be found [here](https://github.com/BepInEx/BepInEx/releases/latest).

3. **Install InfiniteFriends**

   Download the [latest release](https://github.com/Senyksia/InfiniteFriends/releases/latest/download/InfiniteFriends.zip), and unzip it into your SpiderHeck directory.


## Contributing

1. **Clone the latest source code**

   `git clone https://github.com/Senyksia/InfiniteFriends.git`

2. **Link to your SpiderHeck directory**

   Create a file called `InfiniteFriends.csproj.user` next to `InfiniteFriends.csproj`.
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <Project>
     <PropertyGroup>
       <GameFolder>path\to\SpiderHeck</GameFolder>                                              <!-- User-defined absolute path to SpiderHeck -->
       <ReferencePath>$(ReferencePath);$(GameFolder)\SpiderHeckApp_Data\Managed</ReferencePath> <!-- Path to the SH game assemblies -->
       <AssemblySearchPaths>$(AssemblySearchPaths);$(ReferencePath)</AssemblySearchPaths>       <!-- Add that path to the assembly search list -->
     </PropertyGroup>
   </Project>
   ```
   Replace `path\to\SpiderHeck` with the absolute path to your SpiderHeck directory. E.g.
   ```xml
   <GameFolder>C:\Program Files (x86)\Steam\steamapps\common\SpiderHeck</GameFolder>
   ```

3. **Compile**

   Using Visual Studio, the game will be closed, and a copy of the compiled .dll should be placed directly into your mod folder.


## TODO

- [x] Allow more than 4 players to join a single lobby.
- [x] Rewrite player spawning logic to scale fairly with more players.
- [ ] Fix UI not scaling past ~6 players (Scoreboard, Customisation Menu, etc).
- [ ] Add an option for spawning an initial weapon near each player, rather than a static 4.
- [ ] Allow the user to set MaxPlayerHardCap.
- [ ] Online?


## Changelog

See [CHANGELOG.md](https://github.com/Senyksia/InfiniteFriends/blob/main/CHANGELOG.md) for version changes.
