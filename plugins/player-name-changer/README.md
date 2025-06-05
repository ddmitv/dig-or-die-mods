
# Player Name Changer

Allows to change the player name in the game.

By changing the player name, this will also affect the name stored in save file.

## Argument Injection

Also, if you're name has character `|` ("Vertical bar") and you get killed by a monster, your death message will display improperly.
This is because the game uses special `/system` command with a `|` separated arguments.
So, when the monster is killed a player, a `/system CHAT_DEATH_KILLED|<player name>|<monster codename>` command is executed.
When a player name has a `|` character, the game will use a second part of your name as a monster's codename, and the first part as player's name.
For example, if your name is `my name|bossDweller`, when you die by a monster, the death message will always be `my name was killed by a Dweller Lord` regardless by a monster that originally killed you.

## Configuration

> [!TIP]
> You can change config during the game. The name will change automatically.

### `[General]` `PlayerName`

**Setting type:** `string` \
**Default value:** `""`

Overrides the player name in the game.

### `[General]` `Enable`

**Setting type:** `bool` \
**Default value:** `false`

If `true`, will change the player's name to provided in `PlayerName` config. If `false`, will revert the player's name to default one (from Steam).
