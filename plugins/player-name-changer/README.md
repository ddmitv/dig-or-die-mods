
# Player Name Changer

Allows to change the player name in the game.

By changing the player name, this will also affect the name stored in save file.

Also, if you're name has character `|` ("Vertical bar") and you get killed by a monster, your death message will display improperly.
This is because the game uses special `/system` command with a `|` separated arguments.
So, when the monster is killed a player, a `/system CHAT_DEATH_KILLED|<player name>|<monster codename>` command is executed.
When a player name has a `|` character, the game will use a second part of your name as a monster's codename, and the first part as player's name.
For example, if your name is `my name|bossDweller`, when you die by a monster, the death message will always be `my name was killed by a Dweller Lord` regardless by a monster that originally killed you.

## Configuration

### `[General]` `PlayerName`

**Setting type:** `string` \
**Default value:** `null`

Overrides the player name in the game.
Remove this config entry or click "Reset" if you're using [`BepInEx.ConfigurationManager`](https://github.com/BepInEx/BepInEx.ConfigurationManager)
to set the original player name (from Steam).

> [!NOTE]
> This parameter can be changed at any time, not only at startup.

### `[General]` `Enable`

**Setting type:** `bool` \
**Default value:** `true`

Enables the plugin.
