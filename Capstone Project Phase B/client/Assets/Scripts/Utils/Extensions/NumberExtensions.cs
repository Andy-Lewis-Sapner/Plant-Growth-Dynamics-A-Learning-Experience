/// <summary>
/// Extension methods for numeric types to handle cyclic ranges and minimum values.
/// </summary>
public static class NumberExtensions {
    /// <summary>
    /// Cycles a value within a specified range, handling negative values.
    /// </summary>
    /// <param name="value">The value to cycle.</param>
    /// <param name="min">The minimum range value (inclusive).</param>
    /// <param name="max">The maximum range value (exclusive).</param>
    /// <returns>The cycled value within the range.</returns>
    public static int CycleInRange(this int value, int min, int max) {
        int range = max - min;
        int normalized = (value - min) % range;
        return normalized < 0 ? normalized + range + min : normalized + min;
    }
    
    /// <summary>
    /// Returns the minimum of two double values.
    /// </summary>
    /// <param name="a">First double value.</param>
    /// <param name="b">Second double value.</param>
    /// <returns>The smaller of the two values.</returns>
    public static double Min(double a, double b) => a < b ? a : b;
}