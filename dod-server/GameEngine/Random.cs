
using System;

namespace GameEngine;

public static class Random {
    private readonly static System.Random _randomGen = new();

    public static float Float() {
        return _randomGen.NextSingle();
    }
    public static float FloatBetween(float min, float max) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
        return Float() * (max - min) + min;
    }
    public static int IntBetween(int min, int max) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
        return _randomGen.Next(min, max);
    }
}
