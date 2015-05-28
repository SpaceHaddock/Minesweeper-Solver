/*--SUMMARY--
Handles most of the information that is processed by the table
Provides getters and maintains state information about individual cell
Allows for clicking and easy solving
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MinesweeperBot
{
	public enum CellState { Bomb, MarkedBomb, Hidden, Revealed };

	public class MineCell
	{
		public MineTable mine_table; //set this when constructed

		public CellState current_state = CellState.Hidden;
		public Tuple<int, int> index;

		public MineCell() { }
		public MineCell(MineCell copy, MineTable table_parent)
		{
			mine_table = table_parent;
			current_state = copy.current_state;
			adjacent_bombs = copy.adjacent_bombs;
			index = copy.index;
		}

		public float failure_chance = 0;

		public int adjacent_bombs = 0;

		List<MineCell> _adjacent_cells;
		public List<MineCell> adjacent_cells
		{
			get
			{
				if (_adjacent_cells == null || _adjacent_cells.Count == 0)
					CalculateAdjacentCells();
				return _adjacent_cells;
			}
			set { _adjacent_cells = value; }

		}

		///<summary>
		///Reveals the clicked cell and displays a number for how many bombs surround it, returns true if the location was a bomb
		///</summary>
		public bool CalculateAdjacentBombs()
		{
			Tuple<int, int> is_bomb = mine_table.bomb_locations.Find(item => item.Item1 == index.Item1 && item.Item2 == index.Item2);
			if (is_bomb != null)
			{
				adjacent_bombs = 99;
				current_state = CellState.Bomb;
				return true;
			}
			else
			{
				current_state = CellState.Revealed;
				adjacent_bombs = 0;
				foreach (MineCell mc in adjacent_cells)
				{
					Tuple<int, int> found = mine_table.bomb_locations.Find(item => item.Item1 == mc.index.Item1 && item.Item2 == mc.index.Item2);
					if (found != null)
						adjacent_bombs++;
				}

				return false;
			}
		}

		public int NeighboringStates(CellState check)
		{
			int result = 0;
			foreach (MineCell mc in adjacent_cells) if (mc.current_state == check) result++;
			return result;
		}

		///<summary>
		///Puts the adjacent cells into a list. Takes boundaries into account.
		///</summary>
		public List<MineCell> CalculateAdjacentCells()
		{
			_adjacent_cells = new List<MineCell>();

			for (int x = Math.Max(0, index.Item1 - 1); x <= Math.Min(MainWindow.column_count - 1, index.Item1 + 1); x++)
				for (int y = Math.Max(0, index.Item2 - 1); y <= Math.Min(MainWindow.row_count - 1, index.Item2 + 1); y++)
					if (x != index.Item1 || y != index.Item2)
						_adjacent_cells.Add(mine_table[x, y]);

			return _adjacent_cells;
		}

		///<summary>
		///Checks if there is enough spaces for this to fit in
		///</summary>
		public bool is_contradiction
		{
			get { return (NeighboringStates(CellState.MarkedBomb) + NeighboringStates(CellState.Hidden)) < adjacent_bombs; }
		}

		///<summary>
		///Checks if the cell has any surrounding hidden cells
		///</summary>
		public bool is_relevant
		{
			get { return NeighboringStates(CellState.Hidden) != 0; }
		}

		///<summary>
		///Checks if there is an easy answer and marks locations
		///</summary>
		public List<MineCell> EasyAnswer(bool is_perform_check = true)
		{
			List<MineCell> result = new List<MineCell>();

			//Reveal everything when you know where the bombs are
			if (adjacent_bombs == NeighboringStates(CellState.MarkedBomb))
			{
				foreach (MineCell adjacent in adjacent_cells)
					if (adjacent.current_state == CellState.Hidden)
					{
						adjacent.current_state = CellState.Revealed;
						if(is_perform_check)
							adjacent.CalculateAdjacentBombs();
						result.Add(adjacent);
					}
			}

			//Mark bombs if they must be all that remains
			else if (adjacent_bombs == (NeighboringStates(CellState.Hidden) + NeighboringStates(CellState.MarkedBomb)))
			{
				foreach (MineCell adjacent in adjacent_cells)
					if (adjacent.current_state == CellState.Hidden)
					{
						adjacent.current_state = CellState.MarkedBomb;
						result.Add(adjacent);
					}
			}

			return result;
		}

		///<summary>
		///Used for printing out the table
		///</summary>
		public BitmapImage Icon()
		{
			string source = "";
			switch (current_state)
			{
				case CellState.Bomb:
					source = "Bomb";
					break;
				case CellState.Hidden:
					source = "Hidden";
					break;
				case CellState.MarkedBomb:
					source = "MarkedBomb";
					break;
				case CellState.Revealed:
					source = adjacent_bombs.ToString();
					break;
			}
			BitmapImage bi = new BitmapImage();
			bi.BeginInit();
			bi.UriSource = new Uri(string.Format("Images/{0}.png", source), UriKind.Relative);
			bi.EndInit();

			return bi;
		}
	}
}
