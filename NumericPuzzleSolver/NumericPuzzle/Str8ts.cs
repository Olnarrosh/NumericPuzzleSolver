using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NumericPuzzleSolver.NumericPuzzle
{
    internal class Str8ts : LatinSquare
    {
        protected static readonly Predicate<IEnumerable<int?>> clueFullStraight = seq =>
        {
            int? max = seq.Max();
            return seq.All(elt => elt == max || seq.Contains(elt + 1));
        };

        public Str8ts() : base("Str8ts", "s8", PuzzleDefinitionType.ValuesAndWritability, SizesAll, 9) { }

        protected override void AddFixedEntries()
        {
            base.AddFixedEntries();
            foreach (int y in Enumerable.Range(0, GridSize.Height))
                foreach (int x in Enumerable.Range(0, GridSize.Width - 1))
                {
                    TryAddCompartment(y, x, true);
                    TryAddCompartment(x, y, false);
                }
        }

        protected bool TryAddCompartment(int y, int x, bool horizontal)
        {
            int xmod = horizontal ? 1 : 0,
                ymod = horizontal ? 0 : 1,
                dim = horizontal ? x : y,
                gridSize = (horizontal ? GridSize.Width : GridSize.Height) - 1;
            if (IsWritable(new Point(x, y)) && (dim == 0 || !IsWritable(new Point(x - xmod, y - ymod))))
            {
                int length = 0;
                while (IsWritable(new Point(x + (xmod * length), y + (ymod * length))) && length++ + dim < gridSize)
                { }
                if (length > 1)
                {
                    Entries.Add(new PuzzleEntry(new Rectangle(x, y, horizontal ? length : 1, horizontal ? 1 : length), clueFullStraight, CluePartialUnique, false));
                    return true;
                }
            }
            return false;
        }
    }
}
