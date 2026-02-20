
# Autosave Tweaks

Enhances the game's saving system with the following configurable features:
- Confirmation popup when quitting game with unsaved progress.
- Customizable number of Autosave and Quicksave slots.
- Recovery save (prevents losing progress from accidental ALT-F4).

# Configuration

> [!TIP]
> Recommended to install the [`BepInEx.ConfigurationManager`](https://github.com/BepInEx/BepInEx.ConfigurationManager)
> plugin to modify configuration in-game by pressing `F1` key.

| Setting                | Type    | Default | Description                                                                                                             |
| ---------------------- | ------- | ------- | ----------------------------------------------------------------------------------------------------------------------- |
| `QuickSaveIsDefault`   | `bool`  | `false` | When enabled, clicking "SAVE AND QUIT" button performs a quicksave. Hold Shift to open the full save menu instead       |
| `UnsavedTimeThreshold` | `float` | `5`     | Show quit confirmation when you have unsaved progress older than this duration (in minutes). Set to 0 to always show it |
| `ShowQuitConfirmation` | `bool`  | `true`  | Show confirmation popup when quitting with unsaved progress                                                             |
| `AutosaveSlots`        | `int`   | `5`     | Maximum number of Autosave slots (A-Z) <br> **Acceptable value range:** From 1 to 26                                    |
| `QuicksaveSlots`       | `int`   | `3`     | Maximum number of Quicksave slots (A-Z) <br> **Acceptable value range:** From 1 to 26                                   |
| `EnableRecoverySave`   | `bool`  | `false` | Create a recovery save file when closing the game with unsaved progress (DOES NOT protect against crashes)              |
| `ExitOnShift`          | `bool`  | `true`  | When "SAVE AND QUIT" button with Shift modifier is pressed, the game immediately exits instead of opening the home menu |

