using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    internal class Takuzu : Puzzle
    {
        protected static readonly Predicate<IEnumerable<int?>> clueFullBalanced = seq => seq.Sum() == seq.Count() / 2;
        protected static readonly Func<IEnumerable<int?>, int, bool> cluePartialMaxClusterSize = (seq, n) => seq.Count() < 2 || seq.Skip(seq.Count() - 2).Any(elt => elt != n);
        protected static readonly Predicate<IEnumerable<int?>> clueFullUniqueRowsAndColumns = seq =>
        {
            var len = (int)Math.Sqrt(seq.Count());
            for (int x0 = 0; x0 < len - 1; x0++)
            {
                for (int x1 = x0 + 1; x1 < len; x1++)
                {
                    bool rowDiff = false, colDiff = false;
                    for (int y = 0; y < len && !(rowDiff && colDiff); y++)
                    {
                        rowDiff = rowDiff || seq.ElementAt((x0 * len) + y) != seq.ElementAt((x1 * len) + y);
                        colDiff = colDiff || seq.ElementAt(x0 + (len * y)) != seq.ElementAt(x1 + (len * y));
                    }
                    if (!rowDiff || !colDiff)
                        return false;
                }
            }
            return true;
        };

        public Takuzu() : base("Takuzu", "ta", PuzzleDefinitionType.Values, true, SizesEven, 10)
        { }

        public override int MinCellValue => 0;
        public override int MaxCellValue => 1;

        protected override void AddFixedEntries()
        {
            foreach (int i in Enumerable.Range(0, GridSize.Width))
            {
                Entries.Add(new PuzzleEntry(new Rectangle(i, 0, 1, GridSize.Height), clueFullBalanced, cluePartialMaxClusterSize, false));
                Entries.Add(new PuzzleEntry(new Rectangle(0, i, GridSize.Width, 1), clueFullBalanced, cluePartialMaxClusterSize, false));
            }
            Entries.Add(new PuzzleEntry(new Rectangle(0, 0, GridSize.Width, GridSize.Height), clueFullUniqueRowsAndColumns, (seq, n) => true, false));
        }
    }
}
