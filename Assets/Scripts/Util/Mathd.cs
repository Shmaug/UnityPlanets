using System;

public static class Mathd {
    public const double Pi = Math.PI;
    public const double PiTimes2 = Math.PI * 2.0;
    public const double PiOver2 = Math.PI * .50;

    public const double Deg2Rad = 180.0 / Math.PI / 180.0;
    public const double Rad2Deg = Math.PI / 180.0;
    public const double Epsilon = Double.Epsilon;

    public static double Floor(double x) {
        return Math.Floor(x);
    }
    public static double Ceil(double x) {
        return Math.Ceiling(x);
    }

    public static double Lerp(double a, double b, double t) {
        return a + (b - a) * t;
    }

    public static double Exp(double x) {
        return Math.Exp(x);
    }

    public static double Pow(double x, double y) {
        return Math.Pow(x, y);
    }

    public static double Log(double x) {
        return Math.Log(x);
    }

    public static double Log(double x, double b) {
        return Math.Log(x, b);
    }

    public static double Sqrt(double x) {
        return Math.Sqrt(x);
    }

    public static double Clamp01(double x) {
        return x < 0 ? 0 : (x > 1 ? 1 : x);
    }

    public static double Clamp(double x, double low, double high) {
        return x < low ? low : (x > high ? high : x);
    }

    public static double Max(double x, double y) {
        return x > y ? x : y;
    }
    public static double Min(double x, double y) {
        return x < y ? x : y;
    }
    public static double Sign(double x) {
        return x < 0 ? -1 : (x > 0 ? 1 : 0);
    }

    public static double Abs(double x) {
        return x >= 0 ? x : (x * -1.0);
    }

    public static double Cos(double x) {
        return Math.Cos(x);
    }
    public static double Sin(double x) {
        return Math.Sin(x);
    }
    public static double Tan(double x) {
        return Math.Tan(x);
    }

    public static double Acos(double x) {
        return Math.Acos(x);
    }
    public static double Asin(double x) {
        return Math.Asin(x);
    }
    public static double Atan(double x) {
        return Math.Atan(x);
    }
    public static double Atan2(double y, double x) {
        return Math.Atan2(y, x);
    }

}
