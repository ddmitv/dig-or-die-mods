# DODModAPI Example Mod

Example plugin demonstrating how to use `DODModAPI` to add custom content to Dig or Die.

This mod is intended as a reference/template for plugin developers. It shows how to register custom assets, items, recipes, units, events, game modes, save data, network messages, UI screens, chat commands, chat preprocessors, localization strings, and Harmony transpiler helpers.

See the source code [`Plugin.cs`](./Plugin.cs) for more info. It contains a lot of comments explaining the reasoning and examples of `DODModAPI`.

> [!IMPORTANT]
> This plugin is primarily intended for developers reading the source code. It is not designed as a regular gameplay mod.

## Features

### Custom assets

This plugin uses the **AssetPacker** MSBuild task to generate the `ExampleAssets` class from `assets/sprites.cfg`. See [`example-mod.csproj`](./example-mod.csproj) for an example of how to use it.

Registered textures:

| Generated asset                                | Description                                                        |
| ---------------------------------------------- | ------------------------------------------------------------------ |
| `ExampleAssets.TileSpritesheetResource`        | Shared tile spritesheet generated from `TILE` directives           |
| `ExampleAssets.SpritesAtlasResource`           | Shared sprite atlas generated from `SPRITE` directives             |
| `ExampleAssets.SurfaceTopsResource`            | Shared surface top spritesheet generated from `SURFACE` directives |
| `ExampleAssets.exampleSurface_surfaceMaterial` | Custom surface material texture                                    |
| `ExampleAssets.UnitsAtlasResource_100`         | Unit animation atlas for 100x100 unit sprites                      |

See [`assets/sprites.cfg`](./assets/sprites.cfg) for asset configuration.

---

### Custom items

The plugin adds several example items.

| Item | Item type | Description | Recipe group | Ingredients |
| --- | --- | --- | --- | --- |
| "Example Wall" | `CItem_Wall` | A very sturdy wall added via DODModAPI | `EXAMPLE GROUP` | `GItems.iron` x10, `GItems.coal` x5 <br/> outputs 2 |
| "Example Device" | `CItem_Device` | An example passive device that glows like a flashlight | `MK III` | `GItems.flashLightMK2` x1, `GItems.gold` x5 |
| "Example Surface" | `CItem_Mineral` | Description of example surface | `MK III` | `GItems.dirt` x1 |

The custom recipe group `EXAMPLE GROUP` is also registered and assigned to `Auto-Builder MK I`.

---

### Custom unit

Adds `Example Monster`, a custom monster unit descriptor. Use `/example-command spawn <UNIT-POSITION>` to spawn `Example Monster`.

| Property                | Value                                       |
| ----------------------- | ------------------------------------------- |
| Code name               | `exampleMonster`                            |
| Tier                    | 3                                           |
| Speed                   | 4.5                                         |
| Size                    | 1.2 x 1.2                                   |
| Max HP                  | 250                                         |
| Armor                   | 10                                          |
| Attack range            | 10                                          |
| Damage                  | 5                                           |
| Attacks per shot        | 2                                           |
| Attack cooldown         | 1 second                                    |
| Attack knockback own    | 10 cells/s                                  |
| Attack knockback target | 10 cells/s                                  |
| Loot                    | `GItems.gold` 50%, `GItems.lavaFlower` 100% |
| Sprite tiles            | `ExampleAssets.exampleUnit`                 |

---

### Custom event

Adds `Example Event`, a custom hazard environment (event). Can be triggered with the `/example-command event` command (see below).

| Property | Value           |
| -------- | --------------- |
| ID       | `exampleEvent`  |
| Name     | `Example Event` |
| Duration | 10 seconds      |

- On start:
  - Prints a chat message.
  - Adds a large amount of water to the player's current cell.
- While active:
  - Replaces the cell under the mouse cursor with diamonds.
  - Sets the background of that cell to lava background.
- On end:
  - Prints a chat message.
  - Sets the clock to midday.

---

