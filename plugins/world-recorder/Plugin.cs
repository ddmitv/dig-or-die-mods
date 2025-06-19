using BepInEx;
using BepInEx.Configuration;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

internal static class BmpEncoder {
    private static readonly byte[][] _paddingTable = [
        [], [0], [0, 0], [0, 0, 0]
    ];

    public delegate Color32 GetPixelDelegate(uint x, uint y);

    public static void WriteBmp(Stream stream, uint width, uint height, GetPixelDelegate getColor) {
        using var writer = new BinaryWriter(stream);
        uint length = width * height * 3;

        // BM signature
        writer.Write((ushort)0x4D42);
        // File size
        writer.Write((uint)(40 + 14 + length));
        // Reserved
        writer.Write((uint)0);
        // Pixel data offset
        writer.Write((uint)54);
        // Header size
        writer.Write((uint)40);
        // Image width
        writer.Write((uint)width);
        // Image height
        writer.Write((uint)height);
        // Planes
        writer.Write((ushort)1);
        // Bits per pixel (RGB24)
        writer.Write((ushort)24);
        // Compression (none)
        writer.Write((uint)0);
        // Image size (can be 0 for uncompressed)
        writer.Write((uint)0);
        // Horizontal resolution (pixels/meter)
        writer.Write((uint)0);
        // Vertical resolution (pixels/meter)
        writer.Write((uint)0);
        // Colors in palette
        writer.Write((uint)0);
        // Important colors
        writer.Write((uint)0);

        byte[] padding = _paddingTable[(4 - ((width * 3) % 4)) % 4];
        // 0 -> 0, 1 -> 3, 2 -> 2, 3 -> 1

        for (long y = height - 1; y >= 0; --y) {
            for (uint x = 0; x < width; ++x) {
                Color32 color = getColor(x, (uint)y);
                writer.Write(color.b);
                writer.Write(color.g);
                writer.Write(color.r);
            }

            writer.Write(padding);
        }
    }
}

internal sealed class SingleThreadedWorker : IDisposable {
    private readonly Thread _worker;
    private readonly AutoResetEvent _trigger = new(initialState: false);
    private readonly ManualResetEvent _completed = new(initialState: true);

    private readonly Action _task;
    private volatile bool _running = true;
    private bool _disposed = false;

    public SingleThreadedWorker(Action task) {
        _task = task ?? throw new ArgumentNullException(nameof(task));
        _worker = new Thread(WorkerLoop) { IsBackground = true };
        _worker.Start();
    }

    public void Run() {
        ThrowIfDisposed();

        _completed.Reset();
        _trigger.Set();
    }

    public void Wait() {
        ThrowIfDisposed();

        _completed.WaitOne();
    }

    private void WorkerLoop() {
        while (true) {
            _trigger.WaitOne();
            if (!_running) { break; }

            try {
                _task();
            } finally {
                _completed.Set();
            }
        }
    }

    public void Dispose() {
        if (_disposed) { return; }
        _disposed = true;
        
        _running = false;
        _trigger.Set();

        if (!_worker.Join(500)) {
            _worker.Interrupt();
            _worker.Join(100);
        }

        _trigger.Close();
        _completed.Close();
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(SingleThreadedWorker));
        }
    }
}

internal static class CellRenderer {
    private static readonly Color32 MinimapColorNothing = SMisc.GetColor(16049106u);
    private static readonly Color32 MinimapColorGrass = SMisc.GetColor(16777070u);

    public enum LightingMode {
        FullBrightness,
        MonochromeLighting,
        RGBLighting
    }

    public delegate Color32 RenderCellFn(CCell cell);

