
# Extra Commands

A plugin that expands the list of available commands (chat messages starting with `/`).

## Argument Specification

### Relative coordinates

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

### Cursor coordinates

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

### Format language specifications

A string enclosed in `<` and `>` denotes an argument type defined in this list.

A string enclosed in `[` and `]` denotes an optional argument inside it.

---

### `UNIT-POSITION`

Represents a two-dimensional coordinate composed of X and Y values.
Each value must be a single-precision floating-point number, a relative coordinate, or a cursor coordinate.
Note: the position must be within the world boundaries for units (the `SWorld.GridRectM2` grid).

---

### `CELL-POSITION`

Represents a two-dimensional coordinate composed of X and Y values.
Each value must be a integer number, a relative coordinate, or a cursor coordinate.
Note: the position must be within the world boundaries for cells.

---

### `ITEM-ID`

Describes an item as an ID (a non-negative number).
The "null" item (ID 0) does not represent a valid item which can be carried in the inventory, instead, it represents an empty cell (e.g., a cell containing air).
Commands will throw an error if the "null" item is given to the inventory.

**Examples:** `#3` ("Miniaturizor MK III"), `#123` ("Cactus Dirt").

---

### `ITEM`

Describes an item.

Format:
- **Item code name** (e.g., `dirt` ("Dirt"), `gunParticlesShotgun` ("Particle Shotgun")). These correspond to static variables in the game's `GItems` class.
- **Item ID** in the format `#<ITEM-ID>`.
- `air`. Custom item with ID 0.

---

### `<INT>`

