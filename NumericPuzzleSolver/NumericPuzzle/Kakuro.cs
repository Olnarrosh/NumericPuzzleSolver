using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    internal class Kakuro : Puzzle, IDynamicEntryPuzzle
    {
        public Kakuro() : base("Kakuro", "ka", PuzzleDefinitionType.Entries, false, SizesAll, 16)
        { }

        public bool LinearEntries() => true;
        public bool OneEntryPerCell() => false;
        public bool DynamicEntriesReorderable() => true;
        public override int MinCellValue => 1;
        public override int MaxCellValue => 9;
        public int MaxEntryValue(int entrySize) => Enumerable.Range(MaxCellValue - entrySize + 1, entrySize).Sum();
        public int MinEntryValue(int entrySize) => Enumerable.Range(1, entrySize).Sum();
        protected override void AddFixedEntries() { }
        public Predicate<IEnumerable<int?>> GetClueFull(int value) => seq => seq.Sum() == value;
        public Func<IEnumerable<int?>, int, bool> GetCluePartial(int value) => (seq, n) => CluePartialUnique(seq, n) && seq.Sum() + n <= value;
    }
}