    public static RenderCellFn GetRenderer(LightingMode type) {
        return type switch {
            LightingMode.FullBrightness => RenderFullBrightness,
            LightingMode.MonochromeLighting => RenderMonochromeLighting,
            LightingMode.RGBLighting => RenderRGBLighting,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(LightingMode))
        };
    }

    private static Color32 RenderFullBrightness(CCell cell) {
        CItemCell content = cell.GetContent();

        Color32 result = MinimapColorNothing;
        if (content != null) {
            result = cell.HasGrass() ? MinimapColorGrass : content.m_mainColor;
        } else if (cell.m_water > 0.3f) {
            result = cell.IsLava() ? GColors.m_lava.Color32 : GColors.m_water.Color32;
        } else if (cell.HasBgSurface()) {
            result = cell.GetBgSurface().m_color;
        }
        result.a = byte.MaxValue;
        return result;
    }
    private static Color32 RenderMonochromeLighting(CCell cell) {
        CItemCell content = cell.GetContent();

        Color32 result = MinimapColorNothing;
        if (content != null) {
            result = cell.HasGrass() ? MinimapColorGrass : content.m_mainColor;
        } else if (cell.m_water > 0.3f) {
            result = cell.IsLava() ? GColors.m_lava.Color32 : GColors.m_water.Color32;
        } else if (cell.HasBgSurface()) {
            result = cell.GetBgSurface().m_color;
        }
        float light = Mathf.Clamp01((float)cell.Light * (2f / 255f));
        result.r = (byte)((float)result.r * light);
        result.g = (byte)((float)result.g * light);
        result.b = (byte)((float)result.b * light);
        result.a = byte.MaxValue;
        return result;
    }
    private static Color32 RenderRGBLighting(CCell cell) {
        CItemCell content = cell.GetContent();

        Color32 result = MinimapColorNothing;
        if (content != null) {
            result = cell.HasGrass() ? MinimapColorGrass : content.m_mainColor;
        } else if (cell.m_water > 0.3f) {
            result = cell.IsLava() ? GColors.m_lava.Color32 : GColors.m_water.Color32;
        } else if (cell.HasBgSurface()) {
            result = cell.GetBgSurface().m_color;
        }
        result.r = (byte)((float)result.r * Mathf.Clamp01(cell.m_light.r * (2f / 255f)));
        result.g = (byte)((float)result.g * Mathf.Clamp01(cell.m_light.g * (2f / 255f)));
        result.b = (byte)((float)result.b * Mathf.Clamp01(cell.m_light.b * (2f / 255f)));
        result.a = byte.MaxValue;
        return result;
    }
}

[BepInPlugin("world-recorder", "World Recorder", "1.0.0")]
public class WorldRecorder : BaseUnityPlugin {
    private ConfigEntry<double> configFramePeriod;
    private ConfigEntry<string> configOutputDir;
    private ConfigEntry<KeyboardShortcut> configToggleKey;
    private ConfigEntry<bool> configUseEncoder;
    private ConfigEntry<uint> configEncoderFPS;
    private ConfigEntry<string> configEncoderOutputContainer;
    private ConfigEntry<string> configEncoderArgs;
    private ConfigEntry<KeyboardShortcut> configForceCreateFrame;
    private ConfigEntry<bool> configAsyncEncoding;
    private ConfigEntry<CellRenderer.LightingMode> configLightingMode;
    private ConfigEntry<KeyboardShortcut> configScreenshotWorld;

    private bool _isRecording = false;

    private string _localOutputDir;
    private uint _currentFrameIndex = 0;

    private Process ffmpegProcess;

    private double _lastScreenshotTime = double.MinValue;
    private CCell[,] _worldCellsCopy = { { } };
    private SingleThreadedWorker _worker;

