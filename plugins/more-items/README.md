
# More Items

A plugin for adding entirely new items to the base game.

Adds new 28 items with custom recipes:
- 7 devices
- 2 walls and 1 soil
- 1 repair turret and 2 collectors
- 5 turrets
- 5 weapons
- 2 explosives
- 2 electrical generators and 2 water vaporizers
- And more

**Full list of items:**
| Name                                  | Description                                                                                                       |
| ------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| Flashlight MK3                        | More stronger flashlight MK2                                                                                      |
| Miniaturizor MK VI                    | Same as ultimate one but doesn't brake Ancient Basalt                                                             |
| Better Health Regeneration Potion     | Two times better than regular regeneration potion                                                                 |
| Defense Shield MK2                    | Absorbs 2x HP than MK1 version                                                                                    |
| Rebreather MK2                        | Rebreather that lasts *very* long                                                                                 |
| Jetpack MK2                           | Jetpack which lasts longer and flies quicker                                                                      |
| Anti-Gravity Wall                     | A wall that ignores gravity                                                                                       |
| Auto-Repair Turret MK3                | Heals 10 hp/s instead of 6 hp/s (Advanced Auto-Repair Turret)                                                     |
| Particle Turret MK2                   | Deals 50 damage per attack with increased range                                                                   |
| Tesla Turret MK2                      | Tesla Turret for late game                                                                                        |
| Collector                             | Automatically harvests plants nearby                                                                              |
| Basalt Collector                      | Automatically mines nearby basalt cells                                                                           |
| Blue Wall Light                       | Wall Light which glows blue                                                                                       |
| Red Wall Light                        | Wall Light which glows red                                                                                        |
| Green Wall Light                      | Wall Light which glows green                                                                                      |
| Rotating Laser Turret                 | More powerful stationery laser turret                                                                             |
| Gun "Meltdown"                        | Fires powerful plasma particles which explode on contact while emitting lava                                      |
| Mega Explosive                        | An upgraded explosion with shockwave, background destruction and burning everything with 10 cells explosion range |
| Volcanic Explosive                    | Simulates volcano eruption on explosion                                                                           |
| Composite Reinforced Wall             | Improved composite wall by +200 hp and stronger force resiting                                                    |
| Solar Panel MK2                       | A solar panel generating 3kW from sub light                                                                       |
| Radioisotope Thermoelectric Generator | Generates 15kW from radioactive decay while destroying all live and turrets nearby                                |
| Rocket Launcher Gatling               | Machine gun that fires rockets                                                                                    |
| ZF-0 Shotgun                          | Basically a focused shotgun                                                                                       |
| Portable Teleporter                   | Allows you to teleport to other teleporters on the go                                                             |
| Fertile Dirt                          | Universal soil with faster plant grow                                                                             |
| Impact grenade                        | Early-game grenade                                                                                                |
| Impact Shield MK1                     | Absorbs 25% damage from fall and wall hitting with high speed                                                     |
| Impact Shield MK2                     | Same as Impact Shield MK1 but absorbs 50% damage                                                                  |
| Water Vaporizer                       | Placable machine for removing water using electricity                                                             |
| Water Vaporizer MK2                   | Evaporates faster but requires more energy                                                                        |
| Death Pulse Turret MK2                | Improved Death Pulse Turret with 120 damage double-attack and increased angle of attack                           |
| Electrified Spikes MK2                | Improved Electrified Spikes for late-game                                                                         |
| MB-X Plasma Diffuser                  | A gun shooting electrified particles which on contact creates large energy explosion                              |
| Advanced Metal Detector               | Searches for all metal vein with configurable 120m range                                                          |

> [!CAUTION]
> If you want to use this plugin on existing save, please make sure you've backup of it.

> [!NOTE]
> Only English version of item names/descriptions are available.

# Credits

Sprites for:
- "Duo Rotating Turret" (`turretDuo360`)
- "Overcharged Plasma Turret" (`turretMegaSnipe`)
- "MB-X Plasma Diffuser" (`gunEnergyDiffuser`)
- "Impact Shield MK1" (`impactShieldMk1`)
- "Impact Shield MK2" (`impactShieldMk2`)

are created by **@seethejellyfish at Discord**.

# Configuration

### `[General]` `BossRespawnDelay`

**Setting type:** `float` \
**Default value:** `360`

Respawn delay for bosses.
Many recipes requires boss loot items, default value is the half than multiplayer default's one to reduce waiting for boss to respawn.

### `[General]` `UniqualizeVersionBuild`

**Setting type:** `bool` \
**Default value:** `false`

Safe guard to prevent joining to server with different mod version.

When using this config the build version will (usually) look like a random sequence of numbers.
