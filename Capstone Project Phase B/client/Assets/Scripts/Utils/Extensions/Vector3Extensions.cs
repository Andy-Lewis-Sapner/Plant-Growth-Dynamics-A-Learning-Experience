using UnityEngine;

/// <summary>
/// Extension methods for Vector3 comparisons.
/// </summary>
public static class Vector3Extensions {
    /// <summary>
    /// Checks if all components of vector1 are greater than those of vector2.
    /// </summary>
    /// <param name="vector1">The first vector.</param>
    /// <param name="vector2">The second vector.</param>
    /// <returns>True if all components of vector1 are greater than vector2's, else false.</returns>
    public static bool IsGreaterThan(this Vector3 vector1, Vector3 vector2) {
        return vector1.x > vector2.x && vector1.y > vector2.y && vector1.z > vector2.z;
    }
}