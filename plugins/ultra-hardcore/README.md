
# Ultra Hardcore

Makes the game a lot harder (customizable).

All effects will apply at any difficulty level.

## Configuration

### `HpMax`

**Setting type:** `float` \
**Default value:** `0` \
**Acceptable value range:** [`0`,`3.40282347E+38f`] (`float.MaxValue`).

Maximum health for the player. '`0`' for default maximum health for corresponding difficulty.

### `PermanentMist`

**Setting type:** `bool` \
**Default value:** `false`

Makes the mist event permanent, regardless of the current events.

### `PermanentDarkness`

**Setting type:** `bool` \
**Default value:** `false`

Makes the night permanent, but without changing monsters spawn time.
Adds `Sun Light` to the initial item list due to inability to complete the game without it.

Note that this physically removes all light from the sun, so the all plants on the surface will start to die.
**You have limited time**.

### `NoRain`

**Setting type:** `bool` \
**Default value:** `false`

Removes all rains from the game.

### `InverseNight`

**Setting type:** `bool` \
**Default value:** `false`

Monsters attack during the day, but stop at night.
This also means that you can get loot from them only at night and at day they will drop blood.

### `PermanentAcidWater`

**Setting type:** `bool` \
**Default value:** `false`

Makes event `'ACIDIC WATERS'` always active.

### `NoRegeneration`

**Setting type:** `bool` \
**Default value:** `false`

Removes the player's ability to regenerate, so you can only gain health with potions.

### `NoQuickSaves`

**Setting type:** `bool` \
**Default value:** `false`

Removes ability to do quick saves: through key and through menu button.

### `ContinuousEvents`

**Setting type:** `bool` \
**Default value:** `false`

Makes events always active.
Note that day-only and night-only events will appear at the corresponding time of the day.
