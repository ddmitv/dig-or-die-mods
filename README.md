
# Dig or Die Mods

Project for reverse engineering the game [Dig or Die](https://store.steampowered.com/app/315460/Dig_or_Die/).
It includes:
- BepInEx plugins to improve game's experience.
- Documentation of the internal structure of the game.
- Reverse engineering of world simulation algorithm.
- Tool for working with save files.

All available plugins can be found in `plugins/` folder.
Each plugin contains a `REAMDE.md` describing what it does.

Checkout the [Wiki page](https://github.com/NUCLEAR-BOMB/dig-or-die-mods/wiki) for documentation for game internals and other.

# Plugin Installation

Firstly, you need to install BepInEx, which is a patcher/plug-in framework.
You can follow instructions specified in [Automatic BepInEx Installation](#automatic-bepinex-installation) or [Manually Install BepInEx](#manual-bepinex-installation). Then [Download plugin .dll](#download-plugin-dll).

## Automatic BepInEx Installation

Open PowerShell.
1. **Start menu method:**
   - Right-click on the start menu.
   - Choose `Windows PowerShell` (for Windows 10) or `Terminal` (for Windows 11).
2. **Search and launch method:**
   - Press the Windows key.
   - Type `PowerShell` or `Terminal` (for Windows 11).
   - Left-click `Windows PowerShell` match to launch PowerShell.

### One-Command Installation (Recommended)

Copy the following command, paste it into PowerShell console and press `Enter`.
```powershell
irm "https://raw.githubusercontent.com/NUCLEAR-BOMB/dig-or-die-mods/main/Install-BepInEx.ps1" | iex
```

If you encountered an error, please follow provided instructions in error message, or, manually install BepInEx.

### Installation with Extra Options

| Option              | Description |
| ------------------- | ----------- |
| `-Console`          | Enable logging console on game startup. <br> Can be done manually after installation by changing `Enabled = false` -> `Enabled = true` in `[Logging.Console]` section in `Dig or Die/BepInEx/config/BepInEx.cfg` |
| `-InstallCfgMgr`    | Install plugin [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) |
| `-InstallMonoDebug` | Install debug Mono runtime (required when debugging with dnSpy) |

Add these parameters after the command and run it:
```powershell
&([scriptblock]::Create((irm "https://raw.githubusercontent.com/NUCLEAR-BOMB/dig-or-die-mods/main/Install-BepInEx.ps1")))
```

> [!TIP]
> Example:
> ```powershell
> &([scriptblock]::Create((irm "https://raw.githubusercontent.com/NUCLEAR-BOMB/dig-or-die-mods/main/Install-BepInEx.ps1"))) -Console -InstallCfgMgr
> ```

## Manual BepInEx Installation

You can follow the [official BepInEx installation guide](https://docs.bepinex.dev/articles/user_guide/installation/index.html),
but the following instructions are specialized for Dig or Die.

1. **Download BepInEx** \
   Get the **32-bit** (`x86`) Windows BepInEx **version 5.4.23 or newer** from the [BepInEx GitHub releases](https://github.com/BepInEx/BepInEx/releases).
> [!TIP]
> For example, the file name should look like `BepInEx_win_x86_5.4.23.3.zip`.

2. **Install BepInEx** \
   Extract the downloaded `.zip` contents into your game directory **where `DigOrDie.exe` is located**.
> [!TIP]
> In Steam: right-click the game, select `Manage >`, and left-click `Browse local files >`, it will open the directory where game's files are located.
>
> Optionally, back up your game by copying the entire `Dig Or Die` folder elsewhere before extracting.

> [!NOTE]
> After you extracted the archive, the game folder should contain files like `winhttp.dll`, `doorstop_config.ini` and a new folder `BepInEx` with original **`DigOrDie.exe`**. 

3. **First Launch** \
   Run the game **through Steam**. Directly running executable `DigOrDie.exe` won't work.
   
4. **Initial Setup** \
   Wait until the `BepInEx/config` folder and other ones appears, then close the game.
> [!NOTE]
> The game will always show black screen, don't wait until it starts.

5. **BepInEx Configuration** \
   Open `BepInEx/config/BepInEx.cfg` in text editor and find near the end:
   ```ini
   [Preloader.Entrypoint]

   ## The local filename of the assembly to target.
   # Setting type: String
   # Default value: UnityEngine.dll
   Assembly = UnityEngine.dll

   ## The name of the type in the entrypoint assembly to search for the entrypoint method.
   # Setting type: String
   # Default value: Application
   Type = Application

   ## The name of the method in the specified entrypoint assembly and type to hook and load Chainloader from.
   # Setting type: String
   # Default value: .cctor
   Method = .cctor
   ```
   Change `Type = Application` to `Type = MonoBehaviour`.

> [!TIP]
> Recommended to enable the console by locating and changing the following settings accordingly:
> ```ini
> [Logging.Console]
> 
> ## Enables showing a console for log output.
> # Setting type: Boolean
> # Default value: false
> Enabled = true
> ```
> After enabling, with the game start a console window with logging information will appear.

## Download plugin .dll

You can download these from [Github Releases](https://github.com/NUCLEAR-BOMB/dig-or-die-mods/releases) page (or compile them yourself, see [Building Plugins](#building-plugins)).

Place plugin `.dll` (e.g. `precise_clock.dll`) into `Dig or Die/BepInEx/plugins` folder.

Run the game **through Steam**. Plugins should now be active.

> [!NOTE]
> For troubleshooting:
> - Check `Dig or Die/DigOrDie_Data/output_log.txt`
> - Visit [BepInEx Troubleshooting](https://docs.bepinex.dev/articles/user_guide/troubleshooting.html) page.

## Plugin configuration

Many plugins have their configuration files you can edit under `BepInEx/config`.
The appropriate configuration file appears after the plugin that it's associated is used first time when starting a game.

To reset configuration to default delete the configuration file and next time the game is started the plugin will recreate it.

> [!NOTE]
> Usually, the plugin reads settings inside config file at startup. You need to modify them before the game is started, or restart the game to apply them.
>
> However, some plugins also can dynamically read them without need for restarting the game. It's usually said in plugin's documentation when it does that.

## Recommended plugins

- [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager). Easy plugin configuration editing via in-game GUI.
- [`BepInEx.Debug` Demystify Exceptions](https://github.com/BepInEx/BepInEx.Debug#demystifyexceptions). Basically clearer exception messages in logs.
- [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor). In-game inspector and debugging tools.

## Uninstallation

- To uninstall/disable specific plugin: \
  Delete its `.dll` from `BepInEx/plugins` folder.

- To fully uninstall BepInEx: \
  Delete these files/folders:
  - `BepInEx/`
  - `winhttp.dll`
  - `doorstop_config.ini`
  - `.doorstop_version`
  - `changelog.txt`
  
> [!NOTE]
> Recommended to also verify integrity of game files through Steam. \
> Right-click the game, left-click `Properties...`, find section `Installed Files`, click button `Verify integrity of game files`.

- To temporary disable mods: \
  Rename `winhttp.dll` to `winhttp.dll.off`. Rename back to re-enable.

# Building Plugins

## Prerequisites

- **Dig or Die** installed via Steam (default location: `C:\Program Files (x86)\Steam\steamapps\common\Dig or Die\`)
- **.NET SDK** (version supporting C# 12.0)
- [Git](https://github.com/git/git) (optional, you can directly download the `.zip` of project)

## Building the Project  

Clone the repository:
```bash
git clone https://github.com/NUCLEAR-BOMB/dig-or-die-mods.git
cd dig-or-die-mods
```

> [!IMPORTANT]
> If you have Dig or Die installed in non-default location (`C:\Program Files (x86)\Steam\steamapps\common\Dig or Die\`)
> please run `Generate-DevEnv.ps1` powershell script or manually create and fill `DevEnv.targets` file (see `DevEnv.targets.template`).

Build required plugins (run in project root):
```bash
dotnet build plugins/{plugin name}
```

If build successfully, they will be automatically copied to `BepInEx/plugins` in game folder.

# Modifying Save Files

Download `save-tool.exe` from [Github Releases](https://github.com/NUCLEAR-BOMB/dig-or-die-mods/releases) page (or compile it yourself).

The `save-tool` is a command line utility for compressing and decompressing save files.
You need to run it through command line. To list all available options use `--help`.

> [!CAUTION]
> Careless modification of save file could result in the file being corrupted.

> [!NOTE]
> Functionality of ImHex and `dod-save.hexpat` pattern are limited. Some parts of save files are read-only or unavailable.
> For example, you can't edit the parameters, add new elements to list and modify world data when using this method.

1. Install [ImHex](https://github.com/WerWolv/ImHex/blob/master/INSTALL.md) and read about [Pattern Editor](https://docs.werwolv.net/imhex/views/pattern-editor).

2. Find the needed `.save` file: \
   They are located in `C:\Users\%USERNAME%\Documents\Dig or Die\{your steam id}\Saves\{world name}\`.

3. Use `save-tool` to decompress a save file: \
   Run `save-tool.exe -d {path to .save file}`. The file with extension `.uncompressed-save` should appear where `.save` file is located.

4. Open the resulted `.uncompressed-save` file in ImHex.

5. Open or drag-and-drop the pattern file `dod-save.hexpat` in repository root folder into ImHex.

6. Execute pattern in **Pattern editor** view. 

7. Modify whatever data you want in **Pattern Data** view or directly in **Hex editor** and save it.

8. Use `save-tool` to compress back the modified uncompressed save file: \
   Run `save-tool.exe -c {path to .uncompressed-save file}`. This will create a backup of old save and replace the original `.save` file.

9. Open your save file in game.


