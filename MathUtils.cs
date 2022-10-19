namespace Sheep;

/// <summary>
/// Maths related utility functions.
/// </summary>

internal static class Utils
{
    /// <summary>
    /// Ensures value is between the min and max.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="val"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    internal static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0)
        {
            return min;
        }

        if (val.CompareTo(max) > 0)
        {
            return max;
        }

        return val;
    }

    /// <summary>
    /// Computes the distance between 2 points using Pythagoras's theorem a^2 = b^2 + c^2.
    /// </summary>
    /// <param name="pt1">First point.</param>
    /// <param name="pt2">Second point.</param>
    /// <returns></returns>
    public static float DistanceBetweenTwoPoints(PointF pt1, PointF pt2)
    {
        float dx = pt2.X - pt1.X;
        float dy = pt2.Y - pt1.Y;

        return (float)Math.Sqrt(dx * dx + dy * dy);
    }


    /// <summary>
    /// Determines whether a point is on the line
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="c"></param>
    /// <param name="closest"></param>
    /// <returns></returns>
    public static bool IsOnLine(PointF p0, PointF p1, PointF c, out PointF closest)
    {
        // calc delta distance: source point to line start
        var dx = c.X - p0.X;
        var dy = c.Y - p0.Y;

        // calc delta distance: line start to end
        var dxx = p1.X - p0.X;
        var dyy = p1.Y - p0.Y;

        // Calc position on line normalized between 0.00 & 1.00
        // == dot product divided by delta line distances squared
        var t = (dx * dxx + dy * dyy) / (dxx * dxx + dyy * dyy);

        // calc nearest pt on line
        var x = p0.X + dxx * t;
        var y = p0.Y + dyy * t;

        // clamp results to being on the segment
        if (t < 0) { x = p0.X; y = p0.Y; }
        if (t > 1) { x = p1.X; y = p1.Y; }

        closest = new PointF(x, y);

        return (t >= 0 && t <= 1);
    }
}