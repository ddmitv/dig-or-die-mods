
# Better Chat

A plugin that expands the list of available commands (chat messages starting with `/`) and adds dynamic expression evaluator built-in game's chat.

- [Better Chat](#better-chat)
- [Expression Evaluator](#expression-evaluator)
  - [Syntax](#syntax)
  - [Types](#types)
  - [Operators](#operators)
  - [Functions](#functions)
  - [Constants](#constants)
- [Command Argument Specification](#command-argument-specification)
  - [Relative coordinates](#relative-coordinates)
  - [Cursor coordinates](#cursor-coordinates)
  - [Format language specifications](#format-language-specifications)
  - [`<UNIT-POSITION>`](#unit-position)
  - [`<CELL-POSITION>`](#cell-position)
  - [`<ITEM>`](#item)
  - [`<INT>`](#int)
  - [`<STRING>`](#string)
  - [`<FLOAT>`](#float)
  - [`<CELL-PARAMS>`](#cell-params)
  - [`<CELL-ITEM>`](#cell-item)
  - [`<CELL>`](#cell)
  - [`<UNIT>`](#unit)
  - [`<PLAYER>`](#player)
- [Commands](#commands)
  - [`/tp`](#tp)
  - [`/give`](#give)
  - [`/place`](#place)
  - [`/fill`](#fill)
  - [`/killinfo`](#killinfo)
  - [`/spawn`](#spawn)
  - [`/clearinventory`](#clearinventory)
  - [`/clearpickups`](#clearpickups)
  - [`/clone`](#clone)
  - [`/replace`](#replace)
  - [`/freecam`](#freecam)
  - [`/exportpng`](#exportpng)
  - [`/clock`](#clock)
- [Configuration](#configuration)
    - [`[General]` `RepeatLastCommand`](#general-repeatlastcommand)
    - [`[ChatExpressionEvaluator]` `Enable`](#chatexpressionevaluator-enable)
    - [`[ChatExpressionEvaluator]` `Prefix`](#chatexpressionevaluator-prefix)
    - [`[General]` `FullChatHistory`](#general-fullchathistory)
    - [`[General]` `DisableAchievementsOnCommand`](#general-disableachievementsoncommand)

# Expression Evaluator

The expression evaluator allows players to evaluate mathematical expressions and access game data directly in the chat using a special syntax.

Expressions start with the `$` prefix (configurable) followed by an expression enclosed in parentheses (`(` and `)`).
To display both the expression and its result, use `$=`.

**Examples:**

| Input                 | Output                    |
| --------------------- | ------------------------- |
| `$(1 + 2)`            | `3`                       |
| `$(2 + 2 * 3)`        | `8`                       |
| `$(2 / 4)`            | `0.5`                     |
| `$(unit("hound").hp)` | `10`                      |
| `$({1, 2} + {3, 4})`  | `{4,6}`                   |
| `$(\|{3, 4}\|)`       | `5`                       |
| `$=(2 + 2* 3)`        | `2+2*3=8`                 |
| `$=(2 ^ 4)`           | `2^4=16`                  |
| `$(3 * (2 + 3))`      | `15`                      |
| `$(300 % 7 + 2 ^ 3)`  | `14`                      |
| `$(sin(pi/2))`        | `1`                       |
| `$(pi*e)`             | `8.53973422`              |
| `$(item(#30).name)`   | `Rebreather`              |
| `$(cellat(^)).water`  | (Water at mouse position) |

## Syntax

```ebnf
expression = term, { ("+" | "-"), term } ;
term = unary, { ("*" | "/" | "%"), unary } ;
unary = ("+" | "-"), unary 
       | exponent ;
exponent = field_access, [ "^", unary ] ;
field_access = primary, { ".", IDENTIFIER } ;

primary = 
    VALUE
  | IDENTIFIER, [ "(", [ argument_list ], ")" ]
  | "(", expression, ")"
  | "{", expression, ",", expression, "}"
  | "|", expression, "|"
  | "~" ;

argument_list = expression, { ",", expression } ;
```

## Types

- `integer`: whole numbers (`123`, `-456`, `0b1010`, `0xFF`). Internally uses 64-bit signed integer (C#'s `long`/`System.Int64`).
- `float`: decimal numbers (`3.14`, `-0.5`, `6.02e23`). Internally uses double-precision floating point (C#'s `double`/`System.Double`).
- `ID`: identifier of unit/item. Internally uses 16-bit unsigned integer (C#'s `ushort`/`System.UInt16`).
- `string`: sequence of characters (`"hello world"`, `"hound"`, `""`).
- `int2`: two dimensional integer vector (`{1,2}`, `{100,230}`). Internally uses two 32-bit signed integers (C# `int`/`System.Int32`) or game's `int2` type.
- `unit`: represents a game unit.
  
    | Field      | Type      | Description                         |
    | ---------- | --------- | ----------------------------------- |
    | `codename` | `string`  | Internal unit name                  |
    | `name`     | `string`  | Display name                        |
    | `id`       | `ID`      | Unit ordered ID in global unit list |
    | `tier`     | `integer` | Unit tier                           |
    | `speed`    | `float`   | Movement speed                      |
    | `hp`       | `float`   | Unit health                         |
    | `armor`    | `integer` | Armor value                         |
    | `regen`    | `float`   | Regeneration rate                   |

- `cell`: represents a world cell.

    | Field      | Type      | Description                                 |
    | ---------- | --------- | ------------------------------------------- |
    | `flags`    | `integer` | Cell state flags                            |
    | `hp`       | `integer` | Health points                               |
    | `water`    | `float`   | Liquid amount                               |
    | `id`       | `ID`      | Content ID (`0` for empty cell)             |
    | `force`    | `int2`    | Physics force                               |
    | `codename` | `string`  | Content codename (if empty cell: `"-"`)     |
    | `name`     | `string`  | Content display name (if empty cell: `"-"`) |

- `item`: represents an any item.

    | Field         | Type     | Description       |
    | ------------- | -------- | ----------------- |
    | `id`          | `ID`     | Item ID           |
    | `codename`    | `string` | Internal name     |
    | `name`        | `string` | Display name      |
    | `desc`        | `string` | Description text  |
    | `textureIcon` | `string` | Icon texture info |

## Operators

| Operator | Description          | Example                                     |
| -------- | -------------------- | ------------------------------------------- |
| `+`      | Addition             | `1 + 2` -> `3`                              |
| `-`      | Subtraction/Negation | `5 - 3` -> `2`, `-{6, 8}` -> `{-6, -8}`     |
| `*`      | Multiplication       | `2 * 3` -> `6`, `{6, 10} * 3` -> `{18, 30}` |
| `/`      | Division             | `10 / 2` -> `5`                             |
| `%`      | Modulo               | `7 % 3` -> `1`, `{5, 6} % 2` -> `{1, 0}`    |
| `^`      | Exponentiation       | `2 ^ 3` -> `8`                              |
| `\| \|`  | Absolute Value       | `\|-5\|` -> `5`, `\|{3, 4}\|` -> `5`        |
| `.`      | Field Access         | `unit("hound").hp`                          |

## Functions

| Function    | Description                                          | Example                                                  |
| ----------- | ---------------------------------------------------- | -------------------------------------------------------- |
| `sin`       | Sine (`System.Math.Sin`)                             | `sin(pi)` -> `0`                                         |
| `cos`       | Cosine (`System.Math.Cos`)                           | `cos(0)` -> `1`                                          |
| `tan`       | Tangent (`System.Math.Tan`)                          | `tan(pi)` -> `0`                                         |
| `sqrt`      | Square root (`System.Math.Sqrt`)                     | `sqrt(4)` -> `2`                                         |
| `ln`        | Natural logarithm (`System.Math.Log`)                | `ln(e)` -> `1`                                           |
| `exp`       | Euler number raised to the power (`System.Math.Exp`) | `exp(0)` -> `1`                                          |
| `log10`     | Logarithm with base 10 (`System.Math.Log10`)         | `log10(100)` -> `2`                                      |
| `acos`      | Inverse cosine (`System.Math.Acos`)                  | `acos(1)` -> `0`                                         |
| `asin`      | Inverse sine (`System.Math.Asin`)                    | `asin(0)` -> `0`                                         |
| `atan`      | Inverse tangent (`System.Math.Atan`)                 | `atan(0)` -> `0`                                         |
| `ceil`      | Rounds up (`System.Math.Ceiling`)                    | `ceil(3.5)` -> `4`, `ceil(-2.5)` -> `-2`                 |
| `floor`     | Rounds down (`System.Math.Floor`)                    | `floor(3.5)` -> `3`, `floor(-2.5)` -> `-3`               |
| `round`     | Round to nearest integer (`System.Math.Round`)       | `round(3.4)` -> `3`, `round(-2.6)` -> `-3`               |
| `truncate`  | Integral part of number  (`System.Math.Truncate`)    | `truncate(3.3)` -> `3`, `truncate(-2.5)` -> `-2`         |
| `min`       | Minimum of values (accepts non-zero arguments)       | `min(5, 3, 8)` -> `3`                                    |
| `max`       | Maximum of values (accepts non-zero arguments)       | `max(10, 15)` -> `15`                                    |
| `comb`      | Number of combinations                               | `comb(5, 2)` -> `10`                                     |
| `factorial` | Factorial                                            | `factorial(5)` -> `120`                                  |
| `unit`      | Get unit by ID/codename                              | `unit("firefly")`, `unit(#34)`                           |
| `cellat`    | Get cell at position                                 | `cellat(300, 400)`, `cellat({600, 300})`                 |
| `item`      | Get item by ID/codename                              | `item("gunRifle")`, `item(#56)`                          |
| `typename`  | Name of type                                         | `typename(3)` -> `integer`, `typename({2, 3})` -> `int2` |

**Position Shortcuts:**

| Symbol | Type   | Description          | Example                    |
| ------ | ------ | -------------------- | -------------------------- |
| `~`    | `int2` | Player position      | `cellat(~ + {1, 2}).water` |
| `^`    | `int2` | Mouse world position | `cellat(^).codename`       |

## Constants

| Constant | Value                  | Description      |
| -------- | ---------------------- | ---------------- |
| `pi`     | 3.1415926535897931     | $\pi$ constant   |
| `e`      | 2.7182818284590451     | Euler's number   |
| `tau`    | 6.2831853071795862     | $2\pi$           |
| `phi`    | 1.61803398874989484820 | Golden ratio     |
| `invpi`  | 0.31830988618379067153 | $\cfrac{1}{\pi}$ |
| `inf`    | $\infty$               | Infinity         |
| `nan`    | NaN                    | Not-a-Number     |

# Command Argument Specification

## Relative coordinates

Relative coordinates start with a tilde (`~`), optionally followed by a number (which can be negative).
They describe an offset from the current player position along one of the world axes.
A tilde (`~`) alone assumes an offset of 0.
They can represent a single-precision floating point number or an integer.

Examples:
```
/tp ~10 ~
```
> Teleports the player right (+X) by 10 cells. Note that the Y position stays the same.

```
/tp ~-5 ~-2
```
> Teleports the player down (-Y) by 5 cells and left by 2 cells (-X).

---

## Cursor coordinates

Format of coordinates that refer to the current mouse position.
Their format starts with caret (`^`), optionally followed by a number (which can be negative), representing an offset.
They describe an absolute position of the mouse in the world along an axes, plus some provided offset.
A standalone caret (^) assumes an offset of 0.
They can represent a single-precision floating-point number or an integer.

> [!NOTE]
> You can mix relative and cursor coordinates in the same command.

Examples:
```
/tp ^ ^
```
> Teleport to the position pointing by the mouse.

```
/tp ^5 ^
```
> Teleports player to the position pointing by the mouse plus additional offset in right (X+) direction in 5 cells.

---

## Format language specifications

A string enclosed in `<` and `>` denotes an argument type defined in this list.

A string enclosed in `[` and `]` denotes an optional argument inside it.

---

## `<UNIT-POSITION>`

Two-dimensional coordinate composed of X and Y values.
Each value must be a single-precision floating-point number, a relative coordinate, or a cursor coordinate.

**Examples:** `300.4 560.3` (`x=300.4,y=560.3`), `1000 800.25` (`x=1000,y=800.25`)

> [!NOTE]
> The position must be within the world boundaries for units (x: `2` to `SWorld.Gs.x - 4` (usually `1020`), y: `2` to `SWorld.Gs.y - 4` (usually `1020`)).

---

## `<CELL-POSITION>`

Two-dimensional coordinate composed of X and Y values.
Each value must be a integer number, a relative coordinate, or a cursor coordinate.

**Examples:** `630 703` (`x=630,y=703`), `100 25` (`x=100,y=25`)

> [!NOTE]
> The position must be within the world boundaries for cells (x: `0` to `SWorld.Gs.x` (usually `1024`), y: `0` to `SWorld.Gs.y` (usually `1024`)).

---

## `<ITEM>`

Game item.

**Format**:
- **Item code name** (e.g., `dirt` ("Dirt"), `gunParticlesShotgun` ("Particle Shotgun")). These correspond to static variables in the game's `GItems` class.
- **Item ID** (e.g., `#3` ("Miniaturizor MK III"), `#123` ("Cactus Dirt")). ID 0 and negative IDs is forbidden.
- `air`. Custom item with ID 0.

---

## `<INT>`

Integer from -2147483648 to 2147483647 (or C#'s `int`/`System.Int32` type).

---

## `<STRING>`

A string.

---

## `<FLOAT>`

A number defined by single-precision floating-point (C#'s `float`/`System.Single` type).

---

## `<CELL-PARAMS>`

Comma separated list of parameter name (case insensitive) followed by its value (the type of value is defined by the parameter).

| Parameter Name      | Parameter Type | Description |
| ------------------- | -------------- | ----------- |
| `hp`                | `ushort`       | Health of the cell. |
| `forcex`            | `short`        | Force of the cell in X axis. |
| `forcey`            | `short`        | Force of the cell in Y axis. |
| `water`             | `float`        | Pressure of the water/lava in the cell. |
| `elecprod`          | `byte`         | Electric productivity of the cell. |
| `eleccons`          | `byte`         | Electric consumption of the cell. |
| `data0`             | `bool`         | First bit of the custom data of the cell. Each cell uses this data in own ways. |
| `data1`             | `bool`         | Second bit of the custom data of the cell. |
| `data2`             | `bool`         | Third bit of the custom data of the cell. |
| `burning`           | `bool`         | Specifies if cell is in burning state. |
| `mapped`            | `bool`         | Specifies if cell is discovered in the minimap. |
| `backwall`          | `bool`         | Specifies if cell has "Concrete Back Wall" placed. |
| `bg0`               | `bool`         | First bit of data that determines cell's background. If this flag is not specified will not modify the background of existing the cell. |
| `bg1`               | `bool`         | Second bit of data that determines cell's background. If this flag is not specified will not modify the background of existing the cell |
| `bg2`               | `bool`         | Third bit of data that determines cell's background. If this flag is not specified will not modify the background of existing the cell. |
| `waterfall`         | `bool`         | Specifies if cell is in "waterfall" state. |
| `streamlfast`       | `bool`         | Specifies if cell is in "streamlfast" state. |
| `streamrfast`       | `bool`         | Specifies if cell is in "streamrfast" state. |
| `lava`              | `bool`         | Specifies if cell is contains lava. `false` for water. |
| `haswireright`      | `bool`         | Specifies if cell has wire in the right. |
| `haswiretop`        | `bool`         | Specifies if cell has wire in the top. |
| `electricalgostate` | `bool`         | Specifies if cell is in "electricalgostate" state. |
| `powered`           | `bool`         | Specifies if cell is powered. |
| `light`             | `Color24`      | Light that cell is transferring. |
| `temp`              | `Color24`      | Maybe stands for "temporary"? |

If the parameter name is not in this list, the command will return an error.

> [!NOTE]
> The `bool` type parses `"true"`, `"True"` and `"1"` as true and `false`, `False` and `0` as false.
> 
> The `Color24` type has the format `red:green:blue` (`red`, `green` and `blue` are `byte`s) or a HEX color representation (also allowed decimal numbers).
> For example, `255:0:0` (`0xFF0000`) is pure **red**, `0:255:0` (`0x00FF00`) is pure **green**, `0:0:255` (`0x0000FF`) is pure **blue**.
---

## `<CELL-ITEM>`

An item that can be placed (type `CItemCell`).

**Format:** `<ITEM>`, but only placable items are allowed. Special `air` item is also allowed and will be interpreted as empty cell.

---

## `<CELL>`

Cell with it's parameters representing state.

**Format:** `<CELL-ITEM>[{<CELL-PARAMS>}]`.

---

## `<UNIT>`

Describes a unit type.

Format:
- **Unit code name** (e.g., `fireflyBlack` ("Black Firefly"), `bossMadCrab` ("Mad Crab")). These correspond to static variables in the game's `GUnits` class.
- **Unit ID** (e.g., `#8` ("Firefly"), `#30` ("Hell Hound")). ID 0 and negative IDs is forbidden.

> [!WARNING]
> Spawning one unit from this list would lead to game crashing:
> - `player` (ID 1)
> - `playerLocal` (ID 2)
> - `defense` (ID 3)

---

## `<PLAYER>`

Describes a player.
Must contain a case-sensitive player nickname currently joined in the lobby.

# Commands

## `/tp`

```
/tp pos:<UNIT-POSITION>
/tp player:<PLAYER>
```
1. Teleport to `pos`.
2. Teleport to the position of `player`.

> [!NOTE]
> The position must be within the world boundaries for units (usually from `0 0` to `1020 1020` (exclusive)).

**Examples:**
```
/tp 600 700
```
> Teleports to coordinates (600, 700).

```
/tp ~ ~100
```
> Teleports up by 100 cells.

```
/tp ^ ^
```
> Teleports to the position **pointing by the mouse**.

```
/tp ^5 ^
```
> Teleports to the position **pointed to by the mouse**, with a **5-cell offset to the right (+X)**.

```
/tp SomePlayerInLobby
```
> Teleports to the position of another player with name **`SomePlayerInLobby`**.

---

## `/give`

```
/give item:<ITEM>
/give item:<ITEM> count:<INT>
```
1. Gives 1 `item` to current player inventory.
2. Gives `item` with quantity `count` to inventory (`count` can be negative).

**Examples:**
```
/give potionHpBig
```
> Gives 1 "Large Health Potion".

```
/give wood 57
```
> Gives 57 "Log"s.

```
/give #87
```
> Gives 1 "Auto-Builder MK II".

---

## `/place`

```
/place cell:<CELL> pos:<CELL-POSITION>
```
Places `cell` to position `pos`.

> [!IMPORTANT]  
> Cells placed with this command **are not checked for validity**, which can lead to **unstable states**, such as:  
> - Floating blocks without supporting background,
> - Turrets placed mid-air,
> - ...

> [!NOTE]
> Provided `pos` must be within the world boundaries (usually from `0 0` to `1023 1023` (inclusive)).

**Examples:**
```
/place dirt 303 650
```
> Places "Dirt" at cell with coordinates (303, 650).

```
/place air ~ ~5
```
> Removes cell 5 cells above the player position.

```
/place rock ^ ^
```
> Places "Stone" at the cell **pointed to by the mouse**.

```
/place dirt{water=0.1} ^ ^
```
> Places "Dirt" with 10% water inside at the cell **pointed to by the mouse**.

```
/place air{water=5,lava=1} ^ ^
```
> Creates lava (and removes cell) with quantity of 5 units.

```
/place wallDoor{data0=1} ~ ~
```
> Places an **open** "Armored Door" at the **cell that player is standing in**.

```
/place #73{hp=100} ~10 ~
```
> Places a "Concrete Wall" with 100 health at a position **10 cells to the right (+X)** of the player's current location.

---

## `/fill`

```
/fill cell:<CELL> from:<CELL-POSITION> to:<CELL-POSITION>
```
Fills a rectangular region defined by two opposite corners with the `cell`.

The `from` and `to` coordinates define the bounds of the region which will be filled with `cell`.
The total number of filled cells is $(x_{max} - x_{min} + 1) \times (y_{max} - y_{min} + 1)$.

> [!IMPORTANT]
> Cells placed with this command **are not checked for validity**, which can lead to **unstable states**, such as:  
> - Floating blocks without supporting background,
> - Turrets placed mid-air,
> - ...

> [!NOTE]
> The provided region must be within the world boundaries (usually from `0 0` to `1023 1023` (inclusive)).

**Examples:**
```
/fill dirt 650 500 700 530
```
> Fills region from (650, 500) to (700, 530) with "Dirt".

```
/fill wallWood ~ ~ ~10 ~2
```
> Fills region from **the player's current position** to **10 cells right (+X)** and **2 cells up (+Y)** from current player's location with "Wooden Wall".

```
/fill air{water=50} ^-2 ^-2 ^2 ^2
```
> Fills region to nothing with **water pressure** to `50` in a **5x5 region** centered on the mouse cursor (from **2 cells left (–X)** to **2 cells right (+X)**, and **2 cells down (–Y)** to **2 cells up (+Y)**).

```
/fill air 0 0 1023 1023
```
> Clears entire world** but keeps background.

```
/fill air{bg0=0} 0 0 1023 1023
```
> Clears entire world with empty background.

---

## `/killinfo`

```
/killinfo
```
Displays a list of **all species killed**, including their **total count** and the **time elapsed since last kill**.

> [!NOTE]
> This's a local command.

**Example output:**
```
Hound: 1 (393.15)
Firefly: 1 (393.15)
Black Firefly: 4 (1.32)
```

---

## `/spawn`

```
/spawn unit:<UNIT> pos:<UNIT-POSITION>
```
Spawns `unit` at coordinates `pos`.

**Examples:**
```
/spawn hound 300 700
```
> Spawns **Hound** at (300, 700).

```
/spawn #22 ^ ^
```
> Spawn **Acid Ant** at the position **pointing by the mouse**.

---

## `/clearinventory`

```
/clearinventory
```
Removes every item in the current player's inventory.

---

## `/clearpickups`

```
/clearpickups
```
Removes every pickup in the world.

---

## `/clone`

```
/clone from:<CELL-POSITION> to:<CELL-POSITION> dest:<CELL-POSITION>
```
Clones cells from one region (`from` to `to` positions) to another (`dest` to `dest + (to - from)` positions).

> [!IMPORTANT]  
> Cells placed with this command **are not checked for validity**, which can lead to **unstable states**, such as:  
> - Floating blocks without supporting background,
> - Turrets placed mid-air,
> - ...

> [!NOTE]
> The source and destination regions must be within the world boundaries (usually from `0 0` to `1023 1023` (inclusive)).
>
> The provided regions can overlap.

**Examples:**

```
/clone 500 500 599 599 600 500
```
> Copies a 100x100 cell region from (500, 500) to (600, 600) and pastes it starting at the destination corner (600, 500).

```
/clone ~-5 ~-5 ~5 ~5 ^ 6
```
> Copies a 10x10 square around player and pastes it where mouse is located.

---

## `/replace`

```
/replace from:<CELL-POSITION> to:<CELL-POSITION> target:<CELL-ITEM> cell:<CELL>
```

Replaces `target` cell in `from` to `to` region with `cell`.

> [!IMPORTANT]  
> Cells replaced with this command **are not checked for validity**, which can lead to **unstable states**, such as:  
> - Floating blocks without supporting background,
> - Turrets placed mid-air,
> - ...

> [!NOTE]
> The `from` to `to` region must be within the world boundaries (usually from `0 0` to `1023 1023` (inclusive)).

**Examples:**

```
/replace 400 300 500 500 dirt rock{water=1,lava=1}
```
> Replaces "Dirt" from (400, 300) to (500,500) region with "Rock" with each 1 unit of lava inside it.

```
/replace 0 0 1023 1023 iron rock
```
> Replaces all "Iron" in the world with "Rock".

```
/replace 0 0 1023 1023 air air{water=1}
```
> Fill empty cells in the world with 1 unit of water.

---

## `/freecam`

```
/freecam
/freecam speed=<FLOAT>
/freecam zoom=<INT>
/freecam speed
/freecam zoom
```
1. Toggles freecam mode.
2. Sets the freecam camera speed (default: 100).
3. Sets the camera zoom index (default: 0).
   - Positive integers: zoom-in.
   - Negative integers: zoom-out.
4. Prints current freecam speed.
5. Prints current freecam zoom index.

In freecam mode the camera gets detached from player and you now control a camera, leaving the player in the original position.
Use Shift key to slowdown camera speed.

> [!NOTE]
> This's a local command.

**Examples:**

```
/freecam
```
> Toggle freecam mode.

```
/freecam speed=50
```
> Sets freecam speed by half of default.

```
/freecam zoom=2
```
> Doubles the camera zoom.

---

## `/exportpng`

```
/exportpng [path:<STRING>]
```
Creates an image of entire world and saves it in `path` (by default in `SavedScreen.png` in game's root).

**Examples:**

```
/exportpng 
```
> Saves image as `SavedScreen.png` in the game's root folder (where `DigOrDie.exe` located).

```
/exportpng myworld
```
> Saves image as `myworld.png` in the game's root folder (where `DigOrDie.exe` located).

```
/exportpng C:\Users\Public\output.png
```
> Saves image as `C:\Users\Public\output.png`.

```
/exportpng theworld.abc
```
> Saves image as `theworld.abc` in the game's root folder (where `DigOrDie.exe` located).

---

## `/clock`

```
/clock time:<FLOAT>
/clock +delta:<FLOAT>
/clock -delta:<FLOAT>
/clock pause
/clock resume
/clock morning
/clock night
/clock evening
/clock midday
/clock midnight
/clock lavastart
/clock lavaend
```
1. Sets clock time to `time`. `time` must be between `0` and `1` (inclusive).
2. Advances forwards clock by `delta` (wrapping between `0` and `1`).
3. Advances backwards clock by `delta` (wrapping between `0` and `1`).
4. Pauses clock time.
5. Resumes clock time.
6. Sets time after the night ends.
7. Sets time when the night starts.
8. Sets time when the night starts off by 60 seconds.
9. Sets time to `0.5` (when autosave is triggered).
10. Sets time to `0`.
11. Sets time to `0.45` (when volcano lava cycle starts).
12. Sets time to `0.9` (when volcano lava cycle ends).

# Configuration

### `[General]` `RepeatLastCommand`

**Setting Type:** `KeyboardShortcut` \
**Default value:** `~ + ctrl`

Keyboard shortcut for running last command.

### `[ChatExpressionEvaluator]` `Enable`

**Setting type:** `bool` \
**Default value:** `true`

Enable expression evaluator in chat.

### `[ChatExpressionEvaluator]` `Prefix`

**Setting type:** `string` \
**Default value:** `"$"`

Prefix for expression evaluation syntax.

### `[General]` `FullChatHistory`

**Setting type:** `bool` \
**Default value:** `true`

Tracks every message in the chat and adds it to the history.

### `[General]` `DisableAchievementsOnCommand`

**Setting type:** `bool` \
**Default value:** `true`

Disables achievements on any command (like when running commands `/event` and `/param` the achievements gets disable on first ran).