    [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    private static unsafe extern void* memcpy(void* dest, void* src, UIntPtr count);

    void Awake() {
        configFramePeriod = Config.Bind<double>(
            section: "General", key: "Period",
            defaultValue: 5.0,
            description: "Period in seconds between frame creation"
        );
        configOutputDir = Config.Bind<string>(
            section: "General", key: "OutputDir",
            defaultValue: Path.GetFullPath(Utility.CombinePaths(Application.dataPath, "..", "WorldRecordings")),
            description: "Directory where recording are saved (default in games root folder)"
        );
        configToggleKey = Config.Bind<KeyboardShortcut>(
            section: "General", key: "ToggleKey",
            defaultValue: new KeyboardShortcut(KeyCode.F9),
            description: "Start/Stop world recording key"
        );
        configForceCreateFrame = Config.Bind<KeyboardShortcut>(
            section: "General", key: "ForceCreateFrame",
            defaultValue: KeyboardShortcut.Empty,
            description: "Create frame right now"
        );
        configAsyncEncoding = Config.Bind<bool>(
            section: "General", key: "AsyncEncoding",
            defaultValue: false,
            description: "ONLY USE IF YOU HAVE PERFORMANCE ISSUES. Bypasses the intermediate buffer for temporary world state saving. Hypothetically could cause frame corruption"
        );
        configLightingMode = Config.Bind<CellRenderer.LightingMode>(
            section: "General", key: "LightingMode",
            defaultValue: CellRenderer.LightingMode.FullBrightness,
            description: "Lighting calculation method for rendering"
        );
        configScreenshotWorld = Config.Bind<KeyboardShortcut>(
            section: "General", key: "ScreenshotWorld",
            defaultValue: new KeyboardShortcut(KeyCode.F9, KeyCode.LeftShift),
            description: "Create a screenshot (single frame) of the world without recording"
        );
        configUseEncoder = Config.Bind<bool>(
            section: "Encoder", key: "UseEncoder",
            defaultValue: true,
            description: "Dynamically encode frames into video using FFmpeg (FFmpeg should be in the path or in the same folder where plugin is located)"
        );
        configEncoderFPS = Config.Bind<uint>(
            section: "Encoder", key: "FPS",
            defaultValue: 5,
            description: "The frames per second for video encoding (FFmpeg option `-framerate`)"
        );
        configEncoderOutputContainer = Config.Bind<string>(
            section: "Encoder", key: "Container",
            defaultValue: "mp4",
            description: "The container that will use the encoder (i.e. file extension)"
        );
        configEncoderArgs = Config.Bind<string>(
            section: "Encoder", key: "Args",
            defaultValue: "-c:v libx264 -crf 23 -preset fast -pix_fmt yuv420p",
            description: "Command line arguments for FFmpeg"
        );

        Directory.CreateDirectory(configOutputDir.Value);

        _worker = new(task: ImageSavingTask);
    }

    void Update() {
        if (SWorld.Grid == null) { return; }

        if (configScreenshotWorld.Value.IsDown()) {
            uint imageWidth = (uint)SWorld.Grid.GetLength(0);
            uint imageHeight = (uint)SWorld.Grid.GetLength(1);
            var cellRenderer = CellRenderer.GetRenderer(configLightingMode.Value);

            string filename = $"screenshot-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.bmp";
            string filePath = Path.Combine(configOutputDir.Value, filename);

            using FileStream fs = File.Create(filePath);
            BmpEncoder.WriteBmp(fs, imageWidth, imageHeight,
                (x, y) => cellRenderer(SWorld.Grid[x, imageHeight - 1 - y])
            );

            Logger.LogInfo($"Created world frame | saving image to '{filePath}'");
            DisplayScreenMessage($"Created world screenshot {filename}");
        }

        if (configToggleKey.Value.IsDown()) {
            _isRecording ^= true;
            if (_isRecording) {
                StartRecording();
            } else {
                StopRecording();
            }
        }
        if (!_isRecording) { return; }

        if (_lastScreenshotTime >= GVars.m_simuTimeD && !configForceCreateFrame.Value.IsDown()) { return; }
        _lastScreenshotTime = GVars.m_simuTimeD + configFramePeriod.Value;

        _worker.Wait();

        if (configAsyncEncoding.Value) {
            _worldCellsCopy = SWorld.Grid;
        } else {
            if (SWorld.Grid.GetLength(0) != _worldCellsCopy.GetLength(0) || SWorld.Grid.GetLength(1) != _worldCellsCopy.GetLength(1)) {
                _worldCellsCopy = new CCell[SWorld.Grid.GetLength(0), SWorld.Grid.GetLength(1)];
            }
            unsafe {
                fixed (CCell* src = SWorld.Grid, dest = _worldCellsCopy) {
                    memcpy(dest, src, (UIntPtr)(sizeof(CCell) * _worldCellsCopy.Length));
                }
            }
        }

        _worker.Run();
    }

    void OnDestroy() {
        _worker?.Dispose();
        if (_isRecording) { StopRecording(); }
    }

    private void StartRecording() {
        _currentFrameIndex = 0;
        _lastScreenshotTime = double.MinValue;

        if (!configUseEncoder.Value) {
            _localOutputDir = Path.Combine(configOutputDir.Value, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
            Directory.CreateDirectory(_localOutputDir);
        } else {
            string outputPath = Path.Combine(
                configOutputDir.Value,
                $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{configEncoderOutputContainer.Value}"
            );
            string ffmpegArgs =
                $"-y -f rawvideo -pixel_format rgb24 -video_size {SWorld.Gs.x}x{SWorld.Gs.y} " +
                $"-framerate {configEncoderFPS.Value} -i - " +
                $"{configEncoderArgs.Value} \"{outputPath}\"";

            ProcessStartInfo ffmpegStartInfo = new ProcessStartInfo() {
                FileName = GetFFmpegPath(),
                Arguments = ffmpegArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            };

            ffmpegProcess = new Process() { StartInfo = ffmpegStartInfo };
            ffmpegProcess.ErrorDataReceived += (sender, e) => {
                if (string.IsNullOrEmpty(e.Data)) { return; }

                Logger.LogInfo($"ffmpeg: {e.Data}");
            };
            try {
                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine();
            } catch (Win32Exception ex) {
                Logger.LogError($"Failed to find FFmpeg (filename: {ffmpegStartInfo.FileName}): {ex.Message}");
                return;
            } catch (Exception ex) {
                Logger.LogError($"FFmpeg failed to start: {ex.Message}");
                return;
            }
        }

        Logger.LogInfo("Starting world recording");
        DisplayScreenMessage("Starting world recording...");
    }
    private void StopRecording() {
        if (ffmpegProcess != null) {
            try {
                _worker.Wait(); // wait until the frame creation finishes

                if (!ffmpegProcess.HasExited) {
                    ffmpegProcess.StandardInput.Close();
                    ffmpegProcess.WaitForExit();
                }
                ffmpegProcess.Close();
            } catch (Exception e) {
                Logger.LogError($"Error stopping FFmpeg: {e.Message}");
            } finally {
                ffmpegProcess = null;
            }
        }
        Logger.LogInfo($"Stopping world recording ({_currentFrameIndex} frames)");
        DisplayScreenMessage($"Stopping world recording... ({_currentFrameIndex} frames)");
    }

    private void ImageSavingTask() {
        uint imageWidth = (uint)_worldCellsCopy.GetLength(0), imageHeight = (uint)_worldCellsCopy.GetLength(1);

        var cellRenderer = CellRenderer.GetRenderer(configLightingMode.Value);
        if (ffmpegProcess == null) {
            string filePath = Path.Combine(_localOutputDir, $"{_currentFrameIndex:0000}.bmp");
            Logger.LogInfo($"Frame #{_currentFrameIndex} : Saving world image to '{filePath}'");

            using FileStream fs = File.Create(filePath);
            BmpEncoder.WriteBmp(fs, imageWidth, imageHeight,
                (x, y) => cellRenderer(_worldCellsCopy[x, imageHeight - 1 - y])
            );
        } else {
            Logger.LogInfo($"Frame #{_currentFrameIndex}");

            var ffmpegIn = ffmpegProcess.StandardInput.BaseStream;

            byte[] frameBuffer = new byte[imageHeight * imageWidth * 3];
            int bufferIndex = 0;

            for (long y = imageHeight - 1; y >= 0; --y) {
                for (uint x = 0; x < imageWidth; ++x) {
                    Color32 color = cellRenderer(_worldCellsCopy[x, y]);

                    frameBuffer[bufferIndex + 0] = color.r;
                    frameBuffer[bufferIndex + 1] = color.g;
                    frameBuffer[bufferIndex + 2] = color.b;
                    bufferIndex += 3;
                }
            }

            ffmpegIn.Write(frameBuffer, 0, frameBuffer.Length);
            ffmpegIn.Flush();

            Logger.LogInfo($"Frame #{_currentFrameIndex} finished");
        }
        _currentFrameIndex += 1;
    }

    private static string GetFFmpegPath() {
        string localPath = Path.Combine(Paths.PluginPath, "ffmpeg.exe");
        return File.Exists(localPath) ? localPath : "ffmpeg";
    }

    private static void DisplayScreenMessage(string message) {
        SScreenMessages.Inst.AddMessage(message, centerOffset: new Vector2(0, 300f));
    }
}
