
# World Recorder

Allows to record entire world into video (or sequence of .bmp files) or take a single screenshot of it.

![Showcase](readme-res/showcase.gif)

> [!IMPORTANT]
> For video encoding, the FFmpeg program is required.
> <details>
> <summary>Installing FFmpeg</summary>
> 
> ### Method 1: Place in game directory
> 
> 1. Download the same ZIP file as above
> 2. Extract only `ffmpeg.exe` from the `bin` folder
> 3. Place `ffmpeg.exe` in the same folder where plugin's `.dll` (`world_recorder.dll`) is located.
> 
> ### Method 2: Install to system PATH
> 
> 1. Download the Windows build from [FFmpeg's official site](https://ffmpeg.org/download.html#build-windows)
>    - Direct link: https://www.gyan.dev/ffmpeg/builds/#release-builds
> 2. Choose the `ffmpeg-release-essentials.zip` (`.7z`) file
> 3. Extract the `.zip` (`.7z`) to a permanent location (e.g., `C:\Program Files\ffmpeg`)
> 4. Add FFmpeg to your system PATH:
>    - Open Start Menu -> search for "Edit environment variables"
>    - Under "System variables", select `Path` -> Edit
>    - Click New -> Add the path to the `bin` folder (e.g., `C:\Program Files\ffmpeg\bin`)
>    - Click OK to save all changes
> </details>

> [!TIP]
> Use F9 (default) to start/stop recording.
>
> Use F9 + Left Shift (default) to take single screenshot.

# Configuration

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
**Acceptable values:** `FullLighting`, `MonochromeLighting`, `RGBLighting`, `LightingMapMonochrome`, `LightingMapRGB`, `ForceVectorMap`, `ElectricityMap`

Lighting calculation method for rendering.

| Option                  | Description                                                                  |
| ----------------------- | ---------------------------------------------------------------------------- |
| `FullLighting`          | Renders image ignoring all lighting (same color as in minimap) for cells     |
| `MonochromeLighting`    | Renders image using average lighting for cells                               |
| `RGBLighting`           | Renders image using red, green and blue color for cells                      |
| `LightingMapMonochrome` | Renders image as average lighting map (without using cell's colors)          |
| `LightingMapRGB`        | Renders image as red, green and blue lightings (without using cell's colors) |
| `ForceVectorMap`        | Renders image taking force direction as hue and magnitude as brightness      |
| `ElectricityMap`        | Renders image as highlights of electricity production/consumption            |

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