### Custom game mode

Adds `Example Mode (DODModAPI)`. This is a simple example mode based on `CModeSolo`.

- Generates a basic dirt world using a surface height line.
- Sets player spawn and ship positions near the center of the world.
- Disables monster spawning, rain, and events.
- Gives the player `Miniaturizor MK I` on new game.

> [!NOTE]
> The example mode returns `null` from `GetMonstersList` and disables monster spawning to avoid issues with empty monster lists.

---

### Custom save handler

Demonstrates how to store custom mod data inside save files using `DODModAPI.SaveManager`.

| Property        | Value         |
| --------------- | ------------- |
| Mod save ID     | `example_mod` |
| Current version | 1             |

- When saving:
  - Writes a random `float` value (`UnityEngine.Random.value`).
  - Has a 10% chance to skip writing save data entirely.
- When loading:
  - If the saved version is `2`, reads an `int`.
  - Otherwise, reads a `float`.

---

### Custom network messages

Demonstrates how to register and use custom network messages. Can be sent via the `/example-command network <Simple|Dynamic>` command (see below).

- `ExampleNetworkMessage`
  - Automatically assigned message ID.
  - Fixed body size of 4 bytes.
  - Sends a float value.
  - Logs the received value.

- `ExampleDynamicNetworkMessage`
  - Uses fixed message ID `50`.
  - Dynamic body size.
  - Sends an array of integers.
  - On receive, calculates and prints the sum of all received values.

---

### Custom screen

Adds `ExampleScreen`, a simple modal screen. It contains a background (`bmpBack`) and exit button (`btBack`).
Can be opened using the `/example-command screen` command (see below).

---

### Custom chat command

Registers the `/example-command` chat command.

The command is local, disables achievements when used and supports tab completion for the first argument.
It contains several subcommands: `spawn`, `heal`, `event`, `network`, `screen`.

---

### Custom chat preprocessor

Registers a chat preprocessor (priority `10`) that replaces `:)` with `★` and blocks messages containing `:(`.

---

### Example Harmony patch

Demonstrates using `DODModAPI.CodeCursor` to patch `CUnitPlayerLocal.Update`. `DODModAPI.CodeCursor` is a replacement for `Harmony.CodeMatcher` since `CodeMatcher`'s API is hard to use, while `CodeCursor` tries to fix all common issues with it.

The example patch roughly doubles the player's jump by replacing jump force values of `7` with `14`.

---

## Commands

### `/example-command`

```
/example-command spawn <UNIT-POSITION>
/example-command heal
/example-command event
/example-command network <Simple|Dynamic>
/example-command screen
```

1. Spawns `Example Monster` at the given world position.
2. Sets the executing player's health to 100 HP.
3. Triggers `Example Event` after a 5 second delay.
4. Sends an example network message.
   - `Simple` sends a fixed-length message.
   - `Dynamic` sends a dynamic-length message containing several integers.
5. Opens the example modal screen.

> [!NOTE]
> This command is registered as a local command, so it executes only on the local client.

**Examples:**

```
/example-command spawn 600 700
```
> Spawns Example Monster at world coordinates (600, 700).

```
/example-command spawn ~ ~10
```
> Spawns Example Monster 10 cells above the player.

```
/example-command heal
```
> Sets your health to 100 HP.

```
/example-command event
```
> Triggers `Example Event` after 5 seconds (`ExampleEvent`).

```
/example-command network Simple
```
> Sends the fixed-length example network message (`ExampleNetworkMessage`).

```
/example-command network Dynamic
```
> Sends the dynamic-length example network message (`ExampleDynamicNetworkMessage`).

```
/example-command screen
```
> Opens the example screen (`ExampleScreen`).

---

## Localization

The plugin demonstrates adding custom localization strings.

Registered localization strings:

| ID              | Text                          |
| --------------- | ----------------------------- |
| `EXAMPLE_HELLO` | `Hello from the Example Mod!` |

Item and unit display names are also registered through DODModAPI.