using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    internal class LatinSquare : Puzzle
    {
        public LatinSquare() : this("Latin square", "ls", PuzzleDefinitionType.Values, SizesAll, 5)
        { }

        protected LatinSquare(string name, string abbreviation, PuzzleDefinitionType definitionType, IEnumerable<int> validGridSizes, int defaultGridSize) :
            base(name, abbreviation, definitionType, true, validGridSizes, defaultGridSize)
        { }

        protected override void AddFixedEntries()
        {
            foreach (int i in Enumerable.Range(0, GridSize.Width))
            {
                Entries.Add(new PuzzleEntry(new Rectangle(i, 0, 1, GridSize.Height), seq => true, CluePartialUnique));
                Entries.Add(new PuzzleEntry(new Rectangle(0, i, GridSize.Width, 1), seq => true, CluePartialUnique));
            }
        }

        public override int MinCellValue => 1;
        public override int MaxCellValue => GridSize.Width;
    }
}
