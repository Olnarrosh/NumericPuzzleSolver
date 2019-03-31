using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace NumericPuzzleSolver.NumericPuzzle
{
    public abstract class Puzzle
    {
        internal const string GRID_NOT_RECTANGULAR = "The puzzle grid must be rectangular.";
        internal const string GRID_NOT_SQUARE = "The puzzle grid must be square.";
        internal const int MAX_GRID_SIZE = 100;

        public enum PuzzleDefinitionType { Values, ValuesAndWritability, Entries }
        public enum PuzzleState { Uninitialized, Initialized, Solved, Unsolvable }

        /// <summary>
        /// File extensions for the puzzle types, corresponding to their Abbreviation properties.
        /// </summary>
        internal static readonly Dictionary<string, Func<Puzzle>> Abbr = new Dictionary<string, Func<Puzzle>>
        {
            ["hs"] = () => new HyperSudoku(),
            ["in"] = () => new InshiNoHeya(),
            ["ka"] = () => new Kakuro(),
            ["ks"] = () => new KillerSudoku(),
            ["ls"] = () => new LatinSquare(),
            ["s8"] = () => new Str8ts(),
            ["su"] = () => new Sudoku(),
            ["sx"] = () => new SudokuX(),
            ["ta"] = () => new Takuzu()
        };

        protected static readonly Func<IEnumerable<int?>, int, bool> CluePartialUnique = (seq, n) => !seq.Contains(n);

        protected static readonly IEnumerable<int> SizesAll = Enumerable.Range(1, MAX_GRID_SIZE),
            SizesSquares = Enumerable.Range(1, (int)Math.Sqrt(MAX_GRID_SIZE)).Select(i => i * i),
            SizesEven = Enumerable.Range(1, MAX_GRID_SIZE / 2).Select(i => i * 2);

        private static readonly IDictionary<IEnumerable<int>, string> GridSizeExceptionMessage = new Dictionary<IEnumerable<int>, string>
        {
            [SizesAll] = $"The puzzle size must be at least one and at most {MAX_GRID_SIZE}.",
            [SizesSquares] = $"The puzzle size must be a square number between one and {MAX_GRID_SIZE}.",
            [SizesEven] = $"The puzzle size must be an even number between two and {MAX_GRID_SIZE}."
        };

        /// <summary>The numbers each cell can contain.</summary>
        private ISet<int?>[,] Values;
        /// <summary>The condition for a cell to be able and required to be written to.</summary>
        private Predicate<Point> Writable;

        public abstract int MinCellValue { get; }
        public abstract int MaxCellValue { get; }
        public PuzzleDefinitionType DefinitionType { get; }
        public ICollection<PuzzleEntry> Entries { get; }
        public PuzzleState State { set; get; }
        public bool MustBeSquare { get; }
        public IEnumerable<int> ValidGridSizes { get; }
        public int DefaultGridSize { get; }
        public string Name { get; }
        public string Abbreviation { get; }

        protected Puzzle(string name, string abbreviation, PuzzleDefinitionType definitionType, bool mustBeSquare, IEnumerable<int> validGridSizes, int defaultGridSize)
        {
            if (!validGridSizes.Contains(defaultGridSize))
                throw new ArgumentException("The default grid size of a puzzle must be among its valid grid sizes.");
            Name = name;
            Abbreviation = abbreviation;
            DefinitionType = definitionType;
            MustBeSquare = mustBeSquare;
            ValidGridSizes = validGridSizes;
            DefaultGridSize = defaultGridSize;
            Entries = new LinkedList<PuzzleEntry>();
            State = PuzzleState.Uninitialized;
        }

        /// <summary>
        /// Initializes a Puzzle instance, defining its size, entries and potentially cell values and changing its State from Uninitialized to Initialized.
        /// </summary>
        /// <param name="args">
        /// The required arguments depend on the DefinitionType of the Puzzle as follows:
        /// <list type="bullet">
        /// <item>Values: An int?[,] the size of the grid that contains initially known cell values.</item>
        /// <item>ValuesAndWritability: An int?[,] with initially known cell values and a bool[,] of the same size indicating whether each cell can be written to.</item>
        /// <item>Entries: An IDictionary&lt;IEnumerable&lt;Point&gt;, int&gt; with the extent and associated value of every entry where these features can vary.</item>
        /// </list>
        /// </param>
        public void Initialize(params object[] args)
        {
            if (State != PuzzleState.Uninitialized)
                throw new InvalidOperationException("Only uninitialized puzzles can be initialized.");
            int?[,] grid = null;
            IDictionary<IEnumerable<Point>, int> entries = null;
            IDynamicEntryPuzzle thisDEP = null;
            switch (DefinitionType)
            {
                case PuzzleDefinitionType.Values:
                    if (args.Count() != 1 || !(args[0] is int?[,]))
                        throw new ArgumentException("Puzzle types defined by given values must be initialized with exactly one argument of type int?[,].");
                    grid = (int?[,])args[0];
                    Values = new ISet<int?>[grid.GetLength(1), grid.GetLength(0)];
                    Writable = cell => true;
                    break;
                case PuzzleDefinitionType.ValuesAndWritability:
                    if (args.Count() != 2 || !(args[0] is int?[,]) || !(args[1] is bool[,]))
                        throw new ArgumentException("Puzzle types defined by given values and writability must be initialized with exactly two arguments of types int?[,] and bool[,].");
                    grid = (int?[,])args[0];
                    bool[,] writability = (bool[,])args[1];
                    if (grid.GetLength(0) != writability.GetLength(0) || grid.GetLength(1) != writability.GetLength(1))
                        throw new ArgumentException("The grids indicating the given values and writability of a puzzle defined by those features must be the same size.");
                    Values = new ISet<int?>[grid.GetLength(1), grid.GetLength(0)];
                    Writable = cell => writability[cell.Y, cell.X];
                    break;
                case PuzzleDefinitionType.Entries:
                    if (args.Count() != 1 || !(args[0] is IDictionary<IEnumerable<Point>, int>))
                        throw new ArgumentException("Puzzle types defined by the extents and values of their entries must be initialized with exactly one argument of type IDictionary<IEnumerable<Point>, int>.");
                    thisDEP = this as IDynamicEntryPuzzle;
                    if (thisDEP == null)
                        throw new ArgumentException("Puzzle types defined by the extents and values of their entries must implement IDynamicEntryPuzzle.");
                    entries = (IDictionary<IEnumerable<Point>, int>)args[0];
                    Values = new ISet<int?>[entries.Keys.SelectMany(cells => cells).Max(cell => cell.Y) + 1, entries.Keys.SelectMany(cells => cells).Max(cell => cell.X) + 1];
                    if (thisDEP.OneEntryPerCell())
                        Writable = cell => true;
                    else
                        Writable = cell => GetCandidates(cell).Count != 1 || GetCandidates(cell).Single() != null;
                    break;
                default:
                    throw new NotSupportedException();
            }
            if (!ValidGridSizes.Contains(GridSize.Width) || !ValidGridSizes.Contains(GridSize.Height))
                throw new ArgumentException(GridSizeExceptionMessage[ValidGridSizes]);
            if (MustBeSquare && Values.GetLength(0) != Values.GetLength(1))
                throw new ArgumentException("This puzzle type must have the same number of rows and columns.");
            IEnumerable<int?> possibleValues = Enumerable.Range(MinCellValue, MaxCellValue - MinCellValue + 1).Select(n => (int?)n);
            switch (DefinitionType)
            {
                case PuzzleDefinitionType.Values:
                case PuzzleDefinitionType.ValuesAndWritability:
                    foreach (int y in Enumerable.Range(0, Values.GetLength(0)))
                        foreach (int x in Enumerable.Range(0, Values.GetLength(1)))
                            Values[y, x] = grid[y, x].HasValue ? new HashSet<int?> { grid[y, x].Value } : new HashSet<int?>(possibleValues);
                    break;
                case PuzzleDefinitionType.Entries:
                    foreach (KeyValuePair<IEnumerable<Point>, int> entry in entries)
                    {
                        IEnumerable<Point> cells;
                        if (thisDEP.LinearEntries())
                        {
                            if (entry.Key.Count() > 2 || entry.Key.Count() < 1)
                                throw new ArgumentException("Linear entries must be defined by their first and last cell, or by their only cell.");
                            else if (entry.Key.Count() == 1)
                                cells = entry.Key;
                            else
                            {
                                cells = entry.Key.OrderBy(cell => cell.Y).ThenBy(cell => cell.X);
                                if (cells.ElementAt(0).Equals(cells.ElementAt(1)))
                                    cells = cells.Take(1).ToArray();
                                else if (cells.ElementAt(0).Y == cells.ElementAt(1).Y)
                                    cells = Enumerable.Range(cells.ElementAt(0).X, cells.ElementAt(1).X - cells.ElementAt(0).X + 1)
                                        .Select(y => new Point(y, cells.ElementAt(0).Y)).ToArray();
                                else if (cells.ElementAt(0).X == cells.ElementAt(1).X)
                                    cells = Enumerable.Range(cells.ElementAt(0).Y, cells.ElementAt(1).Y - cells.ElementAt(0).Y + 1)
                                        .Select(x => new Point(cells.ElementAt(0).X, x)).ToArray();
                                else
                                    throw new ArgumentException("The first and last cell of a linear entry must be either in the same row or in the same column.");
                            }
                        }
                        else
                        {
                            cells = entry.Key;
                        }
                        if (entry.Value < thisDEP.MinEntryValue(cells.Count()) || entry.Value > thisDEP.MaxEntryValue(cells.Count()))
                            throw new ArgumentException($"An entry of length {cells.Count()} cannot have a value of {entry.Value}.");
                        Entries.Add(new PuzzleEntry(cells, thisDEP.GetClueFull(entry.Value), thisDEP.GetCluePartial(entry.Value), thisDEP.DynamicEntriesReorderable(), entry.Value));
                    }
                    IEnumerable<Point> allCells = Entries.SelectMany(entry => entry.Cells);
                    if (thisDEP.OneEntryPerCell() && allCells.Count() != allCells.Distinct().Count())
                        throw new ArgumentException("This puzzle type does not allow for a cell to belong to multiple entries.");
                    foreach (int y in Enumerable.Range(0, Values.GetLength(0)))
                        foreach (int x in Enumerable.Range(0, Values.GetLength(1)))
                        {
                            if (allCells.Contains(new Point(x, y)))
                            {
                                Values[y, x] = new HashSet<int?>(possibleValues);
                            }
                            else
                            {
                                if (thisDEP.OneEntryPerCell())
                                    throw new ArgumentException("This puzzle type requires each cell to be part of an entry.");
                                else
                                    Values[y, x] = new HashSet<int?> { null };
                            }
                        }
                    break;
                default:
                    throw new NotSupportedException();
            }
            AddFixedEntries();
            State = PuzzleState.Initialized;
        }

        /// <summary>Adds entries whose extents and constraints are fully defined by the puzzle type.</summary>
        protected abstract void AddFixedEntries();
        public Size GridSize => new Size(Values.GetLength(1), Values.GetLength(0));
        public bool IsWritable(Point cell) => Writable(cell);
        /// <summary></summary>
        /// <param name="cell">The target cell.</param>
        /// <returns>The values the cell can contain.</returns>
        public ISet<int?> GetCandidates(Point cell) => Values[cell.Y, cell.X];
        /// <summary>Restricts the possible values of a cell.</summary>
        /// <param name="cell">The target cell.</param>
        /// <param name="vs">The values the cell should still be able contain.</param>
        public void RetainCandidates(Point cell, IEnumerable<int?> vs) => Values[cell.Y, cell.X].IntersectWith(vs);
        /// <summary>The value of a cell, if known.</summary>
        /// <param name="cell">The target cell.</param>
        /// <returns>The only value a cell can contain, if there is such a value, and null otherwise.</returns>
        public int? GetValue(Point cell) => GetCandidates(cell).Count() == 1 ? GetCandidates(cell).Single() : null;

        public override string ToString()
        {
            int maxLength = 0;
            foreach (int y in Enumerable.Range(0, Values.GetLength(0)))
            {
                foreach (int x in Enumerable.Range(0, Values.GetLength(1)))
                {
                    maxLength = Math.Max(maxLength, (GetValue(new Point(x, y))?.ToString()?.Length ?? 0) + (IsWritable(new Point(x, y)) ? 0 : 1));
                }
            }
            var sb = new StringBuilder(Values.Length * (1 + maxLength));
            foreach (int y in Enumerable.Range(0, Values.GetLength(0)))
            {
                foreach (int x in Enumerable.Range(0, Values.GetLength(1)))
                {
                    string val = GetValue(new Point(x, y))?.ToString();
                    sb.Append(IsWritable(new Point(x, y)) ? (val ?? ".").PadRight(maxLength) : ((val ?? "") + "#").PadLeft(maxLength));
                    sb.Append(x == Values.GetLength(1) - 1 ? '\n' : ' ');
                }
            }
            return sb.ToString();
        }
    }
}
