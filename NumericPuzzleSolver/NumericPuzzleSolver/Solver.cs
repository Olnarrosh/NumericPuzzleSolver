using NumericPuzzleSolver.NumericPuzzle;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace NumericPuzzleSolver.NumericPuzzleSolver
{
    public static class Solver
    {
        internal const string PUZZLE_UNSOLVABLE_MESSAGE = "There is no unambiguous solution.";

        // These constants determine how many entries are processed at once.
        // Empirically, these values seem decent for most puzzles, but using different values for each puzzle type or computing them from the
        // features of each individual puzzle might be better.
        private const double CHUNK_RATIO = 1.35;
        private const int CHUNK_SIZE = 10;

        /// <summary>
        /// Attempts to solve a Puzzle instance. Depending on whether this succeeds, its State is changed from Initialized to Solved or Unsolvable.
        /// </summary>
        /// <param name="puzzle">The puzzle to be solved.</param>
        public static void Solve(Puzzle puzzle)
        {
            if (puzzle.State != Puzzle.PuzzleState.Initialized)
                throw new System.InvalidOperationException("Only initialized puzzles can be solved.");
            var entrySeqs = new ConcurrentDictionary<PuzzleEntry, LinkedList<IEnumerable<int?>>>();
            var entries = new List<PuzzleEntry>(puzzle.Entries.Count);
            while (puzzle.Entries.Any())
            // find all valid sequences for "easiest" entries each iteration, then filter out mismatches from all entries processed so far
            {
                IEnumerable<IGrouping<int, PuzzleEntry>> entryGroups = puzzle.Entries
                    .GroupBy(entry => entry.Cells.Sum(cell => puzzle.GetCandidates(cell).Count))
                    .OrderBy(g => g.Key);
                IEnumerable<PuzzleEntry> newEntries = entryGroups
                    .TakeWhile(g => g.Key <= entryGroups.First().Key * CHUNK_RATIO)
                    .SelectMany(g => g).Take(CHUNK_SIZE).ToList();
                foreach (PuzzleEntry entry in newEntries)
                {
                    if (entry.Reorderable)
                        entry.Cells = entry.Cells.OrderBy(cell => puzzle.GetCandidates(cell).Count).ToList();
                    puzzle.Entries.Remove(entry);
                }
                entries.AddRange(newEntries);
                Parallel.ForEach(newEntries, entry => entrySeqs[entry] = GenerateSequences(entry, puzzle));
                while (entries.Aggregate(false, (changed, entry) => changed | FilterSequences(entry, puzzle, entrySeqs)))
                { }
            }
            // check whether all cells' values are known
            foreach (int y in Enumerable.Range(0, puzzle.GridSize.Height))
            {
                foreach (int x in Enumerable.Range(0, puzzle.GridSize.Width))
                {
                    var cell = new Point(x, y);
                    if (puzzle.GetValue(cell) == null && puzzle.IsWritable(cell))
                    {
                        puzzle.State = Puzzle.PuzzleState.Unsolvable;
                        return;
                    }
                }
            }
            puzzle.State = Puzzle.PuzzleState.Solved;
        }

        /// <summary>
        /// Generates sequences that the rules of the puzzle type and any known values allow to be solutions to an entry.
        /// </summary>
        /// <param name="entry">The entry to be solved.</param>
        /// <param name="puzzle">The puzzle the entry belongs to.</param>
        /// <returns>A list of candidate sequences, each in the same order as the cells in the entry.</returns>
        private static LinkedList<IEnumerable<int?>> GenerateSequences(PuzzleEntry entry, Puzzle puzzle)
        {
            var possibleSequences = new LinkedList<IEnumerable<int?>>();
            possibleSequences.AddLast(new List<int?>(0));
            foreach (Point cell in entry.Cells)
            {
                var newPossibleSequences = new LinkedList<IEnumerable<int?>>();
                foreach (IEnumerable<int?> seq in possibleSequences)
                {
                    if (puzzle.IsWritable(cell))
                        foreach (int n in puzzle.GetCandidates(cell))
                        {
                            if (entry.CluePartial(seq, n))
                            {
                                var newSeq = new List<int?>(seq.Count() + 1);
                                newSeq.AddRange(seq);
                                newSeq.Add(n);
                                newPossibleSequences.AddLast(newSeq);
                            }
                        }
                    else
                    {
                        var newSeq = new List<int?>(seq.Count() + 1);
                        newSeq.AddRange(seq);
                        newSeq.Add(puzzle.GetValue(cell));
                        newPossibleSequences.AddLast(newSeq);
                    }
                }
                possibleSequences = newPossibleSequences;
            }
            return new LinkedList<IEnumerable<int?>>(possibleSequences.Where(seq => entry.ClueFull(seq)));
        }

        /// <summary>
        /// Rules out potential solution sequences for an entry that conflict with what is known about other entries.
        /// </summary>
        /// <param name="entry">The entry to be checked.</param>
        /// <param name="puzzle">The puzzle the entry belongs to.</param>
        /// <param name="entrySeqs">The solution sequences to be checked.</param>
        /// <returns>Whether any sequences have been ruled out.</returns>
        private static bool FilterSequences(PuzzleEntry entry, Puzzle puzzle, IDictionary<PuzzleEntry, LinkedList<IEnumerable<int?>>> entrySeqs)
        {
            bool changed = false;
            var remove = new LinkedList<IEnumerable<int?>>();
            // loop through cells and their values in solution sequences
            Parallel.ForEach(entry.Cells
                .Select((point, i) => (point, entrySeqs[entry].Select(l => (l, l.ElementAt(i)))))
                .Where(tuple => puzzle.IsWritable(tuple.point)),
                ((Point cell, IEnumerable<(IEnumerable<int?>, int?)> seqs) t) =>
            {
                var seqs = t.seqs;
                var cell = t.cell;
                var possibleValues = new HashSet<int?>();
                foreach ((IEnumerable<int?> seq, int? val) in seqs)
                {
                    if (puzzle.GetCandidates(cell).Contains(val))
                        possibleValues.Add(val);
                    else
                        lock (remove)
                            remove.AddLast(seq);
                }
                // for each cell, only keep candidate values it attains in any sequence
                if (!possibleValues.IsSupersetOf(puzzle.GetCandidates(cell)))
                {
                    changed = true;
                    puzzle.RetainCandidates(cell, possibleValues);
                }
            }
            );
            // delete any sequences that would require cells to attain values that are not among their candidates
            if (remove.Any())
            {
                changed = true;
                entrySeqs[entry] = new LinkedList<IEnumerable<int?>>(entrySeqs[entry].Except(remove));
            }
            return changed;
        }
    }
}
