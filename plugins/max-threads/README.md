
# Max Threads (WIP)

Makes the game use maximum number of logical threads (`System.Environment.ProcessorCount`) in the world simulation logic.
By default, the game always only uses 4 threads regardless of available ones.

> [!Caution]
> It is currently unknown how the game uses additional threads. Use the plugin at your own risk.

The number of threads is clamped up to 32 in `DigOrDie.dll`, `DllInit` exported function.

## Configuration

### `[General]` `OverrideThreadsNumber`

**Setting type:** `uint` \
**Default value:** `0`

If non-zero, overrides the number of threads used in world simulation instead of using `System.Environment.ProcessorCount`.

### `[General]` `Enabled`

**Setting type:** `bool` \
**Default value:** `true`

Enables the plugin.
