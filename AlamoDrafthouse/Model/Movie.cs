using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.magusoft.drafthouse.Model
{
	public class Movie
	{
		private readonly string mTitle;
		public string Title
		{
			get
			{
				return mTitle;
			}
		}

		private readonly List<ShowTime> mShowTimes;
		public IEnumerable<ShowTime> ShowTimes
		{
			get
			{
				return mShowTimes;
			}
		}

		private readonly Theater mTheater;
		public Theater Theater { get { return mTheater; } }

		public Movie(Theater theater, string title, IEnumerable<ShowTime> showTimes)
		{
			this.mTheater = theater;
			this.mTitle = title;
			this.mShowTimes = new List<ShowTime>(showTimes);
		}
	}
}
