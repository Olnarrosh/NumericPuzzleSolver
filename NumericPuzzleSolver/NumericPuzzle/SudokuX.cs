using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    internal class SudokuX : Sudoku
    {
        public SudokuX() : base("Sudoku X", "sx", PuzzleDefinitionType.Values)
        { }

        protected override void AddFixedEntries()
        {
            base.AddFixedEntries();
            Entries.Add(new PuzzleEntry(Enumerable.Range(0, GridSize.Width).Select(i => new Point(i, i)), seq => true, CluePartialUnique));
            Entries.Add(new PuzzleEntry(Enumerable.Range(0, GridSize.Width).Select(i => new Point(i, GridSize.Width - 1 - i)), seq => true, CluePartialUnique));
        }
    }
}
