
# Precise Clock

A plugin that shows current in-game time in a 24-hour (configurable) clock format.

![Showcase](readme-res/precise-clock-showcase.png)

It also displays the time when an event will start or end:
![Event start time](readme-res/precise-clock-event-start.png)
![Event end time](readme-res/precise-clock-event-end.png)

> [!NOTE]
> 1 minute in-game time does not equal to 1 second in-real time.
> It's because the plugin maps the in-game time that represents a value between 0 and 1 (`GVars.m_clock`) to a 24-hour diapason.

This plugin should work with custom night and day durations, even if they are changed in the middle of the game.

> [!TIP]
> This is client-only plugin. It doesn't require installation on host/everybody in the lobby.

## Configuration

### `[General]` `Color`

**Setting type:** `UnityEngine.Color` \
**Default value:** `AAAAAAFF` (`GColors.m_textGray170.Color`)

The color of the text used by the clock.

### `[General]` `ClockPosition`

**Setting type:** `ClockPosition` \
**Default value:** `Bottom` \
**Acceptable values:** `Bottom`, `AboveTimer`

Location of the clock. \
When `Bottom`, the clock time will be placed at the bottom, between hotbar and day/night clock. \
When `AboveTimer`, the night countdown will be lifted a little bit, and the clock time will be placed at its place.

### `[General]` `TimeFormat`

**Setting type:** `TimeFormat` \
**Default value:** `H24` \
**Acceptable values:** `H12AmPm`, `H24`, `Decimal`, `Unit`, `UnitPoint`

Format of time in the clock.

| Value | Description | Examples |
| ----- | ----------- | -------- |
| `H12AmPm` | Represents 12-hour clock | 5:24 AM, 12:00 PM, 9:30 PM, 12:00 AM |
| `H24` | Represents 24-hour clock | 0:00, 4:10, 13:20, 20:00 |
| `Decimal` | Represents [decimal time](https://en.wikipedia.org/wiki/Decimal_time). Decimal time divides the day into 10 hours, each hour into 100 minutes, creating a metric-based time system. | 0:00, 3:80, 6:40, 9:90 |
| `Unit` | Represents time as a value between 0 and 1 (exclusive) without decimal point | 16400, 18000, 59230, 91735 |
| `UnitPoint` | Represents time as a value between 0 and 1 (exclusive) with decimal point | 0.2194, 0.6234, 0.7400, 0.9000 |

### `[General]` `Enable`

**Setting type:** `bool` \
**Default value:** `true`

Enables the plugin.