Describes an integer from -2147483648 to 2147483647 (or C#'s `int`/`System.Int32` type).

> [!NOTE]
> This type is parsed with `int.TryParse` in C#.

---

### `CELL-PARAMS`

A comma separated list of parameter name (case insensitive) followed by its value (the type of value is defined by the parameter).

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
> The `bool` type has the format `true`, `True` and `1` string for `true` value and `false`, `False` and `0` for `false` value.
> 
> The `Color24` type has the format `red:green:blue` (`red`, `green` and `blue` are `byte`s) or `uint` representing a concatenation of these values.

---

### `CELL`

Describes a cell with it's parameters.

**Format:** `<ITEM>[{<CELL-PARAMS>}]`.

---

### `UNIT-ID`

Describes an unit as an ID (a positive number).
The "null" unit (ID 0) does not represent a valid unit; commands will throw an error if the "null" unit is used.

**Examples:** `#8` ("Firefly"), `#30` ("Hell Hound").

---

### `UNIT`

Describes a unit.

Format:
- **Unit code name** (e.g., `fireflyBlack` ("Black Firefly"), `bossMadCrab` ("Mad Crab")). These correspond to static variables in the game's `GUnits` class.
- **Unit ID** in the format `#<UNIT-ID>`.

> [!WARNING]
> Spawning one unit from this list would lead to game crashing:
> - `player` (ID 1)
> - `playerLocal` (ID 2)
> - `defense` (ID 3)

---

### `PLAYER`

Describes a player.
Must contain a case-sensitive player nickname currently joined in the lobby.

## Commands

### `/tp`

```
/tp <UNIT-POSITION>
/tp <PLAYER>
```
1. Teleport current player to the position `<UNIT-POSITION>`.
2. Teleport current player to the position of another player with name `<PLAYER>`.

> [!NOTE]
> The position must be within the world boundaries for units.

**Examples:**
```
/tp 600 700
```
> Teleports the player to coordinates **X: 600, Y: 700**.

```
/tp ~ ~100
```
> Teleports the player up by **100** cells.

```
/tp ^ ^
```
> Teleports the player to the position **pointing by the mouse**.

```
/tp ^5 ^
```
> Teleports the player to the position **pointed to by the mouse**, with a **5-cell offset to the right (+X)**.

```
/tp SomePlayerInLobby
```
> Teleports the player to the position of another player with name **`SomePlayerInLobby`**.

---

### `/give`

```
/give <ITEM>
/give <ITEM> <INT>
```
1. Gives single `<ITEM>` to current player inventory.
2. Gives `<ITEM>` with quantity `<INT>` to current player inventory.

**Examples:**
```
/give potionHpBig
```
> Gives 1 **Large Health Potion**.

```
/give miniaturizorMK5
```
> Gives 1 **Miniaturizor MK V**.

```
/give wood 57
```
> Gives 57 **Log**s.

---

### `/place`

```
/place <CELL> <CELL-POSITION>
```
Places `<CELL>` to position `<CELL-POSITION>`.

> [!IMPORTANT]  
> Cells placed with this command **are not checked for validity**, which can lead to **unstable states**, such as:  
> - Floating blocks without supporting background,
> - Turrets placed mid-air,
> - Other logically impossible placements.

> [!NOTE]
> The provided position `<CELL-POSITION>` must be within the world boundaries.

**Examples:**
```
/place dirt 303 650
```
> Places **Dirt** at cell with coordinates **X: 303, Y: 650**.

```
/place generatorSun ~ ~5
```
> Places **Solar Panel** 5 cells above the player position.

```
/place rock ^ ^
```
> Places **Stone** at the cell **pointed to by the mouse**.

```
/place air ~ ~
```
> **Removes** cell at the **player position**.

```
/place dirt{water=0.1} ^ ^
```
> Places **Dirt** with 10% water inside at the cell **pointed to by the mouse**.

```
/place air{water=5,lava=1} ^ ^
```
> Creates lava (and removes cell) with quantity of 5 units.

```
/place wallDoor{data0=1} ~ ~
```
> Places **Armored Door** with **Open** state at the **cell that player is standing in**.

```
/place wallConcrete{hp=100} ~10 ~
```
> Places a **Concrete Wall** with **100 HP** at a position **10 cells to the right (+X)** of the player’s current location.

---

### `/fill`

```
/fill <CELL> <CELL-POSITION> <CELL-POSITION>
```
Fills a rectangular region defined by two **opposite corners** with the specified `<CELL>`.

The minimum ($x_{min}$, $y_{min}$) and maximum ($x_{max}$, $y_{max}$) coordinates define the bounds of the region.
The total number of filled cells is $(x_{max} - x_{min} + 1) \times (y_{max} - y_{min} + 1)$.

> [!IMPORTANT]  
> Cells placed with this command **are not checked for validity**, which can lead to **unstable states**, such as:  
> - Floating blocks without supporting background,
> - Turrets placed mid-air,
> - Other logically impossible placements.

> [!NOTE]
> The provided region must be within the world boundaries.

**Examples:**
```
/fill dirt 650 500 700 530
```
> Fills a rectangular region from **X: 650, Y: 500** to **X: 700, Y: 530** with **Dirt**.

```
/fill wallWood ~ ~ ~10 ~2
```
> Fills a rectangular region from **the player's current position** to **10 cells right (+X)** and **2 cells up (+Y)** from current player's location with **Wooden Wall**.

```
/fill air{water=50} ^-2 ^-2 ^2 ^2
```
> Sets **water pressure** to `50` in a **5x5 region** centered on the mouse cursor (from **2 cells left (–X)** to **2 cells right (+X)**, and **2 cells down (–Y)** to **2 cells up (+Y)**).

```
/fill air 0 0 1023 1023
```
> Clears **entire world** but **keeps background**.

```
/fill air{bg0=0} 0 0 1023 1023
```
> Clears **entire world** with **empty background**.

---

### `/killinfo`

```
/killinfo
```
Displays a list of **all species killed**, including their **total count** and the **time elapsed since last kill**.

**Example output:**
```
Hound: 1 (393.15)
Firefly: 1 (393.15)
Black Firefly: 4 (1.32)
```

---

### `/spawn`

```
/spawn <UNIT> <UNIT-POSITION>
```
Spawns `<UNIT>` at coordinates `<UNIT-POSITION>`.

**Examples:**
```
/spawn hound 300 700
```
> Spawns **Hound** at **X: 300, Y: 700**.

```
/spawn dweller ^ ^
```
> Spawn **Dweller** at the position **pointing by the mouse**.

---

### `/clearinventory`

```
/clearinventory
```
Removes every item in the current player's inventory.

---

### `/clearpickups`

```
/clearpickups
```
Removes every pickup in the world.

---

### `/clone`

```
/clone <CELL-POSITION> <CELL-POSITION> <CELL-POSITION>
```
Clones cells from one region to another.

Clones a rectangular region defined by two **opposite corners** to another region defined by destination coordinate.

> [!IMPORTANT]  
> Cells placed with this command **are not checked for validity**, which can lead to **unstable states**, such as:  
> - Floating blocks without supporting background,
> - Turrets placed mid-air,
> - Other logically impossible placements.

> [!NOTE]
> The source and destination regions must be within the world boundaries.
>
> The provided regions can overlap.

**Examples:**
```
/clone 500 500 599 599 500 500
```
> Copies a **100x100 cell region** from **X: 500, Y: 500 to X: 600, Y: 600** and pastes it starting at the **destination corner X: 600, Y: 500**.

## Configuration

### `RepeatLastCommand`

**Setting Type:** `KeyboardShortcut` \
**Default value:** `~ + ctrl`

Keyboard shortcut for running last command.
