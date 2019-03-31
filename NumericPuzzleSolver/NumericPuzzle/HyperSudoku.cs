using System;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    internal class HyperSudoku : Sudoku
    {
        public HyperSudoku() : base("Hyper Sudoku", "hs", PuzzleDefinitionType.Values)
        { }

        protected override void AddFixedEntries()
        {
            base.AddFixedEntries();
            int boxSize = (int)Math.Sqrt(GridSize.Width);
            foreach (int i in Enumerable.Range(0, (boxSize - 1) * (boxSize - 1)))
            {
                Entries.Add(new PuzzleEntry(new Rectangle(1 + (i % (boxSize - 1) * (boxSize + 1)), 1 + (i / (boxSize - 1) * (boxSize + 1)), boxSize, boxSize), seq => true, CluePartialUnique));
            }
        }
    }
}
