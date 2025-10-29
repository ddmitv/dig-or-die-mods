
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace GameEngine;

public readonly struct Realtime {
    private readonly long _ticks;

    private Realtime(long ticks) => _ticks = ticks;

    public static Realtime FromSeconds(double seconds) => new((long)(seconds * TimeSpan.TicksPerSecond));
    public static Realtime FromTicks(long ticks) => new(ticks);

    public double Seconds => (double)_ticks / TimeSpan.TicksPerSecond;
    public long Ticks => _ticks;
}

public static class Game {
    private static readonly Stopwatch stopwatch = new();
    private static long previousTicks = 0;

    private static double startSimuTime = 0;
    private static double simuDeltaTime = 0;
    private static long timeAppStart = 0;

    private static ulong frameCount = 0;

    private static TimeSpan _nextAutoSaveTime;
    private static ulong saveCount = 1;
    private static TimeSpan _autoSaveInterval;

    public static ulong FrameCount => frameCount;

    public static double SimuDeltaTimeD => simuDeltaTime;
    public static float SimuDeltaTime => (float)simuDeltaTime;

    public static double SimuTimeD => GVars.m_simuTimeD;
    public static float SimuTime => (float)GVars.m_simuTimeD;

    public static Realtime RealtimeSinceStartup => Realtime.FromTicks(DateTime.Now.Ticks - timeAppStart); 

    public static void Init() {
        timeAppStart = DateTime.Now.Ticks;

        stopwatch.Start();
        previousTicks = stopwatch.ElapsedTicks;
        _autoSaveInterval = TimeSpan.Parse(Server.Config.AutoSaveInterval);
        _nextAutoSaveTime = stopwatch.Elapsed + _autoSaveInterval;

        startSimuTime = GVars.m_simuTimeD;

        Directory.CreateDirectory(Path.GetDirectoryName(Server.Config.SavePath)!);
    }
    public static void Update() {
        frameCount += 1;

        long currentTicks = stopwatch.ElapsedTicks;
        TimeSpan currentTime = stopwatch.Elapsed;

        simuDeltaTime = (double)(currentTicks - previousTicks) / Stopwatch.Frequency;
        previousTicks = currentTicks;

        GVars.m_simuTimeD = startSimuTime + (double)currentTicks / Stopwatch.Frequency;

        GVars.m_cloudPosRatio = ((float)GVars.m_simuTimeD / GParams.m_cloudCycleDuration) % 1f;
        GVars.m_clock = ((float)GVars.m_simuTimeD / GParams.m_dayDurationTotal) % 1f;

        if (currentTime > _nextAutoSaveTime) {
            _nextAutoSaveTime = currentTime + _autoSaveInterval;
            SaveGame();
        }
    }
    public static void SaveGame() {
        int saveSlotNumber = BitOperations.TrailingZeroCount(saveCount) + 1;
        string savePath = string.Format(Server.Config.SavePath, saveSlotNumber);
        Logging.Info($"Saving game to \"{savePath}\" (saveCount={saveCount})");
        File.WriteAllBytes(savePath, SaveManager.Save());
        saveCount += 1;
    }

    public static bool HasRealtimeElapsed(Realtime startTime, TimeSpan interval) {
        return RealtimeSinceStartup.Ticks > startTime.Ticks + interval.Ticks;
    }

    public static bool IsRocketPrep() {
        return GVars.m_cinematicRocketStep == GVars.RocketStep.Count0_50 || GVars.m_cinematicRocketStep == GVars.RocketStep.Count50_Wait || GVars.m_cinematicRocketStep == GVars.RocketStep.Count50to100;
    }
    public static float GetNightClockHalfDuration() {
        return 0.5f * GParams.m_nightDuration / GParams.m_dayDurationTotal;
    }
    public static bool IsNight() {
        return GVars.m_clock < GetNightClockHalfDuration() || GVars.m_clock > 1f - GetNightClockHalfDuration();
    }
    public static float GetNightDurationLeft() {
        return Utils.PosMod(0.5f * GetNightClockHalfDuration() - GVars.m_clock, 1f) * GParams.m_dayDurationTotal;
    }
}
