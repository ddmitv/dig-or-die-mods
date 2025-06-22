
# Achievement Enabler

Makes achievements available (configurable):
- In multiplayer.
- After using `/event` and `/param` commands.
- Modifying params in `Solo`/`Multi` game modes.
- In custom game modes (not only in `Solo`/`Multi`).
- Starting with creative mode or with events enabled.
- In post-game (all achievements) (after launching rocket).

This might feel like a cheaty plugin for unlocking all Steam achievements, but plenty of software already allows do this easily and without installing any plugins.
The plugin was originally created to enable achievements in multiplayer, because it's really annoying when playing with friend and not receive any legally gained achievements.

<details>

> Interestingly, it seems to me that the game originally planned to have working achievements in multiplayer: there is a static field `GVars.m_achievementsLocked` that gets disabled in particular
> when the current game mode is not either `Solo` or `Multi`. So even when playing with multiple players, `GVars.m_achievementsLocked` is `true`, but, the method `SSteamStats.SetStat`
> designed to set achievement progress additionally checks for multiplayer.

</details>

# Configuration

### `[General]` `Enabled`

**Setting type:** `bool` \
**Default value:** `true`

Enables the plugin.

### `[EnableAchievements]` `InMultiplayer`

**Setting type:** `bool` \
**Default value:** `true`

Makes achievements available in multiplayer game (either if you host or client).

### `[EnableAchievements]` `AfterCheats`

**Setting type:** `bool` \
**Default value:** `false`

Makes achievements available after using `/event` and `/param` commands, modifying params in `Solo`/`Multi` game modes (in `.lua` files) or starting a game with creative mode or with events enabled.

### `[EnableAchievements]` `WithEvents`

**Setting type:** `bool` \
**Default value:** `true`

Makes achievements available after starting a game with events on.

### `[EnableAchievements]` `InCustomMode`

**Setting type:** `bool` \
**Default value:** `false`

Makes achievements accessible in custom game modes (not only in `Solo`/`Multi` modes).

### `[EnableAchievements]` `InPostGameAlways`

**Setting type:** `bool` \
**Default value:** `false`

Force enables the achievements in post-game even if `skipInPostGame` parameter (in `SSteamStats.SetStat`) is `true`.
