using System;

namespace TrafficSimulation.scripts.extensions;

public static class MathExtensions
{
    public static bool AlmostEqualTo(this double value1, double value2)
    {
        return Math.Abs(value1 - value2) < 0.0000001;
    }

    public static bool AlmostEqualTo(this float value1, float value2)
    {
        return Math.Abs(value1 - value2) < 0.0000001;
    }

    public static bool AlmostEqualTo(this float value1, double value2)
    {
        return Math.Abs(value1 - value2) < 0.0000001;
    }

    public static bool AlmostEqualTo(this double value1, float value2)
    {
        return Math.Abs(value1 - value2) < 0.0000001;
    }

    public static int Mod(int x, int m)
    {
        var r = x % m;
        return r < 0 ? r + m : r;
    }

    public static double Mod(double x, double m)
    {
        var r = x % m;
        return r < 0 ? r + m : r;
    }

    public static float Mod(float x, float m)
    {
        var r = x % m;
        return r < 0 ? r + m : r;
    }
}