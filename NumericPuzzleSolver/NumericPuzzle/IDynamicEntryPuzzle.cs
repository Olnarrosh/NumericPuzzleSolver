using System;
using System.Collections.Generic;

namespace NumericPuzzleSolver.NumericPuzzle
{
    interface IDynamicEntryPuzzle
    {
        /// <summary>Whether all cells in an entry must be in the same row/column.</summary>
        bool LinearEntries();
        /// <summary>Whether each cell must belong to exactly one entry.</summary>
        bool OneEntryPerCell();
        /// <summary>
        /// Whether the order of cells in entries not fully specified by the rules of the puzzle type is irrelevant w.r.t. per-entry constraints.
        /// </summary>
        bool DynamicEntriesReorderable();
        Func<IEnumerable<int?>, int, bool> GetCluePartial(int value);
        Predicate<IEnumerable<int?>> GetClueFull(int value);
        int MinEntryValue(int entrySize);
        int MaxEntryValue(int entrySize);
    }
}
