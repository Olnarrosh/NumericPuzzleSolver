Usage
=====
The GUI version can be used to input puzzles, save them to or load them from files, and solve them.

The CLI version reads one or more puzzles from files given as arguments and solves them. In order for it to recognise the puzzle type, it has to be specified either as the extension of each file (e. g. `"Numeric Puzzle Solver" sudoku.su kakuro.ka`), or, if all files are the same type, the extension belonging to that type can be given as the first argument (e. g. `"Numeric Puzzle Solver" su sudoku1 sudoku2`).

Available puzzle types and extensions
-------------------------------------
| Puzzle type   | File extension |
|:-------------:|:--------------:|
| Hyper Sudoku  | hs             |
| Inshi No Heya | in             |
| Kakuro        | ka             |
| Killer Sudoku | ks             |
| Latin Square  | ls             |
| Str8ts        | s8             |
| Sudoku        | su             |
| Sudoku X      | sx             |
| Takuzu        | ta             |

File format
-----------
For Killer Sudoku: Each line in the file contains a list of coordinates and finally a number, all separated by spaces, that indicate the area and value of a cage (e. g. `4,6 4,7 3,7 3,8 10`).

For Kakuro and Inshi No Heya: Entries are represented similarly to cages in Killer Sudoku, except that only the coordinates of the first and the last cell are specified (e. g. `2,4 6,4 16`).

For others: Each line represents a row in the grid and lists its cells, separated by spaces; cells with initially known values are represented by that number, others by dots (`.`). In Str8ts, a hash (`#`) indicates a black cell; if the cell contains no clue, an additional dot is unnecessary, otherwise its value can be before or after the hash, without a separator.
