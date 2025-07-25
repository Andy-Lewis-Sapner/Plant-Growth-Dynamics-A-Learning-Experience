using System.Collections.Generic;

/// <summary>
/// Extension methods for List to add items with limits and duplicate checks.
/// </summary>
public static class ListExtensions {
    /// <summary>
    /// Adds an item to the list if within max count and duplicate rules.
    /// </summary>
    /// <typeparam name="T">Type of list items.</typeparam>
    /// <param name="list">The list to add to.</param>
    /// <param name="item">The item to add.</param>
    /// <param name="maxCount">Maximum allowed items in the list.</param>
    /// <param name="allowDuplicates">Whether duplicates are allowed.</param>
    public static void AddWithLimit<T>(this List<T> list, T item, int maxCount, bool allowDuplicates = true) {
        if (!allowDuplicates && list.Contains(item)) return;
        if (list.Count < maxCount) list.Add(item);
    }
}