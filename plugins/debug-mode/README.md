
# Debug Mode

Enables debug functionalities that are disabled by default, allowing you to test and troubleshoot specific parts of the game.

Additionally, this plugin activates a "Dev Mode", which can be toggled by pressing the "Debug" keybinding (`F4` by default).
When enabled, will give you invincibility and also display extra information in top-left corner.

> [!TIP]
> Recommended to use the [`BepInEx.ConfigurationManager`](https://github.com/BepInEx/BepInEx.ConfigurationManager) plugin to easily manage debug settings.

There is also `G.m_debugCameras` but it doesn't seem to be used anywhere, so it is possible a remains of a scrapped debug feature.

## Configuration

### `[Debug]` `DrawAllBackgrounds`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugDrawAllBackgrounds`.

### `[Debug]` `Bullets`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugBullets`.

### `[Debug]` `Pathfinding`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugPF`.

### `[Debug]` `PathfindingDetails`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugPFDetails`.

### `[Debug]` `Collisions`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugCols`.

### `[Debug]` `Units`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugUnits`.

### `[Debug]` `UnitNetworkControl`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugUnitNetworkControl`.

### `[Debug]` `Defenses`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugDefenses`.

### `[Debug]` `Water`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugWater`.

### `[Debug]` `Light`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugLight`.

### `[Debug]` `Crashes`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugCrashes`.

### `[Debug]` `CrashesFull`

**Setting type:** `bool` \
**Default value:** `false`

Modifies `G.m_debugCrashesFull`. Note: lags the game a bunch.

### `[General]` `Enable`

**Setting type:** `bool` \
**Default value:** `true`

Enables the plugin.

### `[StartUp]` `IsEditor`

**Setting type:** `bool` \
**Default value:** `true`

Forces `Application.isEditor` to always return `true`, mimicking the game is running inside the Unity Editor.
- Increments the game's version build by 1.
- Exports the entire world map to `SavedScreen.png` in game's root path (where `DigOrDie.exe` is located).
- Speeds up credits by 10 times.
- Requests Steam global stats and logs them.

### `[StartUp]` `NoWorldPresimulation`

**Setting type:** `bool` \
**Default value:** `false`

Disables world presimulation (e.g. no initial water and plants are generated), making the world creating process a lot faster. Useful for quick world gen testing.

### `[StartUp]` `InterceptDebugRendering`

**Setting type:** `bool` \
**Default value:** `true`

Use custom drawer for `UnityEngine.Debug.DrawLine` method overloads. Note that without intercepting `Debug.DrawLine` calls they do basically nothing in release version of the game.
