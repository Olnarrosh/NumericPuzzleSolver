using NumericPuzzleSolver.NumericPuzzle;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace NumericPuzzleSolver.NumericPuzzleSolver
{
    internal static class SolverCLI
    {
        private static string AbbrsStr
        {
            get
            {
                var sb = new StringBuilder();
                foreach (KeyValuePair<string, Func<Puzzle>> kvp in Puzzle.Abbr)
                    sb.Append($"    {kvp.Key} ({kvp.Value().Abbreviation})\n");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Reads a puzzle from a file.
        /// For puzzles with given numbers, each line in the file corresponds to a row in the puzzle, and lists its cells, separated by spaces,
        /// which may contain a number and/or a # symbol to indicate the cell is not writable, or anything else to indicate its value is unknown.
        /// For puzzles with variable entries, each line in the file corresponds to one of those entries, and lists its cells or, if the entry
        /// shape is a straight line, its first and last cell, in the format "x,y", separated by spaces, and finally the value associated with it.
        /// </summary>
        /// <param name="file">The file to read from.</param>
        /// <param name="puzzleConstructor">A function returning an uninitialized puzzle of the type the file describes.</param>
        /// <returns>The initialized puzzle.</returns>
        public static Puzzle ReadPuzzle(FileInfo file, Func<Puzzle> puzzleConstructor)
        {
            IEnumerable<IEnumerable<string>> lines;
            Puzzle puzzle = puzzleConstructor();
            using (StreamReader reader = file.OpenText())
                lines = reader.ReadToEnd().Trim().Split('\n').Select(line => line.Split(' ').Where(s => s.Any()));
            switch (puzzle.DefinitionType)
            {
                case Puzzle.PuzzleDefinitionType.Values:
                    puzzle.Initialize(ParseValues(lines, out _));
                    break;
                case Puzzle.PuzzleDefinitionType.ValuesAndWritability:
                    puzzle.Initialize(ParseValues(lines, out bool[,] writable), writable);
                    break;
                case Puzzle.PuzzleDefinitionType.Entries:
                    puzzle.Initialize(ParseEntries(lines, (IDynamicEntryPuzzle)puzzle));
                    break;
                default:
                    throw new NotSupportedException();
            }
            return puzzle;
        }

        private static int?[,] ParseValues(IEnumerable<IEnumerable<string>> lines, out bool[,] writable)
        {
            var values = new int?[lines.Count(), lines.First().Count()];
            writable = new bool[lines.Count(), lines.First().Count()];
            foreach (int y in Enumerable.Range(0, lines.Count()))
            {
                if (lines.ElementAt(y).Count() != values.GetLength(1))
                    throw new ArgumentException(Puzzle.GRID_NOT_RECTANGULAR);
                foreach (int x in Enumerable.Range(0, lines.ElementAt(y).Count()))
                {
                    string s = lines.ElementAt(y).ElementAt(x);
                    values[y, x] = int.TryParse(s.Replace("#", ""), out int i) ? (int?)i : null;
                    writable[y, x] = !s.Contains('#');
                }
            }
            return values;
        }

        private static IDictionary<IEnumerable<Point>, int> ParseEntries(IEnumerable<IEnumerable<string>> lines, IDynamicEntryPuzzle puzzle)
        {
            var entries = new Dictionary<IEnumerable<Point>, int>();
            foreach (IEnumerable<string> line in lines)
            {
                try
                {
                    IEnumerable<Point> cells = line.Take(line.Count() - 1)
                      .Select(pointstr => pointstr.Split(',').Select(coordstr => int.Parse(coordstr)))
                      .Select(coordinates => new Point(coordinates.ElementAt(0), coordinates.ElementAt(1)));
                    if (puzzle.LinearEntries() && (cells.Count() > 2 || cells.Count() < 1))
                        throw new ArgumentOutOfRangeException();
                    entries[cells] = int.Parse(line.Last());
                }
                catch (FormatException e) { throw new FormatException("Cells in an entry must be represented as pairs of comma-separated coordinates.", e); }
                catch (ArgumentOutOfRangeException e)
                {
                    throw new ArgumentOutOfRangeException(puzzle.LinearEntries() ?
                        "An entry must be represented by its first and its last cell as pairs of comma-separated coordinates and its value, all separated by spaces." :
                        "An entry must be represented by a list of its cells as pairs of comma-separated coordinates and its value, all separated by spaces.", e);
                }
                catch (Exception) { throw; }
            }
            return entries;
        }

        public static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.Error.WriteLine("Files containing puzzles to be solved must be given as arguments.\n" +
                    "These files must either each have an extension indicating its type, or the extension of the type of all of them must be given as the first argument.\n" +
                    "Extensions and types correspond as follows:\n" + AbbrsStr);
                return;
            }
            Func<Puzzle> constructor = null;
            IEnumerable<FileInfo> files;
            if (Puzzle.Abbr.ContainsKey(args[0].ToLower()))
            {
                if (args.Length < 2)
                {
                    Console.Error.WriteLine("Files containing puzzles to be solved must be given as arguments.");
                    return;
                }
                constructor = Puzzle.Abbr[args[0].ToLower()];
                files = args.Skip(1).Select(s => new FileInfo(s));
            }
            else
                files = args.Select(s => new FileInfo(s));
            bool abbrsListed = false;
            foreach (FileInfo file in files)
            {
                string output = file.Name + ":\n";
                try
                {
                    Func<Puzzle> fileType = constructor ?? Puzzle.Abbr[file.Extension];
                    Puzzle puzzle = ReadPuzzle(file, fileType);
                    Solver.Solve(puzzle);
                    output += puzzle.State == Puzzle.PuzzleState.Solved ? puzzle.ToString() : (Solver.PUZZLE_UNSOLVABLE_MESSAGE + "\n");
                }
                catch (KeyNotFoundException)
                {
                    if (abbrsListed)
                        output += "Error: The file lacks an appropriate extension.\n";
                    else
                        output += "Error: Each file must have an extension indicating its type, unless such an extension is given for all files as the first argument.\n" +
                            "Extensions and types correspond as follows:\n" + AbbrsStr;
                    abbrsListed = true;
                }
                catch (FileNotFoundException)
                {
                    output += "Error: The file could not be found.\n";
                }
                catch (IOException)
                {
                    output += "Error: The file could not be read.\n";
                }
                catch (Exception e)
                {
                    output += $"Error: {e.Message}\n";
                }
                finally
                {
                    Console.WriteLine(output);
                }
            }
        }
    }
}
