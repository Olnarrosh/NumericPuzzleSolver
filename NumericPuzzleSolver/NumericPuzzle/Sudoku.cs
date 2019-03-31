using System;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    internal class Sudoku : LatinSquare
    {
        public Sudoku() : this("Sudoku", "su", PuzzleDefinitionType.Values)
        { }

        protected Sudoku(string name, string abbreviation, PuzzleDefinitionType definitionType) :
            base(name, abbreviation, definitionType, SizesSquares, 9)
        { }

        protected override void AddFixedEntries()
        {
            base.AddFixedEntries();
            int boxSize = (int)Math.Sqrt(GridSize.Width);
            foreach (int i in Enumerable.Range(0, GridSize.Width))
                Entries.Add(new PuzzleEntry(new Rectangle((i % boxSize) * boxSize, (i / boxSize) * boxSize, boxSize, boxSize), seq => true, CluePartialUnique));
        }
    }
}
