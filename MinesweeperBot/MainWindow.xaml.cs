using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.ComponentModel;


namespace MinesweeperBot
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static int column_count;
		public static int row_count;
		public static int bomb_count;

		List<Image> images = new List<Image>();
		bool is_simulating = false;
		MineTable main_table;

		public event PropertyChangedEventHandler PropertyChanged;
		int _win_width;
		int _win_height;

		public int win_width
		{
			get {return _win_width;}
			set
			{
				_win_width = value;
				OnPropertyChanged("win_width");
			}
		}

		public int win_height
		{
			get { return _win_height; }
			set
			{
				_win_height = value;
				OnPropertyChanged("win_height");
			}
		}

		protected void OnPropertyChanged(string name)
		{
			if(PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;

			int cell_size = Math.Min(Math.Min(
				(int)System.Windows.SystemParameters.PrimaryScreenWidth / column_count, 
				(int)(System.Windows.SystemParameters.PrimaryScreenHeight - 100) / row_count), 
				30);
			win_width = column_count * cell_size;
			win_height = row_count * cell_size + 80;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (is_simulating)
				return;

			SimulateButton.Content = "Simulating...";

			ProgressBar.Value = 0;
			is_simulating = true;

			//Initialize mine matrix			
			main_table = new MineTable();
			main_table.Setup(column_count, row_count, bomb_count);

			//Setup background worker
			BackgroundWorker worker = new BackgroundWorker();
			worker.WorkerReportsProgress = true;
			worker.DoWork += main_table.Solve;
			worker.ProgressChanged += worker_ProgressChanged;
			worker.RunWorkerCompleted += worker_RunWorkerCompleted;
			worker.RunWorkerAsync();
		}

		void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			//Verify correctness
			foreach (MineCell mc in main_table.mine_table)
			{
				if (mc.current_state == CellState.MarkedBomb)
				{
					Tuple<int, int> is_bomb = main_table.bomb_locations.Find(item => item.Item1 == mc.index.Item1 && item.Item2 == mc.index.Item2);
					if (is_bomb == null)
						Console.WriteLine("ERROR: Marked wrong");
				}
			}

			//Remove previous board
			foreach (Image img in images)
				MainGrid.Children.Remove(img);
			images.Clear();

			//Draw pictures into grid
			MainGrid.Columns = column_count;
			MainGrid.Margin = new Thickness(0);
			for (int y = main_table.mine_table.GetLength(1) - 1; y >= 0; y--)
			{
				for (int x = 0; x < main_table.mine_table.GetLength(0); x++)
				{
					var img = new Image();
					img.Stretch = Stretch.Fill;
					img.Source = main_table[x, y].Icon();
					Grid.SetRow(img, y);
					Grid.SetColumn(img, x);
					MainGrid.Children.Add(img);
					images.Add(img);
				}
			}

			SimulateButton.Content = "Run Again";
			ProgressBar.Value = 100;
			is_simulating = false;
		}

		void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			ProgressBar.Value = e.ProgressPercentage;
		}
	}
}