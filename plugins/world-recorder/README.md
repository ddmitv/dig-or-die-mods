
# World Recorder

Allows to record entire world into video (or sequence of .bmp files) or take a screenshot of it.

![Showcase](readme-res/showcase.gif)

> [!TIP]
> Use F9 (default) to start/stop recording.
>
> Use F9 + Left Shift (default) to take single screenshot.

## Configuration

### `[General]` `Period`

**Setting type:** `double` \
**Default value:** `5.0`

Period in seconds between frame creation.

### `[General]` `OutputDir`

**Setting type:** `string` \
**Default value:** `{path to games root}/WorldRecordings`

Directory where recording are saved (default in games root folder)

### `[General]` `ToggleKey`

**Setting type:** `KeyboardShortcut` \
**Default value:** `F9`

Start/Stop world recording key

### `[General]` `ForceCreateFrame`

**Setting type:** `KeyboardShortcut` \
**Default value:** ` `

Imminently create a world frame.

### `[General]` `AsyncEncoding`

**Setting type:** `bool` \
**Default value:** `false`

> [!CAUTION]
> Only use if you have performance issues. Hypothetically could cause frame corruption.

Bypasses the intermediate buffer for temporary world state saving.

### `[General]` `LightingMode`

**Setting type:** `LightingMode` \
**Default value:** `FullLighting` \
**Acceptable values:** `FullLighting`, `MonochromeLighting`, `RGBLighting`

Lighting calculation method for rendering.

| Option               | Description                                                              |
| -------------------- | ------------------------------------------------------------------------ |
| `FullLighting`       | Renders image ignoring all lighting (same color as in minimap) for cells |
| `MonochromeLighting` | Renders image using average lighting for cells                           |
| `RGBLighting`        | Renders image using red, green and blue color for cells                  |
### `[General]` `ScreenshotWorld`

**Setting type:** `KeyboardShortcut` \
**Default value:** `F9 + LeftShift`

Create a screenshot (single frame) of the world without recording and saves it as `screenshot-{date}.bmp`.

### `[General]` `OpenOutputDir`

**Setting type:** `KeyboardShortcut` \
**Default value:** ` `

Opens file explorer in the output directory.

### `[Encoder]` `UseEncoder`

**Setting type:** `bool` \
**Default value:** `true`

Dynamically encodes frames into video using FFmpeg (FFmpeg should be in the path or in the same folder where plugin is located).

If `false`, will dump all frames into separate directory in `.bmp` format. You can then later encode them into different format.

> [!IMPORTANT]
> Because the `.bmp` files are not compressed, they will take up a lot of disk space (~3 MB per frame).

### `[Encoder]` `FPS`

**Setting type:** `uint` \
**Default value:** `5`

Frames per second for video encoding (FFmpeg option `-framerate`).

### `[Encoder]` `Container`

**Setting type:** `string` \
**Default value:** `mp4`

The container that will use the encoder (i.e. file extension).

### `[Encoder]` `Args`

**Setting type:** `string` \
**Default value:** `-c:v libx264 -crf 23 -preset fast -pix_fmt yuv420p`

Additional command line arguments for FFmpeg
