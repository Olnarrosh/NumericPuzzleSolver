using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    internal class InshiNoHeya : LatinSquare, IDynamicEntryPuzzle
    {
        public InshiNoHeya() : base("Inshi no heya", "in", PuzzleDefinitionType.Entries, SizesAll, 9)
        { }

        public bool LinearEntries() => true;
        public bool OneEntryPerCell() => true;
        public bool DynamicEntriesReorderable() => true;
        public int MaxEntryValue(int entrySize) => Enumerable.Range(MaxCellValue - entrySize + 1, entrySize).Aggregate((n, m) => n * m);
        public int MinEntryValue(int entrySize) => Enumerable.Range(1, entrySize).Aggregate((n, m) => n * m);
        public Predicate<IEnumerable<int?>> GetClueFull(int value) => seq => seq.Aggregate(1, (a, b) => a * b.Value) == value;
        public Func<IEnumerable<int?>, int, bool> GetCluePartial(int value) => (seq, n) => CluePartialUnique(seq, n) && seq.Aggregate(1, (a, b) => a * b.Value) <= value;
    }
}
