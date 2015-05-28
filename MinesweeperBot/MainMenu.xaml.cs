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
using System.Windows.Shapes;

namespace MinesweeperBot
{
	/// <summary>
	/// Interaction logic for MainMenu.xaml
	/// </summary>
	public partial class MainMenu : Window
	{
		public MainMenu()
		{
			InitializeComponent();
		}

		private void ComputerSimulationClicked(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(ColumnCount.Text, out MainWindow.column_count)
				&& int.TryParse(RowCount.Text, out MainWindow.row_count)
				&& int.TryParse(BombCount.Text, out MainWindow.bomb_count))
			{
				this.Hide();
				var mm = new MainWindow();
				mm.ShowDialog();
				this.Show();
			}
		}
	}
}
