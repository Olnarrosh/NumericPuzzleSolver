using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    internal class KillerSudoku : Sudoku, IDynamicEntryPuzzle
    {
        public KillerSudoku() : base("Killer Sudoku", "ks", PuzzleDefinitionType.Entries)
        { }

        public bool LinearEntries() => false;
        public bool OneEntryPerCell() => true;
        public bool DynamicEntriesReorderable() => true;
        public int MaxEntryValue(int entrySize) => Enumerable.Range(MaxCellValue - entrySize + 1, entrySize).Sum();
        public int MinEntryValue(int entrySize) => Enumerable.Range(1, entrySize).Sum();
        public Predicate<IEnumerable<int?>> GetClueFull(int value) => seq => seq.Sum() == value;
        public Func<IEnumerable<int?>, int, bool> GetCluePartial(int value) => (seq, n) => CluePartialUnique(seq, n) && seq.Sum() + n <= value;
    }
}
