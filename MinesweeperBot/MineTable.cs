using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MinesweeperBot
{
	public class MineTable
	{
		public MineCell[,] mine_table;
		public OneList<MineCell> relevant_cells = new OneList<MineCell>(); //used to track which cells are actually important to check
		public List<Tuple<int, int>> bomb_locations = new List<Tuple<int, int>>();
		public int depth = 0;
	
		int total_bomb_count = 0;

		public MineCell this[int x, int y]
		{
			get { return mine_table[x, y]; }
			set { mine_table[x, y] = value; }
		}

		public MineTable() {}
		public MineTable(MineTable copy)
		{
			bomb_locations = copy.bomb_locations;
			depth = copy.depth + 1;

			//Create clone of mine table
			mine_table = new MineCell[copy.mine_table.GetLength(0), copy.mine_table.GetLength(1)];
			for (int i = 0; i < mine_table.GetLength(0); i++)
				for (int j = 0; j < mine_table.GetLength(1); j++)
					mine_table[i, j] = new MineCell(copy.mine_table[i, j], this);

			//Match the relevant cells
			relevant_cells.Clear();
			foreach (MineCell mc in copy.relevant_cells)
				relevant_cells.Add(this[mc.index.Item1, mc.index.Item2]);
		}

		///<summary>
		///Sets up the size and bomb location properties of the MineTable
		///</summary>
		public void Setup(int x, int y, int bomb_count)
		{
			total_bomb_count = bomb_count;

			//Initialize mine matrix
			mine_table = new MineCell[x, y];
			for (int i = 0; i < mine_table.GetLength(0); i++)
			{
				for (int j = 0; j < mine_table.GetLength(1); j++)
				{
					this[i, j] = new MineCell();
					this[i, j].index = new Tuple<int, int>(i, j);
				}
			}
			foreach (MineCell mc in mine_table)
				mc.mine_table = this;

			//Initialize bomb locations
			List<int> remaining_positions = new List<int>();
			for (int i = 0; i < x * y; i++) remaining_positions.Add(i);

			Random rand = new Random();
			rand.Next();

			for (int i = 0; i < bomb_count; i++)
			{
				int rand_index = rand.Next(remaining_positions.Count);
				int rand_num = remaining_positions[rand_index];
				bomb_locations.Add(new Tuple<int, int>(rand_num % x, (int)rand_num / x));
				remaining_positions.RemoveAt(rand_index);
			}
		}

		///<summary>
		///Returns all unrevealed spaces that are adjacent to relevant spaces
		///</summary>
		public OneList<MineCell> HiddenSpaces()
		{
			var result = new OneList<MineCell>();
			foreach (MineCell relevant_cell in relevant_cells)
				result.AddList(relevant_cell.adjacent_cells.FindAll(item => item.current_state == CellState.Hidden));
			return result;
		}

		///<summary>
		///Returns true if this was a bomb, reveals cells
		///</summary>
		public bool Click(int x, int y)
		{
			if (mine_table[x, y].current_state != CellState.Hidden)
				Console.WriteLine("ERROR: You fed in an incorrect cell to click");

			mine_table[x, y].CalculateAdjacentBombs();
			return mine_table[x, y].current_state == CellState.Bomb;
		}

		///<summary>
		///Called on the first table created
		///</summary>
		public void Solve(object sender, DoWorkEventArgs e)
		{
			//Run the algorithm for the bot
			int start_x = mine_table.GetLength(0) / 2;
			int start_y = mine_table.GetLength(1) / 2;
			bool quit = Click(start_x, start_y);
			relevant_cells.Add(mine_table[start_x, start_y]);
			int search_mode = 0;
			while (!quit)
			{
				//Go through the possible answers
				if (search_mode == 0)
				{
					search_mode++;

					int marked_bombs = 0;
					foreach (MineCell mc in mine_table)
						if (mc.current_state == CellState.MarkedBomb)
							marked_bombs++;
					(sender as BackgroundWorker).ReportProgress((int)((float) marked_bombs/total_bomb_count * 100));

					//Find the easy answers
					for (int i = 0; i < relevant_cells.Count; i++)
					{
						var revealed = relevant_cells[i].EasyAnswer();
						if (revealed != null && revealed.Count > 0)
						{
							relevant_cells.Remove(relevant_cells[i]);
							relevant_cells.AddRange(
								revealed.FindAll(item => item.current_state == CellState.Revealed));
							search_mode = 0;
							break;
						}
					}
				}
				else if (search_mode == 1) //find the harder answers
				{
					search_mode++;

					var hidden_spaces = new OneList<MineCell>();
					foreach (MineCell relevant_cell in relevant_cells)
						hidden_spaces.AddList(relevant_cell.adjacent_cells.FindAll(item => item.current_state == CellState.Hidden));

					foreach (MineCell mc in hidden_spaces)
					{
						MineTable hard_mode = new MineTable(this);
						if (hard_mode.FindContradiction(mc.index.Item1, mc.index.Item2, true))
						{
							if (Click(mc.index.Item1, mc.index.Item2))
								return;
							relevant_cells.Add(mc);
							relevant_cells.RemoveAll(item => !item.is_relevant);
							search_mode = 0;
							break;
						}
						else
						{
							MineTable hard_mode_2 = new MineTable(this);
							if (hard_mode_2.FindContradiction(mc.index.Item1, mc.index.Item2, false))
							{
								mc.current_state = CellState.MarkedBomb;
								relevant_cells.RemoveAll(item => !item.is_relevant);
								search_mode = 0;
								break;
							}
						}
					}
				}
				else //make a guess
				{
					search_mode = 0;

					var hidden_spaces = HiddenSpaces();
					foreach (MineCell mc in hidden_spaces) mc.failure_chance = 0;

					//Set probability of failure to start
					foreach (MineCell mc in relevant_cells)
					{
						foreach (MineCell adj in mc.adjacent_cells)
						{
							if (adj.current_state == CellState.Hidden)
							{
								float failure_chance = (float)mc.adjacent_bombs / mc.NeighboringStates(CellState.Hidden);
								adj.failure_chance = Math.Max(failure_chance, adj.failure_chance);
							}
						}
					}

					//Pick lowest probability from the hidden spaces
					MineCell best_pick = null;
					if (hidden_spaces.Count == 0)
					{
						foreach (MineCell mc in mine_table)
							if (mc.current_state == CellState.Hidden)
								best_pick = mc;
					}
					else best_pick = hidden_spaces[0];
					
					foreach (MineCell mc in hidden_spaces)
						if (mc.failure_chance < best_pick.failure_chance)
							best_pick = mc;

					if (Click(best_pick.index.Item1, best_pick.index.Item2))
						return;
					else
						relevant_cells.RemoveAll(item => !item.is_relevant);
					relevant_cells.Add(best_pick);
				}

				//Check if you've solved the table
				if (!quit)
				{
					quit = true;
					foreach (MineCell mc in mine_table)
					{
						if (mc.current_state == CellState.Hidden)
						{
							quit = false;
							break;
						}
					}
				}
			}


			return;
		}

		///<summary>
		///Called on tables when the hard answer is needed
		///</summary>
		public bool FindContradiction(int x, int y, bool is_start_bomb = true)
		{
			//Mark cell as bomb
			this[x, y].current_state = is_start_bomb ? CellState.MarkedBomb : CellState.Revealed;
			relevant_cells.RemoveAll(item => !item.is_relevant);

			//Go through relevant and search for easy answers or contradictions
			bool quit = false;
			while (!quit)
			{
				quit = true;
				for (int i = 0; i < relevant_cells.Count; i++)
				{
					var revealed = relevant_cells[i].EasyAnswer(false);
					if (revealed != null && revealed.Count > 0)
					{
						relevant_cells.Remove(relevant_cells[i]);
						quit = false;
						break;
					}
					else if (relevant_cells[i].is_contradiction)
						return true;
				}
			}

			//Move to recursion and call another table for answers
			var hidden_spaces = HiddenSpaces();
			if (hidden_spaces.Count > 0)
			{
				foreach (MineCell mc in hidden_spaces)
				{
					MineTable table_two = new MineTable(this);
					if (depth > 5 || !table_two.FindContradiction(mc.index.Item1, mc.index.Item2))
						return false;
				}
			}
			else return false;

			return true;
		}
	}
}
