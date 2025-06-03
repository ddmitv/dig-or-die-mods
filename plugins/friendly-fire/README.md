
# Friendly Fire

Allows players to damage themselves, damage ground, and defense turrets damage players. Supports hiding player names in the game and on the minimap.

Basically, adds PVP to the game.

> [!IMPORTANT]
> The plugin has guard mechanism of uniqualizing game's version build. 
> This will not allow players to join a host which has different version of this plugin.
> When using this plugin the build version will (usually) look like a random sequence of numbers.

A custom functionality a plugin provides is when a player is killed by another one (or by themselves), the death message will include another players name that killed them.

## Configuration

### `[General]` `Enabled`

**Setting type:** `bool` \
**Default value:** `true`

Enables the plugin.

### `[FriendlyFire]` `DamageAOE`

**Setting type:** `bool` \
**Default value:** `true`

Now damage from explosions and lightning strikes will damage nearby players.

### `[FriendlyFire]` `HideNames`

**Setting type:** `bool` \
**Default value:** `false`

Hides other player names, chat messages above their heads and an arrow pointing to the player's location if they are off-screen.

### `[FriendlyFire]` `HideMinimapPlayers`

**Setting type:** `bool` \
**Default value:** `false`

Hides player icons from minimap.

### `[FriendlyFire]` `PlayerDamageToGround`

**Setting type:** `bool` \
**Default value:** `false`

Bullets now damage the cells they are colliding with.

### `[FriendlyFire]` `PlayerDamageToGround`

**Setting type:** `bool` \
**Default value:** `false`

Bullets now damage the cells they are colliding with.

### `[FriendlyFire]` `DefenseDamagePlayers`

**Setting type:** `bool` \
**Default value:** `false`

Allows defense units (turrets) to hit and damage players theirs bullets are colliding with.
Note that they will usually only target monsters, but if their bullet with hit a player it now will damage them instead of passing by.
