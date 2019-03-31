using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    public class PuzzleEntry
    {
        /// <summary>The coordinates of the cells that comprise the entry.</summary>
        public IEnumerable<Point> Cells { get; set; }
        /// <summary>
        /// The constraints complete sequences have to fulfill to be valid solutions for the entry.
        /// </summary>
        public Predicate<IEnumerable<int?>> ClueFull { get; }
        /// <summary>
        /// The constraints elements have to fulfill to be added to partial solution sequences for the entry.
        /// </summary>
        public Func<IEnumerable<int?>, int, bool> CluePartial { get; }
        /// <summary>The numeric value associated with the entry.</summary>
        public int? Value { get; }
        /// <summary>
        /// True if the rules governing the entry do not require the cells to be considered in a particular order,
        /// false if the order of cells in potential solutions must be the same as the order defined in the entry.
        /// </summary>
        public bool Reorderable { get; }

        public PuzzleEntry(IEnumerable<Point> cells, Predicate<IEnumerable<int?>> clueFull, Func<IEnumerable<int?>, int, bool> cluePartial, bool reorderable = true, int? value = null)
        {
            Cells = new HashSet<Point>(cells);
            ClueFull = clueFull;
            CluePartial = cluePartial;
            Reorderable = reorderable;
            Value = value;
        }

        public PuzzleEntry(Rectangle area, Predicate<IEnumerable<int?>> clueFull, Func<IEnumerable<int?>, int, bool> cluePartial, bool reorderable = true, int? value = null) :
            this(Enumerable.Range(area.X, area.Width).SelectMany(x => Enumerable.Range(area.Y, area.Height).Select(y => new Point(x, y))), clueFull, cluePartial, reorderable, value)
        { }
    }
}
