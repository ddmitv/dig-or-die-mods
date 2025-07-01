
# Ultra Hardcore

Makes the game more harder (customizable).

All effects of this plugin will apply at any difficulty level.

> [!WARNING]
> The plugin was not tested for completing a game with any of configs enabled. If you encountered a problem you can temporary (or permanently) disable problematic settings.

# Configuration

> [!NOTE]
> To apply any of the settings below, restart the game.

### `[UltraHardcore]` `HpMax`

**Setting type:** `float` \
**Default value:** `0` \
**Acceptable value range:** [`0`,`3.40282347E+38f`] (`float.MaxValue`).

Maximum health for the player. `0` for default maximum health for corresponding difficulty.

### `[UltraHardcore]` `PermanentMist`

**Setting type:** `bool` \
**Default value:** `false`

Makes the mist event permanent, regardless of the current events.

> [!NOTE]
> While the original mist event decreases sun light (modifies `G.m_sunLight` in `SWorld.UpdateLightSunValue`), this config doesn't do that.

### `[UltraHardcore]` `PermanentDarkness`

**Setting type:** `bool` \
**Default value:** `false`

Makes the night permanent without changing monsters spawn time.
Adds `Sun Light` to the initial item list due to inability to complete the game without it.

Note that this physically removes all light from the sun, so all plants on the surface will start to die after some time.

### `[UltraHardcore]` `NoRain`

**Setting type:** `bool` \
**Default value:** `false`

Removes all rains from the game. This doesn't affect presimulation (i.e. when you start a new game, the initial water will exist).

### `[UltraHardcore]` `InverseNight`

**Setting type:** `bool` \
**Default value:** `false`

Monsters attack during the day, but stop at night (like swapping day and night only for monsters).
This also means that you can get loot from them only at night and at day they start dropping blood.

### `[UltraHardcore]` `PermanentAcidWater`

**Setting type:** `bool` \
**Default value:** `false`

Makes event `'ACIDIC WATERS'` always active.

### `[UltraHardcore]` `NoRegeneration`

**Setting type:** `bool` \
**Default value:** `false`

Removes the player's ability to regenerate, so you can only gain health from potions.

### `[UltraHardcore]` `NoQuickSaves`

**Setting type:** `bool` \
**Default value:** `false`

Removes ability to do quick saves: through key and through menu button.

### `[UltraHardcore]` `ContinuousEvents`

**Setting type:** `bool` \
**Default value:** `false`

Makes events always active. Day-only (`CEnvironment.m_isDayEnv`) and night-only (`CEnvironment.m_isNightEnv`) events will appear at the corresponding time of the day.

For example, if there's already an ongoing event, and then a night starts, then only after the current events is over will the next event be selected from a list of non-day-only events (due to night time).

### `[UltraHardcore]` `InstantDrowning`

**Setting type:** `bool` \
**Default value:** `false`

Causes the player to instantly die from drowning (without an incremental damage from drowning).

### `[UltraHardcore]` `UnitInstantObservation`

**Setting type:** `bool` \
**Default value:** `false`

Makes every monster target closest player regardless of distance between them or if a player wearing an armor.

### `[UltraHardcore]` `HideClock`

**Setting type:** `bool` \
**Default value:** `false`

Hides the clock from GUI. Note if you're using a `precise-clock` plugin you must manually disable it to hide current in-game time.

### `[UltraHardcore]` `IngredientMultiplier`

**Setting type:** `uint` \
**Default value:** `1`

Multiplies all item recipe's ingredients (ignoring unique).

> [!IMPORTANT]
> If the item is an upgrade to another item (`CRecipe.m_isUpgrade`) and it's first ingredient count is 1 (or less), it's first recipe ingredient won't be affected.
> For example, if you want to craft Miniaturizor MK III, you don't need to have multiple Miniaturizor MK II to craft it.
> 
> If an ingredient is a boss item loot, it won't be affected. What counts as "boss item":
> "Mad Crab Material" (`bossMadCrabMaterial`), "Mad Crab Sonar" (`bossMadCrabSonar`), "Energy Master Gem" (`masterGem`), "Demon's Skin" (`lootBalrog`), "Dweller Lord Shell Spike" (`lootDwellerLord`).

### `[UltraHardcore]` `MonsterDamageMultPerNight`

**Setting type:** `float` \
**Default value:** `1`

On the end of night multiplies monster damage by provided value.
For example, if before monster damage multiplier (param `m_monstersDamagesMult`) was `1.5`, and if the `MonsterDamageMultPerNight = 1.1`, then after the night it would be `1.65`, and then the next night `1.815` and etc.

### `[UltraHardcore]` `MonsterHpMultPerNight`

**Setting type:** `float` \
**Default value:** `1`

On the end of night multiplies monster health by provided value.
For example, if before monster health multiplier (param `m_monstersHpMult`) was `1.5`, and if the `MonsterHpMultPerNight = 1.1`, then after the night it would be `1.65`, and then the next night `1.815` and etc.

### `[General]` `UniqualizeVersionBuild`

**Setting type:** `bool` \
**Default value:** `false`

Safe guard to prevent joining to server with different mod version.

When using this config the build version will (usually) look like a random sequence of numbers.
