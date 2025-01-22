
# Precise Clock

A plugin that shows current time in a 24-hour clock format.

![Showcase](precise-clock-showcase.png)

It also displays the time that an event will start/end:
![Event start time](precise-clock-event-start.png)
![Event end time](precise-clock-event-end.png)

Note that 1 minute in the 24-hour clock is not 1 second in real time.
It is because the plugins simply maps the game's built-in time
that represents a value between 0 and 1 (`GVars.m_clock`) to a 24-hour diapason.

This plugin should work with custom night and day durations, even if they are changed in the middle of the game.

> [!IMPORTANT]
> This is client-only plugin. It doesn't require installation on host/everybody in the lobby.

## Configuration

### `Color`

**Setting type:** `UnityEngine.Color` \
**Default value:** `AAAAAAFF` (`GColors.m_textGray170.Color`)

The color of the text used by the clock.

### `ClockPosition`

**Setting type:** `ClockPosition` \
**Default value:** `Bottom` \
**Acceptable values:** `Bottom`, `AboveTimer`

Location of the clock. \
When `Bottom`, the clock time will be placed at the bottom, between hotbar and day/night clock. \
When `AboveTimer`, the night countdown will be lifted a little bit, and the clock time will be placed at its place.
