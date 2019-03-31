using NumericPuzzleSolver.NumericPuzzle;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static NumericPuzzleSolver.NumericPuzzle.Puzzle;

namespace NumericPuzzleSolver.NumericPuzzleSolver
{
    internal class SolverGUI : Form
    {
        private const string FORM_TITLE = "Numeric Puzzle Solver ‒ ",
            TITLE_SOLVED = " (solved)",
            TITLE_EMPTY = "empty",
            MENU_FILE = "File",
            MENU_FILE_NEW = "New",
            MENU_FILE_SAVE = "Save",
            MENU_FILE_LOAD = "Load",
            MENU_FILE_QUIT = "Quit";

        private const Border3DStyle BORDER_NONE = Border3DStyle.Adjust,
            BORDER_BASIC = Border3DStyle.Flat,
            BORDER_EDGE = Border3DStyle.Raised,
            BORDER_ENTRY = Border3DStyle.Sunken,
            BORDER_BOX = Border3DStyle.Bump;

        private Size GridSize;
        private Puzzle CurrentPuzzle;
        private Panel[,] Cells;
        private NumericUpDown[,,] CellsValues;
        private CheckBox[,] CellsKnown, CellsWritable, ConnectRight, ConnectDown;
        private Panel MainPanel = new Panel { AutoScroll = true, BackColor = SystemColors.AppWorkspace };
        private TableLayoutPanel GridTable = new TableLayoutPanel
        {
            ForeColor = DefaultForeColor,
            BackColor = DefaultBackColor,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        private Label NoPuzzleLabel = new Label
        {
            Text = $"To begin, create an empty puzzle (Menu → {MENU_FILE} → {MENU_FILE_NEW}) or load a puzzle file (Menu → {MENU_FILE} → {MENU_FILE_LOAD}).",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        private Form SizeDialog = new Form
        {
            ControlBox = false,
            Text = "Set puzzle size",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            ShowInTaskbar = false
        };
        private Label WidthLabel = new Label { Text = "Width:" };
        private Label HeightLabel = new Label { Text = "Height:" };
        private DomainUpDown WidthUpDown = new DomainUpDown { AutoSize = true },
            HeightUpDown = new DomainUpDown { AutoSize = true };
        private Button SolveButton = new Button { Text = "Solve", Enabled = false },
            SizeAcceptButton = new Button { Text = "OK" },
            SizeCancelButton = new Button { Text = "Cancel" };
        private MenuStrip MenuBar = new MenuStrip();
        private ToolStripMenuItem MenuFile = new ToolStripMenuItem(MENU_FILE),
            FileNew = new ToolStripMenuItem(MENU_FILE_NEW),
            FileSave = new ToolStripMenuItem(MENU_FILE_SAVE),
            FileLoad = new ToolStripMenuItem(MENU_FILE_LOAD),
            FileQuit = new ToolStripMenuItem(MENU_FILE_QUIT);
        private ToolTip Tooltip = new ToolTip();

        private SolverGUI()
        {
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 600);
            ClientSizeChanged += SolverGui_ClientSizeChanged;
            Text = FORM_TITLE + TITLE_EMPTY;
            MenuBar.Items.AddRange(new ToolStripItem[] { MenuFile });
            MenuFile.DropDownItems.AddRange(new ToolStripItem[] { FileNew, FileSave, FileLoad, FileQuit });
            new Takuzu();
            foreach (Func<Puzzle> constructor in Abbr.Values)
            {
                string typeName = constructor().Name;
                var menuItem = new ToolStripMenuItem { Text = typeName, Tag = constructor };
                menuItem.Click += NewPuzzle_Click;
                FileNew.DropDownItems.Add(menuItem);
                menuItem = new ToolStripMenuItem { Text = typeName, Tag = constructor };
                menuItem.Click += LoadPuzzle_Click;
                FileLoad.DropDownItems.Add(menuItem);
            }
            FileSave.Click += FileSave_Click;
            FileSave.Enabled = false;
            FileQuit.Click += FileQuit_Click;
            MainPanel.Location = new Point(0, MenuBar.Bottom);
            MainPanel.Size = new Size(ClientSize.Width, ClientSize.Height - MenuBar.Height - SolveButton.Height);
            MainPanel.Controls.Add(NoPuzzleLabel);
            GridTable.CellPaint += Cell_Paint;
            SolveButton.Click += SolveButton_Click;
            SolveButton.Location = new Point((ClientSize.Width - SolveButton.PreferredSize.Width) / 2, ClientSize.Height - SolveButton.Height);
            SizeAcceptButton.Click += SizeAcceptButton_Click;
            SizeCancelButton.Click += SizeCancelButton_Click;
            SizeDialog.AcceptButton = SizeAcceptButton;
            SizeDialog.CancelButton = SizeCancelButton;
            WidthUpDown.SelectedItemChanged += SizeUpDown_SelectionChanged;
            HeightUpDown.SelectedItemChanged += SizeUpDown_SelectionChanged;
            WidthLabel.Location = new Point(SizeDialog.Padding.Left, SizeDialog.Padding.Top);
            HeightLabel.Location = new Point(SizeDialog.Padding.Left, WidthLabel.Bottom + WidthLabel.Margin.Bottom);
            WidthLabel.Width = HeightLabel.Width = Math.Max(WidthLabel.PreferredWidth, HeightLabel.PreferredWidth);
            WidthUpDown.Location = new Point(WidthLabel.Right + WidthLabel.Margin.Right, WidthLabel.Top);
            HeightUpDown.Location = new Point(HeightLabel.Right + HeightLabel.Margin.Right, HeightLabel.Top);
            SizeAcceptButton.Location = new Point((HeightLabel.Width + HeightUpDown.Width + HeightLabel.Margin.Right + HeightUpDown.Margin.Left) / 2 - SizeAcceptButton.Width - SizeAcceptButton.Margin.Right, HeightLabel.Bottom + HeightLabel.Margin.Bottom);
            SizeCancelButton.Location = new Point((HeightLabel.Width + HeightUpDown.Width + HeightLabel.Margin.Right + HeightUpDown.Margin.Left) / 2 + SizeCancelButton.Margin.Left, HeightLabel.Bottom + HeightLabel.Margin.Bottom);
            SizeDialog.Controls.AddRange(new Control[] { HeightLabel, WidthLabel, HeightUpDown, WidthUpDown, SizeAcceptButton, SizeCancelButton });
            Controls.AddRange(new Control[] { MenuBar, MainPanel, SolveButton });
        }

        private void NewPuzzle_Click(object sender, EventArgs e)
        {
            // create a new instance of the specified puzzle type
            CurrentPuzzle = ((Func<Puzzle>)((ToolStripMenuItem)sender).Tag)();
            // ask the user for the puzzle size
            HeightUpDown.Items.Clear();
            WidthUpDown.Items.Clear();
            HeightUpDown.Items.AddRange(CurrentPuzzle.ValidGridSizes.Reverse().ToArray());
            WidthUpDown.Items.AddRange(CurrentPuzzle.ValidGridSizes.Reverse().ToArray());
            WidthUpDown.SelectedItem = HeightUpDown.SelectedItem = CurrentPuzzle.DefaultGridSize;
            SizeDialog.ShowDialog(this);
            if (SizeDialog.DialogResult.Equals(DialogResult.Cancel))
                return;
            GridSize = new Size((int)WidthUpDown.SelectedItem, (int)HeightUpDown.SelectedItem);
            // initialize the puzzle with dummy values
            switch (CurrentPuzzle.DefinitionType)
            {
                case PuzzleDefinitionType.Values:
                    CurrentPuzzle.Initialize(new int?[GridSize.Height, GridSize.Width]);
                    break;
                case PuzzleDefinitionType.ValuesAndWritability:
                    CurrentPuzzle.Initialize(new int?[GridSize.Height, GridSize.Width], new bool[GridSize.Height, GridSize.Width]);
                    break;
                case PuzzleDefinitionType.Entries:
                    var entries = new Dictionary<IEnumerable<Point>, int>();
                    int minValue = ((IDynamicEntryPuzzle)CurrentPuzzle).MinEntryValue(1);
                    foreach (Point cell in Enumerable.Range(0, GridSize.Width).SelectMany(x => Enumerable.Range(0, GridSize.Height).Select(y => new Point(x, y))))
                        entries[new List<Point> { cell }] = minValue;
                    CurrentPuzzle.Initialize(entries);
                    break;
                default:
                    throw new NotSupportedException();
            }
            MakeGrid();
        }

        private void LoadPuzzle_Click(object sender, EventArgs e)
        {
            // ask the user for the puzzle file to be loaded
            var constructor = (Func<Puzzle>)((ToolStripMenuItem)sender).Tag;
            using (var openDialog = new OpenFileDialog { Title = "Select file to read puzzle from", Filter = $"{sender.ToString()} files|*.{constructor().Abbreviation}|All files|*.*" })
            {
                if (openDialog.ShowDialog().Equals(DialogResult.OK))
                {
                    try
                    {
                        CurrentPuzzle = SolverCLI.ReadPuzzle(new FileInfo(openDialog.FileName), constructor);
                        // represent the loaded puzzle in the UI
                        GridSize = CurrentPuzzle.GridSize;
                        MakeGrid();
                        switch (CurrentPuzzle.DefinitionType)
                        {
                            case PuzzleDefinitionType.Values:
                            case PuzzleDefinitionType.ValuesAndWritability:
                                foreach (int y in Enumerable.Range(0, CurrentPuzzle.GridSize.Height))
                                    foreach (int x in Enumerable.Range(0, CurrentPuzzle.GridSize.Width))
                                    {
                                        int? cellValue = CurrentPuzzle.GetValue(new Point(x, y));
                                        if (cellValue.HasValue)
                                        {
                                            CellsKnown[y, x].Checked = true;
                                            CellsValues[y, x, 0].Value = cellValue.Value;
                                        }
                                        if (CurrentPuzzle.DefinitionType == PuzzleDefinitionType.ValuesAndWritability)
                                            CellsWritable[y, x].Checked = CurrentPuzzle.IsWritable(new Point(x, y));
                                    }
                                break;
                            case PuzzleDefinitionType.Entries:
                                foreach (PuzzleEntry entry in CurrentPuzzle.Entries.Where(entry => entry.Value.HasValue))
                                {
                                    foreach (Point cell in entry.Cells)
                                    {
                                        if (entry.Cells.Contains(new Point(cell.X + 1, cell.Y)))
                                            ConnectRight[cell.Y, cell.X].Checked = true;
                                        if (entry.Cells.Contains(new Point(cell.X, cell.Y + 1)))
                                            ConnectDown[cell.Y, cell.X].Checked = true;
                                    }
                                    CellsValues[entry.Cells.First().Y, entry.Cells.First().X,
                                        (((IDynamicEntryPuzzle)CurrentPuzzle).OneEntryPerCell() || entry.Cells.First().X != entry.Cells.Last().X) ? 0 : 1]
                                        .Value = entry.Value.Value;
                                }
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
        }

        private void FileSave_Click(object sender, EventArgs e)
        {
            // determine appropriate file extension based on the type of the puzzle and whether it is solved
            string ext = CurrentPuzzle.Abbreviation;
            if (CurrentPuzzle.State == PuzzleState.Solved)
                ext += ".sol";
            string filter = $"{CurrentPuzzle.Name} files|*.{ext}|All files|*.*";
            if (CurrentPuzzle.State == PuzzleState.Solved)
                filter = "Solved " + filter;
            // ask the user for the file to save the puzzle to
            using (var saveDialog = new SaveFileDialog
            {
                Title = "Select file to save puzzle to",
                DefaultExt = ext,
                Filter = filter
            })
            {
                if (saveDialog.ShowDialog().Equals(DialogResult.OK))
                {
                    try
                    {
                        // for unsolved entry-based puzzles, write a list of its entries, otherwise a grid of its cells
                        string s;
                        if (CurrentPuzzle.State == PuzzleState.Solved)
                            s = CurrentPuzzle.ToString();
                        else if (CurrentPuzzle.DefinitionType == PuzzleDefinitionType.Entries)
                        {
                            var sb = new StringBuilder();
                            foreach (KeyValuePair<IEnumerable<Point>, int> kvp in CollectEntries())
                            {
                                IEnumerable<Point> entryCells = kvp.Key.OrderBy(cell => cell.Y).ThenBy(cell => cell.X);
                                foreach (Point cell in entryCells)
                                    sb.Append($"{cell.X},{cell.Y} ");
                                sb.AppendLine(kvp.Value.ToString());
                            }
                            s = sb.ToString();
                        }
                        else
                        {
                            MakePuzzle();
                            s = CurrentPuzzle.ToString();
                        }
                        using (var writer = new StreamWriter(saveDialog.OpenFile()))
                        {
                            writer.Write(s);
                        }
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void FileQuit_Click(object sender, EventArgs e) => Close();

        private void MakePuzzle()
        {
            // create a Puzzle instance from its UI representation
            CurrentPuzzle = Abbr[CurrentPuzzle.Abbreviation]();
            switch (CurrentPuzzle.DefinitionType)
            {
                case PuzzleDefinitionType.Values:
                case PuzzleDefinitionType.ValuesAndWritability:
                    int?[,] grid = new int?[GridSize.Height, GridSize.Width];
                    bool[,] writable = CurrentPuzzle.DefinitionType == PuzzleDefinitionType.Values ? null : new bool[GridSize.Height, GridSize.Width];
                    foreach (int y in Enumerable.Range(0, GridSize.Height))
                    {
                        foreach (int x in Enumerable.Range(0, GridSize.Width))
                        {
                            grid[y, x] = CellsKnown[y, x].Checked ? (int?)CellsValues[y, x, 0].Value : null;
                            writable?.SetValue(CellsWritable[y, x].Checked, y, x);
                        }
                    }
                    if (CurrentPuzzle.DefinitionType == PuzzleDefinitionType.ValuesAndWritability)
                        CurrentPuzzle.Initialize(grid, writable);
                    else
                        CurrentPuzzle.Initialize(grid);
                    break;
                case PuzzleDefinitionType.Entries:
                    CurrentPuzzle.Initialize(CollectEntries());
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private IDictionary<IEnumerable<Point>, int> CollectEntries()
        {
            // check what entries each cell belongs to in the UI representation of the puzzle and add them to a list
            var entries = new Dictionary<IEnumerable<Point>, int>();
            foreach (int y in Enumerable.Range(0, GridSize.Height))
                foreach (int x in Enumerable.Range(0, GridSize.Width))
                {
                    if (((IDynamicEntryPuzzle)CurrentPuzzle).OneEntryPerCell())
                    {
                        if (!entries.Keys.SelectMany(k => k).Contains(new Point(x, y)))
                            entries.Add(GetEntry(new Point(x, y), true, true), (int)CellsValues[y, x, 0].Value);
                    }
                    else
                    {
                        if (!entries.Keys.Any(entryCells => entryCells.Contains(new Point(x, y)) && entryCells.Any(cell => cell.X != x && cell.Y == y)) && CellsValues[y, x, 0].Enabled)
                            entries.Add(GetEntry(new Point(x, y), true, false), (int)CellsValues[y, x, 0].Value);
                        if (!entries.Keys.Any(entryCells => entryCells.Contains(new Point(x, y)) && entryCells.Any(cell => cell.Y != y && cell.X == x)) && CellsValues[y, x, 1].Enabled)
                            entries.Add(GetEntry(new Point(x, y), false, true), (int)CellsValues[y, x, 1].Value);
                    }
                }
            // linear entries are reduced to their first and last cell
            if (((IDynamicEntryPuzzle)CurrentPuzzle).LinearEntries())
                return entries.ToDictionary(kvp => (IEnumerable<Point>)new Point[]
                    { new Point(kvp.Key.Min(p => p.X), kvp.Key.Min(p => p.Y)), new Point(kvp.Key.Max(p => p.X), kvp.Key.Max(p => p.Y)) },
                    kvp => kvp.Value);
            return entries;
        }

        private void MakeGrid()
        {
            // reset and adjust puzzle-dependent parts of the UI
            Cursor = Cursors.WaitCursor;
            GridTable.SuspendLayout();
            GridTable.Visible = false;
            MainPanel.Controls.Clear();
            if (!NoPuzzleLabel.IsDisposed)
                NoPuzzleLabel.Dispose();
            MainPanel.Controls.Add(GridTable);
            GridTable.Controls.Clear();
            GridTable.RowCount = GridSize.Height;
            GridTable.ColumnCount = GridSize.Width;
            if (Cells != null)
                foreach (Panel cell in Cells)
                {
                    foreach (Control cellChild in cell.Controls)
                        cellChild.Dispose();
                    cell.Dispose();
                }
            Cells = new Panel[GridSize.Height, GridSize.Width];
            switch (CurrentPuzzle.DefinitionType)
            {
                // for puzzles with variable entries, the user can set in the UI whether each cell belongs to the same entry as adjacent cells,
                // as well as the value of each entry, which is displayed in its cells
                case PuzzleDefinitionType.Entries:
                    var dynamicEntryPuzzle = (IDynamicEntryPuzzle)CurrentPuzzle;
                    CellsValues = new NumericUpDown[GridSize.Height, GridSize.Width, dynamicEntryPuzzle.OneEntryPerCell() ? 1 : 2];
                    ConnectRight = new CheckBox[GridSize.Height, GridSize.Width - 1];
                    ConnectDown = new CheckBox[GridSize.Height - 1, GridSize.Width];
                    Size? cellSize = null;
                    foreach (int y in Enumerable.Range(0, GridSize.Height))
                    {
                        foreach (int x in Enumerable.Range(0, GridSize.Width))
                        {
                            Cells[y, x] = new Panel { Dock = DockStyle.Fill };
                            CellsValues[y, x, 0] = new NumericUpDown
                            {
                                Tag = (x, y),
                                Minimum = dynamicEntryPuzzle.MinEntryValue(1),
                                Maximum = dynamicEntryPuzzle.MaxEntryValue(Math.Max(CurrentPuzzle.GridSize.Height, CurrentPuzzle.GridSize.Width)),
                                DecimalPlaces = 0
                            };
                            CellsValues[y, x, 0].Size = CellsValues[y, x, 0].PreferredSize;
                            CellsValues[y, x, 0].Maximum = dynamicEntryPuzzle.MaxEntryValue(1);
                            CellsValues[y, x, 0].ValueChanged += EntryValueUpDown_ValueChanged;
                            CellsValues[y, x, 0].Enter += EntryValueUpDown_Enter;
                            CellsValues[y, x, 0].Leave += EntryValueUpDown_Leave;
                            Cells[y, x].Controls.Add(CellsValues[y, x, 0]);
                            if (!dynamicEntryPuzzle.OneEntryPerCell())
                            {
                                CellsValues[y, x, 0].Enabled = false;
                                CellsValues[y, x, 1] = new NumericUpDown
                                {
                                    Top = CellsValues[y, x, 0].Bottom + CellsValues[y, x, 0].Margin.Bottom,
                                    Enabled = false,
                                    Tag = (x, y),
                                    Minimum = dynamicEntryPuzzle.MinEntryValue(1),
                                    Maximum = dynamicEntryPuzzle.MaxEntryValue(Math.Max(CurrentPuzzle.GridSize.Height, CurrentPuzzle.GridSize.Width)),
                                    DecimalPlaces = 0
                                };
                                CellsValues[y, x, 1].Size = CellsValues[y, x, 1].PreferredSize;
                                CellsValues[y, x, 1].Maximum = dynamicEntryPuzzle.MaxEntryValue(1);
                                CellsValues[y, x, 1].ValueChanged += EntryValueUpDown_ValueChanged;
                                CellsValues[y, x, 1].Enter += EntryValueUpDown_Enter;
                                CellsValues[y, x, 1].Leave += EntryValueUpDown_Leave;
                                Cells[y, x].Controls.Add(CellsValues[y, x, 1]);
                                Tooltip.SetToolTip(CellsValues[y, x, 0], "The sum of the horizontal entry this cell belongs to");
                                Tooltip.SetToolTip(CellsValues[y, x, 1], "The sum of the vertical entry this cell belongs to");
                            }
                            if (x < GridSize.Width - 1)
                            {
                                ConnectRight[y, x] = new CheckBox
                                {
                                    Text = "linked right",
                                    Left = CellsValues[y, x, 0].Right + CellsValues[y, x, 0].Margin.Right,
                                    Tag = (x, y),
                                    AutoSize = true
                                };
                                ConnectRight[y, x].CheckedChanged += ConnectRight_CheckedChanged;
                                Cells[y, x].Controls.Add(ConnectRight[y, x]);
                                Tooltip.SetToolTip(ConnectRight[y, x], "Set whether this cell belongs to the same entry as the cell to its right");
                            }
                            if (y < GridSize.Height - 1)
                            {
                                ConnectDown[y, x] = new CheckBox
                                {
                                    Text = "linked down",
                                    Top = CellsValues[y, x, 0].Bottom + CellsValues[y, x, 0].Margin.Bottom,
                                    Tag = (x, y),
                                    AutoSize = true
                                };
                                if (!dynamicEntryPuzzle.OneEntryPerCell())
                                    ConnectDown[y, x].Left = CellsValues[y, x, 1].Right + CellsValues[y, x, 1].Margin.Right;
                                ConnectDown[y, x].CheckedChanged += ConnectDown_CheckedChanged;
                                Cells[y, x].Controls.Add(ConnectDown[y, x]);
                                Tooltip.SetToolTip(ConnectDown[y, x], "Set whether this cell belongs to the same entry as the cell below it");
                            }
                            cellSize = Cells[y, x].Size = cellSize ?? Cells[y, x].PreferredSize;
                            GridTable.Controls.Add(Cells[y, x], x, y);
                        }
                    }
                    break;
                // for puzzles with predetermined entries, the user can enter initially known cell values via the UI, and, if applicable,
                // whether each cell can be written to
                case PuzzleDefinitionType.Values:
                case PuzzleDefinitionType.ValuesAndWritability:
                    CellsValues = new NumericUpDown[GridSize.Height, GridSize.Width, 1];
                    CellsKnown = new CheckBox[GridSize.Height, GridSize.Width];
                    CellsWritable = CurrentPuzzle.DefinitionType == PuzzleDefinitionType.Values ? null : new CheckBox[GridSize.Height, GridSize.Width];
                    foreach (int y in Enumerable.Range(0, GridSize.Height))
                    {
                        foreach (int x in Enumerable.Range(0, GridSize.Width))
                        {
                            CellsKnown[y, x] = new CheckBox
                            {
                                Tag = (x, y),
                                Text = "given",
                                AutoSize = true
                            };
                            CellsValues[y, x, 0] = new NumericUpDown
                            {
                                Minimum = CurrentPuzzle.MinCellValue,
                                Maximum = CurrentPuzzle.MaxCellValue,
                                Value = CurrentPuzzle.MinCellValue,
                                Location = new Point(0, CellsKnown[y, x].PreferredSize.Height + CellsKnown[y, x].Margin.Vertical),
                                Width = CellsKnown[y, x].PreferredSize.Width
                            };
                            Cells[y, x] = new Panel { Dock = DockStyle.Fill };
                            CellsKnown[y, x].CheckedChanged += CellKnown_CheckedChanged;
                            Cells[y, x].Controls.Add(CellsKnown[y, x]);
                            Tooltip.SetToolTip(CellsKnown[y, x], "Set whether the number in this cell is initially known");
                            int cellWidth = Math.Max(CellsKnown[y, x].PreferredSize.Width + CellsKnown[y, x].Margin.Horizontal, CellsValues[y, x, 0].PreferredSize.Width + CellsValues[y, x, 0].Margin.Horizontal),
                                cellHeight = CellsKnown[y, x].PreferredSize.Height + CellsKnown[y, x].Margin.Vertical + CellsValues[y, x, 0].PreferredSize.Height + CellsValues[y, x, 0].Margin.Vertical;
                            if (CurrentPuzzle.DefinitionType == PuzzleDefinitionType.ValuesAndWritability)
                            {
                                CellsWritable[y, x] = new CheckBox
                                {
                                    Text = "writable",
                                    Checked = true,
                                    Location = new Point(0, CellsValues[y, x, 0].Location.Y + CellsValues[y, x, 0].PreferredSize.Height + CellsValues[y, x, 0].Margin.Vertical),
                                    Tag = (x, y),
                                    AutoSize = true
                                };
                                CellsWritable[y, x].CheckedChanged += CellWritable_CheckedChanged;
                                Cells[y, x].Controls.Add(CellsWritable[y, x]);
                                Tooltip.SetToolTip(CellsWritable[y, x], "Set whether this is a cell the puzzle allows and requires a number to be written to");
                                cellWidth = Math.Max(cellWidth, CellsWritable[y, x].PreferredSize.Width + CellsWritable[y, x].Margin.Horizontal);
                                cellHeight += CellsWritable[y, x].PreferredSize.Height + CellsWritable[y, x].Margin.Vertical;
                            }
                            Cells[y, x].Size = new Size(cellWidth, cellHeight);
                            GridTable.Controls.Add(Cells[y, x], x, y);
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
            if (CurrentPuzzle.State == PuzzleState.Solved)
                Text = Text.Remove(Text.Length - TITLE_SOLVED.Length);
            GridTable.ResumeLayout();
            GridTable.Visible = true;
            GridTable.Location = new Point(Math.Max((MainPanel.Width - GridTable.Width) / 2, 0), Math.Max((MainPanel.Height - GridTable.Height) / 2, 0));
            SolveButton.Enabled = true;
            FileSave.Enabled = true;
            Text = FORM_TITLE + CurrentPuzzle.Name;
            Cursor = Cursors.Default;
        }

        private ICollection<Point> GetEntry(Point cell, bool horizontal, bool vertical) => GetEntry(cell, horizontal, vertical, new HashSet<Point>());

        // get all cells in an entry by recursively checking adjacent cells for whether they are marked as connected
        private ICollection<Point> GetEntry(Point cell, bool horizontal, bool vertical, ICollection<Point> entryCells)
        {
            entryCells.Add(cell);
            if (horizontal && cell.X > 0 && !entryCells.Contains(new Point(cell.X - 1, cell.Y)) && ConnectRight[cell.Y, cell.X - 1].Checked)
                GetEntry(new Point(cell.X - 1, cell.Y), horizontal, vertical, entryCells);
            if (vertical && cell.Y > 0 && !entryCells.Contains(new Point(cell.X, cell.Y - 1)) && ConnectDown[cell.Y - 1, cell.X].Checked)
                GetEntry(new Point(cell.X, cell.Y - 1), horizontal, vertical, entryCells);
            if (horizontal && cell.X < GridSize.Width - 1 && !entryCells.Contains(new Point(cell.X + 1, cell.Y)) && ConnectRight[cell.Y, cell.X].Checked)
                GetEntry(new Point(cell.X + 1, cell.Y), horizontal, vertical, entryCells);
            if (vertical && cell.Y < GridSize.Height - 1 && !entryCells.Contains(new Point(cell.X, cell.Y + 1)) && ConnectDown[cell.Y, cell.X].Checked)
                GetEntry(new Point(cell.X, cell.Y + 1), horizontal, vertical, entryCells);
            return entryCells;
        }

        private void SizeAcceptButton_Click(object sender, EventArgs e)
        {
            if (WidthUpDown.SelectedItem == null || HeightUpDown.SelectedItem == null)
            {
                WidthUpDown.SelectedItem = CurrentPuzzle.DefaultGridSize;
                HeightUpDown.SelectedItem = CurrentPuzzle.DefaultGridSize;
            }
            else
            {
                SizeDialog.DialogResult = DialogResult.OK;
                SizeDialog.Close();
            }
        }

        private void SizeCancelButton_Click(object sender, EventArgs e)
        {
            SizeDialog.DialogResult = DialogResult.Cancel;
            SizeDialog.Close();
        }

        private void ConnectionChanged(CheckBox checkBox, bool horizontal)
        {
            var dynamicEntryPuzzle = (IDynamicEntryPuzzle)CurrentPuzzle;
            (int x, int y) = ((int, int))checkBox.Tag;
            int xmod = horizontal ? 1 : 0,
                ymod = horizontal ? 0 : 1,
                dim = horizontal ? x : y,
                dimSize = horizontal ? GridSize.Width : GridSize.Height,
                cellValuePosition = (horizontal || dynamicEntryPuzzle.OneEntryPerCell()) ? 0 : 1;
            CheckBox[,] connectSame = horizontal ? ConnectRight : ConnectDown,
                        connectOther = horizontal ? ConnectDown : ConnectRight;
            // adjust minimum and maximum values of entries whose size has changed
            foreach (int c in Enumerable.Range(dim, 2))
            {
                ICollection<Point> entry = GetEntry(new Point(horizontal ? c : x, horizontal ? y : c), horizontal || !dynamicEntryPuzzle.LinearEntries(), !horizontal || !dynamicEntryPuzzle.LinearEntries());
                foreach (Point cell in entry)
                {
                    CellsValues[cell.Y, cell.X, cellValuePosition].Enabled = false;
                    CellsValues[cell.Y, cell.X, cellValuePosition].Minimum = dynamicEntryPuzzle.MinEntryValue(entry.Count);
                    CellsValues[cell.Y, cell.X, cellValuePosition].Maximum = dynamicEntryPuzzle.MaxEntryValue(entry.Count);
                    CellsValues[cell.Y, cell.X, cellValuePosition].Enabled = true;
                }
            }
            // control whether affected cells are writable or can belong to perpendicular entries
            if (dynamicEntryPuzzle.LinearEntries())
            {
                if (dynamicEntryPuzzle.OneEntryPerCell())
                {
                    foreach (int cx in Enumerable.Range(x - ymod, 2))
                    {
                        foreach (int cy in Enumerable.Range(y - xmod, 2))
                        {
                            int cDim = horizontal ? cx : cy;
                            if (cx >= 0 && cx < GridSize.Width - ymod && cy >= 0 && cy < GridSize.Height - xmod)
                                connectOther[cy, cx].Enabled =
                                    (cDim == dimSize - 1 || !connectSame[cy, cx].Checked) &&
                                    (cDim == 0 || !connectSame[cy - ymod, cx - xmod].Checked) &&
                                    (cDim == dimSize - 1 || !connectSame[cy + xmod, cx + ymod].Checked) &&
                                    (cDim == 0 || !connectSame[cy + (horizontal ? 1 : -1), cx + (horizontal ? -1 : 1)].Checked);
                        }
                    }
                }
                else
                {
                    CellsValues[y, x, cellValuePosition].Enabled = checkBox.Checked || (dim > 0 && connectSame[y - ymod, x - xmod].Checked);
                    CellsValues[y + ymod, x + xmod, cellValuePosition].Enabled = checkBox.Checked || (dim < dimSize - 2 && connectSame[y + ymod, x + xmod].Checked);
                }
            }
            // if the cell has been made part of an entry, the entry takes its value
            if (checkBox.Checked)
                CellsValues[y + ymod, x + xmod, cellValuePosition].Value = CellsValues[y, x, cellValuePosition].Value;
            GridTable.Invalidate(Cells[y, x].Region);
        }

        private void ConnectRight_CheckedChanged(object sender, EventArgs e)
        {
            ConnectionChanged((CheckBox)sender, true);
        }

        private void ConnectDown_CheckedChanged(object sender, EventArgs e)
        {
            ConnectionChanged((CheckBox)sender, false);
        }

        private void SolveButton_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            SolveButton.Enabled = false;
            MakePuzzle();
            Solver.Solve(CurrentPuzzle);
            if (CurrentPuzzle.State == PuzzleState.Solved)
            {
                Text += TITLE_SOLVED;
                GridTable.SuspendLayout();
                foreach (int y in Enumerable.Range(0, GridSize.Height))
                {
                    foreach (int x in Enumerable.Range(0, GridSize.Width))
                    {
                        var l = new Label { Text = CurrentPuzzle.GetValue(new Point(x, y))?.ToString() ?? "", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                        Cells[y, x].Controls.Clear();
                        Cells[y, x].Controls.Add(l);
                    }
                }
                GridTable.ResumeLayout();
            }
            else
            {
                MessageBox.Show(Solver.PUZZLE_UNSOLVABLE_MESSAGE);
                SolveButton.Enabled = true;
            }
            Cursor = Cursors.Default;
        }

        private void SizeUpDown_SelectionChanged(object sender, EventArgs e)
        {
            var upDown = (DomainUpDown)sender;
            if (upDown.SelectedItem == null)
            {
                if (int.TryParse(upDown.Text, out int value) && upDown.Items.Contains(value))
                    upDown.SelectedItem = value;
                else
                    upDown.SelectedItem = CurrentPuzzle.DefaultGridSize;
            }
            if (CurrentPuzzle.MustBeSquare)
            {
                if (sender == WidthUpDown)
                    HeightUpDown.SelectedItem = WidthUpDown.SelectedItem;
                else
                    WidthUpDown.SelectedItem = HeightUpDown.SelectedItem;
            }
        }

        private void CellKnown_CheckedChanged(object sender, EventArgs e)
        {
            var checkBox = (CheckBox)sender;
            (int x, int y) = ((int, int))checkBox.Tag;
            GridTable.SuspendLayout();
            if (checkBox.Checked)
                Cells[y, x].Controls.Add(CellsValues[y, x, 0]);
            else
                Cells[y, x].Controls.Remove(CellsValues[y, x, 0]);
            GridTable.ResumeLayout();
        }

        private void CellWritable_CheckedChanged(object sender, EventArgs e)
        {
            var checkBox = (CheckBox)sender;
            (int x, int y) = ((int, int))checkBox.Tag;
            Cells[y, x].ForeColor = checkBox.Checked ? DefaultForeColor : DefaultBackColor;
            Cells[y, x].BackColor = checkBox.Checked ? DefaultBackColor : DefaultForeColor;
        }

        private void SolverGui_ClientSizeChanged(object sender, EventArgs e)
        {
            MainPanel.Size = new Size(ClientSize.Width, ClientSize.Height - MenuBar.Height - SolveButton.Height);
            SolveButton.Location = new Point((ClientSize.Width - SolveButton.PreferredSize.Width) / 2, ClientSize.Height - SolveButton.Height);
            GridTable.Location = new Point(Math.Max((MainPanel.Width - GridTable.Width) / 2, 0), Math.Max((MainPanel.Height - GridTable.Height) / 2, 0));
        }

        private void EntryValueUpDown_ValueChanged(object sender, EventArgs e)
        {
            var upDown = (NumericUpDown)sender;
            if (!upDown.Enabled)
                return;
            (int x, int y) = ((int, int))upDown.Tag;
            // determine the alignment (horizontal/vertical) of the entry
            int cellValuePosition;
            bool horizontal, vertical;
            if (((IDynamicEntryPuzzle)CurrentPuzzle).OneEntryPerCell())
            {
                cellValuePosition = 0;
                horizontal = vertical = true;
            }
            else
            {
                cellValuePosition = sender == CellsValues[y, x, 0] ? 0 : 1;
                horizontal = cellValuePosition == 0;
                vertical = cellValuePosition == 1;
            }
            // propagate the value to the rest of the entry
            foreach (var cell in GetEntry(new Point(x, y), horizontal, vertical))
            {
                if (cell.X == x && cell.Y == y)
                    continue;
                CellsValues[cell.Y, cell.X, cellValuePosition].Enabled = false;
                CellsValues[cell.Y, cell.X, cellValuePosition].Value = upDown.Value;
                CellsValues[cell.Y, cell.X, cellValuePosition].Enabled = true;
            }
        }

        private void EntryValueUpDown_Enter(object sender, EventArgs e)
        {
            (int x, int y) = ((int, int))((NumericUpDown)sender).Tag;
            foreach (Point cell in GetEntry(
                new Point(x, y),
                CellsValues.GetLength(2) > 1 ? sender == CellsValues[y, x, 0] : true,
                CellsValues.GetLength(2) > 1 ? sender == CellsValues[y, x, 1] : true))
            {
                Cells[cell.Y, cell.X].ForeColor = SystemColors.HighlightText;
                Cells[cell.Y, cell.X].BackColor = SystemColors.Highlight;
            }
        }

        private void EntryValueUpDown_Leave(object sender, EventArgs e)
        {
            (int x, int y) = ((int, int))((NumericUpDown)sender).Tag;
            foreach (Point cell in GetEntry(
                new Point(x, y),
                CellsValues.GetLength(2) > 1 ? sender == CellsValues[y, x, 0] : true,
                CellsValues.GetLength(2) > 1 ? sender == CellsValues[y, x, 1] : true))
            {
                Cells[cell.Y, cell.X].ForeColor = SystemColors.ControlText;
                Cells[cell.Y, cell.X].BackColor = SystemColors.Control;
            }
        }

        private void Cell_Paint(object sender, TableLayoutCellPaintEventArgs e)
        {
            int x = e.Column, y = e.Row;
            Border3DStyle leftStyle = BORDER_BASIC,
                topStyle = BORDER_BASIC,
                rightStyle = BORDER_NONE,
                bottomStyle = BORDER_NONE;
            if (x == 0)
                leftStyle = BORDER_EDGE;
            if (y == 0)
                topStyle = BORDER_EDGE;
            if (x == GridSize.Width - 1)
                rightStyle = BORDER_EDGE;
            if (y == GridSize.Height - 1)
                bottomStyle = BORDER_EDGE;
            if ((CurrentPuzzle as IDynamicEntryPuzzle)?.OneEntryPerCell() ?? false)
            {
                if (x > 0)
                    leftStyle = ConnectRight[y, x - 1].Checked ? BORDER_NONE : BORDER_ENTRY;
                if (y > 0)
                    topStyle = ConnectDown[y - 1, x].Checked ? BORDER_NONE : BORDER_ENTRY;
            }
            if (CurrentPuzzle.GetType().Equals(typeof(Sudoku)) || CurrentPuzzle.GetType().IsSubclassOf(typeof(Sudoku)))
            {
                int boxSize = (int)Math.Sqrt(GridSize.Width);
                if (x < GridSize.Width && x % boxSize == boxSize - 1)
                    rightStyle = BORDER_BOX;
                if (y < GridSize.Height && y % boxSize == boxSize - 1)
                    bottomStyle = BORDER_BOX;
            }
            if (CurrentPuzzle.GetType().Equals(typeof(HyperSudoku)))
            {
                int boxSize = (int)Math.Sqrt(GridSize.Width) + 1;
                if (x % boxSize == 1 && y % boxSize > 0)
                    leftStyle = BORDER_BOX;
                if (y % boxSize == 1 && x % boxSize > 0)
                    topStyle = BORDER_BOX;
                if (x % boxSize == boxSize - 1 && y % boxSize > 0)
                    rightStyle = BORDER_BOX;
                if (y % boxSize == boxSize - 1 && x % boxSize > 0)
                    bottomStyle = BORDER_BOX;
            }
            if (CurrentPuzzle.GetType().Equals(typeof(SudokuX)))
            {
                if (x == y || x == GridSize.Height - y - 1)
                {
                    if (x > 0)
                        leftStyle = BORDER_BOX;
                    if (y > 0)
                        topStyle = BORDER_BOX;
                    if (x < GridSize.Width - 1)
                        rightStyle = BORDER_BOX;
                    if (y < GridSize.Height - 1)
                        bottomStyle = BORDER_BOX;
                }
            }
            ControlPaint.DrawBorder3D(e.Graphics, e.CellBounds, leftStyle, Border3DSide.Left);
            ControlPaint.DrawBorder3D(e.Graphics, e.CellBounds, topStyle, Border3DSide.Top);
            ControlPaint.DrawBorder3D(e.Graphics, e.CellBounds, rightStyle, Border3DSide.Right);
            ControlPaint.DrawBorder3D(e.Graphics, e.CellBounds, bottomStyle, Border3DSide.Bottom);
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SolverGUI());
        }
    }
}
