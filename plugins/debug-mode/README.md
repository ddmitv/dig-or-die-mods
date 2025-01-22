
# Debug Mode

Plugin that enables some functionalities that disabled by default and meant to be used to debug some parts of the game.

You need to manually enable features in the config file to use them.

The plugin also enables the "Dev Mode", that you can activate by pressing the "Debug" keybinding (`F4` by default).
It will give you invincibility and also display extra information.

> [!TIP]
> The config will automatically reload if you change it even if you are in the game. You can use external plugins like `BepInEx.ConfigurationManager` to manage these settings while playing the game.

## List of available parameters

- drawAllBackgrounds (`G.m_debugDrawAllBackgrounds`)
- bullets (`G.m_debugBullets`)
- pathfinding (`G.m_debugPF`)
- pathfindingDetails (`G.m_debugPFDetails`)
- collisions (`G.m_debugCols`)
- units (`G.m_debugUnits`)
- unitNetworkControl (`G.m_debugUnitNetworkControl`)
- defenses (`G.m_debugDefenses`)
- water (`G.m_debugWater`)
- light (`G.m_debugLight`)
- crashes (`G.m_debugCrashes`)
- crashesFull (`G.m_debugCrashesFull`)

There is also `G.m_debugCameras` but it doesn't seem to be used anywhere, possible it is a remains of a scrapped debug feature.

> [!NOTE]
> When I was testing them, only a few actually worked, don't know why.
> If you need the description of them, you can read them names or find instances of them being used in the game.
