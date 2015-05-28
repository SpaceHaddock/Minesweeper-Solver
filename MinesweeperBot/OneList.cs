using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinesweeperBot
{
	public class OneList<T> : List<T>
	{
		new public void Add(T item)
		{
			if(!this.Contains(item))
				base.Add(item);
		}

		new public void AddList(List<T> items)
		{
			foreach (T item in items)
				Add(item);
		}
	}
}
